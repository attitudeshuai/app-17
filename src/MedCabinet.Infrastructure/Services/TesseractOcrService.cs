using MedCabinet.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tesseract;

namespace MedCabinet.Infrastructure.Services;

public class TesseractOcrService : IOcrService
{
    private readonly ILogger<TesseractOcrService> _logger;
    private readonly string _tessDataPath;
    private readonly string _language;
    private readonly bool _isAvailable;

    public TesseractOcrService(
        ILogger<TesseractOcrService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _tessDataPath = configuration["Ocr:TessDataPath"] ?? Path.Combine(AppContext.BaseDirectory, "tessdata");
        _language = configuration["Ocr:Language"] ?? "chi_sim+eng";

        _isAvailable = CheckTessDataAvailability();
        if (!_isAvailable)
        {
            _logger.LogWarning($"Tesseract OCR 语言包未找到，路径: {_tessDataPath}。请将 chi_sim.traineddata 和 eng.traineddata 放置到该目录。");
        }
    }

    public async Task<OcrResult> RecognizeAsync(Stream imageStream, string fileName)
    {
        try
        {
            _logger.LogInformation($"Tesseract OCR 开始识别图片: {fileName}");

            if (!_isAvailable)
            {
                return new OcrResult
                {
                    Success = false,
                    RawText = string.Empty,
                    Confidence = 0,
                    ErrorMessage = "OCR 语言包未配置，请联系管理员",
                    TextLines = new List<OcrTextLine>()
                };
            }

            return await Task.Run(() =>
            {
                try
                {
                    imageStream.Position = 0;
                    using var ms = new MemoryStream();
                    imageStream.CopyTo(ms);
                    var imageBytes = ms.ToArray();

                    using var pix = Pix.LoadFromMemory(imageBytes);
                    using var engine = new TesseractEngine(_tessDataPath, _language, EngineMode.Default);
                    engine.SetVariable("preserve_interword_spaces", "1");
                    engine.SetVariable("chop_enable", "1");

                    using var page = engine.Process(pix);

                    var text = page.GetText();
                    var confidence = page.GetMeanConfidence() / 100.0;

                    var textLines = ExtractTextLines(page);

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        return new OcrResult
                        {
                            Success = false,
                            RawText = string.Empty,
                            Confidence = 0,
                            ErrorMessage = "未能识别到有效文字，请确保图片清晰且包含药品说明书内容",
                            TextLines = new List<OcrTextLine>()
                        };
                    }

                    var result = new OcrResult
                    {
                        Success = true,
                        RawText = text,
                        Confidence = Math.Round(confidence, 3),
                        TextLines = textLines
                    };

                    _logger.LogInformation($"Tesseract OCR 识别完成，置信度: {result.Confidence}, 行数: {textLines.Count}");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Tesseract OCR 内部处理失败");
                    return new OcrResult
                    {
                        Success = false,
                        RawText = string.Empty,
                        Confidence = 0,
                        ErrorMessage = $"OCR处理失败: {ex.Message}",
                        TextLines = new List<OcrTextLine>()
                    };
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Tesseract OCR 识别失败: {fileName}");
            return new OcrResult
            {
                Success = false,
                RawText = string.Empty,
                Confidence = 0,
                ErrorMessage = $"OCR识别失败: {ex.Message}",
                TextLines = new List<OcrTextLine>()
            };
        }
    }

    private bool CheckTessDataAvailability()
    {
        try
        {
            if (!Directory.Exists(_tessDataPath))
            {
                return false;
            }

            var languages = _language.Split('+');
            foreach (var lang in languages)
            {
                var dataFile = Path.Combine(_tessDataPath, $"{lang.Trim()}.traineddata");
                if (!File.Exists(dataFile))
                {
                    _logger.LogWarning($"缺少语言包: {dataFile}");
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static List<OcrTextLine> ExtractTextLines(Page page)
    {
        var lines = new List<OcrTextLine>();

        try
        {
            using var iter = page.GetIterator();
            iter.Begin();

            do
            {
                if (iter.IsAtBeginningOf(PageIteratorLevel.TextLine))
                {
                    var text = iter.GetText(PageIteratorLevel.TextLine);
                    var confidence = iter.GetConfidence(PageIteratorLevel.TextLine) / 100.0;

                    if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out var rect))
                    {
                        lines.Add(new OcrTextLine
                        {
                            Text = text?.Trim() ?? string.Empty,
                            Confidence = Math.Round(confidence, 3),
                            Left = rect.X1,
                            Top = rect.Y1,
                            Width = rect.Width,
                            Height = rect.Height
                        });
                    }
                    else
                    {
                        lines.Add(new OcrTextLine
                        {
                            Text = text?.Trim() ?? string.Empty,
                            Confidence = Math.Round(confidence, 3)
                        });
                    }
                }
            } while (iter.Next(PageIteratorLevel.TextLine));
        }
        catch
        {
            var text = page.GetText();
            var textArr = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int top = 0;
            foreach (var line in textArr)
            {
                lines.Add(new OcrTextLine
                {
                    Text = line.Trim(),
                    Confidence = page.GetMeanConfidence() / 100.0,
                    Top = top,
                    Height = 20
                });
                top += 24;
            }
        }

        return lines.Where(l => !string.IsNullOrWhiteSpace(l.Text)).ToList();
    }
}
