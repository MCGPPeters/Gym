# Gymnasium .NET Port

This is a C# port of the OpenAI Gymnasium library (https://gymnasium.farama.org/index.html), targeting .NET 8+ and C# 13 preview features.

## Features
- Classic control, toy text, Box2D, Atari, and MuJoCo environments (fully or as stubs)
- Space abstractions: Discrete, Box, MultiDiscrete, MultiBinary, Dict, Tuple
- Environment registry and type-safe creation (EnvRegistry)
- Wrappers: TimeLimit, RecordEpisodeStatistics, ObservationWrapper, ActionWrapper, RewardWrapper
- Vectorized environments (VectorEnv)
- Seeding, spec, metadata, and reward range support
- xUnit test suite
- Semantic versioning with Nerdbank.GitVersioning
- NuGet package and CI pipeline

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

## CI/CD
- See `.github/workflows/dotnet.yml` for build, test, and pack pipeline.
- Semantic versioning is managed by Nerdbank.GitVersioning (`version.json`).

## License
See LICENSE file.
