using SB.AdminDashboard.EF.Models;
using AdminDashboardService.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdminDashboardService.Interfaces
{
    public interface IRequestDataAccessor
    {
        Task<List<RequestView>> GetPendingForUserAsync(string userId);
        Task<List<RequestView>> GetAllRequestViewsAsync();
        Task<Request> GetByIdAsync(int id);
        Task<Request> InsertAsync(Request request);
        Task UpdateAsync(Request request);
        Task<bool> HasActiveRequestAsync(string objectName, string metaDataKey);
        
        // New methods replacing service layer
        Task<List<RequestSummaryDto>> GetPendingSummaryForUserAsync(string userId);
        Task<List<RequestSummaryDto>> GetAllSummaryAsync();
        Task<RequestDetailDto> GetDetailByIdAsync(int id);
        Task<int> CreateRequestAsync(CreateRequestDto dto);
        Task UpdateRequestAsync(int id, UpdateRequestDto dto);
        Task CheckOutstandingRequestsAsync();
    }
}
