# Gymnasium .NET Port

This is a C# port of the OpenAI Gymnasium library (https://gymnasium.farama.org/index.html), targeting .NET 8+ and C# 13 preview features.

## Features
- All classic control, toy text, Box2D (stub), Atari (stub), and MuJoCo (stub) environments fully ported
- Space abstractions: Discrete, Box, MultiDiscrete, MultiBinary, Dict, Tuple
- Environment registry and type-safe creation (EnvRegistry)
- Wrappers: TimeLimit, RecordEpisodeStatistics, ObservationWrapper, ActionWrapper, RewardWrapper
- Vectorized environments (VectorEnv)
- Seeding, spec, metadata, and reward range support
- xUnit test suite for all classic environments
- Semantic versioning with Nerdbank.GitVersioning
- NuGet package and GitHub Actions CI/CD pipeline
- Modern Avalonia UI for environment/agent selection, training configuration, and live visualization
- Agent plugin system (MEF/Composition) for custom agents
- Live graphical rendering for all environments (with stubs for Box2D/Atari/MuJoCo)
- Reward, episode length, and loss charts with moving averages
- Per-episode reward/length/loss tables
- Best/worst episode trajectory reporting (UI and export)
- Summary statistics (mean, min, max, std, median, percentiles, success rate)
- Session save/load (JSON)
- HTML and PDF report export (QuestPDF) with all charts, tables, and metrics
- Error handling and UI for agent plugin DLL import/reload
- All scientific metrics and visualizations available in UI and exported reports

## Getting Started
1. Build the solution:
   ```bash
   dotnet build
   ```
2. Run tests:
   ```bash
   dotnet test
   ```
3. Create a NuGet package:
   ```bash
   dotnet pack Gymnasium/Gymnasium.csproj -c Release
   ```
4. Run the Avalonia UI:
   ```bash
   dotnet run --project Gymnasium.UI
   ```

## CI/CD
- See `.github/workflows/dotnet.yml` for build, test, and pack pipeline (full git history is fetched for versioning).
- Semantic versioning is managed by Nerdbank.GitVersioning (`version.json`).
- Test step uses `dotnet test --configuration Release --no-build --logger "trx;LogFileName=test_results.trx"` for compatibility with GitHub Actions.

## License
See LICENSE file.
