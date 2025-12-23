using AdminDashboardService.Interfaces;
using LoggerUtility.Netcore;
using LoggerUtility.Netcore.Logger;
using System;

namespace AdminDashboardService.Services
{
    public class LoggingService :logger, ILoggingService
    {
        public void LogError(string message)
        {
            Error(message);
        }

        public void LogError(Exception exception)
        {
            Error(exception.Message);
            Error(exception.ToString());
            Error(exception.InnerException?.ToString());
        }

        public void LogInfo(string message)
        {
            Info(message);
        }

        public void LogWarning(string message)
        {
            Warn(message);
        }
    }
}
