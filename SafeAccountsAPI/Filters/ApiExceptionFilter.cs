using System;
using SafeAccountsAPI.Logging;
using SafeAccountsAPI.Helpers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SafeAccountsAPI.Filters
{
    /// <summary>
    /// 
    /// </summary>
    public class ApiExceptionFilter : ExceptionFilterAttribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public override void OnException(ExceptionContext context)
        {
            Logger.WriteError(HelperMethods.GetLoggingInfo(context.Exception));
            base.OnException(context);
        }
    }
}