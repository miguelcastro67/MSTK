using System;
using System.Linq;

namespace MSTK.Gateway
{
    public class MissingGatewayEndpointException : Exception
    {
        public MissingGatewayEndpointException(string message) : base(message)
        {
        }

        public MissingGatewayEndpointException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
