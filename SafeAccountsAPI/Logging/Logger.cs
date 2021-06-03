using Serilog;
using Serilog.Events;
using System;
using System.Configuration;

namespace SafeAccountsAPI.Logging
{
    ///TODO Add comments 
    static class Logger
    {
        private static readonly ILogger _perfLogger;
        private static readonly ILogger _usageLogger;
        private static readonly ILogger _errorLogger;
        private static readonly ILogger _diagnosticLogger;
        static Logger()
        {

            _perfLogger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();


            _usageLogger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            _errorLogger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateLogger();

            _diagnosticLogger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateLogger();

        }

        public static void WritePerf(LoggingInfo logDetail)
        {
            _perfLogger.Write(LogEventLevel.Information, "{@LogDetail}", logDetail);
        }


        public static void WriteUsage(LoggingInfo logDetail)
        {
            _usageLogger.Write(LogEventLevel.Information, "{@LogDetail}", logDetail);
        }


        public static void WriteError(LoggingInfo logDetail)
        {
            _errorLogger.Write(LogEventLevel.Information, "{@LogDetail}", logDetail);
        }


        public static void WriteDiagnostic(LoggingInfo logDetail)
        {
            _diagnosticLogger.Write(LogEventLevel.Information, "{@LogDetail}", logDetail);
        }
    }
}