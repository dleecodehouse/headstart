﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Headstart.Common.Models;
using OrderCloud.SDK;

namespace Headstart.Common.Services
{
    public class MockTaxService : ITaxCodesProvider, ITaxCalculator
    {
        public Task<OrderTaxCalculation> CalculateEstimateAsync(HSOrderWorksheet orderWorksheet, List<OrderPromotion> promotions)
        {
            return Task.FromResult(new OrderTaxCalculation()
            {
                ExternalTransactionID = "Mock Tax Response for Headstart",
                TotalTax = 123.45m,
            });
        }

        public Task<OrderTaxCalculation> CommitTransactionAsync(HSOrderWorksheet orderWorksheet, List<OrderPromotion> promotions)
        {
            return Task.FromResult(new OrderTaxCalculation()
            {
                ExternalTransactionID = "Mock Tax Response for Headstart",
                TotalTax = 123.45m,
            });
        }

        public Task<TaxCategorizationResponse> ListTaxCodesAsync(string searchTerm)
        {
            return Task.FromResult(new TaxCategorizationResponse()
            {
                ProductsShouldHaveTaxCodes = false,
                Categories = new List<TaxCategorization>()
                {
                    new TaxCategorization(),
                },
            });
        }
    }
}
