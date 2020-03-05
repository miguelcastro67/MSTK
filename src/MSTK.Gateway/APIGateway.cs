using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MSTK.Gateway
{
    public class APIGateway
    {
        protected object _Payload = null;

        public IEnumerable<GatewayEndpoint> Endpoints { get; private set; }

        public void SetEndpoint(GatewayEndpoint endpoint)
        {
            List<GatewayEndpoint> endpoints = Endpoints?.ToList();
            if (endpoints == null)
                endpoints = new List<GatewayEndpoint>();

            endpoint.IsActive = true;

            endpoints.Add(endpoint);

            Endpoints = endpoints;
        }

        public void SetPayload(object payload)
        {
            _Payload = payload;
        }

        public T Call<T>(object payload)
        {
            T result = default(T);

            Type payloadType = payload.GetType();
            if (payloadType.GetTypeInfo().UnderlyingSystemType.FullName.ToLower() == "system.string") // TODO: what about simple types (int)
            {
                result = Call<T>(new object[] { payload });
            }
            else
            {
                object tempPayload = _Payload;
                _Payload = payload;
                result = Call<T>();
                _Payload = tempPayload;
            }

            return result;
        }

        public Task<T> CallAsync<T>(object payload)
        {
            Task<T> task = Task.Run(() =>
            {
                return Call<T>(payload);
            });

            return task;
        }

        public T Call<T>(params object[] args)
        {
            if (Endpoints == null || Endpoints.Count() == 0)
                return default(T);

            GatewayEndpoint endpoint = Endpoints.ToList()[0]; // the routes should all be the same on all the endpoints

            bool exit = false;
            List<string> arguments = new List<string>();
            string route = endpoint.Route;

            while (!exit)
            {
                int startPos = route.IndexOf("{");
                if (startPos > -1)
                {
                    int endPos = route.Substring(startPos).IndexOf("}");
                    if (endPos > -1)
                    {
                        string argument = route.Substring(startPos + 1, endPos - 1);
                        arguments.Add(argument);
                        route = route.Substring(startPos + endPos + 1);
                    }
                }
                else
                    exit = true;
            }

            dynamic payload = new ExpandoObject();

            IDictionary<string, object> dictPayload = payload as IDictionary<string, object>;

            int index = 0;
            foreach (string argument in arguments)
            {
                if (args.Length >= index + 1)
                {
                    dictPayload[argument] = args[index];
                    index++;
                }
            }

            SetPayload(payload);

            return Call<T>();
        }

        public Task<T> CallAsync<T>(params object[] args)
        {
            Task<T> task = Task.Run(() =>
            {
                return Call<T>(args);
            });

            return task;
        }

        public T Call<T>()
        {
            object result = default(T);

            if (_Payload == null)
            {
                // api call has no arguments - use this technique so that not confused with an empty string as an actual payload value
                dynamic payload = new ExpandoObject();
                IDictionary<string, object> dictPayload = payload as IDictionary<string, object>;
                SetPayload(payload);
            }

            result = CallEndpoint<T>((endpoint =>
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    string endpointHostAddress = endpoint.Address + (endpoint.Address.EndsWith(@"/") ? "" : "/");

                    string route = endpoint.Route;

                    Type payloadType = _Payload.GetType();
                    if (payloadType.Equals(typeof(ExpandoObject)))
                    {
                        IDictionary<string, object> dictPayload = _Payload as IDictionary<string, object>;
                        foreach (KeyValuePair<string, object> arg in dictPayload)
                            route = route.Replace("{" + arg.Key + "}", arg.Value.ToString());
                    }
                    else
                    {
                        PropertyInfo[] properties = payloadType.GetTypeInfo().GetProperties();
                        foreach (PropertyInfo propertyInfo in properties)
                            route = route.Replace("{" + propertyInfo.Name + "}",
                                        propertyInfo.GetValue(_Payload).ToString());
                    }

                    // remove empty route arguments so their value is not the same as the argument name
                    //    example: hostName={hostName}
                    if (route.IndexOf("?") > -1)
                    {
                        string query = route.Substring(route.IndexOf("?") + 1);
                        string newRoute = route.Substring(0, route.IndexOf("?") + 1);

                        string[] queryItems = query.Split('&');
                        foreach (string queryItem in queryItems)
                        {
                            string[] queryPair = queryItem.Split('=');
                            if (queryPair[0] != queryPair[1].Replace("{", "").Replace("}", ""))
                            {
                                newRoute += queryItem + "&";
                            }
                        }

                        if (newRoute.EndsWith("&"))
                            newRoute = newRoute.Substring(0, newRoute.Length - 1);

                        if (newRoute.EndsWith("?"))
                            newRoute = newRoute.Substring(0, newRoute.Length - 1);

                        route = newRoute;
                    }

                    string resource = (route.StartsWith(@"/") ? route.Substring(1) : route);

                    string url = endpointHostAddress + resource;

                    Func<HttpResponseMessage, object> obtainResponse = (response) =>
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            if (typeof(T).Equals(typeof(HttpResponseMessage)))
                                result = response;
                            else
                                result = response.Content.ReadAsAsync<T>().Result;
                        }

                        return result;
                    };

                    HttpContent content = null;
                    if (endpoint.Method.ToUpper() == "POST" || endpoint.Method.ToUpper() == "PUT")
                    {
                        HttpContent postContent = new StringContent(
                        JsonConvert.SerializeObject(_Payload), Encoding.UTF8, "application/json");
                    }

                    switch (endpoint.Method.ToUpper())
                    {
                        case "GET":
                            result = obtainResponse(httpClient.GetAsync(url).Result);                            
                            break;
                        case "POST":
                            result = obtainResponse(httpClient.PostAsync(url, content).Result);
                            break;
                        case "PUT":
                            result = obtainResponse(httpClient.PutAsync(url, content).Result);
                            break;
                        case "DELETE":
                            result = obtainResponse(httpClient.DeleteAsync(url).Result);
                            break;
                    }
                }

                return (T)result;
            }));

            return (T)result;
        }

        public Task<T> CallAsync<T>()
        {
            Task<T> task = Task.Run(() =>
            {
                return Call<T>();
            });

            return task;
        }

        GatewayEndpoint SelectEndpoint()
        {
            IEnumerable<GatewayEndpoint> activeEndpoints =
                Endpoints.Where(item => item.IsActive == true);

            if (activeEndpoints.Count() > 0)
            {
                Random rnd = new Random();
                int itemIndex = rnd.Next(0, activeEndpoints.Count());

                GatewayEndpoint endpoint = activeEndpoints.ToList()[itemIndex];

                endpoint.LastCall = DateTime.Now;
                return endpoint;

            }
            else
                return null;
        }

        T CallEndpoint<T>(Func<GatewayEndpoint, T> callToInvoke)
        {
            T result = default(T);

            bool exitCall = false;
            while (!exitCall)
            {
                GatewayEndpoint endpoint = SelectEndpoint();
                if (endpoint != null)
                {
                    try
                    {
                        result = callToInvoke.Invoke(endpoint);

                        exitCall = true;
                    }
                    catch (Exception)
                    {
                        endpoint.IsActive = false;
                    }
                }
                else
                {
                    exitCall = true;

                    throw new MissingGatewayEndpointException("No available gateway data found to achieve service call.");
                }
            }

            return result;
        }
    }
}
