﻿using Headstart.API.Commands;
using Headstart.Common;
using Headstart.Common.Services;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading.Tasks;

namespace Headstart.Jobs
{
    // check out the README.md at the root of this project to get started
    public class Jobs
    {
        private readonly ISyncOrgCommand _sync;
        private readonly IProductUpdateJob _productUpdates;
        private readonly PaymentCaptureJob _paymentCapture;
        private readonly ZohoJob _zoho;
        private readonly SendRecentOrdersJob _sendRecentOrdersJob;
        private readonly ReceiveRecentSalesOrdersJob _receiveRecentSalesOrdersJob;
        private readonly ReceiveRecentPurchaseOrdersJob _receiveRecentPurchaseOrdersJob;
        private readonly ReceiveRecentLineItemsJob _receiveRecentLineItemsJob;
        private readonly ReceiveRecentOrdersAndShipmentsJob _receiveRecentOrdersAndShipmentsJob;
        private readonly ReceiveProductDetailsJob _receiveProductDetailsJob;
        private readonly AppSettings _settings;

        public Jobs(
            ISyncOrgCommand sync,
            IProductUpdateJob productUpdates,
            PaymentCaptureJob paymentCapture,
            ZohoJob zoho,
            SendRecentOrdersJob sendRecentOrdersJob,
            ReceiveRecentSalesOrdersJob receiveRecentSalesOrdersJob,
            ReceiveRecentPurchaseOrdersJob receiveRecentPurchaseOrdersJob,
            ReceiveRecentLineItemsJob receiveRecentLineItemsJob,
            ReceiveRecentOrdersAndShipmentsJob receiveRecentOrdersAndShipmentsJob,
            ReceiveProductDetailsJob receiveProductDetailsJob,
            AppSettings settings
        )
        {
            _sync = sync;
            _productUpdates = productUpdates;
            _paymentCapture = paymentCapture;
            _zoho = zoho;
            _sendRecentOrdersJob = sendRecentOrdersJob;
            _receiveRecentSalesOrdersJob = receiveRecentSalesOrdersJob;
            _receiveRecentPurchaseOrdersJob = receiveRecentPurchaseOrdersJob;
            _receiveRecentLineItemsJob = receiveRecentLineItemsJob;
            _receiveRecentOrdersAndShipmentsJob = receiveRecentOrdersAndShipmentsJob;
            _receiveProductDetailsJob = receiveProductDetailsJob;
            _settings = settings;
        }

        // Every day at 5AM CST(11AM UTC)
        [FunctionName("SyncAnytimeOrganization")]
        public async Task SyncAnytimeOrganization([TimerTrigger("0 11 * * *")] TimerInfo myTimer, ILogger logger) =>
            await _sync.SyncAllLocations(logger, _settings.OrderCloudSettings.DataConfig.AfBuyerID);

        // Every day at 5AM CST(11AM UTC)
        [FunctionName("SyncWaxingOrganization")]
        public async Task SyncWaxingOrganization([TimerTrigger("0 11 * * *")] TimerInfo myTimer, ILogger logger) =>
            await _sync.SyncAllLocations(logger, _settings.OrderCloudSettings.DataConfig.WtcBuyerID);

        // Every day at 3:15AM CST (9:15AM UTC)
        [FunctionName("EmailProductUpdates")]
        public async Task EmailProductUpdates([TimerTrigger("15 9 * * *")] TimerInfo myTimer, ILogger logger) => await _productUpdates.Run(logger);

        // Every day at 1:00AM CST (7:00AM UTC)
        [FunctionName("CapturePayments")]
        public async Task CapturePayments([TimerTrigger("0 7 * * *")] TimerInfo myTimer, ILogger logger) => await _paymentCapture.Run(logger);

        // Product Detail Cosmos Sync
        // Every day at 1:00AM CST (7:00AM UTC)
        [FunctionName("ReceiveRecentProductDetails")]
        public async Task ReceiveRecentProductDetails([TimerTrigger("0 7 * * *")] TimerInfo myTimer, ILogger logger) => await _receiveProductDetailsJob.Run(logger);

        // these settings need to be set directly on the Azure App Service (for local testing) instead of the Azure App Configuration until this bug is resolved https://github.com/Azure/azure-functions-host/issues/7210
        [FunctionName("ZohoOrderProcessing")]
        public async Task Run(
            [ServiceBusTrigger(
            queueName: "%ServiceBusSettings:ZohoQueueName%",
            Connection = "ServiceBusSettings:ConnectionString")]
        Message message,
            MessageReceiver messageReceiver,
            [ServiceBus(
            queueOrTopicName: "%ServiceBusSettings:ZohoQueueName%",
            Connection = "ServiceBusSettings:ConnectionString" )]
        MessageSender messageSender,
            ILogger logger) => await _zoho.Run(logger, message, messageReceiver, messageSender);

        [FunctionName("SendRecentOrders")]
        // Runs every ten minutes
        public async Task ListRecentOrders([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer, ILogger logger) => await _sendRecentOrdersJob.Run(logger);

        // Sales Order Detail Cosmos Sync
        [FunctionName("ReceiveRecentSalesOrders")]
        public async Task ReceiveRecentSalesOrders(
        [ServiceBusTrigger(
            topicName: "%ServiceBusSettings:OrderReportsTopicName%",
            subscriptionName: "%ServiceBusSettings:SalesOrderDetailSubscriptionName%",
            Connection = "ServiceBusSettings:ConnectionString")]
        Message message,
        MessageReceiver messageReceiver,
        [ServiceBus(
            queueOrTopicName: "%ServiceBusSettings:OrderReportsTopicName%",
            Connection = "ServiceBusSettings:ConnectionString")]
        MessageSender messageSender,
        ILogger logger) => await _receiveRecentSalesOrdersJob.Run(logger, message, messageReceiver, messageSender);

        // Purchase Order Cosmos Sync
        [FunctionName("ReceiveRecentPurchaseOrders")]
        public async Task ReceiveRecentPurchaseOrders(
        [ServiceBusTrigger(
            topicName: "%ServiceBusSettings:OrderReportsTopicName%",
            subscriptionName: "%ServiceBusSettings:PurchaseOrderDetailSubscriptionName%",
            Connection = "ServiceBusSettings:ConnectionString")]
        Message message,
        MessageReceiver messageReceiver,
        [ServiceBus(
            queueOrTopicName: "%ServiceBusSettings:OrderReportsTopicName%",
            Connection = "ServiceBusSettings:ConnectionString")]
        MessageSender messageSender,
        ILogger logger) => await _receiveRecentPurchaseOrdersJob.Run(logger, message, messageReceiver, messageSender);

        // Line Item Detail Cosmos Sync
        [FunctionName("ReceiveRecentLineItems")]
        public async Task ReceiveRecentLineItems(
        [ServiceBusTrigger(
            topicName: "%ServiceBusSettings:OrderReportsTopicName%",
            subscriptionName: "%ServiceBusSettings:LineItemDetailSubscriptionName%",
            Connection = "ServiceBusSettings:ConnectionString")]
        Message message,
        MessageReceiver messageReceiver,
        [ServiceBus(
            queueOrTopicName: "%ServiceBusSettings:OrderReportsTopicName%",
            Connection = "ServiceBusSettings:ConnectionString")]
        MessageSender messageSender,
        ILogger logger) => await _receiveRecentLineItemsJob.Run(logger, message, messageReceiver, messageSender);

        // Shipment Detail Cosmos Sync
        [FunctionName("ReceiveRecentOrdersAndShipments")]
        public async Task ReceiveRecentOrdersAndShipments(
        [ServiceBusTrigger(
            topicName: "%ServiceBusSettings:OrderReportsTopicName%",
            subscriptionName: "%ServiceBusSettings:ShipmentDetailSubscriptionName%",
            Connection = "ServiceBusSettings:ConnectionString")]
        Message message,
        MessageReceiver messageReceiver,
        [ServiceBus(
            queueOrTopicName: "%ServiceBusSettings:OrderReportsTopicName%",
            Connection = "ServiceBusSettings:ConnectionString")]
        MessageSender messageSender,
        ILogger logger) => await _receiveRecentOrdersAndShipmentsJob.Run(logger, message, messageReceiver, messageSender);
    }
}
