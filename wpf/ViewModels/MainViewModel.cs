using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using BLL;
using DAL.Models;
using System.Text;

namespace wpf.ViewModels;

public class PersonalityOption : ViewModelBase
{
    private bool _isSelected;
    private string _name;
    private string _promptInstruction;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string PromptInstruction
    {
        get => _promptInstruction;
        set => SetProperty(ref _promptInstruction, value);
    }

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
        _name = name;
        _promptInstruction = promptInstruction;
    }
}

public class PersonalityCritique : ViewModelBase
{
    private bool _isExpanded;

    public string PersonalityName { get; }
    public string CritiqueText { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public PersonalityCritique(string personalityName, string critiqueText)
    {
        PersonalityName = personalityName;
        CritiqueText = critiqueText;
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
    public ObservableCollection<PersonalityCritique> Critiques { get; } = [];

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
    public ICommand EditPersonalityCommand { get; }
    public ICommand AddPersonalityCommand { get; }

    public MainViewModel(IImageService imageService, ILlmService llmService, IHistoryService historyService)
    {
        _imageService = imageService;
        _llmService = llmService;
        _historyService = historyService;

        SelectImageCommand = new RelayCommand(_ => SelectImage());
        SendToApiCommand = new RelayCommandAsync(_ => SendToApiAsync(), _ => HasImage && !IsLoading);
        RestoreHistoryCommand = new RelayCommand(entry => RestoreHistory(entry as HistoryEntry));
        DeleteHistoryCommand = new RelayCommand(entry => DeleteHistory(entry as HistoryEntry));
        EditPersonalityCommand = new RelayCommand(entry => EditPersonality(entry as PersonalityOption));
        AddPersonalityCommand = new RelayCommand(_ => AddPersonality());

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
        Critiques.Clear();

        try
        {
            var critiques = new List<PersonalityCritique>();
            foreach (var personality in selectedPersonalities)
            {
                StatusMessage = $"Analyse en cours : {personality.Name}...";

                var personalityPrompt =
                    $"Tu dois adopter strictement la personnalite suivante : {personality.Name}. " +
                    $"{personality.PromptInstruction} " +
                    "Analyse uniquement l'interface visible sur l'image. " +
                    "Fais une critique concrete et actionnable en sections : Points forts, Points faibles, Recommandations prioritaires.";

                var critique = await _llmService.AnalyzeImageAsync(_currentBase64, _currentExtension, personalityPrompt);

                critiques.Add(new PersonalityCritique(personality.Name, critique.Trim())
                {
                    IsExpanded = false
                });
            }

            foreach (var critique in critiques)
                Critiques.Add(critique);

            var combinedResult = SerializeCritiques(critiques);

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
            HistoryEntries.Add(entry);

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

        Critiques.Clear();
        foreach (var critique in ParseCritiques(entry.ResultText))
            Critiques.Add(critique);

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

    private void AddPersonality()
    {
        var name = PromptForText("Nouvelle personnalite", "Nom de la personnalite :", string.Empty, false);
        if (name is null)
            return;

        var sanitizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            MessageBox.Show("Le nom est obligatoire.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (Personalities.Any(p => string.Equals(p.Name, sanitizedName, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("Une personnalite avec ce nom existe deja.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var prompt = PromptForText("Prompt de personnalite", "Prompt de role :", string.Empty, true);
        if (prompt is null)
            return;

        var item = new PersonalityOption(sanitizedName, prompt.Trim()) { SelectionChanged = UpdateSelectedPersonalitiesSummary };
        Personalities.Add(item);
        item.IsSelected = true;
        StatusMessage = $"Personnalite ajoutee : {item.Name}";
    }

    private void EditPersonality(PersonalityOption? personality)
    {
        if (personality is null)
            return;

        var updatedPrompt = PromptForText(
            $"Editer {personality.Name}",
            "Prompt de role :",
            personality.PromptInstruction,
            true);

        if (updatedPrompt is null)
            return;

        personality.PromptInstruction = updatedPrompt.Trim();
        StatusMessage = $"Prompt mis a jour : {personality.Name}";
    }

    private static string? PromptForText(string title, string label, string initialText, bool multiline)
    {
        var window = new Window
        {
            Title = title,
            Width = multiline ? 640 : 520,
            Height = multiline ? 420 : 220,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(0x1B, 0x1C, 0x24)),
            Foreground = Brushes.White
        };

        if (Application.Current?.MainWindow is not null)
            window.Owner = Application.Current.MainWindow;

        var grid = new Grid { Margin = new Thickness(14) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var textLabel = new TextBlock
        {
            Text = label,
            Margin = new Thickness(0, 0, 0, 8),
            Foreground = new SolidColorBrush(Color.FromRgb(0xB3, 0xBC, 0xD3))
        };
        Grid.SetRow(textLabel, 0);
        grid.Children.Add(textLabel);

        var editor = new TextBox
        {
            Text = initialText,
            AcceptsReturn = multiline,
            TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            VerticalScrollBarVisibility = multiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
            Background = new SolidColorBrush(Color.FromRgb(0x31, 0x38, 0x4B)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x4F, 0x69)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8),
            MinHeight = multiline ? 260 : 34
        };
        Grid.SetRow(editor, 1);
        grid.Children.Add(editor);

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var cancelButton = new Button
        {
            Content = "Annuler",
            MinWidth = 90,
            Margin = new Thickness(0, 0, 8, 0)
        };
        cancelButton.Click += (_, _) => window.DialogResult = false;

        var saveButton = new Button
        {
            Content = "Valider",
            MinWidth = 90
        };
        saveButton.Click += (_, _) => window.DialogResult = true;

        buttonRow.Children.Add(cancelButton);
        buttonRow.Children.Add(saveButton);
        Grid.SetRow(buttonRow, 2);
        grid.Children.Add(buttonRow);

        window.Content = grid;

        var result = window.ShowDialog();
        return result == true ? editor.Text : null;
    }

    private static string SerializeCritiques(IEnumerable<PersonalityCritique> critiques)
    {
        var builder = new StringBuilder();
        foreach (var critique in critiques)
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.AppendLine($"=== {critique.PersonalityName} ===");
            builder.AppendLine(critique.CritiqueText.Trim());
        }

        return builder.ToString().Trim();
    }

    private static IEnumerable<PersonalityCritique> ParseCritiques(string raw)
    {
        var result = new List<PersonalityCritique>();
        var normalized = raw.Replace("\r", string.Empty);
        var lines = normalized.Split('\n');

        string? currentName = null;
        var currentText = new StringBuilder();

        foreach (var line in lines)
        {
            if (line.StartsWith("=== ") && line.EndsWith(" ===") && line.Length > 8)
            {
                if (!string.IsNullOrWhiteSpace(currentName))
                {
                    result.Add(new PersonalityCritique(currentName, currentText.ToString().Trim()) { IsExpanded = false });
                    currentText.Clear();
                }

                currentName = line[4..^4].Trim();
                continue;
            }

            currentText.AppendLine(line);
        }

        if (!string.IsNullOrWhiteSpace(currentName))
            result.Add(new PersonalityCritique(currentName, currentText.ToString().Trim()) { IsExpanded = false });

        if (result.Count == 0 && !string.IsNullOrWhiteSpace(raw))
            result.Add(new PersonalityCritique("Critique", raw.Trim()) { IsExpanded = false });

        return result;
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
