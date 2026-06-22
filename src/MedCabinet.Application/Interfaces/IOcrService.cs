namespace MedCabinet.Application.Interfaces;

public interface IOcrService
{
    Task<OcrResult> RecognizeAsync(Stream imageStream, string fileName);
}

public class OcrResult
{
    public bool Success { get; set; }
    public string RawText { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string? ErrorMessage { get; set; }
    public List<OcrTextLine> TextLines { get; set; } = new();
}

public class OcrTextLine
{
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
