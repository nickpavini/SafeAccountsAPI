using System;

namespace SafeAccountsAPI.Helpers
{
    public class HttpResponseException : Exception
    {
        public int StatusCode { get; set; } = 500;
        public object Value { get; set; }

        public HttpResponseException(string message) : base(message)
        {
        }

        public HttpResponseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
