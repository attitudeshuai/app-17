using System.Text.RegularExpressions;
using Mapster;
using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.Medicine;
using MedCabinet.Application.DTOs.MedicineRecognition;
using MedCabinet.Application.Interfaces;
using MedCabinet.Domain.Entities;
using MedCabinet.Domain.Enums;
using MedCabinet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedCabinet.Application.Services;

public class MedicineRecognitionService : IMedicineRecognitionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOcrService _ocrService;
    private readonly IMedicineService _medicineService;
    private readonly ILogger<MedicineRecognitionService> _logger;

    public MedicineRecognitionService(
        IUnitOfWork unitOfWork,
        IOcrService ocrService,
        IMedicineService medicineService,
        ILogger<MedicineRecognitionService> logger)
    {
        _unitOfWork = unitOfWork;
        _ocrService = ocrService;
        _medicineService = medicineService;
        _logger = logger;
    }

    public async Task<ApiResponse<MedicineRecognitionResultDto>> RecognizeFromImageAsync(
        Stream imageStream, string fileName, string contentType, long fileSize, int? householdId, int userId)
    {
        try
        {
            if (imageStream == null || fileSize == 0)
            {
                return ApiResponse<MedicineRecognitionResultDto>.Error("请上传有效的图片文件", 400);
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
            var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return ApiResponse<MedicineRecognitionResultDto>.Error("不支持的图片格式，支持JPG、PNG、BMP、WebP", 400);
            }

            if (fileSize > 10 * 1024 * 1024)
            {
                return ApiResponse<MedicineRecognitionResultDto>.Error("图片大小不能超过10MB", 400);
            }

            if (householdId.HasValue)
            {
                var hasAccess = await HasHouseholdAccessAsync(householdId.Value, userId);
                if (!hasAccess)
                {
                    return ApiResponse<MedicineRecognitionResultDto>.Error("无权限访问此家庭", 403);
                }
            }

            string imageUrl = await SaveImageAsync(imageStream, fileName);

            OcrResult ocrResult;
            imageStream.Position = 0;
            ocrResult = await _ocrService.RecognizeAsync(imageStream, fileName);

            var recognizedInfo = ParseMedicineInfo(ocrResult);

            var (recognitionStatus, missingFields) = EvaluateRecognitionStatus(recognizedInfo);

            var record = new MedicineRecognitionRecord
            {
                UserId = userId,
                HouseholdId = householdId,
                ImageUrl = imageUrl,
                RecognitionStatus = recognitionStatus,
                ConfirmStatus = RecognitionConfirmStatus.Pending,
                RecognizedName = recognizedInfo.Name,
                RecognizedSpecification = recognizedInfo.Specification,
                RecognizedExpiryDate = recognizedInfo.ExpiryDate,
                RecognizedDosage = recognizedInfo.Dosage,
                RecognizedManufacturer = recognizedInfo.Manufacturer,
                RawOcrText = ocrResult.RawText,
                ConfidenceScore = recognizedInfo.ConfidenceScore,
                RecognitionError = ocrResult.ErrorMessage,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.MedicineRecognitionRecords.AddAsync(record);
            await _unitOfWork.SaveChangesAsync();

            var matchedMedicines = new List<MatchedMedicineDto>();
            if (!string.IsNullOrEmpty(recognizedInfo.Name))
            {
                var matchResult = await MatchMedicineInternalAsync(
                    recognizedInfo.Name, recognizedInfo.Specification, householdId, userId);
                if (matchResult != null)
                {
                    matchedMedicines = matchResult;
                    if (matchedMedicines.Any())
                    {
                        var topMatch = matchedMedicines.First();
                        record.MatchedMedicineId = topMatch.MedicineId;
                        record.MatchScore = topMatch.MatchScore;
                        await _unitOfWork.MedicineRecognitionRecords.UpdateAsync(record);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }
            }

            recognizedInfo.MissingFields = missingFields;

            var result = new MedicineRecognitionResultDto
            {
                RecordId = record.Id,
                Status = recognitionStatus,
                RecognizedInfo = recognizedInfo,
                MatchedMedicines = matchedMedicines,
                RawOcrText = ocrResult.RawText,
                ImageUrl = imageUrl,
                ErrorMessage = ocrResult.ErrorMessage
            };

            if (recognitionStatus == OcrRecognitionStatus.Failed)
            {
                return ApiResponse<MedicineRecognitionResultDto>.Error(
                    "识别失败，请手动输入药品信息", 422);
            }

            return ApiResponse<MedicineRecognitionResultDto>.Success(result,
                recognitionStatus == OcrRecognitionStatus.Success ? "识别成功" : "部分识别成功，请核对后手动补充");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "药品图片识别失败");
            return ApiResponse<MedicineRecognitionResultDto>.Error("识别失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MatchedMedicineDto>> MatchMedicineAsync(
        string medicineName, string? specification, int? householdId, int userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(medicineName))
            {
                return ApiResponse<MatchedMedicineDto>.Error("药品名称不能为空", 400);
            }

            var matches = await MatchMedicineInternalAsync(medicineName, specification, householdId, userId);

            if (matches == null || !matches.Any())
            {
                return ApiResponse<MatchedMedicineDto>.Error("未找到匹配的药品", 404);
            }

            var bestMatch = matches.First();
            return ApiResponse<MatchedMedicineDto>.Success(bestMatch, "匹配成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "药品匹配失败");
            return ApiResponse<MatchedMedicineDto>.Error("匹配失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedicineRecognitionRecordDto>> GetRecognitionRecordAsync(int recordId, int userId)
    {
        try
        {
            var record = await _unitOfWork.MedicineRecognitionRecords.GetByIdAsync(recordId);
            if (record == null)
            {
                return ApiResponse<MedicineRecognitionRecordDto>.Error("识别记录不存在", 404);
            }

            if (record.UserId != userId)
            {
                if (record.HouseholdId.HasValue)
                {
                    var hasAccess = await HasHouseholdAccessAsync(record.HouseholdId.Value, userId);
                    if (!hasAccess)
                    {
                        return ApiResponse<MedicineRecognitionRecordDto>.Error("无权限查看此记录", 403);
                    }
                }
                else
                {
                    return ApiResponse<MedicineRecognitionRecordDto>.Error("无权限查看此记录", 403);
                }
            }

            var dto = record.Adapt<MedicineRecognitionRecordDto>();
            return ApiResponse<MedicineRecognitionRecordDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取识别记录失败");
            return ApiResponse<MedicineRecognitionRecordDto>.Error("获取失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedicineDto>> ConfirmAndSaveAsync(
        ConfirmRecognitionRequestDto request, int userId)
    {
        try
        {
            var record = await _unitOfWork.MedicineRecognitionRecords.GetByIdAsync(request.RecordId);
            if (record == null)
            {
                return ApiResponse<MedicineDto>.Error("识别记录不存在", 404);
            }

            if (record.UserId != userId)
            {
                return ApiResponse<MedicineDto>.Error("无权限操作此记录", 403);
            }

            if (record.ConfirmStatus != RecognitionConfirmStatus.Pending)
            {
                return ApiResponse<MedicineDto>.Error("该记录已处理，无法重复保存", 400);
            }

            var hasPermission = await HasPermissionAsync(request.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse<MedicineDto>.Error("无权限添加药品", 403);
            }

            var createRequest = new CreateMedicineRequestDto
            {
                HouseholdId = request.HouseholdId,
                Name = request.Name,
                Category = request.Category,
                Indication = request.Indication,
                Dosage = request.Dosage,
                ExpiryDate = request.ExpiryDate,
                StockQuantity = request.StockQuantity,
                StorageLocation = request.StorageLocation,
                Contraindications = request.Contraindications,
                PhotoUrl = request.PhotoUrl ?? record.ImageUrl
            };

            var result = await _medicineService.CreateMedicineAsync(createRequest, userId);

            if (result.Code != 200 || result.Data == null)
            {
                return ApiResponse<MedicineDto>.Error(result.Message, result.Code);
            }

            record.ConfirmStatus = request.IsModified
                ? RecognitionConfirmStatus.Modified
                : RecognitionConfirmStatus.Confirmed;
            record.CorrectedName = request.Name;
            record.CorrectedSpecification = null;
            record.CorrectedExpiryDate = request.ExpiryDate.ToString("yyyy-MM-dd");
            record.CorrectedDosage = request.Dosage;
            record.CorrectedManufacturer = null;
            record.FinalMedicineId = result.Data.Id;
            record.ConfirmedAt = DateTime.UtcNow;

            await _unitOfWork.MedicineRecognitionRecords.UpdateAsync(record);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"用户{userId}确认并保存了识别记录{record.Id}，药品ID: {result.Data.Id}");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "确认并保存药品失败");
            return ApiResponse<MedicineDto>.Error("保存失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<PagedResult<MedicineRecognitionRecordDto>>> GetRecognitionRecordsAsync(
        RecognitionRecordQueryParamsDto queryParams, int userId)
    {
        try
        {
            var userMembers = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == userId);
            var householdIds = userMembers.Select(hm => hm.HouseholdId).ToList();

            if (queryParams.HouseholdId.HasValue && !householdIds.Contains(queryParams.HouseholdId.Value))
            {
                return ApiResponse<PagedResult<MedicineRecognitionRecordDto>>.Error("无权限访问此家庭", 403);
            }

            var householdIdFilter = queryParams.HouseholdId;
            var recognitionStatusFilter = queryParams.RecognitionStatus;
            var confirmStatusFilter = queryParams.ConfirmStatus;
            var startDate = queryParams.StartDate;
            var endDate = queryParams.EndDate;

            var (items, totalCount) = await _unitOfWork.MedicineRecognitionRecords.GetPagedAsync(
                queryParams.PageIndex,
                queryParams.PageSize,
                r => (r.UserId == userId || householdIds.Contains(r.HouseholdId ?? 0)) &&
                     (!householdIdFilter.HasValue || r.HouseholdId == householdIdFilter.Value) &&
                     (!recognitionStatusFilter.HasValue || r.RecognitionStatus == recognitionStatusFilter.Value) &&
                     (!confirmStatusFilter.HasValue || r.ConfirmStatus == confirmStatusFilter.Value) &&
                     (!startDate.HasValue || r.CreatedAt >= startDate.Value) &&
                     (!endDate.HasValue || r.CreatedAt <= endDate.Value),
                queryParams.SortBy ?? "CreatedAt",
                queryParams.SortDescending);

            var dtos = items.Adapt<List<MedicineRecognitionRecordDto>>();

            var result = new PagedResult<MedicineRecognitionRecordDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageIndex = queryParams.PageIndex,
                PageSize = queryParams.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize),
                HasPreviousPage = queryParams.PageIndex > 1,
                HasNextPage = queryParams.PageIndex * queryParams.PageSize < totalCount
            };

            return ApiResponse<PagedResult<MedicineRecognitionRecordDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取识别记录列表失败");
            return ApiResponse<PagedResult<MedicineRecognitionRecordDto>>.Error("获取失败: " + ex.Message, 500);
        }
    }

    private RecognizedMedicineInfoDto ParseMedicineInfo(OcrResult ocrResult)
    {
        var info = new RecognizedMedicineInfoDto
        {
            ConfidenceScore = ocrResult.Confidence
        };

        if (!ocrResult.Success || string.IsNullOrWhiteSpace(ocrResult.RawText))
        {
            return info;
        }

        var text = ocrResult.RawText;
        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();

        info.Name = ExtractMedicineName(lines);
        info.Specification = ExtractSpecification(lines);
        info.ExpiryDate = ExtractExpiryDate(text, lines);
        info.Dosage = ExtractDosage(lines);
        info.Manufacturer = ExtractManufacturer(lines);

        var fieldCount = 0;
        var totalFields = 5;
        if (!string.IsNullOrWhiteSpace(info.Name)) fieldCount++;
        if (!string.IsNullOrWhiteSpace(info.Specification)) fieldCount++;
        if (!string.IsNullOrWhiteSpace(info.ExpiryDate)) fieldCount++;
        if (!string.IsNullOrWhiteSpace(info.Dosage)) fieldCount++;
        if (!string.IsNullOrWhiteSpace(info.Manufacturer)) fieldCount++;

        info.ConfidenceScore = ocrResult.Confidence * (fieldCount / (double)totalFields);

        return info;
    }

    private static string? ExtractMedicineName(List<string> lines)
    {
        var namePatterns = new[]
        {
            @"^【药品名称】\s*(.+)",
            @"^药品名称[：:]\s*(.+)",
            @"^通用名称[：:]\s*(.+)",
            @"^通用名[：:]\s*(.+)",
            @"^商品名称[：:]\s*(.+)",
            @"^商品名[：:]\s*(.+)",
        };

        foreach (var pattern in namePatterns)
        {
            foreach (var line in lines)
            {
                var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var name = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(name) && name.Length <= 200)
                    {
                        return name;
                    }
                }
            }
        }

        if (lines.Count > 0)
        {
            var firstLine = lines[0].Trim();
            if (firstLine.Length >= 2 && firstLine.Length <= 50 &&
                !Regex.IsMatch(firstLine, @"^[\d\s\.\,\-\(\)\[\]【】]+$"))
            {
                return firstLine;
            }
        }

        foreach (var line in lines)
        {
            if (line.Length >= 2 && line.Length <= 30 &&
                Regex.IsMatch(line, @"^[\u4e00-\u9fa5A-Za-z]+$"))
            {
                return line;
            }
        }

        return null;
    }

    private static string? ExtractSpecification(List<string> lines)
    {
        var specPatterns = new[]
        {
            @"^【规格】\s*(.+)",
            @"^规格[：:]\s*(.+)",
            @"^包装规格[：:]\s*(.+)",
            @"^剂型规格[：:]\s*(.+)",
        };

        foreach (var pattern in specPatterns)
        {
            foreach (var line in lines)
            {
                var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var spec = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(spec) && spec.Length <= 200)
                    {
                        return spec;
                    }
                }
            }
        }

        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, @"\d+(\.\d+)?\s*(mg|g|ml|μg|IU|粒|片|袋|支|瓶|盒)"))
            {
                var match = Regex.Match(line, @"\d+(\.\d+)?\s*(mg|g|ml|μg|IU|粒|片|袋|支|瓶|盒)[\s\S]*");
                if (match.Success && match.Value.Length <= 200)
                {
                    return match.Value.Trim();
                }
            }
        }

        return null;
    }

    private static string? ExtractExpiryDate(string fullText, List<string> lines)
    {
        var datePatterns = new[]
        {
            @"有效期至?[：:\s]*(\d{4}[\-\/年]\d{1,2}[\-\/月]?(\d{1,2}日?)?)",
            @"失效日期?[：:\s]*(\d{4}[\-\/年]\d{1,2}[\-\/月]?(\d{1,2}日?)?)",
            @"过期日期?[：:\s]*(\d{4}[\-\/年]\d{1,2}[\-\/月]?(\d{1,2}日?)?)",
            @"有效期[：:\s]*(\d{4}[\-\/年]\d{1,2}[\-\/月]?(\d{1,2}日?)?)",
            @"EXP[：:\s]*(\d{4}[\-\/]\d{1,2}[\-\/]?\d{0,2})",
        };

        foreach (var pattern in datePatterns)
        {
            var match = Regex.Match(fullText, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var dateStr = match.Groups[1].Value.Trim();
                if (TryNormalizeDate(dateStr, out var normalized))
                {
                    return normalized;
                }
            }
        }

        var dateMatches = Regex.Matches(fullText, @"\b(\d{4})[\-\/年](\d{1,2})[\-\/月](\d{1,2})日?\b");
        if (dateMatches.Count > 0)
        {
            var dates = new List<DateTime>();
            foreach (Match m in dateMatches)
            {
                if (DateTime.TryParse($"{m.Groups[1].Value}-{m.Groups[2].Value}-{m.Groups[3].Value}", out var dt))
                {
                    dates.Add(dt);
                }
            }
            if (dates.Any())
            {
                var latest = dates.Max();
                if (latest > DateTime.Now)
                {
                    return latest.ToString("yyyy-MM-dd");
                }
            }
        }

        return null;
    }

    private static bool TryNormalizeDate(string dateStr, out string normalized)
    {
        normalized = string.Empty;
        try
        {
            var cleaned = dateStr.Replace("年", "-").Replace("月", "-").Replace("日", "")
                                 .Replace("/", "-").Trim();

            var parts = cleaned.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && parts.Length <= 3)
            {
                var year = int.Parse(parts[0]);
                var month = int.Parse(parts[1]);
                var day = parts.Length > 2 ? int.Parse(parts[2]) : 1;

                if (year >= 2000 && year <= 2100 && month >= 1 && month <= 12 && day >= 1 && day <= 31)
                {
                    var dt = new DateTime(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
                    normalized = dt.ToString("yyyy-MM-dd");
                    return true;
                }
            }
        }
        catch
        {
        }
        return false;
    }

    private static string? ExtractDosage(List<string> lines)
    {
        var dosagePatterns = new[]
        {
            @"^【用法用量】\s*(.+)",
            @"^用法用量[：:]\s*(.+)",
            @"^用法与用量[：:]\s*(.+)",
            @"^口服[：:]\s*(.+)",
        };

        var dosageLines = new List<string>();
        var capture = false;

        foreach (var line in lines)
        {
            foreach (var pattern in dosagePatterns)
            {
                if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
                {
                    capture = true;
                    var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups[1].Length > 0)
                    {
                        dosageLines.Add(match.Groups[1].Value.Trim());
                    }
                    break;
                }
            }

            if (capture && !dosagePatterns.Any(p => Regex.IsMatch(line, p, RegexOptions.IgnoreCase)))
            {
                if (Regex.IsMatch(line, @"^【.*】") || Regex.IsMatch(line, @"^[^\s：:]+[：:]") &&
                    !Regex.IsMatch(line, @"(一次|一日|每天|每次|口服)"))
                {
                    break;
                }
                dosageLines.Add(line.Trim());
            }

            if (dosageLines.Count > 5) break;
        }

        if (dosageLines.Any())
        {
            var dosage = string.Join(" ", dosageLines.Where(l => !string.IsNullOrWhiteSpace(l)));
            if (dosage.Length <= 200)
            {
                return dosage;
            }
            return dosage.Substring(0, 200);
        }

        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, @"(一次|每日|每天|每次|一日|口服)\s*\d+") && line.Length <= 200)
            {
                return line.Trim();
            }
        }

        return null;
    }

    private static string? ExtractManufacturer(List<string> lines)
    {
        var mfrPatterns = new[]
        {
            @"^【生产企业】\s*(.+)",
            @"^生产企业[：:]\s*(.+)",
            @"^生产厂家[：:]\s*(.+)",
            @"^制造厂家[：:]\s*(.+)",
            @"^制造商[：:]\s*(.+)",
            @"^厂商[：:]\s*(.+)",
        };

        foreach (var pattern in mfrPatterns)
        {
            foreach (var line in lines)
            {
                var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var mfr = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(mfr) && mfr.Length <= 200)
                    {
                        return mfr;
                    }
                }
            }
        }

        foreach (var line in lines)
        {
            if ((line.Contains("药业") || line.Contains("制药") || line.Contains("医药") ||
                 line.Contains("生物") || line.Contains("有限公司")) && line.Length <= 200)
            {
                return line.Trim();
            }
        }

        return null;
    }

    private static (OcrRecognitionStatus Status, List<string> MissingFields) EvaluateRecognitionStatus(
        RecognizedMedicineInfoDto info)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(info.Name)) missing.Add("Name");
        if (string.IsNullOrWhiteSpace(info.Specification)) missing.Add("Specification");
        if (string.IsNullOrWhiteSpace(info.ExpiryDate)) missing.Add("ExpiryDate");
        if (string.IsNullOrWhiteSpace(info.Dosage)) missing.Add("Dosage");
        if (string.IsNullOrWhiteSpace(info.Manufacturer)) missing.Add("Manufacturer");

        if (missing.Count == 5)
        {
            return (OcrRecognitionStatus.Failed, missing);
        }
        if (missing.Count == 0)
        {
            return (OcrRecognitionStatus.Success, missing);
        }
        return (OcrRecognitionStatus.PartialSuccess, missing);
    }

    private async Task<List<MatchedMedicineDto>?> MatchMedicineInternalAsync(
        string medicineName, string? specification, int? householdId, int userId)
    {
        try
        {
            var userMembers = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == userId);
            var householdIds = userMembers.Select(hm => hm.HouseholdId).ToList();

            if (!householdIds.Any())
            {
                return new List<MatchedMedicineDto>();
            }

            if (householdId.HasValue && !householdIds.Contains(householdId.Value))
            {
                return null;
            }

            var targetHouseholdIds = householdId.HasValue
                ? new List<int> { householdId.Value }
                : householdIds.ToList();

            var allMedicines = await _unitOfWork.Medicines
                .FindAsync(m => targetHouseholdIds.Contains(m.HouseholdId));

            var matches = new List<(Medicine Medicine, double Score)>();
            var nameLower = medicineName.ToLower().Trim();

            foreach (var med in allMedicines)
            {
                var medNameLower = med.Name.ToLower().Trim();
                double score = 0;

                if (medNameLower == nameLower)
                {
                    score = 100;
                }
                else if (medNameLower.Contains(nameLower) || nameLower.Contains(medNameLower))
                {
                    var longer = Math.Max(medNameLower.Length, nameLower.Length);
                    var shorter = Math.Min(medNameLower.Length, nameLower.Length);
                    score = 60 + (40 * (shorter / (double)longer));
                }
                else
                {
                    var similarity = CalculateSimilarity(medNameLower, nameLower);
                    score = similarity * 50;
                }

                if (!string.IsNullOrWhiteSpace(specification) && score > 0)
                {
                    var specLower = specification.ToLower().Trim();
                    var medDosageLower = med.Dosage.ToLower().Trim();
                    if (medDosageLower.Contains(specLower) || specLower.Contains(medDosageLower))
                    {
                        score = Math.Min(100, score + 10);
                    }
                }

                if (score >= 30)
                {
                    matches.Add((Medicine: med, Score: score));
                }
            }

            var result = matches
                .OrderByDescending(m => m.Score)
                .Take(5)
                .Select(m => new MatchedMedicineDto
                {
                    MedicineId = m.Medicine.Id,
                    Name = m.Medicine.Name,
                    Specification = m.Medicine.Dosage,
                    Category = m.Medicine.Category,
                    MatchScore = Math.Round(m.Score, 2),
                    IsExactMatch = m.Score >= 95
                })
                .ToList();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "药品匹配内部错误");
            return null;
        }
    }

    private static double CalculateSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;

        int distance = LevenshteinDistance(s1, s2);
        int maxLength = Math.Max(s1.Length, s2.Length);

        if (maxLength == 0)
            return 1.0;

        return 1.0 - (distance / (double)maxLength);
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
        if (string.IsNullOrEmpty(s2)) return s1.Length;

        int[,] matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    private async Task<string> SaveImageAsync(Stream imageStream, string fileName)
    {
        var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ocr");
        Directory.CreateDirectory(uploadFolder);

        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        var newFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(uploadFolder, newFileName);

        imageStream.Position = 0;
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await imageStream.CopyToAsync(stream);
        }

        return $"/uploads/ocr/{newFileName}";
    }

    private async Task<bool> HasHouseholdAccessAsync(int householdId, int userId)
    {
        var members = await _unitOfWork.HouseholdMembers
            .FindAsync(hm => hm.HouseholdId == householdId && hm.UserId == userId);
        return members.Any();
    }

    private async Task<bool> HasPermissionAsync(int householdId, int userId, params string[] allowedRoles)
    {
        var members = await _unitOfWork.HouseholdMembers
            .FindAsync(hm => hm.HouseholdId == householdId && hm.UserId == userId);
        var member = members.FirstOrDefault();

        if (member == null)
        {
            return false;
        }

        return allowedRoles.Contains(member.Role);
    }
}
