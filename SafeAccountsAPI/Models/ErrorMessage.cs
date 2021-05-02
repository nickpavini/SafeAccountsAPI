namespace SafeAccountsAPI.Models
{
    public class ErrorMessage
    {

        public string Error { get; set; }
        public string Input { get; set; }
        public string Exception { get; set; }

        public ErrorMessage(string error, string input, string exception)
        {
            Error = error; Input = input; Exception = exception;
        }
    }
}
