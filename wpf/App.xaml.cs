using System.IO;
using System.Windows;
using wpf.BLL;
using wpf.DAL;
using wpf.Services;
using wpf.ViewModels;

namespace wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Charger le .env depuis la racine du repo (deux niveaux au-dessus de l'exe)
        var envPath = FindEnvFile();
        EnvService.Load(envPath);

        // Initialiser la base de données SQLite
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LlmImageAnalyzer", "history.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        IHistoryRepository historyRepo = new HistoryRepository(dbPath);
        historyRepo.Initialize();

        // Composer les services
        IApiService apiService = new ApiService();
        IImageService imageService = new ImageService();
        ILlmService llmService = new LlmService(apiService);
        IHistoryService historyService = new HistoryService(historyRepo);

        var viewModel = new MainViewModel(imageService, llmService, historyService);
        var window = new MainWindow(viewModel);
        window.Show();
    }

    private static string FindEnvFile()
    {
        // Remonte depuis le répertoire courant pour trouver .env
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 6; i++)
        {
            var candidate = Path.Combine(dir, ".env");
            if (File.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }
        return ".env"; // fallback
    }
}

