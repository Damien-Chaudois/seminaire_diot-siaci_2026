using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using wpf.BLL;
using wpf.Models;

namespace wpf.ViewModels;

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

    // ── Prompt ───────────────────────────────────────────────────────────────
    private string _prompt = "Décris cette image en détail.";
    public string Prompt { get => _prompt; set => SetProperty(ref _prompt, value); }

    // ── Results (liste déroulante de blocs de texte) ─────────────────────────
    public ObservableCollection<string> ResultBlocks { get; } = [];
    private string? _selectedResultBlock;
    public string? SelectedResultBlock { get => _selectedResultBlock; set => SetProperty(ref _selectedResultBlock, value); }

    private string _fullResultText = string.Empty;
    public string FullResultText { get => _fullResultText; set => SetProperty(ref _fullResultText, value); }

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

        LoadHistory();
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
        IsLoading = true;
        StatusMessage = "Envoi à l'API en cours...";
        ResultBlocks.Clear();
        FullResultText = string.Empty;

        try
        {
            var result = await _llmService.AnalyzeImageAsync(_currentBase64, _currentExtension, Prompt);

            FullResultText = result;

            // Découper en blocs par paragraphe pour la liste déroulante
            var blocks = result.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);
            foreach (var block in blocks)
                ResultBlocks.Add(block.Trim());

            if (ResultBlocks.Count > 0)
                SelectedResultBlock = ResultBlocks[0];

            // Sauvegarder dans l'historique
            var entry = new HistoryEntry
            {
                ImageBase64 = _currentBase64,
                ImageExtension = _currentExtension,
                ResultText = result,
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

        FullResultText = entry.ResultText;
        ResultBlocks.Clear();
        var blocks = entry.ResultText.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);
        foreach (var block in blocks)
            ResultBlocks.Add(block.Trim());

        if (ResultBlocks.Count > 0)
            SelectedResultBlock = ResultBlocks[0];

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
