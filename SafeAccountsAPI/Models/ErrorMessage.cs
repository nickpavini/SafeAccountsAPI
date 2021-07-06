namespace SafeAccountsAPI.Models
{
    public class ErrorMessage
    {

        public string Error { get; set; }
        public string ErrorDetails { get; set; }

        public ErrorMessage(string error, string errorDetails)
        {
            Error = error; ErrorDetails = errorDetails;
        }
    }
}
