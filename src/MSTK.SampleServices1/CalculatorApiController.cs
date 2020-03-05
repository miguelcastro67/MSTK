using Microsoft.AspNetCore.Mvc;
using MSTK.SDK;
using System;

namespace MSTK.SampleServices1
{
    [Route("api/calculator")]
    [Discovery("Calculator")]
    public class CalculatorApiController : Controller
    {
        [HttpGet("add/{x}/{y}")]
        [Discovery("AddOperation")]
        public IActionResult Add(int x, int y)
        {
            int sum = x + y;

            IClientProxy clientProxy = new ClientProxy();

            Console.WriteLine("Add method called in the CalulatorApiController class, as a result of being discovered by the AddOperation scope.");

            clientProxy.PublishEvent("MyEvent1", new { Sum = sum });

            Console.WriteLine("MyEvent1 published from Add method.");

            return new ObjectResult(sum);
        }

        [HttpGet("calc1")] 
        [Discovery("ComplexCalc1", Dependency = "UserOperation")]
        public IActionResult Calc1()
        {
            return new ObjectResult(3.14151926);
        }

        [HttpGet("calc2")]
        [Discovery("ComplexCalc2", Dependency = "UserOperation")]
        public IActionResult Calc2()
        {
            return new ObjectResult(3.14151926);
        }


        [HttpPost("calc3")]
        [Discovery("ComplexCalc3", Dependency = "UserService_GetUser")]
        [Subscribe("MyEvent1")]
        public IActionResult Calc3([FromForm]object payload, string hostName, string instance)
        {
            // Note that if hostName and/or instance were not sent in, they will be {hostName} and {instance}

            return new ObjectResult(3.14151926);
        }
    }
}
