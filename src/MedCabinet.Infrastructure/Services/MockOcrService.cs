using MedCabinet.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedCabinet.Infrastructure.Services;

public class MockOcrService : IOcrService
{
    private readonly ILogger<MockOcrService> _logger;
    private static readonly Random _random = new();

    private static readonly string[] SampleNames = new[]
    {
        "阿莫西林胶囊",
        "布洛芬缓释胶囊",
        "感冒灵颗粒",
        "维生素C片",
        "复方甘草片",
        "头孢克肟分散片",
        "蒙脱石散",
        "奥美拉唑肠溶胶囊",
        "氯雷他定片",
        "人工牛黄甲硝唑胶囊"
    };

    private static readonly string[] SampleSpecifications = new[]
    {
        "0.25g*24粒/盒",
        "0.3g*20粒/盒",
        "10g*9袋/盒",
        "100mg*100片/瓶",
        "0.2g*12片/盒",
        "0.1g*6片/盒",
        "3g*10袋/盒",
        "20mg*14粒/盒",
        "10mg*6片/盒",
        "0.4g*12粒/盒"
    };

    private static readonly string[] SampleDosages = new[]
    {
        "口服，一次0.5g，每6～8小时1次，一日剂量不超过4g。",
        "口服，成人一次1粒，一日2次。",
        "开水冲服，一次1袋，一日3次。",
        "口服，一次1～2片，一日3次。",
        "口服，一次2～3片，一日3次。",
        "口服，一次100～200mg，一日2次。",
        "口服，成人一次1袋，一日3次。",
        "口服，一次20mg，一日1～2次。",
        "口服，成人及12岁以上儿童一次1片，一日1次。",
        "口服，一次2粒，一日3次。"
    };

    private static readonly string[] SampleManufacturers = new[]
    {
        "华北制药股份有限公司",
        "中美天津史克制药有限公司",
        "三九医药股份有限公司",
        "东北制药集团股份有限公司",
        "广州白云山制药股份有限公司",
        "浙江医药股份有限公司",
        "江苏恒瑞医药股份有限公司",
        "山东新华制药股份有限公司",
        "上海罗氏制药有限公司",
        "石药集团欧意药业有限公司"
    };

    public MockOcrService(ILogger<MockOcrService> logger)
    {
        _logger = logger;
    }

    public async Task<OcrResult> RecognizeAsync(Stream imageStream, string fileName)
    {
        try
        {
            _logger.LogInformation($"Mock OCR 处理图片: {fileName}");

            await Task.Delay(_random.Next(100, 300));

            var failureChance = _random.Next(0, 100);
            if (failureChance < 5)
            {
                return new OcrResult
                {
                    Success = false,
                    RawText = string.Empty,
                    Confidence = 0,
                    ErrorMessage = "图片质量不佳，无法识别",
                    TextLines = new List<OcrTextLine>()
                };
            }

            var nameIndex = _random.Next(SampleNames.Length);
            var specIndex = _random.Next(SampleSpecifications.Length);
            var dosageIndex = _random.Next(SampleDosages.Length);
            var mfrIndex = _random.Next(SampleManufacturers.Length);

            var name = SampleNames[nameIndex];
            var specification = SampleSpecifications[specIndex];
            var dosage = SampleDosages[dosageIndex];
            var manufacturer = SampleManufacturers[mfrIndex];

            var expiryDate = DateTime.Now.AddMonths(_random.Next(6, 24)).ToString("yyyy年MM月dd日");

            var rawText = GenerateMockOcrText(name, specification, dosage, manufacturer, expiryDate);

            var lines = rawText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select((text, i) => new OcrTextLine
                {
                    Text = text.Trim(),
                    Confidence = 0.7 + _random.NextDouble() * 0.29,
                    Left = 50,
                    Top = 50 + i * 30,
                    Width = 400,
                    Height = 24
                }).ToList();

            var overallConfidence = 0.75 + _random.NextDouble() * 0.2;

            var result = new OcrResult
            {
                Success = true,
                RawText = rawText,
                Confidence = Math.Round(overallConfidence, 3),
                TextLines = lines
            };

            _logger.LogInformation($"Mock OCR 识别完成: {name}, 置信度: {result.Confidence}");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mock OCR 识别失败");
            return new OcrResult
            {
                Success = false,
                RawText = string.Empty,
                Confidence = 0,
                ErrorMessage = ex.Message,
                TextLines = new List<OcrTextLine>()
            };
        }
    }

    private static string GenerateMockOcrText(
        string name, string specification, string dosage, string manufacturer, string expiryDate)
    {
        var hasNameLabel = _random.Next(0, 10) > 1;
        var hasSpecLabel = _random.Next(0, 10) > 2;
        var hasDosageLabel = _random.Next(0, 10) > 1;
        var hasMfrLabel = _random.Next(0, 10) > 2;
        var hasExpiryLabel = _random.Next(0, 10) > 1;

        var lines = new List<string>
        {
            hasNameLabel ? $"【药品名称】{name}" : name,
            "",
            hasSpecLabel ? $"【规格】{specification}" : specification,
            "",
            $"【成份】本品主要成份为{name.Substring(0, Math.Min(4, name.Length))}",
            "",
            $"【性状】本品为胶囊剂，内容物为白色或类白色粉末。",
            "",
            hasDosageLabel ? $"【用法用量】{dosage}" : dosage,
            "",
            "【不良反应】可能出现恶心、呕吐、腹泻等胃肠道反应。",
            "",
            "【禁忌】对本品过敏者禁用。",
            "",
            "【注意事项】请仔细阅读说明书并按说明使用或在药师指导下购买和使用。",
            "",
            hasMfrLabel ? $"【生产企业】{manufacturer}" : manufacturer,
            "",
            hasExpiryLabel ? $"有效期至：{expiryDate}" : expiryDate,
        };

        return string.Join("\n", lines.Where(l => l != null));
    }
}
