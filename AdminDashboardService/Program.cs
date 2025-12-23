using ActiveDirectoryAuthorization.ActiveDirectoryAuthorization;
using ActiveDirectoryAuthorization.Interfaces;
using AdminDashboardService;
using AdminDashboardService.Accessors;
using AdminDashboardService.Interfaces;
using AdminDashboardService.Middleware;
using AdminDashboardService.Services;
using CommonApiUtilities;
using CommonApiUtilities.Interfaces;
using EmailUtility.Netcore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Web;
using PolicyAdmin;
using PolicyAdmin.Models;
using SB.AdminDashboard.EF.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

var builder = WebApplication.CreateBuilder(args);
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

if (string.IsNullOrEmpty(environment))
    throw new Exception("ASPNETCORE_ENVIRONMENT is not found in the System Environment variables. Please add it and do an IIS Reset!");

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

void SetProcessEnvironmentOverride(string key)
{
    var value = builder.Configuration[key];
    if (!string.IsNullOrWhiteSpace(value))
    {
        Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
    }
}

SetProcessEnvironmentOverride("SqlDatabaseConnectionString");
SetProcessEnvironmentOverride("SqlDatabaseConnectionStringFO");
SetProcessEnvironmentOverride("SqlDatabaseConnectionStringMO");
SetProcessEnvironmentOverride("DodsDevOpsConnectionString");

// -------------------------------------------------------
// LOGGER SETUP (log4net + NLog)
// -------------------------------------------------------

builder.Logging.ClearProviders();
builder.Host.UseNLog();

var log4netFile = $"log4net.{environment}.config";
if (!File.Exists(log4netFile))
{
    log4netFile = "log4net.config"; // fallback
}
builder.Logging.AddLog4Net(log4netFile);

// -------------------------------------------------------
// COMMON API UTILITIES INTEGRATION
// -------------------------------------------------------

builder.Logging.AddConsole();
var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
var logger = loggerFactory.CreateLogger("Program");
ICommonUtilitiesLoader commonUtilitiesLoader;
IApplicationConfiguration appConfiguration;

try
{
    logger.LogInformation("Initializing CommonApiUtilities...");
    var consts = new ReadOnlyDictionary<string, ReadOnlyCollection<string>>(new Dictionary<string, ReadOnlyCollection<string>>
    {
        { "ADMIN", new List<string>{ "CREATE", "Dashboard:Read", "UPDATE", "DELETE", "REFRESH" }.AsReadOnly() },
        { "MO", new List<string>{ "Dashboard:Read" }.AsReadOnly() },
        { "FO", new List<string>{ "Dashboard:Read" }.AsReadOnly() },
        { "READONLY", new List<string>{ "Dashboard:Read" }.AsReadOnly() }
    });

    var fileArgs = new Dictionary<string, object>
    {
        { "ApplicationName", typeof(string)},
        { "ArchiveFileLocation", typeof(string)},
        { "SqlDatabaseConnectionString", typeof(string) },
        { "SqlDatabaseConnectionStringFO", typeof(string) },
        { "SqlDatabaseConnectionStringMO", typeof(string) },
        { "AuditTableName", typeof(string)},
        { "MappingTableName", typeof(string)},
        { "UserRoleTableName", typeof(string)},
        { "LogLocation", typeof(string)},
        { "WhiteList", typeof(Dictionary<string, List<string>>)},
        { "CORSOrigins", typeof(List<string>)}
    };

    var sqlArgs = new Dictionary<string, (string TableName, string WhereStatement, string ConnectionString)>
    {
        { "ApplicationMapping", ("[DODSDevOps].[dbo].[AdminDashboardConfigMapping]", "", "SqlDatabaseConnectionStringFO")},
        { "ApplicationSqlMapping", ("[DODSDevOps].[dbo].[AdminDashboardConfigMapping]", "WHERE [Type] = 'SQL' ORDER BY [Application]", "SqlDatabaseConnectionStringFO")},
        { "ApplicationCacheMapping", ("[DODSDevOps].[dbo].[AdminDashboardConfigMapping]", "WHERE [Type] = 'CACHE' ORDER BY [Application]", "SqlDatabaseConnectionStringFO")},
        { "ApplicationFileMapping", ("[DODSDevOps].[dbo].[AdminDashboardConfigMapping]", "WHERE [Type] NOT IN ('SQL', 'CACHE') ORDER BY [Application]", "SqlDatabaseConnectionStringFO")},
        { "ApplicationUserGroups", ("[DODSDevOps].[dbo].[UserRoles]", "WHERE [Application] like '%AdminDashboard%' ORDER BY [Application]", "SqlDatabaseConnectionStringFO")}
    };

    commonUtilitiesLoader = new CommonUtilitiesLoaderAll(consts, fileArgs, sqlArgs);
    commonUtilitiesLoader.Startup(loggerFactory);
    appConfiguration = commonUtilitiesLoader.GetApplicationConfiguration();

    if (appConfiguration == null)
        throw new InvalidOperationException("CommonApiUtilities failed to initialize configuration.");
    commonUtilitiesLoader.Configure(builder.Services);
    builder.Services.AddSingleton(appConfiguration);
    builder.Services.AddSingleton(commonUtilitiesLoader);

    DataCache.Current = new DataCache(appConfiguration, logger);
    DataCache.Current.Initialize();

    logger.LogInformation("CommonApiUtilities initialized successfully.");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error initializing CommonApiUtilities.");
    throw;
}

var allowedOrigins = builder.Configuration.GetSection("CORSOrigins").Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        build =>
        {
            build
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()     // <-- absolutely required
            .AllowCredentials();
        });
});


// User Access
const string AppName = "AdminDashboard";
IPolicyHandler policyHandler;

Dictionary<string, ReadOnlyCollection<string>> rolePolicyMap = new Dictionary<string, ReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);
Dictionary<string, string> policyAdminConfigurationItems = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var configSection_PolicyAdmin = builder.Configuration.GetSection("PolicyAdmin");

foreach (IConfigurationSection section in configSection_PolicyAdmin.GetChildren())
{
    policyAdminConfigurationItems.Add(section.Key, section.Value);
}

string connectionString = policyAdminConfigurationItems["PolicyConnectionString"];
string policyTable = policyAdminConfigurationItems["PolicyTable"];
string groupTable = policyAdminConfigurationItems["GroupTable"];


IActiveDirectoryAuthorizationHandlerConfiguration m_activeDirectoryAuthorizationHandlerConfiguration = new ActiveDirectoryAuthorizationHandlerConfiguration()
{
    Whitelist = new Dictionary<string, List<string>>(),
    SqlConnectionString = connectionString,
    UserRoleTableName = "[dbo].[UserRoles]",
    ApplicationName = AppName
};

policyHandler = PolicyHandler.Create(connectionString, policyTable, groupTable);

foreach (string role in policyHandler.Cache.ApplicationRoles(AppName))
{
    var rolePolicies = new List<string>();
    foreach (IPolicy policy in policyHandler.Cache.ApplicationPolicies(AppName, role))
    {
        rolePolicies.Add(policy.Name);
    }
    rolePolicyMap.Add(role, new ReadOnlyCollection<string>(rolePolicies));
}

var authConsts = new ReadOnlyDictionary<string, ReadOnlyCollection<string>>(rolePolicyMap);
IActiveDirectoryAuthorizationConstants m_activeDirectoryAuthorizationConstants = new ActiveDirectoryAuthorizationConstants(authConsts);
// Configure Authentication with explicit Windows Authentication
builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);

// Add authorization with debugging
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AllUsers", policy => policy.RequireAuthenticatedUser());

builder.Services.AddAuthorization(options =>
{
    // Add basic policy for authentication debugging
    options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());

    // Add policies from policy handler
    foreach (IPolicy p in policyHandler.Cache.ApplicationPolicies(AppName))
    {
        options.AddPolicy(p.Name, policy => policy.Requirements.Add(new ActiveDirectoryAuthorizationRequirements(p.Name)));
    }

    // Set fallback policy to require authentication
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddScoped<EmailConfiguration>();
builder.Services.AddSingleton(m_activeDirectoryAuthorizationConstants);
builder.Services.AddSingleton(m_activeDirectoryAuthorizationHandlerConfiguration);
builder.Services.AddSingleton(policyHandler);
builder.Services.AddScoped<ActiveDirectoryAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ActiveDirectoryAuthorizationHandler>();

// Add DbContext for DODSDevOps
builder.Services.AddDbContext<DODSDevOps>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlDatabaseConnectionString")));

// Add HttpContextAccessor for UserAccessor
builder.Services.AddHttpContextAccessor();

// Add Accessor interfaces
builder.Services.AddSingleton<ILoggingService, LoggingService>();

builder.Services.AddScoped<IRequestDataAccessor, RequestDataAccessor>();
builder.Services.AddScoped<IApprovalDataAccessor, ApprovalDataAccessor>();
builder.Services.AddScoped<IUserAccessor, UserAccessor>();

builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddControllers();
builder.Services.AddMvc().AddNewtonsoftJson();
builder.Services.AddControllers()
.AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile));

var app = builder.Build();
app.UseCors("AllowSpecificOrigin");
app.UseMiddleware<OptionsMiddleware>();
app.UseMiddleware<ExceptionHandler>();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AdminDashboard API v1");
});

app.MapControllers();
app.Run();

