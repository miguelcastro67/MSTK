using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using MSTK.Core;
using MSTK.SDK;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Timers;

namespace MSTK.Hosting
{
    public class Hoster
    {
        public Hoster()
        {
            IConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("host.json");
            IConfigurationRoot configuration = configBuilder.Build();

            _DiscoveryHubHost = configuration["discoveryHubHost"];
            _EventHubHost = configuration["eventHubHost"];
            _MonitorHubHost = configuration["monitorHubHost"];

            _HostAddress = CreateAddress("http://{0}:{1}");
            _WebHost = StartHost();
        }
        
        public Hoster(string hostAddress)
        {
            if (string.IsNullOrWhiteSpace(hostAddress))
                throw new ArgumentNullException("Host address cannot be empty");

            _HostAddress = hostAddress;
            _WebHost = StartHost();
        }

        public static List<Type> ControllerTypes = new List<Type>();

        string _DiscoveryHubHost = string.Empty;
        string _EventHubHost = string.Empty;
        string _MonitorHubHost = string.Empty;
        string _HostName = string.Empty;
        string _Instance = string.Empty;
        string _HostAddress = string.Empty;
        IWebHost _WebHost = null;
        DiscoveryMetadata _DiscoveryMetadata = null;
        List<SubscriptionInfo> _EventSubscriptions = null;
        HostMetadata _HostMetadata = null;
        bool _ConnectedToDiscoveryHub = false;
        bool _SubscribedToEvents = false;
        bool _RegisteredWithMonitorHub = false;
        Timer _DiscoveryHubTimer = null;
        Timer _EventHubTimer = null;
        Timer _MonitorHubTimer = null;

        public event EventHandler<HubConnectEventArgs> DiscoveryHubConnect;
        public event EventHandler<HubConnectEventArgs> EventHubSubscribed;
        public event EventHandler<HubConnectEventArgs> MonitorHubRegistered;

        public string HostName
        {
            get { return _HostName; }
        }

        public string Instance
        {
            get { return _Instance; }
        }

        protected virtual void OnDiscoveryHubConnect(bool connected)
        {
            if (this.DiscoveryHubConnect != null)
                this.DiscoveryHubConnect(this, new HubConnectEventArgs(connected));
        }

        protected virtual void OnEventHubSubscribed(bool connected)
        {
            if (this.EventHubSubscribed != null)
                this.EventHubSubscribed(this, new HubConnectEventArgs(connected));
        }

        protected virtual void OnMonitorHubRegistered(bool connected)
        {
            if (this.MonitorHubRegistered != null)
                this.MonitorHubRegistered(this, new HubConnectEventArgs(connected));
        }

        public void Start()
        {
            IConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("host.json");
            IConfigurationRoot configuration = configBuilder.Build();

            _HostName = configuration["hostName"];
            _Instance = GenerateUniqueInstanceIdentifier();

            _WebHost.Start();
            _DiscoveryMetadata = GetDiscoveryMetadata();
            _EventSubscriptions = GetEventSubscriptions();
            _HostMetadata = GetHostMetadata();

            // discovery hub

            _DiscoveryHubTimer = new Timer(5000);
            _DiscoveryHubTimer.Elapsed += (s, e) =>
            {
                _DiscoveryHubTimer.Stop();

                if (!_ConnectedToDiscoveryHub)
                {
                    _ConnectedToDiscoveryHub = ConnectToDiscoveryHub();
                    OnDiscoveryHubConnect(_ConnectedToDiscoveryHub);
                }
                else
                    _ConnectedToDiscoveryHub = PingDiscoveryHub();
                
                _DiscoveryHubTimer.Start();
            };
            
            _DiscoveryHubTimer.Start();

            // event hub

            _EventHubTimer = new Timer(5000);
            _EventHubTimer.Elapsed += (s, e) =>
            {
                _EventHubTimer.Stop();

                if (!_SubscribedToEvents)
                {
                    _SubscribedToEvents = SubscribeToEvents();
                    OnEventHubSubscribed(_SubscribedToEvents);
                }
                else
                    _SubscribedToEvents = PingEventHub();

                _EventHubTimer.Start();
            };

            _EventHubTimer.Start();


            // monitor hub

            _MonitorHubTimer = new Timer(5000);
            _MonitorHubTimer.Elapsed += (s, e) =>
            {
                _MonitorHubTimer.Stop();

                if (!_RegisteredWithMonitorHub)
                {
                    _RegisteredWithMonitorHub = RegisterWithMonitorHub();
                    OnMonitorHubRegistered(_RegisteredWithMonitorHub);
                }
                else
                    _RegisteredWithMonitorHub = PingMonitorHub();

                _MonitorHubTimer.Start();
            };

            _MonitorHubTimer.Start();
        }

        public void Stop()
        {
            DisconnectFromDiscoveryHub();
            UnsubscribeFromEvents();
            UnregisterFromMonitorHub();
        }

        IWebHost StartHost()
        {
            IWebHost host = new WebHostBuilder()
                        .UseKestrel()
                        .UseUrls(new string[] { _HostAddress })
                        //.UseContentRoot(Directory.GetCurrentDirectory())
                        .UseStartup<Startup>()
                        .Build();

            return host;
        }
        
        DiscoveryMetadata GetDiscoveryMetadata()
        {
            List<Service> services = new List<Service>();

            foreach (Type controllerType in ControllerTypes)
            {
                Service service = ProcessService(controllerType);
                if (service != null)
                    services.Add(service);
            }

            DiscoveryMetadata discoveryMetadata = new DiscoveryMetadata()
            {
                HostName = _HostName,
                Instance = _Instance,
                HostAddress = _HostAddress + (_HostAddress.EndsWith("/") ? "" : "/"),
                Services = services
            };

            return discoveryMetadata;
        }

        List<SubscriptionInfo> GetEventSubscriptions()
        {
            List<SubscriptionInfo> eventSubscriptions = new List<SubscriptionInfo>();

            foreach (Type controllerType in ControllerTypes)
            {
                string callbackUrl = _DiscoveryMetadata.HostAddress;
                if (!callbackUrl.EndsWith("/")) callbackUrl += "/";

                RouteAttribute routeAttr = controllerType.GetCustomAttribute<RouteAttribute>(false);
                if (routeAttr != null)
                {
                    callbackUrl += routeAttr.Template;
                    if (!callbackUrl.EndsWith("/")) callbackUrl += "/";
                }

                MethodInfo[] methods = controllerType.GetMethods();
                foreach (MethodInfo methodInfo in methods)
                {
                    string route = string.Empty;

                    HttpPostAttribute httpPostAttr = methodInfo.GetCustomAttribute<HttpPostAttribute>();
                    if (httpPostAttr != null)
                    {
                        if (!string.IsNullOrWhiteSpace(httpPostAttr.Template))
                            route = httpPostAttr.Template;
                        else
                        {
                            routeAttr = methodInfo.GetCustomAttribute<RouteAttribute>();
                            if (routeAttr != null)
                                route = routeAttr.Template;
                        }

                        IEnumerable<SubscribeAttribute> subscribeAttrs = methodInfo.GetCustomAttributes<SubscribeAttribute>(true);
                        if (subscribeAttrs != null)
                        {
                            foreach (SubscribeAttribute subscribeAttribute in subscribeAttrs)
                            {
                                SubscriptionInfo subscriptionInfo = new SubscriptionInfo()
                                {
                                    HostName = _HostName,
                                    Instance = _Instance,
                                    CallbackAddress = callbackUrl + route,
                                    EventName = subscribeAttribute.EventName
                                };

                                eventSubscriptions.Add(subscriptionInfo);
                            }
                        }
                    }
                    else
                    {
                        // subscription callback must be a POST
                    }
                }
            }

            return eventSubscriptions;
        }

        HostMetadata GetHostMetadata()
        {
            HostMetadata hostMetadata = new HostMetadata()
            {
                HostName = _HostName,
                Instance = _Instance,
                HostAddress = _HostAddress + (_HostAddress.EndsWith("/") ? "" : "/")
            };

            return hostMetadata;
        }

        bool ConnectToDiscoveryHub()
        {
            bool connected = false;

            string url = _DiscoveryHubHost + "/mstk/discovery/register";
            
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpContent postContent = new StringContent(JsonConvert.SerializeObject(_DiscoveryMetadata), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = httpClient.PostAsync(url, postContent).Result;
                    if (response.IsSuccessStatusCode)
                        connected = true;
                }
            }
            catch (Exception)
            {
            }
            
            return connected;
        }

        bool SubscribeToEvents()
        {
            bool subscribed = true;

            if (_EventSubscriptions != null && _EventSubscriptions.Count > 0)
            {
                try
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        foreach (SubscriptionInfo subscriptionInfo in _EventSubscriptions)
                        {
                            string url = _EventHubHost + "/mstk/eventing/subscribe/" + subscriptionInfo.EventName;

                            HttpContent postContent = new StringContent(JsonConvert.SerializeObject(subscriptionInfo), Encoding.UTF8, "application/json");

                            HttpResponseMessage response = httpClient.PostAsync(url, postContent).Result;
                            if (!response.IsSuccessStatusCode)
                            {
                                subscribed = false;
                                break;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            return subscribed;
        }

        bool RegisterWithMonitorHub()
        {
            bool connected = false;

            string url = _MonitorHubHost + "/mstk/monitor/register";

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpContent postContent = new StringContent(JsonConvert.SerializeObject(_HostMetadata), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = httpClient.PostAsync(url, postContent).Result;
                    if (response.IsSuccessStatusCode)
                        connected = true;
                }
            }
            catch (Exception)
            {
            }

            return connected;
        }

        void DisconnectFromDiscoveryHub()
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    string url = _DiscoveryHubHost + "/mstk/discovery/deregister/" + _DiscoveryMetadata.HostName + "/" + _DiscoveryMetadata.Instance;

                    HttpResponseMessage response = httpClient.GetAsync(url).Result;
                }
            }
            catch (Exception)
            {
            }
        }

        void UnsubscribeFromEvents()
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    if (_EventSubscriptions != null && _EventSubscriptions.Count > 0)
                    {
                        foreach (SubscriptionInfo subscriptionInfo in _EventSubscriptions)
                        {
                            string url = _EventHubHost + "/mstk/eventing/unsubscribe/" + subscriptionInfo.EventName + "/" + subscriptionInfo.HostName + "/" + subscriptionInfo.Instance;

                            HttpResponseMessage response = httpClient.GetAsync(url).Result;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        void UnregisterFromMonitorHub()
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    string url = _MonitorHubHost + "/mstk/monitor/deregister/" + _HostMetadata.HostName + "/" + _HostMetadata.Instance;

                    HttpResponseMessage response = httpClient.GetAsync(url).Result;
                }
            }
            catch (Exception)
            {
            }
        }

        bool PingDiscoveryHub()
        {
            bool connected = false;

            {
                string url = _DiscoveryHubHost + "/mstk/discovery/ping";

                try
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        HttpResponseMessage response = httpClient.GetAsync(url).Result;
                        if (response.IsSuccessStatusCode)
                            connected = true;
                    }
                }
                catch (Exception)
                {
                }
            }

            return connected;
        }

        bool PingEventHub()
        {
            bool connected = false;

            {
                string url = _EventHubHost + "/mstk/eventing/ping";

                try
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        HttpResponseMessage response = httpClient.GetAsync(url).Result;
                        if (response.IsSuccessStatusCode)
                            connected = true;
                    }
                }
                catch (Exception)
                {
                }
            }

            return connected;
        }

        bool PingMonitorHub()
        {
            bool connected = false;

            {
                string url = _MonitorHubHost + "/mstk/monitor/ping";

                try
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        HttpResponseMessage response = httpClient.GetAsync(url).Result;
                        if (response.IsSuccessStatusCode)
                            connected = true;
                    }
                }
                catch (Exception)
                {
                }
            }

            return connected;
        }

        Service ProcessService(Type type)
        {
            bool enabled = true;

            Service service = new Service()
            {
                Name = type.Name.Replace("Api", "").Replace("Controller", ""),
            };

            DiscoveryAttribute discoveryAttr = type.GetCustomAttribute<DiscoveryAttribute>(true);
            if (discoveryAttr != null)
            {
                service.DiscoveryName = discoveryAttr.DiscoveryName;
                service.Dependency = discoveryAttr.Dependency;
                enabled = discoveryAttr.Enabled;
            }

            if (enabled)
            {
                string hostAddress = _HostAddress + (_HostAddress.EndsWith("/") ? "" : "/");
                string routePrefix = string.Empty;

                RouteAttribute routePrefixAttr = type.GetCustomAttribute<RouteAttribute>(true);
                if (routePrefixAttr != null)
                    routePrefix = routePrefixAttr.Template + (routePrefixAttr.Template.EndsWith("/") ? "" : "/");

                List<Operation> operations = new List<Operation>();

                MethodInfo[] methods = type.GetMethods();
                foreach (MethodInfo methodInfo in methods)
                {
                    string method = string.Empty;
                    string route = string.Empty;
                    string discoveryName = string.Empty;
                    string dependency = string.Empty;

                    // public method only declared in this class
                    if (methodInfo.IsPublic && methodInfo.DeclaringType.FullName == type.FullName)
                    {
                        object[] methodAttrs = methodInfo.GetCustomAttributes(false);
                        foreach (object methodAttr in methodAttrs)
                        {
                            Type methodAttrType = methodAttr.GetType();

                            Type[] httpAttrInterfaces = methodAttrType.GetInterfaces();
                            foreach (Type interfaceType in httpAttrInterfaces)
                            {
                                if (interfaceType.Equals(typeof(IActionHttpMethodProvider)))
                                {
                                    // found HTTP attribute
                                    method = methodAttrType.Name.Replace("Http", "").Replace("Attribute", "").ToUpper();
                                    route = ((HttpMethodAttribute)methodAttr).Template;
                                    break;
                                }
                            }

                            if (methodAttrType.Equals(typeof(RouteAttribute)))
                            {
                                // process route and method arguments - Route attribute template overrides very attribute template
                                RouteAttribute routeAttr = methodAttr as RouteAttribute;
                                if (routeAttr != null)
                                    route = routeAttr.Template;
                            }

                            //if (methodInfo.Name == "Calc3")
                            //    System.Diagnostics.Debugger.Break();

                            if (!string.IsNullOrWhiteSpace(route))
                            {
                                ParameterInfo[] arguments = methodInfo.GetParameters();
                                if (arguments != null)
                                {
                                    List<ParameterInfo> parameters = new List<ParameterInfo>();
                                    ParameterInfo fromBodyParameter = null;
                                    foreach (ParameterInfo parameterInfo in arguments)
                                    {
                                        if (!parameterInfo.ParameterType.Equals(typeof(HttpRequestMessage)))
                                        {
                                            FromBodyAttribute fromBodyAttr = parameterInfo.GetCustomAttribute<FromBodyAttribute>(false);
                                            FromFormAttribute fromFormAttr = parameterInfo.GetCustomAttribute<FromFormAttribute>(false);
                                            if (fromBodyAttr == null && fromFormAttr == null)
                                            {
                                                // ensure parameter name is not already in the route
                                                if (route.IndexOf("{" + parameterInfo.Name + "}") == -1)
                                                {
                                                    // parameter is in method signature but not in route
                                                    parameters.Add(parameterInfo);
                                                }
                                            }
                                            else
                                                fromBodyParameter = parameterInfo;
                                        }
                                    }

                                    if (parameters.Count > 0)
                                    {
                                        int index = 0;
                                        foreach (ParameterInfo parameterInfo in parameters)
                                        {
                                            route += (index == 0 ? "?" : "&") + parameterInfo.Name + "={" + parameterInfo.Name + "}";
                                            index++;
                                        }
                                    }
                                }
                            }

                            if (methodAttrType.Equals(typeof(DiscoveryAttribute)))
                            {
                                DiscoveryAttribute opDiscAttr = methodAttr as DiscoveryAttribute;
                                if (opDiscAttr != null)
                                {
                                    discoveryName = opDiscAttr.DiscoveryName;
                                    dependency = opDiscAttr.Dependency;
                                }
                            }
                        }

                        if (method == string.Empty) method = "GET";

                        if (route.StartsWith("~/"))
                            route = route.Substring(2);
                        else
                            route = routePrefix + route;

                        Operation operation = new Operation()
                        {
                            Name = methodInfo.Name,
                            Method = method,
                            Route = route,
                            DiscoveryName = discoveryName,
                            Dependency = dependency
                        };

                        // if no discovery nane found, make one out of route
                        if (string.IsNullOrWhiteSpace(operation.DiscoveryName) && !string.IsNullOrWhiteSpace(operation.Route))
                        {
                            Uri uri = new Uri(this._HostAddress + operation.Route);
                            string localPath = uri.LocalPath;
                            if (localPath.IndexOf("?") > -1)
                                operation.DiscoveryName = localPath.Substring(1, uri.LocalPath.IndexOf("?") - 1);
                            else if (localPath.IndexOf("{") > -1)
                                operation.DiscoveryName = localPath.Substring(1, uri.LocalPath.IndexOf("{") - 1);
                            else
                                operation.DiscoveryName = localPath;

                            if (operation.DiscoveryName.StartsWith("/"))
                                operation.DiscoveryName = operation.DiscoveryName.Substring(1);

                            if (operation.DiscoveryName.EndsWith("/"))
                                operation.DiscoveryName = operation.DiscoveryName.Substring(0, operation.DiscoveryName.Length - 1);
                        }

                        operations.Add(operation);
                    }
                }

                service.Operations = operations;
            }
            else
                service = null;

            return service;
        }

        string GenerateUniqueInstanceIdentifier()
        {
            StringBuilder suffix = new StringBuilder();

            Random rnd = new Random();

            for (int i = 0; i < 3; i++)
            {
                int ascii = 0;
                bool valid = false;
                while (!valid)
                {
                    ascii = rnd.Next(0, 42) + 48;
                    if ((ascii >= 48 && ascii <= 57) || (ascii >= 65 && ascii <= 90))
                        valid = true;
                }
                char c = (char)(ascii);
                suffix.Append(c);
            }

            return suffix.ToString();
        }

        string GetHostName()
        {
            string server = Dns.GetHostName();
            string hostName = Dns.GetHostEntry(server).HostName;

            return hostName;
        }

        int GetAvailablePort()
        {
            int port = 0;

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(endPoint);
                IPEndPoint local = (IPEndPoint)socket.LocalEndPoint;
                port = local.Port;
            }

            return port;
        }

        string CreateAddress(string format)
        {
            string hostName = GetHostName();
            int port = GetAvailablePort();

            return string.Format(format, hostName, port);
        }
    }
}
