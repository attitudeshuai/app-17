using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.MedicineShare;

namespace MedCabinet.Application.Interfaces;

public interface IBorrowService
{
    Task<ApiResponse<PagedResult<BorrowRequestDto>>> GetBorrowRequestsAsync(BorrowQueryParamsDto queryParams, int userId);
    Task<ApiResponse<BorrowRequestDto>> GetBorrowRequestByIdAsync(int id, int userId);
    Task<ApiResponse<BorrowRequestDto>> CreateBorrowRequestAsync(CreateBorrowRequestDto request, int userId);
    Task<ApiResponse<BorrowRequestDto>> ApproveBorrowRequestAsync(int id, ApproveBorrowRequestDto request, int userId);
    Task<ApiResponse<BorrowRequestDto>> RejectBorrowRequestAsync(int id, RejectBorrowRequestDto request, int userId);
    Task<ApiResponse<BorrowRequestDto>> CancelBorrowRequestAsync(int id, int userId);
    Task<ApiResponse<PagedResult<BorrowRecordDto>>> GetBorrowRecordsAsync(BorrowQueryParamsDto queryParams, int userId);
    Task<ApiResponse<BorrowRecordDto>> GetBorrowRecordByIdAsync(int id, int userId);
    Task<ApiResponse<BorrowRecordDto>> ReturnBorrowedMedicineAsync(int recordId, ReturnBorrowedMedicineDto request, int userId);
    Task<ApiResponse> CheckAndSendOverdueRemindersAsync();
    Task<ApiResponse<PagedResult<BorrowRecordDto>>> GetAllBorrowRecordsForAdminAsync(BorrowQueryParamsDto queryParams, int userId);
    Task<ApiResponse<PagedResult<MedicineShareDto>>> GetAllSharesForAdminAsync(ShareQueryParamsDto queryParams, int userId);
}
