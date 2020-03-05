using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;

namespace MSTK.SDK
{
    public class ClientProxy : IClientProxy
    {
        public ClientProxy()
        {
            IConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("host.json");
            IConfigurationRoot configuration = configBuilder.Build();

            _DiscoveryHubHost = configuration["discoveryHubHost"];
            _EventHubHost = configuration["eventHubHost"];
            _MonitorHubHost = configuration["monitorHubHost"];

            if (!_DiscoveryHubHost.EndsWith("/"))
                _DiscoveryHubHost += "/";

            if (!_EventHubHost.EndsWith("/"))
                _EventHubHost += "/";

            if (!_MonitorHubHost.EndsWith("/"))
                _MonitorHubHost += "/";
        }

        string _DiscoveryHubHost = string.Empty;
        string _EventHubHost = string.Empty;
        string _MonitorHubHost = string.Empty;

        void IClientProxy.PublishEvent(string eventName, object payload)
        {
            string url = _EventHubHost + "mstk/eventing/publish/" + eventName;

            using (HttpClient httpClient = new HttpClient())
            {
                string jsonData = JsonConvert.SerializeObject(payload);
                HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                
                try
                {
                    HttpResponseMessage response = httpClient.PostAsync(url, content).Result;
                    if (!response.IsSuccessStatusCode)
                        throw new ApplicationException(string.Format("HTTP error trying to publish an event to the Host Server Event Hub. Event: {0}, Response Status Code: {1}", eventName, response.StatusCode));
                    else
                    {

                    }
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(string.Format("HTTP error trying to publish an event to the Host Server Event Hub. Event: {0}", eventName), ex);
                }
            }
        }
    }
}
