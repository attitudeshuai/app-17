using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.HouseholdMember;

namespace MedCabinet.Application.Interfaces;

public interface IHouseholdMemberService
{
    Task<ApiResponse<PagedResult<HouseholdMemberDto>>> GetHouseholdMembersAsync(HouseholdMemberQueryParamsDto queryParams, int userId);
    Task<ApiResponse<HouseholdMemberDto>> GetHouseholdMemberByIdAsync(int id, int userId);
    Task<ApiResponse<HouseholdMemberDto>> AddMemberAsync(CreateHouseholdMemberRequestDto request, int userId);
    Task<ApiResponse<HouseholdMemberDto>> UpdateMemberAsync(int id, UpdateHouseholdMemberRequestDto request, int userId);
    Task<ApiResponse> RemoveMemberAsync(int id, int userId);
    Task<ApiResponse<HouseholdMemberDto>> JoinHouseholdByInviteCodeAsync(JoinHouseholdRequestDto request, int userId);
    Task<ApiResponse<PagedResult<HouseholdMemberDto>>> GetMyMembersAsync(HouseholdMemberQueryParamsDto queryParams, int userId);
}
