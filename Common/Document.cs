namespace PdfHandler.Common;

public class Document
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? Content { get; set; }
    public ProcessingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}