using System;
using SafeAccountsAPI.Logging;
using SafeAccountsAPI.Helpers;
using SafeAccountsAPI.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SafeAccountsAPI.Filters
{
    /// <summary>
    /// 
    /// </summary>
    public class ApiExceptionFilter : ExceptionFilterAttribute
    {

        private string _message;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public ApiExceptionFilter(string message)
        {
            _message = message;
        }

        /// <summary>
        /// 
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