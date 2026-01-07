using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using NLog;
using NLog.Web;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace AdminDashboardService
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // NLog: setup the logger first to catch all errors
            var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

            try
            {
                var builder = WebApplication.CreateBuilder(args);
                
                // Configure NLog
                builder.Logging.ClearProviders();
                builder.Logging.AddNLog();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            try
            {
                logger.Info("Initializing main");
                BuildWebHost(args).Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                logger.Info("Shutting down");
                NLog.LogManager.Shutdown();
            }
        }

        /// <summary>
        /// Detects if the application is running inside a Docker container
        /// </summary>
        private static bool IsRunningInDocker()
        {
            // Check for Docker-specific environment variable
            var dotnetRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            if (!string.IsNullOrEmpty(dotnetRunningInContainer))
            {
                return true;
            }

            // Check for /.dockerenv file (Linux containers)
            if (File.Exists("/.dockerenv"))
            {
                return true;
            }

            // Check for docker in cgroup (Linux containers)
            try
            {
                if (File.Exists("/proc/1/cgroup"))
                {
                    var cgroup = File.ReadAllText("/proc/1/cgroup");
                    if (cgroup.Contains("/docker") || cgroup.Contains("/kubepods"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore errors reading cgroup
            }

            return false;
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders(); // Remove all default providers first
                    
                    var env = context.HostingEnvironment.EnvironmentName;
                    var isDocker = IsRunningInDocker();
                    
                    if (isDocker)
                    {
                        // Running in Docker: Use Console logging for all environments
                        logging.AddConsole(options =>
                        {
                            options.FormatterName = "redacted";
                        });
                        
                        // Add custom formatter that redacts sensitive information
                        logging.AddConsoleFormatter<RedactedConsoleFormatter, ConsoleFormatterOptions>();
                        
                        logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
                        
                        Console.WriteLine($"[Program] Running in Docker - Configured Console logging with sensitive data redaction for {env} environment");
                    }
                    else
                    {
                        // Running on host/VM: Use NLog file logging
                        logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                        Console.WriteLine($"[Program] Running on host - Configured NLog file logging for {env} environment");
                    }
                })
                .UseNLog()  // NLog: setup NLog for Dependency injection (only active when not using console)
                .Build();
    }

    /// <summary>
    /// Custom console formatter that redacts sensitive information
    /// </summary>
    public class RedactedConsoleFormatter : ConsoleFormatter
    {
        public RedactedConsoleFormatter() : base("redacted") { }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            string message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
            
            if (message == null)
                return;

            // Redact sensitive information
            message = RedactSensitiveInfo(message);

            // Format: level: category[eventId] message
            textWriter.Write($"{logEntry.LogLevel.ToString().ToLower()}: ");
            textWriter.Write($"{logEntry.Category}[{logEntry.EventId.Id}]");
            textWriter.WriteLine();
            textWriter.Write($"      {message}");
            
            if (logEntry.Exception != null)
            {
                textWriter.WriteLine();
                textWriter.Write($"      {logEntry.Exception}");
            }
            
            textWriter.WriteLine();
        }

        private static string RedactSensitiveInfo(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            // Redact User Id values
            message = Regex.Replace(message, @"User Id=[^;]+", "User Id={REDACTED}", RegexOptions.IgnoreCase);
            message = Regex.Replace(message, @"User=[^;]+", "User={REDACTED}", RegexOptions.IgnoreCase);
            message = Regex.Replace(message, @"UID=[^;]+", "UID={REDACTED}", RegexOptions.IgnoreCase);
            
            // Redact Password values
            message = Regex.Replace(message, @"Password=[^;]+", "Password={REDACTED}", RegexOptions.IgnoreCase);
            message = Regex.Replace(message, @"Pwd=[^;]+", "Pwd={REDACTED}", RegexOptions.IgnoreCase);
            
            // Redact already masked placeholders like {{}}
            message = Regex.Replace(message, @"\{\{\}\}", "{REDACTED}");

            return message;
        }
    }
}
