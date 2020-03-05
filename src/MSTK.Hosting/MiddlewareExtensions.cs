using Microsoft.AspNetCore.Builder;
using System;

namespace MSTK.Hosting
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseCallInfoMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CallInfoMiddleware>();
        }
    }
}
