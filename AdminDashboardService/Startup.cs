using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Server.IISIntegration;
using PolicyAdmin;

using CommonApiUtilities;
using CommonApiUtilities.Interfaces;
using CommonApiUtilities.Security.ActiveDirectoryAuthorization;
using System.Diagnostics;
//using ApiActiveDirectoryAuthorization.Interfaces;
//using ApiActiveDirectoryAuthorization.ActiveDirectoryAuthorization;

namespace AdminDashboardService
{
    public class Startup
    {
        private const string AppName = "AdminDashboard";
        private readonly ILogger<Startup> m_logger;
        private readonly ICommonUtilitiesLoader m_commonUtilitiesLoader;
        private readonly IApplicationConfiguration m_applicationConfiguration;

        //private readonly IActiveDirectoryAuthorizationConstants m_activeDirectoryAuthorizationConstants;
        //private readonly IActiveDirectoryAuthorizationHandlerConfiguration m_activeDirectoryAuthorizationHandlerConfiguration;

        public IConfiguration AppConfiguration { get; }

                        
        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {            
            m_logger = loggerFactory.CreateLogger<Startup>();
            AppConfiguration = configuration;
            try
            {
                var authConsts =
                   new ReadOnlyDictionary<string, ReadOnlyCollection<string>>(
                       new Dictionary<string, ReadOnlyCollection<string>>
                       {
                           { "ADMIN", new List<string>{ "CREATE", "READ", "UPDATE", "DELETE", "REFRESH" }.AsReadOnly() },
                           { "MO",  new List<string>{ "READ" }.AsReadOnly() },
                           { "FO", new List<string>{ "READ" }.AsReadOnly() },
                           { "READONLY", new List<string>{ "READ" }.AsReadOnly() }
                       }
                   );

                Dictionary<string, object> fileArgs = new Dictionary<string, object>()
            {
                { "ApplicationName", typeof(string)},
                { "ArchiveFileLocation", typeof(string)},
                { "SqlDatabaseConnectionString", typeof(string) },
                { "AuditTableName", typeof(string)},
                { "MappingTableName", typeof(string)},
                { "UserRoleTableName", typeof(string)},
                { "LogLocation", typeof(string)},
                { "WhiteList", typeof(Dictionary<string, List<string>>)},
                { "CORSOrigins", typeof(List<string>)},
            };

                var sqlArgs = new Dictionary<string, (string TableName, string WhereStatement, string ConnectionString)>()
            {
                { "ApplicationMapping", ("[DODSDevOps].[dbo].[AdminDashboardMapping]", "", "SqlDatabaseConnectionString")},
                { "ApplicationSqlMapping", ("[DODSDevOps].[dbo].[AdminDashboardMapping]", "WHERE [Type] = 'SQL' ORDER BY [Application]", "SqlDatabaseConnectionString")},
                { "ApplicationCacheMapping", ("[DODSDevOps].[dbo].[AdminDashboardMapping]", "WHERE [Type] = 'CACHE' ORDER BY [Application]", "SqlDatabaseConnectionString")},
                { "ApplicationFileMapping", ("[DODSDevOps].[dbo].[AdminDashboardMapping]", "WHERE [Type] NOT IN ('SQL', 'CACHE') ORDER BY [Application]", "SqlDatabaseConnectionString")},
                { "ApplicationUserGroups", ("[DODSDevOps].[dbo].[UserRoles]", "WHERE [Application] like '%AdminDashboard%' ORDER BY [Application]", "SqlDatabaseConnectionString")}
            };

                m_commonUtilitiesLoader = new CommonUtilitiesLoaderAll(authConsts, fileArgs, sqlArgs);
                m_commonUtilitiesLoader.Startup(loggerFactory);
                m_applicationConfiguration = m_commonUtilitiesLoader.GetApplicationConfiguration();

                DataCache.Current = new DataCache(m_applicationConfiguration, m_logger);
                Debug.WriteLine(DataCache.Current.Initialize());
            }

            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
            

            //m_activeDirectoryAuthorizationHandlerConfiguration = new ActiveDirectoryAuthorizationHandlerConfiguration()
            //{
            //    Whitelist = null,
            //    // SqlConnectionString = m_applicationConfiguration.GetApplicationFileConfiguration<string>("SqlDatabaseConnectionString"),
            //    SqlConnectionString = "data source=SBAWSDTFORDS01Q;integrated security=True",
            //    UserRoleTableName = "[DODSDevOps].[dbo].[UserRoles]",
            //    ApplicationName = "AdminDashboard"
            //};

            //m_activeDirectoryAuthorizationConstants = new ActiveDirectoryAuthorizationConstants(authConsts);
        }

        
        private static bool IsOriginAllowed(string host)
        {
            return (host.Contains("http://localhost:4200/"));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            m_logger.LogInformation("Executing ConfigureServices in Startup");
            try
            {
                m_logger.LogInformation("Configuring CORS in Startup");
                services.AddCors(options =>
                {
                    options.AddPolicy("AllowHeaders",
                        builder =>
                        {
                            builder
                                .WithOrigins(m_applicationConfiguration.GetApplicationFileConfiguration<List<string>>("CORSOrigins").ToArray())
                                // .WithOrigins("http://localhost:4200")
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                        });
                });

                m_logger.LogInformation("Configuring Authorization in Startup");
                services.AddAuthorization(options =>
                {
                    options.AddPolicy("RequireAuthenticatedUser", policy => policy.RequireAuthenticatedUser());
                    options.AddPolicy("CREATE", policy => policy.Requirements.Add(new ActiveDirectoryAuthorizationRequirements("CREATE")));
                    options.AddPolicy("READ", policy => policy.Requirements.Add(new ActiveDirectoryAuthorizationRequirements("READ")));
                    options.AddPolicy("UPDATE", policy => policy.Requirements.Add(new ActiveDirectoryAuthorizationRequirements("UPDATE")));
                    options.AddPolicy("DELETE", policy => policy.Requirements.Add(new ActiveDirectoryAuthorizationRequirements("DELETE")));
                    options.AddPolicy("REFRESH", policy => policy.Requirements.Add(new ActiveDirectoryAuthorizationRequirements("REFRESH")));
                });

                m_logger.LogInformation("Configuring MVC in Startup");

                m_logger.LogInformation("Configuring Authorization in Startup");
                services.AddAuthentication(IISDefaults.AuthenticationScheme);
                
                m_logger.LogInformation("Configuring singletons in Startup");

                services.AddSingleton<IConfiguration>(AppConfiguration);

                m_commonUtilitiesLoader.Configure(services);
                                
                //services.AddSingleton<IActiveDirectoryAuthorizationConstants>(m_activeDirectoryAuthorizationConstants);
                //services.AddSingleton<IActiveDirectoryAuthorizationHandlerConfiguration>(m_activeDirectoryAuthorizationHandlerConfiguration);

                services.AddSingleton<IAuthorizationHandler, ActiveDirectoryAuthorizationHandler>();
                services.AddControllers();

                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "AdminDashboard Api", Version = "1" });
                });
                services.AddMvc().AddNewtonsoftJson();
                m_logger.LogInformation("Startup Completed!");
            }
            catch (Exception e)
            {
                m_logger.LogError(e, "Error ConfigureServices for Startup");
                throw;
            }
        }
        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseOptions();

            app.UseMiddleware<OptionsMiddleware>();
            app.UseMiddleware<ExceptionHandler>();

            app.UseCors("AllowHeaders");

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AdminDashboard API v1"));
            app.UseRouting();
            app.UseCors("AllowHeaders");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
