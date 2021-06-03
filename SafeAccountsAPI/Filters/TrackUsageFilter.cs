using System.Collections.Generic;
using SafeAccountsAPI.Logging;
using SafeAccountsAPI.Helpers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SafeAccountsAPI.Filters
{
    /// <summary>
    /// 
    /// </summary>
    public class TrackUsageFilter : ActionFilterAttribute
    {
        private string _operation;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operation"></param>
        public TrackUsageFilter(string operation)
        {
            _operation = operation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            Logger.WriteUsage(HelperMethods.GetLoggingInfo(_operation, context.RouteData.Values));
        }
    }
}