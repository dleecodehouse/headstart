﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using OrderCloud.Integrations.Smarty.Models;
using SmartyStreets;
using SmartyStreets.USStreetApi;

namespace OrderCloud.Integrations.Smarty
{
    public interface ISmartyStreetsService
    {
        // returns many incomplete address suggestions
        Task<AutoCompleteResponse> USAutoCompletePro(string search);

        // returns one or zero very complete suggestions
        Task<List<Candidate>> ValidateSingleUSAddress(Lookup lookup);
    }

    public class SmartyStreetsService : ISmartyStreetsService
    {
        private readonly SmartyStreetSettings config;
        private readonly Client smartyStreetsClient;
        private readonly string autoCompleteBaseUrl = "https://us-autocomplete-pro.api.smartystreets.com";

        public SmartyStreetsService(SmartyStreetSettings config)
        {
            this.config = config;
            this.smartyStreetsClient = new ClientBuilder(config.AuthID, config.AuthToken).BuildUsStreetApiClient();
        }

        public async Task<List<Candidate>> ValidateSingleUSAddress(Lookup lookup)
        {
            smartyStreetsClient.Send(lookup);
            return await Task.FromResult(lookup.Result);
        }

        public async Task<AutoCompleteResponse> USAutoCompletePro(string search)
        {
            var suggestions = await autoCompleteBaseUrl
                .AppendPathSegment("lookup")
                .SetQueryParam("auth-id", config.AuthID)
                .SetQueryParam("auth-token", config.AuthToken)
                .SetQueryParam("search", search)
                .GetJsonAsync<AutoCompleteResponse>();

            return suggestions;
        }
    }
}
