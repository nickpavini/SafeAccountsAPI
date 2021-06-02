using Serilog;
using System;
using System.Configuration;

namespace SafeAccountsAPI.Logging
{
    static class Logger
    {
        private static readonly ILogger _perfLogger;
        private static readonly ILogger _usageLogger;
        private static readonly ILogger _errorLogger;
        private static readonly ILogger _diagnosticLogger;
        static Logger()
        {

        }
    }
}