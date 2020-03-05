using Microsoft.AspNetCore.Mvc;
using MSTK.Core;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace MSTK.Discovery
{
    [Route("mstk/discovery")]
    public class DiscoveryHubApiController : Controller
    {
        [HttpPost("register")]
        public IActionResult Register(HttpRequestMessage request, [FromBody]DiscoveryMetadata host)
        {
            lock (Hub.Hosts)
            {
                if (Hub.Hosts == null)
                    Hub.Hosts = new List<DiscoveryMetadata>();

                DiscoveryMetadata existingHost = Hub.Hosts.FirstOrDefault(item => item.HostAddress.ToLower() == host.HostAddress.ToLower());
                if (existingHost == null)
                    Hub.Hosts.Add(host);
            }

            return new ObjectResult("OK");
        }

        [HttpGet("deregister/{name}/{instance}")]
        public IActionResult Deregister(HttpRequestMessage request, string name, string instance)
        {
            lock (Hub.Hosts)
            {
                DiscoveryMetadata host = Hub.Hosts.FirstOrDefault(item => item.HostName.ToLower() == name.ToLower() &&
                                                                          item.Instance.ToLower() == instance.ToLower());
                if (host != null)
                    Hub.Hosts.Remove(host);
            }

            return new ObjectResult("OK");
        }

        [HttpGet("discover/{scope}")]
        public IActionResult Discover(HttpRequestMessage request, string scope)
        {
            return DiscoverAll(request, scope, false);
        }

        [HttpGet("discover/{scope}/all")]
        public IActionResult DiscoverAll(HttpRequestMessage request, string scope, bool discoverAll = true)
        {
            // TODO: If discoverAll is NOT true, use randomizer to select a server from the list
            // perhaps turn off round-robin for first release, or put it on a toggle
            
            IActionResult response = null;
            
            scope = scope.Replace("@SLASH@", "/");

            List<DiscoveryResult> discoveryResults = GetDiscoveryResults(scope, discoverAll);

            if (discoveryResults.Count > 0)
                response = new OkObjectResult(discoveryResults);
            else
                response = new StatusCodeResult(404);
            
            return response;
        }

        [HttpGet("ping")]
        public IActionResult Ping(HttpRequestMessage request)
        {
            return new ObjectResult("");
        }

        List<DiscoveryResult> GetDiscoveryResults(string scope, bool discoverAll)
        {
            // TODO: discover by Service only. Figure out how to handle it and what to return

            List<DiscoveryResult> discoveryResults = new List<DiscoveryResult>();

            if (scope != "")
            {
                if (scope.IndexOf('_') > -1) // {service disovery name}_{operation}
                {
                    string[] arrScope = scope.Split('_');
                    string discoveryName = arrScope[0];
                    string operationName = arrScope[1];

                    foreach (DiscoveryMetadata host in Hub.Hosts)
                    {
                        Service service = host.Services.FirstOrDefault(item => item.DiscoveryName == discoveryName);
                        if (service != null)
                        {
                            Operation operation = service.Operations.FirstOrDefault(item => item.Name == operationName);
                            if (operation != null)
                            {
                                bool discoverySuccess = true;

                                // check for dependency
                                if (!string.IsNullOrWhiteSpace(operation.Dependency))
                                {
                                    List<DiscoveryResult> dependencyDiscoveryResults = GetDiscoveryResults(operation.Dependency, false);
                                    discoverySuccess = dependencyDiscoveryResults.Count > 0;
                                }

                                if (discoverySuccess)
                                {
                                    discoveryResults.Add(new DiscoveryResult()
                                    {
                                        HostAddress = host.HostAddress,
                                        Operation = operation
                                    });

                                    if (!discoverAll)
                                        break;
                                }
                            }
                        }
                    }
                }
                else // {operation discovery name}
                {
                    string discoveryName = scope;

                    foreach (DiscoveryMetadata host in Hub.Hosts)
                    {
                        foreach (Service service in host.Services)
                        {
                            Operation operation = service.Operations.FirstOrDefault(item => item.DiscoveryName == discoveryName);
                            if (operation != null)
                            {
                                bool discoverySuccess = true;
                                
                                // check for dependency
                                if (!string.IsNullOrWhiteSpace(operation.Dependency))
                                {
                                    List<DiscoveryResult> dependencyDiscoveryResults = GetDiscoveryResults(operation.Dependency, false);
                                    discoverySuccess = dependencyDiscoveryResults.Count > 0;
                                }

                                if (discoverySuccess)
                                {
                                    discoveryResults.Add(new DiscoveryResult()
                                    {
                                        HostAddress = host.HostAddress,
                                        Operation = operation
                                    });

                                    if (!discoverAll)
                                        break;
                                }
                            }
                        }

                        if (discoveryResults.Count > 0 && !discoverAll)
                            break;
                    }
                }
            }

            return discoveryResults;
        }
    }
}
