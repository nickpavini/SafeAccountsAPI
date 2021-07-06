using System;
using SafeAccountsAPI.Logging;
using SafeAccountsAPI.Helpers;
using SafeAccountsAPI.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SafeAccountsAPI.Filters
{
    /// <summary>
    /// ApiExceptionFilter that is triggered when any API endpoint is decorated with this.
    /// </summary>
    public class ApiExceptionFilter : ExceptionFilterAttribute
    {

        private string _message;

        /// <summary>
        /// Constructor that accepts the custom message
        /// </summary>
        /// <param name="message"></param>
        public ApiExceptionFilter(string message)
        {
            _message = message;
        }

        /// <summary>
        /// Log the errors to a sink
        /// </summary>
        /// <param name="context"></param>
        public override void OnException(ExceptionContext context)
        {
            var error = new ErrorDetails
            {
                CorrelationID = Guid.NewGuid(),
                Message = _message
            };
            Logger.WriteError(HelperMethods.GetLoggingInfo(context.Exception, error.CorrelationID));
            context.Result = new InternalServerErrorResult(error);
        }
    }
}