namespace AdminDashboardService.Dtos
{
    public class ApiResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public ApiResponseDto(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static ApiResponseDto SuccessResponse(string message) => new ApiResponseDto(true, message);
        public static ApiResponseDto FailureResponse(string message) => new ApiResponseDto(false, message);
    }
}
