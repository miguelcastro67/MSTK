using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace MSTK.Hosting
{
    public class CallInfoMiddleware
    {
        private readonly RequestDelegate _Next;
        
        public CallInfoMiddleware(RequestDelegate next)
        {
            this._Next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Do some request logic here.
            
            string uri = context.Request.Path;
            string verb = context.Request.Method;            

            await this._Next.Invoke(context).ConfigureAwait(false);

            // Do some response logic here.

            Console.WriteLine("{0} :: {1} call made to route {2}", DateTime.Now, verb, uri);
        }
    }
}
