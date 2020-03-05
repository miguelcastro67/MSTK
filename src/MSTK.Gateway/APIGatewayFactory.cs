using MSTK.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MSTK.Gateway
{
    public class APIGatewayFactory
    {        
        public APIGatewayFactory()
        {
            _DiscoveryHubAddress = "http://localhost:8084/mstk/discovery";
        }

        public APIGatewayFactory(string discoveryHubAddress)
        {
            _DiscoveryHubAddress = discoveryHubAddress;
        }

        string _DiscoveryHubAddress = string.Empty;
        
        public APIGateway Discover(string scope, string accessToken = "", bool discoverAll = false)
        {
            APIGateway apiGateway = null;

            string operation = string.Empty;
            
            if (scope.IndexOf('_') > -1)
            { 
                // discovery based on {Service Discovery Name}_{Operation}
                string[] arrScope = scope.Split('_');
                if (arrScope.Length > 1)
                    operation = arrScope[1];
            }
            else
            {
                // discovery based on {Service/Operation Discovery Name}
            }

            string encodedScope = scope.Replace("/", "@SLASH@");
            string url = _DiscoveryHubAddress + "/discover/" + encodedScope + (discoverAll ? "/all" : "");

            if (url.StartsWith(@"/")) url = url.Substring(1);

            //string url = _DiscoveryHubAddress + "/discover/" + scope + (discoverAll ? "/all" : "");

            HttpClient httpClient = new HttpClient();

            HttpResponseMessage response = httpClient.GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                string data = response.Content.ReadAsStringAsync().Result;
                List<DiscoveryResult> discoveryResults = JsonConvert.DeserializeObject<List<DiscoveryResult>>(data);
                
                apiGateway = new APIGateway();

                foreach (DiscoveryResult discoveryResult in discoveryResults)
                {
                    apiGateway.SetEndpoint(new GatewayEndpoint()
                    {
                        Address = discoveryResult.HostAddress,
                        Method = discoveryResult.Operation.Method,
                        Route = discoveryResult.Operation.Route
                    });
                }
            }

            return apiGateway;
        }

        public Task<APIGateway> DiscoverAsync(string scope, string accessToken = "", bool discoverAll = false)
        {
            Task<APIGateway> task = Task.Run(() =>
            {
                return Discover(scope, accessToken, discoverAll);
            });

            return task;
        }

        public void IsAvailable(string scope)
        {
            
        }
    }

    public class AvailabilityResult
    {
        
    }

    // TODO: instead of Discover returning APIGateway, return DiscoverResult which contains Availability and APIGateway
    
}
