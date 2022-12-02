using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Headstart.Common.Constants;
using Headstart.Common.Extensions;
using Headstart.Common.Models;
using Headstart.Common.Services;
using Headstart.Common.Utils;
using Newtonsoft.Json;
using OrderCloud.Catalyst;
using OrderCloud.Integrations.Emails;
using OrderCloud.SDK;
using ITaxCalculator = Headstart.Common.Services.ITaxCalculator;

namespace Headstart.API.Commands
{
    public interface IPostSubmitCommand
    {
        Task<OrderSubmitResponse> HandleBuyerOrderSubmit(HSOrderWorksheet order);

        Task<OrderSubmitResponse> ExportOrder(string orderID);

        Task<OrderSubmitResponse> HandleShippingValidate(string orderID, DecodedToken decodedToken);
    }

    public class PostSubmitCommand : IPostSubmitCommand
    {
        private readonly IOrderCloudClient oc;
        private readonly IOMSService omsService;
        private readonly ITaxCalculator taxCalculator;
        private readonly IEmailServiceProvider emailServiceProvider;
        private readonly ILineItemCommand lineItemCommand;
        private readonly AppSettings settings;

        public PostSubmitCommand(
            IEmailServiceProvider emailServiceProvider,
            ITaxCalculator taxCalculator,
            IOrderCloudClient oc,
            IOMSService omsService,
            ILineItemCommand lineItemCommand,
            AppSettings settings)
        {
            this.oc = oc;
            this.taxCalculator = taxCalculator;
            this.omsService = omsService;
            this.emailServiceProvider = emailServiceProvider;
            this.lineItemCommand = lineItemCommand;
            this.settings = settings;
        }

        public async Task<OrderSubmitResponse> HandleShippingValidate(string orderID, DecodedToken decodedToken)
        {
            var worksheet = await oc.IntegrationEvents.GetWorksheetAsync<HSOrderWorksheet>(OrderDirection.Incoming, orderID);
            return await CreateOrderSubmitResponse(
                new List<ProcessResult>()
                {
                    new ProcessResult()
                    {
                        Type = ProcessType.Accounting,
                        Activity = new List<ProcessResultAction>()
                        {
                            await ProcessAction.Execute(
                                ProcessType.Shipping,
                                "Validate Shipping",
                                ValidateShipping(worksheet)),
                        },
                    },
                },
                new List<HSOrder> { worksheet.Order });
        }

        public async Task<OrderSubmitResponse> ExportOrder(string orderID)
        {
            var worksheet = await oc.IntegrationEvents.GetWorksheetAsync<HSOrderWorksheet>(OrderDirection.Incoming, orderID);
            var supplierOrders = await Throttler.RunAsync(worksheet.LineItems.GroupBy(g => g.SupplierID).Select(s => s.Key), 100, 10, item => oc.Orders.GetAsync<HSOrder>(
                OrderDirection.Outgoing,
                $"{worksheet.Order.ID}-{item}"));

            return await CreateOrderSubmitResponse(
                new List<ProcessResult>() { await omsService.ExportOrder(worksheet, supplierOrders, false) },
                new List<HSOrder> { worksheet.Order });
        }

        public async Task<OrderSubmitResponse> HandleBuyerOrderSubmit(HSOrderWorksheet orderWorksheet)
        {
            var results = new List<ProcessResult>();

            // STEP 1
            var (supplierOrders, buyerOrder, activities) = await HandlingForwarding(orderWorksheet);
            results.Add(new ProcessResult()
            {
                Type = ProcessType.Forwarding,
                Activity = activities,
            });

            // step 1 failed. we don't want to attempt the integrations. return error for further action
            if (activities.Any(a => !a.Success))
            {
                return await CreateOrderSubmitResponse(results, new List<HSOrder> { orderWorksheet.Order });
            }

            // STEP 2 (integrations)
            var integrations = await HandleIntegrations(supplierOrders, buyerOrder);
            results.AddRange(integrations);

            // STEP 3: return OrderSubmitResponse
            return await CreateOrderSubmitResponse(results, new List<HSOrder> { orderWorksheet.Order });
        }

        public async Task<List<HSOrder>> CreateOrderRelationshipsAndTransferXP(HSOrderWorksheet buyerOrder, List<Order> supplierOrders)
        {
            var payment = (await oc.Payments.ListAsync(OrderDirection.Incoming, buyerOrder.Order.ID))?.Items?.FirstOrDefault();
            var updatedSupplierOrders = new List<HSOrder>();
            var supplierIDs = new List<string>();
            var lineItems = await oc.LineItems.ListAllAsync(OrderDirection.Incoming, buyerOrder.Order.ID);
            var shipFromAddressIDs = lineItems.DistinctBy(li => li.ShipFromAddressID).Select(li => li.ShipFromAddressID).ToList();

            foreach (var supplierOrder in supplierOrders)
            {
                supplierIDs.Add(supplierOrder.ToCompanyID);
                var shipFromAddressIDsForSupplierOrder = shipFromAddressIDs.Where(addressID => addressID != null && addressID.Contains(supplierOrder.ToCompanyID)).ToList();
                var supplier = await oc.Suppliers.GetAsync<HSSupplier>(supplierOrder.ToCompanyID);
                var suppliersShipEstimates = buyerOrder.ShipEstimateResponse?.ShipEstimates?.Where(se => se.xp.SupplierID == supplier.ID);
                var supplierOrderPatch = new PartialOrder()
                {
                    ID = $"{buyerOrder.Order.ID}-{supplierOrder.ToCompanyID}",
                    xp = new OrderXp()
                    {
                        ShipFromAddressIDs = shipFromAddressIDsForSupplierOrder,
                        SupplierIDs = new List<string>() { supplier.ID },
                        StopShipSync = false,
                        OrderType = buyerOrder.Order.xp.OrderType,
                        QuoteOrderInfo = buyerOrder.Order.xp.QuoteOrderInfo,
                        Currency = supplier.xp.Currency,
                        ShippingStatus = ShippingStatus.Processing,
                        SubmittedOrderStatus = SubmittedOrderStatus.Open,
                        SelectedShipMethodsSupplierView = suppliersShipEstimates != null ? MapSelectedShipMethod(suppliersShipEstimates) : null,

                        // ShippingAddress needed for Purchase Order Detail Report
                        ShippingAddress = new HSAddressBuyer()
                        {
                            ID = buyerOrder?.Order?.xp?.ShippingAddress?.ID,
                            CompanyName = buyerOrder?.Order?.xp?.ShippingAddress?.CompanyName,
                            FirstName = buyerOrder?.Order?.xp?.ShippingAddress?.FirstName,
                            LastName = buyerOrder?.Order?.xp?.ShippingAddress?.LastName,
                            Street1 = buyerOrder?.Order?.xp?.ShippingAddress?.Street1,
                            Street2 = buyerOrder?.Order?.xp?.ShippingAddress?.Street2,
                            City = buyerOrder?.Order?.xp?.ShippingAddress?.City,
                            State = buyerOrder?.Order?.xp?.ShippingAddress?.State,
                            Zip = buyerOrder?.Order?.xp?.ShippingAddress?.Zip,
                            Country = buyerOrder?.Order?.xp?.ShippingAddress?.Country,
                        },
                    },
                };
                var updatedSupplierOrder = await oc.Orders.PatchAsync<HSOrder>(OrderDirection.Outgoing, supplierOrder.ID, supplierOrderPatch);
                var supplierLineItems = lineItems.Where(li => li.SupplierID == supplier.ID).ToList();
                await SaveShipMethodByLineItem(supplierLineItems, supplierOrderPatch.xp.SelectedShipMethodsSupplierView, buyerOrder.Order.ID);
                await OverrideOutgoingLineQuoteUnitPrice(updatedSupplierOrder.ID, supplierLineItems);
                updatedSupplierOrders.Add(updatedSupplierOrder);
            }

            await lineItemCommand.SetInitialSubmittedLineItemStatuses(buyerOrder.Order.ID);
            var sellerShipEstimates = buyerOrder.ShipEstimateResponse?.ShipEstimates?.Where(se => se.xp.SupplierID == null);

            // Patch Buyer Order after it has been submitted
            var buyerOrderPatch = new PartialOrder()
            {
                xp = new
                {
                    ShipFromAddressIDs = shipFromAddressIDs,
                    SupplierIDs = supplierIDs,
                    ShippingStatus = ShippingStatus.Processing,
                    SubmittedOrderStatus = SubmittedOrderStatus.Open,
                    HasSellerProducts = buyerOrder.LineItems.Any(li => li.SupplierID == null),
                    PaymentMethod = payment.Type == PaymentType.CreditCard ? "Credit Card" : "Purchase Order",

                    // If we have seller ship estimates for a seller owned product save selected method on buyer order.
                    SelectedShipMethodsSupplierView = sellerShipEstimates != null ? MapSelectedShipMethod(sellerShipEstimates) : null,
                },
            };

            await oc.Orders.PatchAsync(OrderDirection.Incoming, buyerOrder.Order.ID, buyerOrderPatch);
            return updatedSupplierOrders;
        }

        private static async Task ValidateShipping(HSOrderWorksheet orderWorksheet)
        {
            if (orderWorksheet.ShipEstimateResponse.HttpStatusCode != 200)
            {
                throw new Exception(orderWorksheet.ShipEstimateResponse.UnhandledErrorBody);
            }

            if (orderWorksheet.ShipEstimateResponse.ShipEstimates.Any(s => s.SelectedShipMethodID == ShippingConstants.NoRatesID))
            {
                throw new Exception("No shipping rates could be determined - fallback shipping rate of $20 3-day was used");
            }

            await Task.CompletedTask;
        }

        private async Task<List<ProcessResult>> HandleIntegrations(List<HSOrder> supplierOrders, HSOrderWorksheet orderWorksheet)
        {
            // STEP 1: SendGrid notifications
            var results = new List<ProcessResult>();

            var notifications = await ProcessAction.Execute(
                ProcessType.Notification,
                "Sending Order Submit Emails",
                emailServiceProvider.SendOrderSubmitEmail(orderWorksheet));
            results.Add(new ProcessResult()
            {
                Type = ProcessType.Notification,
                Activity = new List<ProcessResultAction>() { notifications },
            });

            if (!orderWorksheet.IsStandardOrder())
            {
                return results;
            }

            // STEP 2: Tax transaction
            var tax = await ProcessAction.Execute(
                ProcessType.Tax,
                "Creating Tax Transaction",
                HandleTaxTransactionCreationAsync(orderWorksheet.Reserialize<HSOrderWorksheet>()));
            results.Add(new ProcessResult()
            {
                Type = ProcessType.Tax,
                Activity = new List<ProcessResultAction>() { tax },
            });

            // STEP 3: export order
            var exportedOrderResult = await omsService.ExportOrder(orderWorksheet, supplierOrders);
            if (exportedOrderResult != null)
            {
                results.Add(exportedOrderResult);
            }

            // STEP 4: Validate shipping
            var shipping = await ProcessAction.Execute(
                ProcessType.Shipping,
                "Validate Shipping",
                ValidateShipping(orderWorksheet));
            results.Add(new ProcessResult()
            {
                Type = ProcessType.Shipping,
                Activity = new List<ProcessResultAction>() { shipping },
            });

            return results;
        }

        private async Task<OrderSubmitResponse> CreateOrderSubmitResponse(List<ProcessResult> processResults, List<HSOrder> ordersRelatingToProcess)
        {
            try
            {
                if (processResults.All(i => i.Activity.All(a => a.Success)))
                {
                    await UpdateOrderNeedingAttention(ordersRelatingToProcess, false);
                    return new HSOrderSubmitResponse()
                    {
                        HttpStatusCode = 200,
                        xp = new OrderSubmitResponseXp()
                        {
                            ProcessResults = processResults,
                        },
                    };
                }

                await UpdateOrderNeedingAttention(ordersRelatingToProcess, true);
                return new HSOrderSubmitResponse()
                {
                    HttpStatusCode = 500,
                    xp = new OrderSubmitResponseXp()
                    {
                        ProcessResults = processResults,
                    },
                };
            }
            catch (OrderCloudException ex)
            {
                return new HSOrderSubmitResponse()
                {
                    HttpStatusCode = 500,
                    UnhandledErrorBody = JsonConvert.SerializeObject(ex.Errors),
                };
            }
        }

        private async Task UpdateOrderNeedingAttention(IList<HSOrder> orders, bool isError)
        {
            var partialOrder = new PartialOrder() { xp = new { NeedsAttention = isError } };

            var orderInfos = new List<Tuple<OrderDirection, string>> { };

            var buyerOrder = orders.First();
            orderInfos.Add(new Tuple<OrderDirection, string>(OrderDirection.Incoming, buyerOrder.ID));
            orders.RemoveAt(0);
            orderInfos.AddRange(orders.Select(o => new Tuple<OrderDirection, string>(OrderDirection.Outgoing, o.ID)));

            await Throttler.RunAsync(orderInfos, 100, 3, (orderInfo) => oc.Orders.PatchAsync(orderInfo.Item1, orderInfo.Item2, partialOrder));
        }

        private async Task<Tuple<List<HSOrder>, HSOrderWorksheet, List<ProcessResultAction>>> HandlingForwarding(HSOrderWorksheet orderWorksheet)
        {
            var activities = new List<ProcessResultAction>();

            // forwarding
            var (forwardAction, forwardedOrders) = await ProcessAction.Execute(
                ProcessType.Forwarding,
                "OrderCloud API Order.ForwardAsync",
                oc.Orders.ForwardAsync(OrderDirection.Incoming, orderWorksheet.Order.ID));
            activities.Add(forwardAction);

            var supplierOrders = forwardedOrders.OutgoingOrders.ToList();

            // creating relationship between the buyer order and the supplier order
            // no relationship exists currently in the platform
            var (updateAction, hsOrders) = await ProcessAction.Execute(
                ProcessType.Forwarding,
                "Create Order Relationships And Transfer XP",
                CreateOrderRelationshipsAndTransferXP(orderWorksheet, supplierOrders));
            activities.Add(updateAction);

            // need to get fresh order worksheet because this process has changed things about the worksheet
            var (getAction, hsOrderWorksheet) = await ProcessAction.Execute(
                ProcessType.Forwarding,
                "Get Updated Order Worksheet",
                oc.IntegrationEvents.GetWorksheetAsync<HSOrderWorksheet>(OrderDirection.Incoming, orderWorksheet.Order.ID));
            activities.Add(getAction);

            return await Task.FromResult(new Tuple<List<HSOrder>, HSOrderWorksheet, List<ProcessResultAction>>(hsOrders, hsOrderWorksheet, activities));
        }

        private List<ShipMethodSupplierView> MapSelectedShipMethod(IEnumerable<HSShipEstimate> shipEstimates)
        {
            var selectedShipMethods = shipEstimates.Select(shipEstimate =>
            {
                var selected = shipEstimate.ShipMethods.FirstOrDefault(sm => sm.ID == shipEstimate.SelectedShipMethodID);
                return new ShipMethodSupplierView()
                {
                    EstimatedTransitDays = selected.EstimatedTransitDays,
                    Name = selected.GetServiceName(),
                    ShipFromAddressID = shipEstimate.xp.ShipFromAddressID,
                };
            }).ToList();
            return selectedShipMethods;
        }

        private async Task HandleTaxTransactionCreationAsync(HSOrderWorksheet orderWorksheet)
        {
            var taxCalculation = await taxCalculator.CommitTransactionAsync(orderWorksheet);
            await oc.Orders.PatchAsync<HSOrder>(OrderDirection.Incoming, orderWorksheet.Order.ID, new PartialOrder()
            {
                TaxCost = taxCalculation.TotalTax,  // Set this again just to make sure we have the most up to date info
                xp = new { ExternalTaxTransactionID = taxCalculation.ExternalTransactionID },
            });
        }

        private async Task SaveShipMethodByLineItem(List<LineItem> lineItems, List<ShipMethodSupplierView> shipMethods, string buyerOrderID)
        {
            if (shipMethods != null)
            {
                foreach (LineItem lineItem in lineItems)
                {
                    string shipFromID = lineItem.ShipFromAddressID;
                    if (shipFromID != null)
                    {
                        ShipMethodSupplierView shipMethod = shipMethods.Find(shipMethod => shipMethod.ShipFromAddressID == shipFromID);
                        string readableShipMethod = shipMethod.Name.Replace("_", " ");
                        PartialLineItem lineItemToPatch = new PartialLineItem { xp = new { ShipMethod = readableShipMethod } };
                        LineItem patchedLineItem = await oc.LineItems.PatchAsync(OrderDirection.Incoming, buyerOrderID, lineItem.ID, lineItemToPatch);
                    }
                }
            }
        }

        private async Task OverrideOutgoingLineQuoteUnitPrice(string supplierOrderID, List<LineItem> supplierLineItems)
        {
            foreach (LineItem lineItem in supplierLineItems)
            {
                if (lineItem?.Product?.xp?.ProductType == ProductType.Quote.ToString())
                {
                    var patch = new PartialLineItem { UnitPrice = lineItem.UnitPrice };
                    await oc.LineItems.PatchAsync(OrderDirection.Outgoing, supplierOrderID, lineItem.ID, patch);
                }
            }
        }
    }
}
