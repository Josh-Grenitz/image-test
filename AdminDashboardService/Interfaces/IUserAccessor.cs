using System.Collections.Generic;

namespace AdminDashboardService.Interfaces
{
    public interface IUserAccessor
    {
        List<string> GetCurrentUserRole();
    }
}
