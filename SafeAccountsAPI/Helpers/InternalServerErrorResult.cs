using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace SafeAccountsAPI.Helpers
{
    /// <summary>
    /// An <see cref="ObjectResult"/> that when  executed will produce a Internal Server Error (500) response. 
    /// </summary>

    [DefaultStatusCode(DefaultStatusCode)]
    public class InternalServerErrorResult : ObjectResult
    {
        private const int DefaultStatusCode = StatusCodes.Status500InternalServerError;
        public InternalServerErrorResult([ActionResultObjectValue] object error) : base(error)
        {
            StatusCode = DefaultStatusCode;
        }

    }
}
