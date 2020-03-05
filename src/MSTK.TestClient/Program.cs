using MSTK.Core.UI;
using MSTK.Gateway;
using System;
using System.Linq;

namespace MSTK.TestClient
{
    class Program
    {
        static string _DiscoveryHubAddress = "http://localhost:8084/mstk/discovery";

        static void Main(string[] args)
        {
            ConsoleHelper consoleHelper = new ConsoleHelper();

            consoleHelper.ShowMenu(
                new MenuItem[]
                {
                    new MenuItem("Test discovered call", () => 
                    {
                        string scope = "AddOperation";

                        APIGatewayFactory factory = new APIGatewayFactory(_DiscoveryHubAddress);
                        APIGateway apiGateway = factory.Discover(scope, discoverAll: true);
                        if (apiGateway != null)
                        {
                            Console.WriteLine("{0} endpoint(s) discovered.", apiGateway.Endpoints.Count());

                            //int sum = apiGateway.Call<int>(new { x = 4, y = 5 });
                            int sum = apiGateway.Call<int>(4, 5);
                            Console.WriteLine("The result is {0}.", sum);

                            //HttpResponseMessage response = apiGateway.Call<HttpResponseMessage>(4, 5);
                            //Console.WriteLine("The result is {0}.", response.Content.ReadAsAsync<int>().Result);
                        }
                        else
                        {
                            Console.WriteLine("No endpoints discovered for scope '{0}'.", scope);
                            Console.WriteLine("It is also possible that one of its dependencies is not available.");
                        }
                    }),
                    new MenuItem("Test discovered call with dependencies", () => 
                    {
                        string scope = "ComplexCalc3";

                        APIGatewayFactory factory = new APIGatewayFactory(_DiscoveryHubAddress);
                        APIGateway apiGateway = factory.Discover(scope, discoverAll: true);
                        if (apiGateway != null)
                        {
                            Console.WriteLine("{0} endpoint(s) discovered.", apiGateway.Endpoints.Count());

                            decimal pi = apiGateway.Call<decimal>();
                            Console.WriteLine("The result is {0}.", pi);
                        }
                        else
                        {
                            Console.WriteLine("No endpoints discovered for scope '{0}'.", scope);
                            Console.WriteLine("It is also possible that one of its dependencies is not available.");
                        }
                    })
                });
        }
    }
}
