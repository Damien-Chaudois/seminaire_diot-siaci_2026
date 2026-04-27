using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using BLL;
using DAL.Models;
using System.Text;

namespace wpf.ViewModels;

public class PersonalityOption : ViewModelBase
{
    private bool _isSelected;

    public string Name { get; }
    public string PromptInstruction { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                SelectionChanged?.Invoke();
            }
        }
    }

    public Action? SelectionChanged { get; set; }

    public PersonalityOption(string name, string promptInstruction)
    {
        Name = name;
        PromptInstruction = promptInstruction;
    }
}

public class MainViewModel : ViewModelBase
{
    private readonly IImageService _imageService;
    private readonly ILlmService _llmService;
    private readonly IHistoryService _historyService;

    // ── Image ────────────────────────────────────────────────────────────────
    private string _currentBase64 = string.Empty;
    private string _currentExtension = "jpeg";
    private BitmapImage? _previewImage;
    public BitmapImage? PreviewImage { get => _previewImage; set => SetProperty(ref _previewImage, value); }

    // ── Personalities ─────────────────────────────────────────────────────────
    public ObservableCollection<PersonalityOption> Personalities { get; } = [];

    private string _selectedPersonalitiesSummary = "Aucune personnalité sélectionnée";
    public string SelectedPersonalitiesSummary
    {
        get => _selectedPersonalitiesSummary;
        set => SetProperty(ref _selectedPersonalitiesSummary, value);
    }

    // ── Result ────────────────────────────────────────────────────────────────
    private string _resultText = string.Empty;
    public string ResultText { get => _resultText; set => SetProperty(ref _resultText, value); }

    // ── History (liste de boutons) ────────────────────────────────────────────
    public ObservableCollection<HistoryEntry> HistoryEntries { get; } = [];

    // ── Status ───────────────────────────────────────────────────────────────
    private string _statusMessage = "Prêt.";
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    private bool _hasImage;
    public bool HasImage { get => _hasImage; set => SetProperty(ref _hasImage, value); }

    // ── Commands ─────────────────────────────────────────────────────────────
    public ICommand SelectImageCommand { get; }
    public ICommand SendToApiCommand { get; }
    public ICommand RestoreHistoryCommand { get; }
    public ICommand DeleteHistoryCommand { get; }

    public MainViewModel(IImageService imageService, ILlmService llmService, IHistoryService historyService)
    {
        _imageService = imageService;
        _llmService = llmService;
        _historyService = historyService;

        SelectImageCommand = new RelayCommand(_ => SelectImage());
        SendToApiCommand = new RelayCommandAsync(_ => SendToApiAsync(), _ => HasImage && !IsLoading);
        RestoreHistoryCommand = new RelayCommand(entry => RestoreHistory(entry as HistoryEntry));
        DeleteHistoryCommand = new RelayCommand(entry => DeleteHistory(entry as HistoryEntry));

        InitializePersonalities();
        LoadHistory();
    }

    private void InitializePersonalities()
    {
        var defaults = new[]
        {
            new PersonalityOption("UX Coach", "Analyse l'intuitivite, la clarte des parcours et la comprehension immediate de l'interface."),
            new PersonalityOption("Art Director", "Analyse la beaute visuelle, la coherence graphique, la hierarchie visuelle et l'impact esthetique."),
            new PersonalityOption("Product Designer", "Analyse l'elegance des interactions, la structure des informations et la qualite des composants."),
            new PersonalityOption("Power User", "Analyse la praticite, la vitesse d'execution des taches et l'efficacite operationnelle."),
            new PersonalityOption("Accessibility Expert", "Analyse l'accessibilite : contrastes, lisibilite, taille des zones cliquables et inclusion."),
            new PersonalityOption("Conversion Specialist", "Analyse la capacite de l'interface a guider vers les actions principales et a reduire les frictions.")
        };

        Personalities.Clear();
        foreach (var item in defaults)
        {
            item.SelectionChanged = UpdateSelectedPersonalitiesSummary;
            Personalities.Add(item);
        }

        UpdateSelectedPersonalitiesSummary();
    }

    private void UpdateSelectedPersonalitiesSummary()
    {
        var selected = Personalities.Where(p => p.IsSelected).Select(p => p.Name).ToArray();
        SelectedPersonalitiesSummary = selected.Length == 0
            ? "Aucune personnalité sélectionnée"
            : string.Join(" | ", selected);
    }

    private void SelectImage()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Sélectionner une image",
            Filter = "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            (_currentBase64, _currentExtension) = _imageService.LoadImage(dialog.FileName);
            PreviewImage = LoadBitmapFromFile(dialog.FileName);
            HasImage = true;
            StatusMessage = $"Image chargée : {System.IO.Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur : {ex.Message}";
            MessageBox.Show(ex.Message, "Erreur de chargement", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task SendToApiAsync()
    {
        var selectedPersonalities = Personalities.Where(p => p.IsSelected).ToList();
        if (selectedPersonalities.Count == 0)
        {
            MessageBox.Show("Selectionnez au moins une personnalite.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsLoading = true;
        StatusMessage = "Envoi à l'API en cours...";
        ResultText = string.Empty;

        try
        {
            var builder = new StringBuilder();
            foreach (var personality in selectedPersonalities)
            {
                StatusMessage = $"Analyse en cours : {personality.Name}...";

                var personalityPrompt =
                    $"Tu dois adopter strictement la personnalite suivante : {personality.Name}. " +
                    $"{personality.PromptInstruction} " +
                    "Analyse uniquement l'interface visible sur l'image. " +
                    "Fais une critique concrete et actionnable en sections : Points forts, Points faibles, Recommandations prioritaires.";

                var critique = await _llmService.AnalyzeImageAsync(_currentBase64, _currentExtension, personalityPrompt);

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine();
                }

                builder.AppendLine($"=== {personality.Name} ===");
                builder.AppendLine(critique.Trim());
            }

            var combinedResult = builder.ToString().Trim();
            ResultText = combinedResult;

            // Sauvegarder dans l'historique
            var entry = new HistoryEntry
            {
                ImageBase64 = _currentBase64,
                ImageExtension = _currentExtension,
                SelectedPersonalitiesCsv = string.Join(";", selectedPersonalities.Select(p => p.Name)),
                ResultText = combinedResult,
                CreatedAt = DateTime.Now
            };
            _historyService.SaveEntry(entry);
            HistoryEntries.Insert(0, entry);

            StatusMessage = "Analyse terminée.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur : {ex.Message}";
            MessageBox.Show(ex.Message, "Erreur API", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RestoreHistory(HistoryEntry? entry)
    {
        if (entry is null) return;

        _currentBase64 = entry.ImageBase64;
        _currentExtension = entry.ImageExtension;
        PreviewImage = LoadBitmapFromBase64(entry.ImageBase64);
        HasImage = true;

        ResultText = entry.ResultText;

        var selectedSet = entry.SelectedPersonalitiesCsv
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var personality in Personalities)
        {
            personality.IsSelected = selectedSet.Contains(personality.Name);
        }

        UpdateSelectedPersonalitiesSummary();

        StatusMessage = $"Historique restauré : {entry.CreatedAt:dd/MM/yyyy HH:mm}";
    }

    private void DeleteHistory(HistoryEntry? entry)
    {
        if (entry is null) return;

        var confirm = MessageBox.Show(
            $"Supprimer cette entrée du {entry.CreatedAt:dd/MM/yyyy HH:mm} ?",
            "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        _historyService.DeleteEntry(entry.Id);
        HistoryEntries.Remove(entry);
        StatusMessage = "Entrée supprimée.";
    }

    private void LoadHistory()
    {
        HistoryEntries.Clear();
        foreach (var entry in _historyService.GetHistory())
            HistoryEntries.Add(entry);
    }

    private static BitmapImage LoadBitmapFromFile(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapImage LoadBitmapFromBase64(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var bitmap = new BitmapImage();
        using var stream = new System.IO.MemoryStream(bytes);
        bitmap.BeginInit();
        bitmap.StreamSource = stream;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
