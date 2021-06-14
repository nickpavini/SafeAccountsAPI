using System;
namespace SafeAccountsAPI.Models
{
    public class ErrorDetails
    {
        // Status Code to be sent
        public int StatusCode { get; set; } = 500;
        public Guid CorrelationID { get; set; }
        public string Message { get; set; }
    }
}