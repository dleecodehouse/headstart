﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Headstart.API.Commands;
using Headstart.Common.Models;
using Microsoft.AspNetCore.Mvc;
using OrderCloud.Catalyst;
using OrderCloud.SDK;

namespace Headstart.API.Controllers
{
    public class OrderReturnController
    {
        private readonly IOrderReturnCommand orderReturnCommand;

        public OrderReturnController(IOrderReturnCommand orderReturnCommand)
        {
            this.orderReturnCommand = orderReturnCommand;
        }

        [Route("orderreturns/{orderReturnId}/complete")]
        [HttpPost]
        [OrderCloudUserAuth(ApiRole.OrderAdmin), UserTypeRestrictedTo(CommerceRole.Seller)]
        public async Task<HSOrderReturn> CalculateOrderReturn(string orderReturnId)
        {
            return await orderReturnCommand.CompleteReturn(orderReturnId);
        }
    }
}
