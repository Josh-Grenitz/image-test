using ActiveDirectoryAuthorization.ActiveDirectoryAuthorization;
using ActiveDirectoryAuthorization.Interfaces;
using AdminDashboardService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SB.AdminDashboard.EF.Data;
using System.Collections.Generic;

namespace AdminDashboardService.Accessors
{
    public class UserAccessor : IUserAccessor
    {
        private readonly ActiveDirectoryAuthorizationHandler _authorizationHandler;
        private readonly ILogger<UserAccessor> _logger;

        public UserAccessor(ILoggerFactory loggerFactory, 
            IActiveDirectoryAuthorizationConstants activeDirectoryAuthorizationConstants,
            IActiveDirectoryAuthorizationHandlerConfiguration activeDirectoryAuthorizationHandlerConfiguration,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = loggerFactory.CreateLogger<UserAccessor>();
            _authorizationHandler = new(loggerFactory,
               activeDirectoryAuthorizationConstants,
               activeDirectoryAuthorizationHandlerConfiguration, httpContextAccessor);
        }

        public List<string> GetCurrentUserRole()
        {
            return _authorizationHandler.GetLoggedInUserRoles();
        }

    }
}
