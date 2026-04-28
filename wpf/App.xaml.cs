using System.IO;
using System.Windows;
using BLL;
using DAL;
using Services;
using wpf.ViewModels;

namespace wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Définir les variables d'environnement directement
        Environment.SetEnvironmentVariable("API_BASE_URL", "https://models.inference.ai.azure.com");
        Environment.SetEnvironmentVariable("MODEL_NAMES", "gpt-4o,gpt-4.1,gpt-4.1-mini,gpt-4o-mini");

        // Initialiser la base de données SQLite
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LlmImageAnalyzer", "history.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        IHistoryRepository historyRepo = new HistoryRepository(dbPath);
        historyRepo.Initialize();
        IPersonalityRepository personalityRepo = new PersonalityRepository(dbPath);
        personalityRepo.Initialize();
        IConfigRepository configRepo = new ConfigRepository(dbPath);
        configRepo.Initialize();

        // Récupérer la clé API depuis la configuration
        string? apiKey = configRepo.Get("GITHUB_PAT");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            Environment.SetEnvironmentVariable("GITHUB_PAT", apiKey);
        }

        // Composer les services
        IApiService apiService = new ApiService();
        IDiceBearAvatarService diceBearAvatarService = new DiceBearAvatarService();
        IImageService imageService = new ImageService();
        ILlmService llmService = new LlmService(apiService);
        IHistoryService historyService = new HistoryService(historyRepo);
        IPersonalityService personalityService = new PersonalityService(personalityRepo);

        var viewModel = new MainViewModel(imageService, llmService, historyService, personalityService, apiService, diceBearAvatarService, configRepo);
        var window = new MainWindow(viewModel);
        window.Show();
    }
}


