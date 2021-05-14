namespace SafeAccountsAPI.Models
{
    public class ErrorMessage
    {

        public string Error { get; set; }
        public string Exception { get; set; }

        public ErrorMessage(string error, string exception)
        {
            Error = error; Exception = exception;
        }
    }
}
