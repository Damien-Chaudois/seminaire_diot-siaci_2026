# Seminaire Diot-Sciaci 2026

Application desktop WPF permettant de simuler des tests utilisateurs sur des maquettes ou captures d'ecran en s'appuyant sur des LLM configures avec des personnalites distinctes.

L'objectif est de charger une image d'interface, de la soumettre a plusieurs profils utilisateurs simules, puis de comparer leurs critiques, leurs notes et leurs recommandations prioritaires. L'application sert donc de support de revue UX assistee par IA, avec historisation locale des analyses.

## Fonctionnalites principales

- Import d'une image d'interface a analyser.
- Selection d'un ou plusieurs modeles/personnalites pour simuler des retours utilisateurs.
- Generation de critiques structurees et notees par des LLM.
- Gestion de personnalites parametrables : description, traits, contraintes, avatar.
- Historisation locale des analyses et des profils dans une base SQLite.
- Configuration des modeles et de l'acces API via un fichier `.env`.

## Architecture

La solution est decoupee en plusieurs projets pour separer interface, logique metier, acces aux donnees et integrations externes.

### `wpf/`

Projet de presentation. Il contient l'interface desktop, le demarrage de l'application et le `MainViewModel` qui orchestre les interactions utilisateur : selection d'image, choix des personnalites, appel aux services, affichage des critiques et gestion de l'historique.

Points importants :

- composition manuelle des dependances au demarrage dans `App.xaml.cs` ;
- logique UI centralisee dans `MainViewModel.cs` ;
- chargement du fichier `.env` au lancement ;
- base SQLite stockee dans `%LocalAppData%/LlmImageAnalyzer/history.db`.

### `BLL/`

Couche metier. Elle expose les services applicatifs qui encapsulent les cas d'usage sans dependre de la couche UI : historique, personnalites, traitement d'image et appels LLM.

Cette couche sert de point de coordination entre l'interface WPF et les repositories de la DAL ou les services techniques.

### `DAL/`

Couche d'acces aux donnees. Elle gere la persistence SQLite via `Microsoft.Data.Sqlite`.

Points importants :

- creation et migration defensive des tables au demarrage ;
- stockage de l'historique des analyses ;
- stockage des personnalites utilisateur personnalisables ;
- modeles simples dans `DAL/Models/`.

### `Services/`

Couche d'integration technique. Elle regroupe les services lies a l'environnement et aux API externes.

Points importants :

- `ApiService` consomme l'endpoint compatible chat completions ;
- `EnvService` charge les variables d'environnement depuis `.env` ;
- `DiceBearAvatarService` genere des avatars pour les personnalites ;
- configuration multi-modeles via `MODEL_NAMES` et `MODEL_NAME`.

### SQLite

SQLite est utilise comme stockage local embarque. Cela permet de garder une application simple a distribuer, sans serveur ni base externe, tout en conservant :

- les analyses precedentes ;
- les notes attribuees ;
- les personnalites definies dans l'outil.

## Configuration

L'application attend un fichier `.env` a la racine du depot avec au minimum :

```env
GITHUB_PAT=
API_BASE_URL=https://models.inference.ai.azure.com
MODEL_NAMES=gpt-4o,gpt-4.1,gpt-4.1-mini,gpt-4o-mini
```

Notes importantes :

- ne publiez jamais de token reel sur GitHub ;
- utilisez un PAT ayant les permissions necessaires pour GitHub Models ;
- le fichier `.env` est copie a la sortie de build pour etre charge par l'application.

## Prerequis

- Windows
- SDK .NET 10 ou plus recent

## Developpement

Depuis la racine du depot :

```powershell
dotnet restore seminaire.slnx
dotnet build seminaire.slnx
dotnet run --project wpf/wpf.csproj
```

## Publication

Le projet WPF est configure pour une publication `SelfContained` et `PublishSingleFile`.

Exemple de publication Windows x64 :

```powershell
dotnet publish wpf/wpf.csproj -c Release -r win-x64
```

Le binaire publie se trouvera dans `wpf/bin/Release/net10.0-windows/win-x64/publish/`.

## Parties importantes a connaitre

- Le coeur du comportement applicatif se trouve dans `wpf/ViewModels/MainViewModel.cs`.
- Le prompt systeme de chaque personnalite est construit dynamiquement a partir de traits et contraintes utilisateur.
- Les repositories SQLite initialisent et migrent leur schema au lancement, ce qui simplifie l'evolution locale de l'application.
- Le projet est pense pour une execution locale simple, sans infrastructure serveur.
