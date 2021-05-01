using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SafeAccountsAPI.Controllers
{
    // error message class so we can easily json the response
    public class ErrorMessage
    {
        public readonly string _result = "error";
        public string _error { get; set; }
        public string _input { get; set; }
        public string _exception { get; set; }

        public ErrorMessage(string error, string input, string exception) {
            _error = error; _input = input; _exception = exception;
        }
    }

    public static class SuccessMessage
    {
        public static readonly string _result = @"{""_result"":""success""}"; // done this way sice static message
    }
}
