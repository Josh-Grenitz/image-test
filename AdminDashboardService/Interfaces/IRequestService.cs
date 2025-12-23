using AdminDashboardService.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdminDashboardService.Interfaces
{
    public interface IRequestService
    {
        Task<List<RequestSummaryDto>> GetPendingAsync();
        Task<RequestDetailDto> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateRequestDto dto);
        Task UpdateAsync(int id, UpdateRequestDto dto);
        Task<List<RequestSummaryDto>> GetAllAsync();
        Task<bool> HasActiveRequestAsync(string objectName, string metaDataKey);
        Task CheckOutstandingRequestsAsync();
    }
}
