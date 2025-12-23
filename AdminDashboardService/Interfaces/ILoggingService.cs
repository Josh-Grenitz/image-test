using System;

namespace AdminDashboardService.Interfaces
{
    public interface ILoggingService
    {
        public void LogError(string message);
        public void LogError(Exception exception);
        public void LogInfo(string message);
        public void LogWarning(string message);
    }
}
