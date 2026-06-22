using Mapster;
using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.MedicineShare;
using MedCabinet.Application.Interfaces;
using MedCabinet.Domain.Entities;
using MedCabinet.Domain.Enums;
using MedCabinet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedCabinet.Application.Services;

public class BorrowService : IBorrowService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BorrowService> _logger;

    public BorrowService(IUnitOfWork unitOfWork, ILogger<BorrowService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<BorrowRequestDto>>> GetBorrowRequestsAsync(BorrowQueryParamsDto queryParams, int userId)
    {
        try
        {
            var userHouseholdIds = await GetUserHouseholdIdsAsync(userId);
            if (!userHouseholdIds.Any())
            {
                return ApiResponse<PagedResult<BorrowRequestDto>>.Success(CreateEmptyRequestPagedResult(queryParams));
            }

            var statusFilter = queryParams.RequestStatus;

            var allRequests = await _unitOfWork.BorrowRequests.GetAllAsync();
            var filteredRequests = allRequests
                .Where(br =>
                {
                    var shareId = br.MedicineShareId;
                    var share = _unitOfWork.MedicineShares.GetByIdAsync(shareId).Result;
                    if (share == null) return false;
                    return userHouseholdIds.Contains(share.LenderHouseholdId) ||
                           userHouseholdIds.Contains(share.BorrowerHouseholdId);
                })
                .Where(br => !statusFilter.HasValue || br.Status == statusFilter.Value)
                .ToList();

            var totalCount = filteredRequests.Count;
            var pagedItems = filteredRequests
                .OrderByDescending(br => br.CreatedAt)
                .Skip((queryParams.PageIndex - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToList();

            var dtos = pagedItems.Adapt<List<BorrowRequestDto>>();
            var result = CreateRequestPagedResult(dtos, totalCount, queryParams);

            return ApiResponse<PagedResult<BorrowRequestDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取借用申请列表失败");
            return ApiResponse<PagedResult<BorrowRequestDto>>.Error("获取借用申请列表失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<BorrowRequestDto>> GetBorrowRequestByIdAsync(int id, int userId)
    {
        try
        {
            var request = await _unitOfWork.BorrowRequests.GetByIdAsync(id);
            if (request == null)
            {
                return ApiResponse<BorrowRequestDto>.Error("借用申请不存在", 404);
            }

            var hasAccess = await HasAccessToRequestAsync(request, userId);
            if (!hasAccess)
            {
                return ApiResponse<BorrowRequestDto>.Error("无权限访问此借用申请", 403);
            }

            var dto = request.Adapt<BorrowRequestDto>();
            return ApiResponse<BorrowRequestDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取借用申请详情失败");
            return ApiResponse<BorrowRequestDto>.Error("获取借用申请详情失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<BorrowRequestDto>> CreateBorrowRequestAsync(CreateBorrowRequestDto request, int userId)
    {
        try
        {
            var share = await _unitOfWork.MedicineShares.GetByIdAsync(request.MedicineShareId);
            if (share == null)
            {
                return ApiResponse<BorrowRequestDto>.Error("共享关系不存在", 404);
            }

            if (share.Status != ShareStatus.Active)
            {
                return ApiResponse<BorrowRequestDto>.Error("共享关系未激活", 400);
            }

            var hasBorrowerPermission = await HasHouseholdAccessAsync(share.BorrowerHouseholdId, userId);
            if (!hasBorrowerPermission)
            {
                return ApiResponse<BorrowRequestDto>.Error("无权限发起借用申请", 403);
            }

            var sharedMedicine = (await _unitOfWork.SharedMedicines
                .FindAsync(sm => sm.MedicineShareId == request.MedicineShareId && sm.MedicineId == request.MedicineId))
                .FirstOrDefault();

            if (sharedMedicine == null || !sharedMedicine.IsActive)
            {
                return ApiResponse<BorrowRequestDto>.Error("该药品未被共享", 400);
            }

            var medicine = await _unitOfWork.Medicines.GetByIdAsync(request.MedicineId);
            if (medicine == null)
            {
                return ApiResponse<BorrowRequestDto>.Error("药品不存在", 404);
            }

            if (request.RequestedQuantity <= 0)
            {
                return ApiResponse<BorrowRequestDto>.Error("申请数量必须大于0", 400);
            }

            if (request.RequestedQuantity > medicine.StockQuantity)
            {
                return ApiResponse<BorrowRequestDto>.Error($"申请数量超过可用库存，当前库存: {medicine.StockQuantity}", 400);
            }

            if (request.ExpectedReturnDate <= DateTime.Now)
            {
                return ApiResponse<BorrowRequestDto>.Error("预计归还日期必须晚于当前时间", 400);
            }

            var borrowRequest = new BorrowRequest
            {
                MedicineShareId = request.MedicineShareId,
                MedicineId = request.MedicineId,
                RequesterUserId = userId,
                RequestedQuantity = request.RequestedQuantity,
                Purpose = request.Purpose,
                ExpectedReturnDate = request.ExpectedReturnDate,
                Status = BorrowRequestStatus.Pending,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.BorrowRequests.AddAsync(borrowRequest);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"创建借用申请成功: ID={borrowRequest.Id}, 药品={medicine.Name}, 数量={request.RequestedQuantity}");

            var dto = borrowRequest.Adapt<BorrowRequestDto>();
            return ApiResponse<BorrowRequestDto>.Success(dto, "申请已提交");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建借用申请失败");
            return ApiResponse<BorrowRequestDto>.Error("创建借用申请失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<BorrowRequestDto>> ApproveBorrowRequestAsync(int id, ApproveBorrowRequestDto request, int userId)
    {
        try
        {
            var borrowRequest = await _unitOfWork.BorrowRequests.GetByIdAsync(id);
            if (borrowRequest == null)
            {
                return ApiResponse<BorrowRequestDto>.Error("借用申请不存在", 404);
            }

            if (borrowRequest.Status != BorrowRequestStatus.Pending)
            {
                return ApiResponse<BorrowRequestDto>.Error("只有待审批的申请可以批准", 400);
            }

            var share = await _unitOfWork.MedicineShares.GetByIdAsync(borrowRequest.MedicineShareId);
            if (share == null)
            {
                return ApiResponse<BorrowRequestDto>.Error("共享关系不存在", 404);
            }

            var hasLenderPermission = await HasHouseholdPermissionAsync(share.LenderHouseholdId, userId, "Owner", "Admin", "Member");
            if (!hasLenderPermission)
            {
                return ApiResponse<BorrowRequestDto>.Error("无权限审批此申请", 403);
            }

            var approvedQuantity = request.ApprovedQuantity ?? borrowRequest.RequestedQuantity;
            if (approvedQuantity <= 0)
            {
                return ApiResponse<BorrowRequestDto>.Error("批准数量必须大于0", 400);
            }

            if (approvedQuantity > borrowRequest.RequestedQuantity)
            {
                return ApiResponse<BorrowRequestDto>.Error("批准数量不能超过申请数量", 400);
            }

            var medicine = await _unitOfWork.Medicines.GetByIdAsync(borrowRequest.MedicineId);
            if (medicine == null)
            {
                return ApiResponse<BorrowRequestDto>.Error("药品不存在", 404);
            }

            if (approvedQuantity > medicine.StockQuantity)
            {
                return ApiResponse<BorrowRequestDto>.Error($"批准数量超过可用库存，当前库存: {medicine.StockQuantity}", 400);
            }

            await _unitOfWork.BeginTransactionAsync();

            medicine.StockQuantity -= approvedQuantity;
            if (medicine.StockQuantity == 0)
            {
                medicine.Status = MedicineStatus.Empty;
            }
            else if (medicine.StockQuantity <= 5)
            {
                medicine.Status = MedicineStatus.NearExpiry;
            }

            await _unitOfWork.Medicines.UpdateAsync(medicine);

            borrowRequest.Status = BorrowRequestStatus.Approved;
            borrowRequest.ApprovedByUserId = userId;
            borrowRequest.ApprovedAt = DateTime.Now;
            borrowRequest.UpdatedAt = DateTime.Now;

            await _unitOfWork.BorrowRequests.UpdateAsync(borrowRequest);

            var borrowRecord = new BorrowRecord
            {
                MedicineShareId = borrowRequest.MedicineShareId,
                BorrowRequestId = borrowRequest.Id,
                MedicineId = borrowRequest.MedicineId,
                LenderHouseholdId = share.LenderHouseholdId,
                BorrowerHouseholdId = share.BorrowerHouseholdId,
                BorrowerUserId = borrowRequest.RequesterUserId,
                BorrowedQuantity = approvedQuantity,
                ReturnedQuantity = 0,
                BorrowedAt = DateTime.Now,
                ExpectedReturnDate = borrowRequest.ExpectedReturnDate,
                Status = BorrowRecordStatus.Active,
                ReminderSent = false
            };

            await _unitOfWork.BorrowRecords.AddAsync(borrowRecord);
            await _unitOfWork.SaveChangesAsync();

            await CreateStockAlertForLenderAsync(medicine, share.LenderHouseholdId);

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation($"批准借用申请成功: ID={id}, 批准数量={approvedQuantity}, 库存扣减完成");

            var dto = borrowRequest.Adapt<BorrowRequestDto>();
            return ApiResponse<BorrowRequestDto>.Success(dto, "批准成功，已扣减库存并生成借用记录");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "批准借用申请失败");
            return ApiResponse<BorrowRequestDto>.Error("批准借用申请失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<BorrowRequestDto>> RejectBorrowRequestAsync(int id, RejectBorrowRequestDto request, int userId)
    {
        try
        {
            var borrowRequest = await _unitOfWork.BorrowRequests.GetByIdAsync(id);
            if (borrowRequest == null)
            {
                return ApiResponse<BorrowRequestDto>.Error("借用申请不存在", 404);
            }

            if (borrowRequest.Status != BorrowRequestStatus.Pending)
            {
                return ApiResponse<BorrowRequestDto>.Error("只有待审批的申请可以拒绝", 400);
            }

            var share = await _unitOfWork.MedicineShares.GetByIdAsync(borrowRequest.MedicineShareId);
            if (share == null)
            {
                return ApiResponse<BorrowRequestDto>.Error("共享关系不存在", 404);
            }

            var hasLenderPermission = await HasHouseholdPermissionAsync(share.LenderHouseholdId, userId, "Owner", "Admin", "Member");
            if (!hasLenderPermission)
            {
                return ApiResponse<BorrowRequestDto>.Error("无权限审批此申请", 403);
            }

            borrowRequest.Status = BorrowRequestStatus.Rejected;
            borrowRequest.RejectionReason = request.RejectionReason;
            borrowRequest.UpdatedAt = DateTime.Now;

            await _unitOfWork.BorrowRequests.UpdateAsync(borrowRequest);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"拒绝借用申请成功: ID={id}");

            var dto = borrowRequest.Adapt<BorrowRequestDto>();
            return ApiResponse<BorrowRequestDto>.Success(dto, "已拒绝申请");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "拒绝借用申请失败");
            return ApiResponse<BorrowRequestDto>.Error("拒绝借用申请失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<BorrowRequestDto>> CancelBorrowRequestAsync(int id, int userId)
    {
        try
        {
            var borrowRequest = await _unitOfWork.BorrowRequests.GetByIdAsync(id);
            if (borrowRequest == null)
            {
                return ApiResponse<BorrowRequestDto>.Error("借用申请不存在", 404);
            }

            if (borrowRequest.Status != BorrowRequestStatus.Pending)
            {
                return ApiResponse<BorrowRequestDto>.Error("只有待审批的申请可以取消", 400);
            }

            if (borrowRequest.RequesterUserId != userId)
            {
                return ApiResponse<BorrowRequestDto>.Error("只能取消自己发起的申请", 403);
            }

            borrowRequest.Status = BorrowRequestStatus.Cancelled;
            borrowRequest.UpdatedAt = DateTime.Now;

            await _unitOfWork.BorrowRequests.UpdateAsync(borrowRequest);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"取消借用申请成功: ID={id}, 用户={userId}");

            var dto = borrowRequest.Adapt<BorrowRequestDto>();
            return ApiResponse<BorrowRequestDto>.Success(dto, "已取消申请");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消借用申请失败");
            return ApiResponse<BorrowRequestDto>.Error("取消借用申请失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<PagedResult<BorrowRecordDto>>> GetBorrowRecordsAsync(BorrowQueryParamsDto queryParams, int userId)
    {
        try
        {
            var userHouseholdIds = await GetUserHouseholdIdsAsync(userId);
            if (!userHouseholdIds.Any())
            {
                return ApiResponse<PagedResult<BorrowRecordDto>>.Success(CreateEmptyRecordPagedResult(queryParams));
            }

            var statusFilter = queryParams.RecordStatus;

            var allRecords = await _unitOfWork.BorrowRecords.GetAllAsync();
            var filteredRecords = allRecords
                .Where(br => userHouseholdIds.Contains(br.LenderHouseholdId) || userHouseholdIds.Contains(br.BorrowerHouseholdId))
                .Where(br => !statusFilter.HasValue || br.Status == statusFilter.Value)
                .ToList();

            var totalCount = filteredRecords.Count;
            var pagedItems = filteredRecords
                .OrderByDescending(br => br.BorrowedAt)
                .Skip((queryParams.PageIndex - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToList();

            var dtos = pagedItems.Adapt<List<BorrowRecordDto>>();
            var result = CreateRecordPagedResult(dtos, totalCount, queryParams);

            return ApiResponse<PagedResult<BorrowRecordDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取借用记录列表失败");
            return ApiResponse<PagedResult<BorrowRecordDto>>.Error("获取借用记录列表失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<BorrowRecordDto>> GetBorrowRecordByIdAsync(int id, int userId)
    {
        try
        {
            var record = await _unitOfWork.BorrowRecords.GetByIdAsync(id);
            if (record == null)
            {
                return ApiResponse<BorrowRecordDto>.Error("借用记录不存在", 404);
            }

            var userHouseholdIds = await GetUserHouseholdIdsAsync(userId);
            if (!userHouseholdIds.Contains(record.LenderHouseholdId) && !userHouseholdIds.Contains(record.BorrowerHouseholdId))
            {
                return ApiResponse<BorrowRecordDto>.Error("无权限访问此借用记录", 403);
            }

            var dto = record.Adapt<BorrowRecordDto>();
            return ApiResponse<BorrowRecordDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取借用记录详情失败");
            return ApiResponse<BorrowRecordDto>.Error("获取借用记录详情失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<BorrowRecordDto>> ReturnBorrowedMedicineAsync(int recordId, ReturnBorrowedMedicineDto request, int userId)
    {
        try
        {
            var record = await _unitOfWork.BorrowRecords.GetByIdAsync(recordId);
            if (record == null)
            {
                return ApiResponse<BorrowRecordDto>.Error("借用记录不存在", 404);
            }

            if (record.Status != BorrowRecordStatus.Active && record.Status != BorrowRecordStatus.Overdue)
            {
                return ApiResponse<BorrowRecordDto>.Error("此借用记录已处理", 400);
            }

            var userHouseholdIds = await GetUserHouseholdIdsAsync(userId);
            if (!userHouseholdIds.Contains(record.LenderHouseholdId) && !userHouseholdIds.Contains(record.BorrowerHouseholdId))
            {
                return ApiResponse<BorrowRecordDto>.Error("无权限操作此借用记录", 403);
            }

            if (request.ReturnedQuantity <= 0)
            {
                return ApiResponse<BorrowRecordDto>.Error("归还数量必须大于0", 400);
            }

            if (request.ReturnedQuantity > record.RemainingQuantity)
            {
                return ApiResponse<BorrowRecordDto>.Error($"归还数量不能超过剩余未还数量，剩余数量: {record.RemainingQuantity}", 400);
            }

            var medicine = await _unitOfWork.Medicines.GetByIdAsync(record.MedicineId);
            if (medicine == null)
            {
                return ApiResponse<BorrowRecordDto>.Error("药品不存在", 404);
            }

            await _unitOfWork.BeginTransactionAsync();

            medicine.StockQuantity += request.ReturnedQuantity;
            if (medicine.StockQuantity > 0 && medicine.Status == MedicineStatus.Empty)
            {
                var daysUntilExpiry = (medicine.ExpiryDate - DateTime.Now).TotalDays;
                if (daysUntilExpiry <= 0)
                {
                    medicine.Status = MedicineStatus.Expired;
                }
                else if (daysUntilExpiry <= 30)
                {
                    medicine.Status = MedicineStatus.NearExpiry;
                }
                else
                {
                    medicine.Status = MedicineStatus.Valid;
                }
            }

            await _unitOfWork.Medicines.UpdateAsync(medicine);

            record.ReturnedQuantity += request.ReturnedQuantity;
            record.LastReturnedAt = DateTime.Now;

            if (!string.IsNullOrEmpty(request.Notes))
            {
                record.Notes = string.IsNullOrEmpty(record.Notes) 
                    ? request.Notes 
                    : $"{record.Notes}; {DateTime.Now:yyyy-MM-dd HH:mm}: {request.Notes}";
            }

            if (record.ReturnedQuantity == record.BorrowedQuantity)
            {
                record.Status = BorrowRecordStatus.Returned;

                var borrowRequest = await _unitOfWork.BorrowRequests.GetByIdAsync(record.BorrowRequestId);
                if (borrowRequest != null && borrowRequest.Status == BorrowRequestStatus.Approved)
                {
                    borrowRequest.Status = BorrowRequestStatus.Returned;
                    borrowRequest.UpdatedAt = DateTime.Now;
                    await _unitOfWork.BorrowRequests.UpdateAsync(borrowRequest);
                }
            }

            await _unitOfWork.BorrowRecords.UpdateAsync(record);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var statusMessage = record.Status == BorrowRecordStatus.Returned 
                ? "全部归还成功，库存已同步恢复" 
                : $"部分归还成功，已归还 {record.ReturnedQuantity}/{record.BorrowedQuantity}，剩余 {record.RemainingQuantity}，库存已同步恢复";

            _logger.LogInformation($"归还药品: 记录ID={recordId}, 本次归还={request.ReturnedQuantity}, 累计归还={record.ReturnedQuantity}/{record.BorrowedQuantity}");

            var dto = record.Adapt<BorrowRecordDto>();
            return ApiResponse<BorrowRecordDto>.Success(dto, statusMessage);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "归还药品失败");
            return ApiResponse<BorrowRecordDto>.Error("归还药品失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse> CheckAndSendOverdueRemindersAsync()
    {
        try
        {
            var now = DateTime.Now;
            var overdueRecords = await _unitOfWork.BorrowRecords
                .FindAsync(br => br.Status == BorrowRecordStatus.Active &&
                                br.ExpectedReturnDate < now &&
                                !br.ReminderSent);

            var overdueCount = 0;

            foreach (var record in overdueRecords)
            {
                record.Status = BorrowRecordStatus.Overdue;
                record.ReminderSent = true;

                await _unitOfWork.BorrowRecords.UpdateAsync(record);

                var lenderMembers = await _unitOfWork.HouseholdMembers
                    .FindAsync(hm => hm.HouseholdId == record.LenderHouseholdId);

                foreach (var member in lenderMembers)
                {
                    var lenderAlert = new MedAlert
                    {
                        MedicineId = record.MedicineId,
                        UserId = member.UserId,
                        AlertType = AlertType.UsageReminder,
                        Message = $"借出的药品已逾期未归还，借用ID: {record.Id}，请联系借入方催还。",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };
                    await _unitOfWork.MedAlerts.AddAsync(lenderAlert);
                }

                var borrowerAlert = new MedAlert
                {
                    MedicineId = record.MedicineId,
                    UserId = record.BorrowerUserId,
                    AlertType = AlertType.UsageReminder,
                    Message = $"您借用的药品已逾期，请尽快归还。借用ID: {record.Id}",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                await _unitOfWork.MedAlerts.AddAsync(borrowerAlert);

                overdueCount++;
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"检查逾期借用完成，共处理 {overdueCount} 条逾期记录");

            return ApiResponse.Success($"已处理 {overdueCount} 条逾期借用记录并发送提醒");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查逾期借用失败");
            return ApiResponse.Error("检查逾期借用失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<PagedResult<BorrowRecordDto>>> GetAllBorrowRecordsForAdminAsync(BorrowQueryParamsDto queryParams, int userId)
    {
        try
        {
            var isAdmin = await IsAdminUserAsync(userId);
            if (!isAdmin)
            {
                return ApiResponse<PagedResult<BorrowRecordDto>>.Error("无管理员权限", 403);
            }

            var statusFilter = queryParams.RecordStatus;

            var (items, totalCount) = await _unitOfWork.BorrowRecords.GetPagedAsync(
                queryParams.PageIndex,
                queryParams.PageSize,
                br => !statusFilter.HasValue || br.Status == statusFilter.Value,
                queryParams.SortBy ?? "BorrowedAt",
                queryParams.SortDescending);

            var dtos = items.Adapt<List<BorrowRecordDto>>();
            var result = CreateRecordPagedResult(dtos, totalCount, queryParams);

            return ApiResponse<PagedResult<BorrowRecordDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "管理员获取所有借用记录失败");
            return ApiResponse<PagedResult<BorrowRecordDto>>.Error("获取借用记录失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<PagedResult<MedicineShareDto>>> GetAllSharesForAdminAsync(ShareQueryParamsDto queryParams, int userId)
    {
        try
        {
            var isAdmin = await IsAdminUserAsync(userId);
            if (!isAdmin)
            {
                return ApiResponse<PagedResult<MedicineShareDto>>.Error("无管理员权限", 403);
            }

            var statusFilter = queryParams.Status;

            var (items, totalCount) = await _unitOfWork.MedicineShares.GetPagedAsync(
                queryParams.PageIndex,
                queryParams.PageSize,
                ms => !statusFilter.HasValue || ms.Status == statusFilter.Value,
                queryParams.SortBy ?? "CreatedAt",
                queryParams.SortDescending);

            var dtos = items.Adapt<List<MedicineShareDto>>();

            var shareIds = items.Select(s => s.Id).ToList();
            var allSharedMedicines = await _unitOfWork.SharedMedicines.FindAsync(sm => shareIds.Contains(sm.MedicineShareId));
            foreach (var dto in dtos)
            {
                dto.SharedMedicines = allSharedMedicines
                    .Where(sm => sm.MedicineShareId == dto.Id)
                    .Adapt<List<SharedMedicineDto>>();
            }

            var result = CreateEmptyPagedResult(queryParams);
            result.Items = dtos;
            result.TotalCount = totalCount;
            result.TotalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize);
            result.HasPreviousPage = queryParams.PageIndex > 1;
            result.HasNextPage = queryParams.PageIndex * queryParams.PageSize < totalCount;

            return ApiResponse<PagedResult<MedicineShareDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "管理员获取所有共享关系失败");
            return ApiResponse<PagedResult<MedicineShareDto>>.Error("获取共享关系失败: " + ex.Message, 500);
        }
    }

    private async Task<List<int>> GetUserHouseholdIdsAsync(int userId)
    {
        var members = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == userId);
        return members.Select(hm => hm.HouseholdId).ToList();
    }

    private async Task<bool> HasHouseholdAccessAsync(int householdId, int userId)
    {
        var members = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.HouseholdId == householdId && hm.UserId == userId);
        return members.Any();
    }

    private async Task<bool> HasHouseholdPermissionAsync(int householdId, int userId, params string[] allowedRoles)
    {
        var members = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.HouseholdId == householdId && hm.UserId == userId);
        var member = members.FirstOrDefault();
        return member != null && allowedRoles.Contains(member.Role);
    }

    private async Task<bool> HasAccessToRequestAsync(BorrowRequest request, int userId)
    {
        var userHouseholdIds = await GetUserHouseholdIdsAsync(userId);
        var share = await _unitOfWork.MedicineShares.GetByIdAsync(request.MedicineShareId);
        if (share == null) return false;
        return userHouseholdIds.Contains(share.LenderHouseholdId) ||
               userHouseholdIds.Contains(share.BorrowerHouseholdId) ||
               request.RequesterUserId == userId;
    }

    private async Task<bool> IsAdminUserAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return user != null && user.IsSystemAdmin;
    }

    private async Task CreateStockAlertForLenderAsync(Medicine medicine, int lenderHouseholdId)
    {
        var lenderMembers = await _unitOfWork.HouseholdMembers
            .FindAsync(hm => hm.HouseholdId == lenderHouseholdId);

        foreach (var member in lenderMembers)
        {
            if (medicine.StockQuantity <= 0)
            {
                var existingAlert = await _unitOfWork.MedAlerts
                    .ExistsAsync(a => a.MedicineId == medicine.Id &&
                                     a.UserId == member.UserId &&
                                     a.AlertType == AlertType.EmptyStock &&
                                     !a.IsRead);

                if (!existingAlert)
                {
                    var alert = new MedAlert
                    {
                        MedicineId = medicine.Id,
                        UserId = member.UserId,
                        AlertType = AlertType.EmptyStock,
                        Message = $"{medicine.Name}库存已空（因借出），请及时补充。",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };
                    await _unitOfWork.MedAlerts.AddAsync(alert);
                }
            }
            else if (medicine.StockQuantity <= 5)
            {
                var existingAlert = await _unitOfWork.MedAlerts
                    .ExistsAsync(a => a.MedicineId == medicine.Id &&
                                     a.UserId == member.UserId &&
                                     a.AlertType == AlertType.LowStock &&
                                     !a.IsRead);

                if (!existingAlert)
                {
                    var alert = new MedAlert
                    {
                        MedicineId = medicine.Id,
                        UserId = member.UserId,
                        AlertType = AlertType.LowStock,
                        Message = $"{medicine.Name}库存不足（因借出），仅剩{medicine.StockQuantity}份。",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };
                    await _unitOfWork.MedAlerts.AddAsync(alert);
                }
            }
        }
    }

    private static PagedResult<BorrowRequestDto> CreateEmptyRequestPagedResult(BorrowQueryParamsDto queryParams)
    {
        return new PagedResult<BorrowRequestDto>
        {
            Items = new List<BorrowRequestDto>(),
            TotalCount = 0,
            PageIndex = queryParams.PageIndex,
            PageSize = queryParams.PageSize,
            TotalPages = 0,
            HasPreviousPage = false,
            HasNextPage = false
        };
    }

    private static PagedResult<BorrowRequestDto> CreateRequestPagedResult(List<BorrowRequestDto> items, int totalCount, BorrowQueryParamsDto queryParams)
    {
        return new PagedResult<BorrowRequestDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = queryParams.PageIndex,
            PageSize = queryParams.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize),
            HasPreviousPage = queryParams.PageIndex > 1,
            HasNextPage = queryParams.PageIndex * queryParams.PageSize < totalCount
        };
    }

    private static PagedResult<BorrowRecordDto> CreateEmptyRecordPagedResult(BorrowQueryParamsDto queryParams)
    {
        return new PagedResult<BorrowRecordDto>
        {
            Items = new List<BorrowRecordDto>(),
            TotalCount = 0,
            PageIndex = queryParams.PageIndex,
            PageSize = queryParams.PageSize,
            TotalPages = 0,
            HasPreviousPage = false,
            HasNextPage = false
        };
    }

    private static PagedResult<BorrowRecordDto> CreateRecordPagedResult(List<BorrowRecordDto> items, int totalCount, BorrowQueryParamsDto queryParams)
    {
        return new PagedResult<BorrowRecordDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = queryParams.PageIndex,
            PageSize = queryParams.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize),
            HasPreviousPage = queryParams.PageIndex > 1,
            HasNextPage = queryParams.PageIndex * queryParams.PageSize < totalCount
        };
    }

    private static PagedResult<MedicineShareDto> CreateEmptyPagedResult(ShareQueryParamsDto queryParams)
    {
        return new PagedResult<MedicineShareDto>
        {
            Items = new List<MedicineShareDto>(),
            TotalCount = 0,
            PageIndex = queryParams.PageIndex,
            PageSize = queryParams.PageSize,
            TotalPages = 0,
            HasPreviousPage = false,
            HasNextPage = false
        };
    }
}
