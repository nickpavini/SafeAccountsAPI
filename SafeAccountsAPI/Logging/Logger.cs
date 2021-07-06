using Serilog;
using Serilog.Events;

namespace SafeAccountsAPI.Logging
{
    /// <summary>
    /// Logger class containing the functionality to write to various sinks
    /// This should be expanded to use appsettings , so that this can be changed
    /// without a rebuild.
    /// </summary>
    static class Logger
    {
        private static readonly ILogger _errorLogger;

        static Logger()
        {

            _errorLogger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();


        }
        /// <summary>
        /// Write to Error Logs
        /// </summary>
        /// <param name="logDetail"></param>
        public static void WriteError(LoggingInfo logDetail)
        {
            _errorLogger.Write(LogEventLevel.Information, "{@LogDetail}", logDetail);
        }
    }
}
