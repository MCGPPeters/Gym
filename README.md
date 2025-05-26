# Gymnasium .NET Port

This is a C# port of the OpenAI Gymnasium library (https://gymnasium.farama.org/index.html), targeting .NET 8+ and C# 13 preview features.

## Features
- All classic control, toy text environments fully ported
- **Physics-based environments with VelcroPhysics engine:**
  - LunarLander: Real physics simulation with rocket thrust, gravity, fuel consumption
  - BipedalWalker: Multi-body walker with realistic joint mechanics and motor torque
  - CarRacing: Top-down car physics with steering, acceleration, braking, and collision detection
- **Complete Atari environments with authentic arcade graphics:**
  - Pong-v4: Classic two-player Pong with AI opponent and authentic visuals
  - Breakout-v4: Brick-breaking game with colored brick layers and realistic physics
  - SpaceInvaders-v4: Space shooter with alien formations and multi-wave gameplay
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
- **Live physics-based rendering for all environments with real-time visualization:**
  - LunarLander: Diamond lander with thrust effects, velocity vectors, landing pad
  - BipedalWalker: Multi-body walker with hull, legs, joints, and ground contact
  - CarRacing: Top-down car with track boundaries, steering, and velocity indicators
  - **Atari environments: Pixel-perfect authentic arcade game visuals with real-time gameplay**
  - MuJoCo (stub) environments for future expansion
- Reward, episode length, and loss charts with moving averages
- Per-episode reward/length/loss tables
- Best/worst episode trajectory reporting (UI and export)
- Summary statistics (mean, min, max, std, median, percentiles, success rate)
- Session save/load (JSON)
- HTML and PDF report export (QuestPDF) with all charts, tables, and metrics
- Error handling and UI for agent plugin DLL import/reload
- All scientific metrics and visualizations available in UI and exported reports

## Physics Engine Integration

This port includes a real physics engine integration using **VelcroPhysics** (a .NET port of Box2D) to provide authentic physics simulation for environments that require it:

### Implemented Physics Environments

#### LunarLander
- **Real physics simulation** with gravity (9.8 m/s²) and realistic rocket dynamics
- **Body mechanics**: Diamond-shaped lander with proper mass, friction, and restitution
- **Propulsion system**: Main engine and left/right side engines with fuel consumption
- **Landing mechanics**: Ground collision detection with landing pad scoring
- **State vector**: 8 elements including position, velocity, angle, angular velocity, and leg contact sensors
- **Reward system**: Based on distance to landing pad, fuel efficiency, and successful landing

#### BipedalWalker
- **Multi-body simulation** with hull (torso), upper legs, lower legs, and physics joints
- **Motor torque system**: Realistic joint motors for walking mechanics
- **Balance dynamics**: Hull stability and forward movement simulation
- **Contact sensors**: Ground contact detection for legs
- **State vector**: 24 elements including joint angles, velocities, and contact information
- **Reward system**: Forward progress with penalties for falling or excessive energy use

#### CarRacing
- **Top-down car physics** with realistic steering, acceleration, and braking
- **Vehicle dynamics**: Proper mass, friction, and momentum simulation
- **Track boundaries**: Collision detection with track walls using EdgeShape
- **Control mechanics**: Steering wheel angle, gas pedal, and brake system
- **State vector**: 8 elements including position, velocity, angle, and wheel angle
- **Reward system**: Speed-based rewards with penalties for leaving the track

### Physics Engine Details
- **Engine**: VelcroPhysics 0.2.2 (Microsoft.Xna.Framework-based Box2D port)
- **Simulation rate**: 50 Hz (20ms timesteps) for stable physics
- **Coordinate system**: World units with proper scaling for visualization
- **Performance**: Optimized for real-time simulation and rendering

## Atari Environments

This port includes complete implementations of classic Atari arcade games with authentic pixel-perfect graphics:

### Implemented Atari Games

#### Pong-v4
- **Classic two-player Pong** with intelligent AI opponent
- **Authentic visuals**: White paddles, ball, and score on pure black background matching original 1972 arcade game
- **Realistic physics**: Ball bouncing with paddle collision detection and speed variation
- **Action space**: 4 actions (NOOP, FIRE, UP, DOWN) for paddle control
- **Observation space**: RGB pixel data (210x160x3) representing the game screen
- **Scoring system**: First to 21 points wins with proper score display

#### Breakout-v4  
- **Classic brick-breaking game** with paddle and ball mechanics
- **Authentic color scheme**: Multi-colored brick rows (red, orange, yellow, green, blue, purple) matching original 1976 arcade game
- **Ball physics**: Realistic bouncing with paddle angle influence and brick destruction
- **Action space**: 4 actions (NOOP, FIRE, RIGHT, LEFT) for paddle movement
- **Lives system**: 5 lives with game over when all lives are lost
- **Scoring**: Different point values for different colored brick rows

#### SpaceInvaders-v4
- **Classic space shooter** with player cannon and alien formations
- **Authentic appearance**: Colored alien rows (magenta, cyan, yellow, green, red) matching original 1978 arcade game
- **Combat mechanics**: Player bullets (yellow), alien bullets (red), collision detection
- **Multi-wave gameplay**: Aliens descend and speed up as numbers decrease
- **Action space**: 6 actions (NOOP, FIRE, RIGHT, LEFT, RIGHTFIRE, LEFTFIRE) for movement and shooting
- **Progressive difficulty**: Faster alien movement and increased bullet frequency in later waves

### Atari Rendering System
- **Pixel-perfect graphics**: Direct RGB pixel data rendering with authentic arcade colors
- **Real-time visualization**: Live game state display in Avalonia UI
- **Optimized performance**: Efficient pixel sampling (every 2nd pixel) for smooth rendering
- **Authentic aesthetics**: Pure black backgrounds and exact color matching to original arcade games
- **Scalable display**: Automatically scales to fit UI canvas while maintaining proper aspect ratio

### Visualization
The Avalonia UI provides real-time physics visualization with:
- **Real-time body rendering** showing actual physics object positions and rotations
- **Visual feedback** for forces, velocities, and contact points
- **Interactive controls** for stepping through physics simulation
- **Debug information** displaying physics state values

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
