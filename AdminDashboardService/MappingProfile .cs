using AdminDashboardService.Constants;
using AdminDashboardService.Dtos;
using AdminDashboardService.Enums;
using AutoMapper;
using SB.AdminDashboard.EF.Models;

namespace AdminDashboardService
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<RequestView, RequestSummaryDto>()
                .ForMember(d => d.ApprovalStatus, o => o.MapFrom(s => GetStatusDescription(s.ApprovalStatus)))
                .ForMember(d => d.OriginalData, o => o.MapFrom(s => s.OriginalData ?? string.Empty))
                .ForMember(d => d.CurrentApprovalLevel, o => o.Ignore())
                .ForMember(d => d.TotalApprovalLevels, o => o.Ignore());

            CreateMap<Approval, ApprovalDto>()
                .ForMember(d => d.LevelName, o => o.MapFrom(s => s.Level.LevelName));

            CreateMap<Request, RequestDetailDto>();
        }

        private static string GetStatusDescription(int? statusId)
        {

            return statusId.Value switch
            {
                (int)ApprovalStatusEnum.Pending => ApprovalStatusConstants.Pending,
                (int)ApprovalStatusEnum.FirstReview => ApprovalStatusConstants.FirstReview,
                (int)ApprovalStatusEnum.FinalReview => ApprovalStatusConstants.FinalReview,
                (int)ApprovalStatusEnum.Denied => ApprovalStatusConstants.Denied,
                (int)ApprovalStatusEnum.Completed => ApprovalStatusConstants.Completed
            };
        }
    }
}
