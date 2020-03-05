using Microsoft.AspNetCore.Mvc;
using MSTK.SDK;
using System;

namespace MSTK.SampleServices1
{
    [Route("api/data")]
    [Discovery("UserService")]
    public class UserDataApiController : Controller
    {
        [HttpGet]
        [Route("user/{userName}")]
        [Discovery("UserOperation")]
        public IActionResult GetUser(string userName)
        {
            return new ObjectResult("The user's name is " + userName);
        }
        
        [HttpPost("recalc")]
        [Discovery("Recalc")]
        [Subscribe("MyEvent1")]
        public IActionResult Recalc([FromForm]object payload, string hostName, string instance)
        {
            // Note that if hostName and/or instance were not sent in, they will be {hostName} and {instance}

            Console.WriteLine("Recalc method called in the UserDataApiController class as a result of being subscribed to MyEvent1.");

            return new ObjectResult(payload.ToString());
        }
    }
}