namespace wpf.Models;

public class HistoryEntry
{
    public int Id { get; set; }
    public string ImageBase64 { get; set; } = string.Empty;
    public string ImageExtension { get; set; } = "jpeg";
    public string ResultText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string DisplayLabel => $"[{CreatedAt:dd/MM/yyyy HH:mm}] {ResultText[..Math.Min(40, ResultText.Length)]}...";
}
