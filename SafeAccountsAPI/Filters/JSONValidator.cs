﻿using Microsoft.AspNetCore.Mvc.Filters;

namespace SafeAccountsAPI.Filters
{
    public class JSONValidator : ActionFilterAttribute
    {
        /// <summary>
        /// TODO Add validations
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuting(ActionExecutingContext context)

        {
            base.OnActionExecuting(context);
        }
    }
}