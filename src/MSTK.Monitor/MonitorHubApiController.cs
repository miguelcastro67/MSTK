using Microsoft.AspNetCore.Mvc;
using MSTK.Core;
using MSTK.Monitor;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace MSTK.Discovery
{
    [Route("mstk/monitor")]
    public class MonitorHubApiController : Controller
    {
        [HttpPost("register")]
        public IActionResult Register(HttpRequestMessage request, [FromBody]HostMetadata host)
        {
            lock (Hub.Hosts)
            {
                if (Hub.Hosts == null)
                    Hub.Hosts = new List<HostMetadata>();

                HostMetadata existingHost = Hub.Hosts.FirstOrDefault(item => item.HostAddress.ToLower() == host.HostAddress.ToLower() &&
                                                                     item.Instance.ToLower() == host.Instance.ToLower());
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
                HostMetadata host = Hub.Hosts.FirstOrDefault(item => item.HostName.ToLower() == name.ToLower() &&
                                                                     item.Instance.ToLower() == instance.ToLower());
                if (host != null)
                    Hub.Hosts.Remove(host);
            }

            return new ObjectResult("OK");
        }


        [HttpGet("ping")]
        public IActionResult Ping(HttpRequestMessage request)
        {
            return new ObjectResult("");
        }
    }
}
