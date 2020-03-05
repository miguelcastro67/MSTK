using Microsoft.AspNetCore.Mvc;
using MSTK.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace MSTK.Eventing
{
    [Route("mstk/eventing")]
    public class EventHubApiController : Controller
    {
        [HttpPost("subscribe/{eventName}")]
        public IActionResult Subscribe(HttpRequestMessage request, string eventName, [FromBody]SubscriptionInfo subscription)
        {
            // TODO: note that if a host goes down and comes back up, it will have a new name so old one is left in subscription list

            lock (Hub.Subscriptions)
            {
                if (Hub.Subscriptions == null)
                    Hub.Subscriptions = new List<SubscriptionInfo>();

                SubscriptionInfo existingSubscription = Hub.Subscriptions.FirstOrDefault(item => item.EventName == eventName &&
                                item.HostName.ToLower() == subscription.HostName.ToLower() &&
                                item.Instance.ToLower() == subscription.Instance.ToLower());
                if (existingSubscription == null)
                    Hub.Subscriptions.Add(subscription);
            }

            return new ObjectResult("OK");
        }
        
        [HttpGet("unsubscribe/{eventName}/{hostName}")]
        public IActionResult Unsubscribe(HttpRequestMessage request, string eventName, string hostName, string instance)
        {
            lock (Hub.Subscriptions)
            {
                SubscriptionInfo existingSubscription = Hub.Subscriptions.FirstOrDefault(item => item.EventName == eventName &&
                                item.HostName.ToLower() == hostName.ToLower() &&
                                item.Instance.ToLower() == instance.ToLower());
                if (existingSubscription != null)
                    Hub.Subscriptions.Remove(existingSubscription);
            }

            return new ObjectResult("OK");
        }

        [HttpGet("ping")]
        public IActionResult Ping(HttpRequestMessage request)
        {
            return new ObjectResult("");
        }

        [HttpPost]
        [Route("publish/{eventName}")]
        public IActionResult PublishEvent(string eventName, [FromBody]object payload)
        {
            IActionResult result = null;

            IEnumerable<SubscriptionInfo> subscribers = Hub.Subscriptions.Where(item => item.EventName.ToUpper() == eventName.ToUpper());
            if (subscribers != null)
            {
                // This increases the chance that each time a given event is published, it will be picked up by a different instance of each Host Server.
                var shuffledSubscribers = Shuffle<SubscriptionInfo>(subscribers.ToList());

                var subscribersByHostName = new Dictionary<string, List<SubscriptionInfo>>();
                foreach (SubscriptionInfo subscriptionInfo in shuffledSubscribers)
                {
                    List<SubscriptionInfo> hostNameSubscribers;
                    if (subscribersByHostName.ContainsKey(subscriptionInfo.HostName))
                        hostNameSubscribers = subscribersByHostName[subscriptionInfo.HostName];
                    else
                    {
                        hostNameSubscribers = new List<SubscriptionInfo>();
                        subscribersByHostName.Add(subscriptionInfo.HostName, hostNameSubscribers);
                    }

                    hostNameSubscribers.Add(subscriptionInfo);
                }

                using (HttpClient httpClient = new HttpClient())
                {
                    string jsonData = JsonConvert.SerializeObject(payload);
                    HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                    foreach (var hostNameSubscriber in subscribersByHostName)
                    {
                        var subscriberInstances = hostNameSubscriber.Value;
                        foreach (var subscriberInstance in subscriberInstances)
                        {
                            string url = string.Format(subscriberInstance.CallbackAddress + "?eventName={0}&hostName={1}&instance={2}",
                                eventName, subscriberInstance.HostName, subscriberInstance.Instance);

                            try
                            {
                                HttpResponseMessage response = httpClient.PostAsync(url, content).Result;
                                if (response.IsSuccessStatusCode)
                                    break; // skip all other instances
                            }
                            catch { }
                        }
                    }
                }
            }

            result = new OkResult(); // TODO: when should this NOT be OK?

            return result;
        }

        private static Random rnd = new Random();

        IList<T> Shuffle<T>(IEnumerable<T> collection)
        {
            IList<T> list = (IList<T>)collection;
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rnd.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            return list;
        }
    }
}
