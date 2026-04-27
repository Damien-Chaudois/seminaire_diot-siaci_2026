using System.Globalization;

namespace DAL.Models;

public class HistoryRatingItem
{
    public string PersonalityName { get; init; } = string.Empty;
    public double Rating { get; init; }
    public IEnumerable<bool> StarStates => Enumerable.Range(1, 5).Select(index => index <= Math.Clamp((int)Math.Floor(Rating), 0, 5));
}

public class HistoryEntry
{
    public int Id { get; set; }
    public string ImageBase64 { get; set; } = string.Empty;
    public string ImageExtension { get; set; } = "jpeg";
    public string SelectedPersonalitiesCsv { get; set; } = string.Empty;
    public string RatingsCsv { get; set; } = string.Empty;
    public string ResultText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string DisplayLabel => $"[{CreatedAt:dd/MM/yyyy HH:mm}] {ResultText[..Math.Min(40, ResultText.Length)]}...";
    public IEnumerable<HistoryRatingItem> Ratings => ParseRatings(RatingsCsv);

    private static IEnumerable<HistoryRatingItem> ParseRatings(string ratingsCsv)
    {
        if (string.IsNullOrWhiteSpace(ratingsCsv))
            yield break;

        var entries = ratingsCsv.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var entry in entries)
        {
            var separatorIndex = entry.LastIndexOf(':');
            if (separatorIndex <= 0 || separatorIndex >= entry.Length - 1)
                continue;

            var personalityName = entry[..separatorIndex].Trim();
            var ratingPart = entry[(separatorIndex + 1)..].Trim();
            var slashIndex = ratingPart.IndexOf('/');
            if (slashIndex >= 0)
                ratingPart = ratingPart[..slashIndex];

            ratingPart = ratingPart.Replace(',', '.');
            if (!double.TryParse(ratingPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var rating))
                rating = 0;

            yield return new HistoryRatingItem
            {
                PersonalityName = personalityName,
                Rating = Math.Clamp(rating, 0, 5)
            };
        }
    }
}
