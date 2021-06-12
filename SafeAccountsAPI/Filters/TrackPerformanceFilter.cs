using System.Collections.Generic;
using SafeAccountsAPI.Logging;
using SafeAccountsAPI.Helpers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SafeAccountsAPI.Filters
{
    public class TrackPerformanceFilter : ActionFilterAttribute
    {
        
        public TrackPerformanceFilter(string product)
        {

        }
    }
}