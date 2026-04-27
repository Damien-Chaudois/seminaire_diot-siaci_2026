using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using BLL;
using DAL.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace wpf.ViewModels;

public class PersonalityOption : ViewModelBase
{
    private bool _isSelected;
    private string _name;
    private string _roleDescription;
    private int _curiosity;
    private int _competence;
    private int _practicality;
    private int _aestheticSensitivity;
    private int _rigor;
    private bool _limitedVisionFlag;
    private bool _elderlyFlag;
    private bool _lowMobilityFlag;
    private bool _lowDigitalLiteracyFlag;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string RoleDescription
    {
        get => _roleDescription;
        set => SetProperty(ref _roleDescription, value);
    }

    public int Curiosity
    {
        get => _curiosity;
        set => SetProperty(ref _curiosity, value);
    }

    public int Competence
    {
        get => _competence;
        set => SetProperty(ref _competence, value);
    }

    public int Practicality
    {
        get => _practicality;
        set => SetProperty(ref _practicality, value);
    }

    public int AestheticSensitivity
    {
        get => _aestheticSensitivity;
        set => SetProperty(ref _aestheticSensitivity, value);
    }

    public int Rigor
    {
        get => _rigor;
        set => SetProperty(ref _rigor, value);
    }

    public bool LimitedVisionFlag
    {
        get => _limitedVisionFlag;
        set => SetProperty(ref _limitedVisionFlag, value);
    }

    public bool ElderlyFlag
    {
        get => _elderlyFlag;
        set => SetProperty(ref _elderlyFlag, value);
    }

    public bool LowMobilityFlag
    {
        get => _lowMobilityFlag;
        set => SetProperty(ref _lowMobilityFlag, value);
    }

    public bool LowDigitalLiteracyFlag
    {
        get => _lowDigitalLiteracyFlag;
        set => SetProperty(ref _lowDigitalLiteracyFlag, value);
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

    public PersonalityOption(string name, string roleDescription)
    {
        _name = name;
        _roleDescription = roleDescription;
    }

    public string BuildSystemPrompt()
    {
        var flags = new List<string>();
        if (LimitedVisionFlag) flags.Add("vision limitée");
        if (ElderlyFlag) flags.Add("personne âgée");
        if (LowMobilityFlag) flags.Add("motricité réduite");
        if (LowDigitalLiteracyFlag) flags.Add("faible aisance numérique");

        var flagsText = flags.Count == 0
            ? "aucune contrainte particulière"
            : string.Join(", ", flags);

        string DescribeLevel(int value) => value switch
        {
            <= 20 => "très faible",
            <= 40 => "faible",
            <= 60 => "modéré",
            <= 80 => "élevé",
            _     => "très élevé"
        };

        return
            $"Tu es {Name}, {RoleDescription}. " +
            $"Tu as les traits de personnalité suivants : " +
            $"curiosité {DescribeLevel(Curiosity)} ({Curiosity}/100), " +
            $"compétence technique {DescribeLevel(Competence)} ({Competence}/100), " +
            $"pragmatisme {DescribeLevel(Practicality)} ({Practicality}/100), " +
            $"sensibilité esthétique {DescribeLevel(AestheticSensitivity)} ({AestheticSensitivity}/100), " +
            $"rigueur {DescribeLevel(Rigor)} ({Rigor}/100). " +
            (flags.Count > 0 ? $"Tu as les caractéristiques suivantes : {flagsText}. " : "") +
            "Reste dans ce personnage tout au long de ta réponse : ton ton, tes priorités et ta façon d'évaluer doivent refléter fidèlement cette personnalité. " +
            "Analyse uniquement l'interface visible sur l'image fournie par l'utilisateur. " +
            "Commence ta réponse par une ligne au format exact : NOTE: x/5 (avec x un entier entre 0 et 5). " +
            "Puis fournis une critique concrète et actionnnable structurée en trois sections : Points forts, Points faibles, Recommandations prioritaires.";
    }

    [System.Obsolete("Use BuildSystemPrompt() instead.")]
    public string BuildInstruction() => BuildSystemPrompt();
}

public class PersonalityCritique : ViewModelBase
{
    private bool _isExpanded;

    public string PersonalityName { get; }
    public double Rating { get; }
    public string CritiqueText { get; }
    public string RatingDisplay => $"{Rating:0.0}/5";

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public PersonalityCritique(string personalityName, double rating, string critiqueText)
    {
        PersonalityName = personalityName;
        Rating = rating;
        CritiqueText = critiqueText;
    }
}

public class MainViewModel : ViewModelBase
{
    private readonly IImageService _imageService;
    private readonly ILlmService _llmService;
    private readonly IHistoryService _historyService;
    private readonly Services.IApiService _apiService;
    private PersonalityOption? _editingPersonality;

    // ── Image ────────────────────────────────────────────────────────────────
    private string _currentBase64 = string.Empty;
    private string _currentExtension = "jpeg";
    private BitmapImage? _previewImage;
    public BitmapImage? PreviewImage { get => _previewImage; set => SetProperty(ref _previewImage, value); }

    // ── Personalities ─────────────────────────────────────────────────────────
    public ObservableCollection<PersonalityOption> Personalities { get; } = [];

    private int _selectedTabIndex;
    public int SelectedTabIndex { get => _selectedTabIndex; set => SetProperty(ref _selectedTabIndex, value); }

    private string _personalityFormTitle = "Creation d'une personnalite";
    public string PersonalityFormTitle { get => _personalityFormTitle; set => SetProperty(ref _personalityFormTitle, value); }

    private string _personalityEditorName = string.Empty;
    public string PersonalityEditorName { get => _personalityEditorName; set => SetProperty(ref _personalityEditorName, value); }

    private string _personalityEditorRole = string.Empty;
    public string PersonalityEditorRole { get => _personalityEditorRole; set => SetProperty(ref _personalityEditorRole, value); }

    private int _editorCuriosity = 60;
    public int EditorCuriosity { get => _editorCuriosity; set => SetProperty(ref _editorCuriosity, value); }

    private int _editorCompetence = 60;
    public int EditorCompetence { get => _editorCompetence; set => SetProperty(ref _editorCompetence, value); }

    private int _editorPracticality = 60;
    public int EditorPracticality { get => _editorPracticality; set => SetProperty(ref _editorPracticality, value); }

    private int _editorAestheticSensitivity = 60;
    public int EditorAestheticSensitivity { get => _editorAestheticSensitivity; set => SetProperty(ref _editorAestheticSensitivity, value); }

    private int _editorRigor = 60;
    public int EditorRigor { get => _editorRigor; set => SetProperty(ref _editorRigor, value); }

    private bool _editorLimitedVisionFlag;
    public bool EditorLimitedVisionFlag { get => _editorLimitedVisionFlag; set => SetProperty(ref _editorLimitedVisionFlag, value); }

    private bool _editorElderlyFlag;
    public bool EditorElderlyFlag { get => _editorElderlyFlag; set => SetProperty(ref _editorElderlyFlag, value); }

    private bool _editorLowMobilityFlag;
    public bool EditorLowMobilityFlag { get => _editorLowMobilityFlag; set => SetProperty(ref _editorLowMobilityFlag, value); }

    private bool _editorLowDigitalLiteracyFlag;
    public bool EditorLowDigitalLiteracyFlag { get => _editorLowDigitalLiteracyFlag; set => SetProperty(ref _editorLowDigitalLiteracyFlag, value); }

    private string _selectedPersonalitiesSummary = "Aucune personnalité sélectionnée";
    public string SelectedPersonalitiesSummary
    {
        get => _selectedPersonalitiesSummary;
        set => SetProperty(ref _selectedPersonalitiesSummary, value);
    }

    // ── Modeles IA ───────────────────────────────────────────────────────────
    public ObservableCollection<string> AvailableModels { get; } = [];

    private string _selectedModel = string.Empty;
    public string SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (!SetProperty(ref _selectedModel, value))
                return;

            if (string.IsNullOrWhiteSpace(value))
                return;

            _apiService.SetModel(value);
            CommandManager.InvalidateRequerySuggested();
        }
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

    // ── API Key Banner ────────────────────────────────────────────────────────
    private bool _apiKeyMissing;
    public bool ApiKeyMissing { get => _apiKeyMissing; set => SetProperty(ref _apiKeyMissing, value); }

    private string _apiKeyInput = string.Empty;
    public string ApiKeyInput { get => _apiKeyInput; set => SetProperty(ref _apiKeyInput, value); }

    private string _apiKeyValidationError = string.Empty;
    public string ApiKeyValidationError { get => _apiKeyValidationError; set => SetProperty(ref _apiKeyValidationError, value); }

    // ── Commands ─────────────────────────────────────────────────────────────
    public ICommand SelectImageCommand { get; }
    public ICommand SendToApiCommand { get; }
    public ICommand RestoreHistoryCommand { get; }
    public ICommand DeleteHistoryCommand { get; }
    public ICommand EditPersonalityCommand { get; }
    public ICommand AddPersonalityCommand { get; }
    public ICommand SavePersonalityCommand { get; }
    public ICommand CancelPersonalityCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand ValidateApiKeyCommand { get; }

    public MainViewModel(IImageService imageService, ILlmService llmService, IHistoryService historyService, Services.IApiService apiService)
    {
        _imageService = imageService;
        _llmService = llmService;
        _historyService = historyService;
        _apiService = apiService;

        SelectImageCommand = new RelayCommand(_ => SelectImage());
        SendToApiCommand = new RelayCommandAsync(_ => SendToApiAsync(), _ => HasImage && !IsLoading && !string.IsNullOrWhiteSpace(SelectedModel));
        RestoreHistoryCommand = new RelayCommand(entry => RestoreHistory(entry as HistoryEntry));
        DeleteHistoryCommand = new RelayCommand(entry => DeleteHistory(entry as HistoryEntry));
        EditPersonalityCommand = new RelayCommand(entry => OpenPersonalityEditor(entry as PersonalityOption));
        AddPersonalityCommand = new RelayCommand(_ => OpenCreatePersonalityEditor());
        SavePersonalityCommand = new RelayCommand(_ => SavePersonality());
        CancelPersonalityCommand = new RelayCommand(_ => CancelPersonalityEdit());
        ResetCommand = new RelayCommand(_ => ResetInterface());
        ValidateApiKeyCommand = new RelayCommandAsync(_ => ValidateApiKeyAsync(), _ => !string.IsNullOrWhiteSpace(ApiKeyInput));

        ApiKeyMissing = !_apiService.HasApiKey;

        InitializeModels();
        InitializePersonalities();
        LoadHistory();
    }

    private void InitializeModels()
    {
        AvailableModels.Clear();
        foreach (var model in _apiService.GetAvailableModels())
            AvailableModels.Add(model);

        if (AvailableModels.Count == 0)
        {
            _selectedModel = string.Empty;
            OnPropertyChanged(nameof(SelectedModel));
            StatusMessage = "Aucun modele configure. Ajoutez MODEL_NAMES ou MODEL_NAME dans .env.";
            CommandManager.InvalidateRequerySuggested();
            return;
        }

        _selectedModel = _apiService.CurrentModel;
        OnPropertyChanged(nameof(SelectedModel));
        CommandManager.InvalidateRequerySuggested();
    }

    private async Task ValidateApiKeyAsync()
    {
        ApiKeyValidationError = string.Empty;
        var key = ApiKeyInput.Trim();

        var valid = await _apiService.ValidateApiKeyAsync(key);

        if (valid)
        {
            _apiService.SetApiKey(key);
            ApiKeyMissing = false;
            StatusMessage = "Clé API validée.";
        }
        else
        {
            ApiKeyValidationError = "Clé invalide ou accès refusé. Vérifiez votre Personal Access Token GitHub.";
        }
    }

    private void InitializePersonalities()
    {
        var defaults = new[]
        {
            new PersonalityOption("Camille", "UX Coach: analyse l'intuitivite, la clarte des parcours et la comprehension immediate.")
            {
                Curiosity = 70, Competence = 85, Practicality = 90, AestheticSensitivity = 55, Rigor = 80
            },
            new PersonalityOption("Leo", "Art Director: analyse la beaute visuelle, la coherence graphique et l'impact esthetique.")
            {
                Curiosity = 65, Competence = 88, Practicality = 45, AestheticSensitivity = 95, Rigor = 72
            },
            new PersonalityOption("Nora", "Accessibility Expert: analyse lisibilite, contrastes, tailles cliquables et inclusion.")
            {
                Curiosity = 75, Competence = 92, Practicality = 82, AestheticSensitivity = 58, Rigor = 96,
                LimitedVisionFlag = true, ElderlyFlag = true
            },
            new PersonalityOption("Yanis", "Power User: analyse vitesse d'execution des taches et efficacite operationnelle.")
            {
                Curiosity = 55, Competence = 80, Practicality = 96, AestheticSensitivity = 40, Rigor = 78
            }
        };

        Personalities.Clear();
        foreach (var item in defaults)
        {
            item.SelectionChanged = UpdateSelectedPersonalitiesSummary;
            Personalities.Add(item);
        }

        SetEditorDefaults();
        SelectedTabIndex = 0;

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
        if (string.IsNullOrWhiteSpace(SelectedModel))
        {
            MessageBox.Show("Selectionnez un modele IA avant de lancer la critique.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

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

                var personalityPrompt = personality.BuildSystemPrompt();

                var rawCritique = await _llmService.AnalyzeImageAsync(_currentBase64, _currentExtension, personalityPrompt);
                var (rating, critique) = ExtractRatingAndBody(rawCritique);

                critiques.Add(new PersonalityCritique(personality.Name, rating, critique.Trim())
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
                RatingsCsv = string.Join(";", critiques.Select(c => $"{c.PersonalityName}:{c.Rating:0.0}/5")),
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
        SelectedTabIndex = 0;

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

    private void OpenCreatePersonalityEditor()
    {
        _editingPersonality = null;
        PersonalityFormTitle = "Creation d'une personnalite";
        SetEditorDefaults();
        SelectedTabIndex = 1;
    }

    private void OpenPersonalityEditor(PersonalityOption? personality)
    {
        if (personality is null)
            return;

        _editingPersonality = personality;
        PersonalityFormTitle = $"Edition de {personality.Name}";
        PersonalityEditorName = personality.Name;
        PersonalityEditorRole = personality.RoleDescription;
        EditorCuriosity = personality.Curiosity;
        EditorCompetence = personality.Competence;
        EditorPracticality = personality.Practicality;
        EditorAestheticSensitivity = personality.AestheticSensitivity;
        EditorRigor = personality.Rigor;
        EditorLimitedVisionFlag = personality.LimitedVisionFlag;
        EditorElderlyFlag = personality.ElderlyFlag;
        EditorLowMobilityFlag = personality.LowMobilityFlag;
        EditorLowDigitalLiteracyFlag = personality.LowDigitalLiteracyFlag;
        SelectedTabIndex = 1;
    }

    private void SavePersonality()
    {
        var name = PersonalityEditorName.Trim();
        var role = PersonalityEditorRole.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(role))
        {
            MessageBox.Show("Le nom et le role sont obligatoires.", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_editingPersonality is null && Personalities.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("Une personnalite avec ce nom existe deja.", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_editingPersonality is not null &&
            !string.Equals(_editingPersonality.Name, name, StringComparison.OrdinalIgnoreCase) &&
            Personalities.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("Une personnalite avec ce nom existe deja.", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_editingPersonality is null)
        {
            var created = new PersonalityOption(name, role)
            {
                SelectionChanged = UpdateSelectedPersonalitiesSummary
            };
            ApplyEditorValuesTo(created);
            Personalities.Add(created);
            created.IsSelected = true;
            StatusMessage = $"Personnalite ajoutee : {created.Name}";
        }
        else
        {
            _editingPersonality.Name = name;
            _editingPersonality.RoleDescription = role;
            ApplyEditorValuesTo(_editingPersonality);
            StatusMessage = $"Personnalite mise a jour : {_editingPersonality.Name}";
        }

        UpdateSelectedPersonalitiesSummary();
        _editingPersonality = null;
        SelectedTabIndex = 0;
    }

    private void CancelPersonalityEdit()
    {
        _editingPersonality = null;
        SetEditorDefaults();
        SelectedTabIndex = 0;
    }

    private void ResetInterface()
    {
        _currentBase64 = string.Empty;
        _currentExtension = "jpeg";
        PreviewImage = null;
        HasImage = false;
        IsLoading = false;
        Critiques.Clear();
        InitializePersonalities();
        StatusMessage = "Interface reinitialisee.";
    }

    private void SetEditorDefaults()
    {
        PersonalityEditorName = string.Empty;
        PersonalityEditorRole = string.Empty;
        EditorCuriosity = 60;
        EditorCompetence = 60;
        EditorPracticality = 60;
        EditorAestheticSensitivity = 60;
        EditorRigor = 60;
        EditorLimitedVisionFlag = false;
        EditorElderlyFlag = false;
        EditorLowMobilityFlag = false;
        EditorLowDigitalLiteracyFlag = false;
    }

    private void ApplyEditorValuesTo(PersonalityOption personality)
    {
        personality.Curiosity = EditorCuriosity;
        personality.Competence = EditorCompetence;
        personality.Practicality = EditorPracticality;
        personality.AestheticSensitivity = EditorAestheticSensitivity;
        personality.Rigor = EditorRigor;
        personality.LimitedVisionFlag = EditorLimitedVisionFlag;
        personality.ElderlyFlag = EditorElderlyFlag;
        personality.LowMobilityFlag = EditorLowMobilityFlag;
        personality.LowDigitalLiteracyFlag = EditorLowDigitalLiteracyFlag;
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

            builder.AppendLine($"=== {critique.PersonalityName} | {critique.Rating:0.0}/5 ===");
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
        double currentRating = 0;
        var currentText = new StringBuilder();

        foreach (var line in lines)
        {
            var headerMatch = Regex.Match(line, "^===\\s*(.+?)\\s*\\|\\s*([0-5](?:[.,]\\d+)?)\\s*/5\\s*===$");
            if (headerMatch.Success)
            {
                if (!string.IsNullOrWhiteSpace(currentName))
                {
                    result.Add(new PersonalityCritique(currentName, currentRating, currentText.ToString().Trim()) { IsExpanded = false });
                    currentText.Clear();
                }

                currentName = headerMatch.Groups[1].Value.Trim();
                currentRating = ParseRating(headerMatch.Groups[2].Value);
                continue;
            }

            currentText.AppendLine(line);
        }

        if (!string.IsNullOrWhiteSpace(currentName))
            result.Add(new PersonalityCritique(currentName, currentRating, currentText.ToString().Trim()) { IsExpanded = false });

        if (result.Count == 0 && !string.IsNullOrWhiteSpace(raw))
            result.Add(new PersonalityCritique("Critique", 0, raw.Trim()) { IsExpanded = false });

        return result;
    }

    private static (double Rating, string Body) ExtractRatingAndBody(string rawCritique)
    {
        var ratingMatch = Regex.Match(rawCritique, "NOTE\\s*:\\s*([0-5](?:[.,]\\d+)?)\\s*/\\s*5", RegexOptions.IgnoreCase);
        var rating = ratingMatch.Success ? ParseRating(ratingMatch.Groups[1].Value) : 0;

        var body = Regex.Replace(
            rawCritique,
            "^\\s*NOTE\\s*:\\s*[0-5](?:[.,]\\d+)?\\s*/\\s*5\\s*$",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline).Trim();

        return (rating, body);
    }

    private static double ParseRating(string value)
    {
        if (double.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var rating))
        {
            if (rating < 0) return 0;
            if (rating > 5) return 5;
            return rating;
        }
        return 0;
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
