using System.ComponentModel.DataAnnotations;

namespace AdminDashboardService.Dtos
{
    public class ApproveRequestDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Request ID must be greater than 0")]
        public int RequestId { get; set; }

        [Required(ErrorMessage = "User ID is required")]
        [StringLength(200, ErrorMessage = "User ID cannot exceed 200 characters")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Comments are required")]
        [StringLength(350, ErrorMessage = "Comments cannot exceed 350 characters")]
        [MinLength(1, ErrorMessage = "Comments cannot be empty")]
        public string Comments { get; set; } = string.Empty;
    }
}