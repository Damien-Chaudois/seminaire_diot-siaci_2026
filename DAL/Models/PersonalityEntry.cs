namespace DAL.Models;

public class PersonalityEntry
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FinalPersonality { get; set; } = string.Empty;
    public int Curiosity { get; set; }
    public int Competence { get; set; }
    public int Practicality { get; set; }
    public int AestheticSensitivity { get; set; }
    public int Rigor { get; set; }
    public bool LimitedVisionFlag { get; set; }
    public bool ElderlyFlag { get; set; }
    public bool LowMobilityFlag { get; set; }
    public bool LowDigitalLiteracyFlag { get; set; }
    public string AvatarPngBase64 { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}