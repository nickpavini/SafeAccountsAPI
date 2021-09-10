namespace SafeAccountsAPI.Models
{
    public class ErrorMessage
    {

        public string Error { get; set; }
        public string Message { get; set; }

        public ErrorMessage(string error, string errorDetails)
        {
            Error = error; Message = errorDetails;
        }
    }
}
