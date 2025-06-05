using System;
using System.Collections.Generic;
using System.Linq;
using Gymnasium.Spaces;
using Gymnasium;

namespace Gymnasium.Envs;

/// <summary>
/// SpaceInvaders environment: Classic Atari Space Invaders game implementation.
/// Player controls a cannon at the bottom, shooting at descending alien invaders.
/// </summary>
public class SpaceInvaders : Env<byte[], int>
{
    // Screen dimensions (Atari standard)
    private const int SCREEN_WIDTH = 160;
    private const int SCREEN_HEIGHT = 210;
    private const int CHANNELS = 3; // RGB
    
    // Game constants
    private const double PLAYER_SPEED = 3.0;
    private const double BULLET_SPEED = 6.0;
    private const double ALIEN_SPEED = 0.5;
    private const double ALIEN_DROP_SPEED = 8.0;
    private const int PLAYER_WIDTH = 8;
    private const int PLAYER_HEIGHT = 6;
    private const int ALIEN_WIDTH = 6;
    private const int ALIEN_HEIGHT = 6;
    private const int BULLET_WIDTH = 2;
    private const int BULLET_HEIGHT = 4;
    private const int ALIEN_ROWS = 5;
    private const int ALIEN_COLS = 11;
    
    // Game state
    private double _playerX;
    private List<(double x, double y)> _playerBullets;
    private List<(double x, double y)> _alienBullets;
    private bool[,] _aliens;
    private double _alienFormationX;
    private double _alienFormationY;
    private bool _alienMoveRight;
    private byte[] _screen;
    private Random _random;
    private int _score;
    private int _lives;
    private int _steps;
    private int _frameCounter;
    
    // Action space: 0=NOOP, 1=FIRE, 2=RIGHT, 3=LEFT, 4=RIGHTFIRE, 5=LEFTFIRE
    public Discrete ActionSpace { get; } = new(6);
      // Observation space: RGB image (210, 160, 3)
    public Box ObservationSpace { get; } = new(
        new float[SCREEN_HEIGHT * SCREEN_WIDTH * CHANNELS], 
        Enumerable.Repeat(255f, SCREEN_HEIGHT * SCREEN_WIDTH * CHANNELS).ToArray());
    
    public override object? Spec => new { id = "SpaceInvaders-v4", max_episode_steps = 10000 };
    public override object? Metadata => new { render_modes = new[] { "human", "rgb_array" } };
    public override (double, double)? RewardRange => (0, 200); // Max score for clearing all aliens
    
    public SpaceInvaders()
    {
        _screen = new byte[SCREEN_HEIGHT * SCREEN_WIDTH * CHANNELS];
        _playerBullets = new List<(double x, double y)>();
        _alienBullets = new List<(double x, double y)>();
        _aliens = new bool[ALIEN_ROWS, ALIEN_COLS];
        _random = new Random();
        Reset();
    }
    
    public override byte[] Reset()
    {
        // Initialize aliens
        for (int row = 0; row < ALIEN_ROWS; row++)
        {
            for (int col = 0; col < ALIEN_COLS; col++)
            {
                _aliens[row, col] = true;
            }
        }
        
        // Reset game state
        _playerX = SCREEN_WIDTH / 2.0 - PLAYER_WIDTH / 2.0;
        _alienFormationX = 10;
        _alienFormationY = 40;
        _alienMoveRight = true;
        
        _playerBullets.Clear();
        _alienBullets.Clear();
        
        _score = 0;
        _lives = 3;
        _steps = 0;
        _frameCounter = 0;
        
        RenderScreen();
        return _screen;
    }
    
    public override (byte[] state, double reward, bool done, IDictionary<string, object> info) Step(int action)
    {
        _steps++;
        _frameCounter++;
        double reward = 0.0;
        
        // Player actions
        switch (action)
        {
            case 1: // FIRE
                FirePlayerBullet();
                break;
            case 2: // RIGHT
                _playerX = Math.Min(SCREEN_WIDTH - PLAYER_WIDTH, _playerX + PLAYER_SPEED);
                break;
            case 3: // LEFT
                _playerX = Math.Max(0, _playerX - PLAYER_SPEED);
                break;
            case 4: // RIGHTFIRE
                _playerX = Math.Min(SCREEN_WIDTH - PLAYER_WIDTH, _playerX + PLAYER_SPEED);
                FirePlayerBullet();
                break;
            case 5: // LEFTFIRE
                _playerX = Math.Max(0, _playerX - PLAYER_SPEED);
                FirePlayerBullet();
                break;
        }
        
        // Update bullets
        UpdateBullets();
        
        // Update aliens
        if (_frameCounter % 10 == 0) // Aliens move every 10 frames
        {
            UpdateAliens();
        }
        
        // Alien shooting
        if (_frameCounter % 60 == 0 && _random.NextDouble() < 0.3) // Random alien shooting
        {
            FireAlienBullet();
        }
        
        // Check collisions
        reward += CheckCollisions();
        
        // Check if aliens reached bottom
        if (_alienFormationY + (ALIEN_ROWS * 8) > SCREEN_HEIGHT - 40)
        {
            _lives = 0; // Game over if aliens reach player level
        }
        
        // Check win condition (all aliens destroyed)
        bool allAliensGone = true;
        for (int row = 0; row < ALIEN_ROWS && allAliensGone; row++)
        {
            for (int col = 0; col < ALIEN_COLS && allAliensGone; col++)
            {
                if (_aliens[row, col])
                    allAliensGone = false;
            }
        }
        
        if (allAliensGone)
        {
            reward += 100; // Bonus for clearing all aliens
            // Respawn aliens for next wave
            for (int row = 0; row < ALIEN_ROWS; row++)
            {
                for (int col = 0; col < ALIEN_COLS; col++)
                {
                    _aliens[row, col] = true;
                }
            }
            _alienFormationY = 40;
            _alienFormationX = 10;
        }
        
        // Game termination
        bool done = _lives <= 0 || _steps >= 10000;
        
        RenderScreen();
        
        var info = new Dictionary<string, object>
        {
            ["score"] = _score,
            ["lives"] = _lives,
            ["aliens_remaining"] = CountRemainingAliens(),
            ["player_x"] = _playerX
        };
        
        return (_screen, reward, done, info);
    }
    
    private void FirePlayerBullet()
    {
        // Limit to 3 player bullets on screen
        if (_playerBullets.Count < 3)
        {
            _playerBullets.Add((_playerX + PLAYER_WIDTH / 2.0, SCREEN_HEIGHT - 40));
        }
    }
    
    private void FireAlienBullet()
    {
        // Find bottom-most aliens to fire from
        var shooters = new List<(int row, int col)>();
        for (int col = 0; col < ALIEN_COLS; col++)
        {
            for (int row = ALIEN_ROWS - 1; row >= 0; row--)
            {
                if (_aliens[row, col])
                {
                    shooters.Add((row, col));
                    break;
                }
            }
        }
        
        if (shooters.Count > 0)
        {
            var (row, col) = shooters[_random.Next(shooters.Count)];
            double x = _alienFormationX + col * 12 + ALIEN_WIDTH / 2.0;
            double y = _alienFormationY + row * 10 + ALIEN_HEIGHT;
            _alienBullets.Add((x, y));
        }
    }
    
    private void UpdateBullets()
    {
        // Update player bullets
        for (int i = _playerBullets.Count - 1; i >= 0; i--)
        {
            var bullet = _playerBullets[i];
            bullet.y -= BULLET_SPEED;
            if (bullet.y < 0)
            {
                _playerBullets.RemoveAt(i);
            }
            else
            {
                _playerBullets[i] = bullet;
            }
        }
        
        // Update alien bullets
        for (int i = _alienBullets.Count - 1; i >= 0; i--)
        {
            var bullet = _alienBullets[i];
            bullet.y += BULLET_SPEED * 0.7; // Alien bullets slightly slower
            if (bullet.y > SCREEN_HEIGHT)
            {
                _alienBullets.RemoveAt(i);
            }
            else
            {
                _alienBullets[i] = bullet;
            }
        }
    }
    
    private void UpdateAliens()
    {
        if (_alienMoveRight)
        {
            _alienFormationX += ALIEN_SPEED;
            if (_alienFormationX + ALIEN_COLS * 12 > SCREEN_WIDTH - 10)
            {
                _alienMoveRight = false;
                _alienFormationY += ALIEN_DROP_SPEED;
            }
        }
        else
        {
            _alienFormationX -= ALIEN_SPEED;
            if (_alienFormationX < 10)
            {
                _alienMoveRight = true;
                _alienFormationY += ALIEN_DROP_SPEED;
            }
        }
    }
    
    private double CheckCollisions()
    {
        double reward = 0.0;
        
        // Player bullets vs aliens
        for (int i = _playerBullets.Count - 1; i >= 0; i--)
        {
            var bullet = _playerBullets[i];
            for (int row = 0; row < ALIEN_ROWS; row++)
            {
                for (int col = 0; col < ALIEN_COLS; col++)
                {
                    if (_aliens[row, col])
                    {
                        double alienX = _alienFormationX + col * 12;
                        double alienY = _alienFormationY + row * 10;
                        
                        if (bullet.x >= alienX && bullet.x <= alienX + ALIEN_WIDTH &&
                            bullet.y >= alienY && bullet.y <= alienY + ALIEN_HEIGHT)
                        {
                            _aliens[row, col] = false;
                            _playerBullets.RemoveAt(i);
                            
                            // Different points for different alien types
                            int points = row switch
                            {
                                0 => 30,  // Top row (most points)
                                1 => 20,  // Second row
                                2 => 20,  // Third row
                                3 => 10,  // Fourth row
                                4 => 10,  // Bottom row (least points)
                                _ => 10
                            };
                            
                            _score += points;
                            reward += points;
                            goto NextBullet; // Exit nested loops
                        }
                    }
                }
            }
            NextBullet:;
        }
        
        // Alien bullets vs player
        for (int i = _alienBullets.Count - 1; i >= 0; i--)
        {
            var bullet = _alienBullets[i];
            if (bullet.x >= _playerX && bullet.x <= _playerX + PLAYER_WIDTH &&
                bullet.y >= SCREEN_HEIGHT - 40 && bullet.y <= SCREEN_HEIGHT - 40 + PLAYER_HEIGHT)
            {
                _alienBullets.RemoveAt(i);
                _lives--;
                reward -= 50; // Penalty for getting hit
            }
        }
        
        return reward;
    }
    
    private int CountRemainingAliens()
    {
        int count = 0;
        for (int row = 0; row < ALIEN_ROWS; row++)
        {
            for (int col = 0; col < ALIEN_COLS; col++)
            {
                if (_aliens[row, col])
                    count++;
            }
        }
        return count;
    }
    
    private void RenderScreen()
    {
        // Clear screen (black background)
        Array.Fill(_screen, (byte)0);
        
        // Draw aliens with different colors for each row
        for (int row = 0; row < ALIEN_ROWS; row++)
        {
            for (int col = 0; col < ALIEN_COLS; col++)
            {
                if (_aliens[row, col])
                {
                    int x = (int)(_alienFormationX + col * 12);
                    int y = (int)(_alienFormationY + row * 10);
                    
                    // Different colors for different alien types
                    byte r, g, b;
                    switch (row)
                    {
                        case 0: r = 255; g = 0; b = 255; break;   // Magenta (top tier)
                        case 1: r = 0; g = 255; b = 255; break;   // Cyan
                        case 2: r = 255; g = 255; b = 0; break;   // Yellow
                        case 3: r = 0; g = 255; b = 0; break;     // Green
                        case 4: r = 255; g = 0; b = 0; break;     // Red (bottom tier)
                        default: r = 255; g = 255; b = 255; break; // White
                    }
                    
                    DrawAlien(x, y, r, g, b);
                }
            }
        }
        
        // Draw player (white)
        DrawPlayer((int)_playerX, SCREEN_HEIGHT - 40);
        
        // Draw player bullets (yellow)
        foreach (var bullet in _playerBullets)
        {
            DrawRectangle((int)bullet.x, (int)bullet.y, BULLET_WIDTH, BULLET_HEIGHT, 255, 255, 0);
        }
        
        // Draw alien bullets (red)
        foreach (var bullet in _alienBullets)
        {
            DrawRectangle((int)bullet.x, (int)bullet.y, BULLET_WIDTH, BULLET_HEIGHT, 255, 0, 0);
        }
        
        // Draw UI elements
        DrawScore(_score, 10, 10);
        DrawLives(_lives, SCREEN_WIDTH - 40, 10);
        
        // Draw ground line
        for (int x = 0; x < SCREEN_WIDTH; x++)
        {
            DrawRectangle(x, SCREEN_HEIGHT - 20, 1, 2, 0, 255, 0);
        }
    }
    
    private void DrawAlien(int x, int y, byte r, byte g, byte b)
    {
        // Simple alien sprite (6x6 pixels)
        bool[,] sprite = {
            {false, true, false, false, true, false},
            {false, false, true, true, false, false},
            {false, true, true, true, true, false},
            {true, true, false, false, true, true},
            {true, true, true, true, true, true},
            {false, true, false, false, true, false}
        };
        
        for (int dy = 0; dy < 6; dy++)
        {
            for (int dx = 0; dx < 6; dx++)
            {
                if (sprite[dy, dx])
                {
                    DrawRectangle(x + dx, y + dy, 1, 1, r, g, b);
                }
            }
        }
    }
    
    private void DrawPlayer(int x, int y)
    {
        // Simple player cannon sprite (8x6 pixels)
        bool[,] sprite = {
            {false, false, false, true, true, false, false, false},
            {false, false, true, true, true, true, false, false},
            {false, false, true, true, true, true, false, false},
            {false, true, true, true, true, true, true, false},
            {true, true, true, true, true, true, true, true},
            {true, true, true, true, true, true, true, true}
        };
        
        for (int dy = 0; dy < 6; dy++)
        {
            for (int dx = 0; dx < 8; dx++)
            {
                if (sprite[dy, dx])
                {
                    DrawRectangle(x + dx, y + dy, 1, 1, 0, 255, 0);
                }
            }
        }
    }
    
    private void DrawRectangle(int x, int y, int width, int height, byte r, byte g, byte b)
    {
        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                int px = x + dx;
                int py = y + dy;
                if (px >= 0 && px < SCREEN_WIDTH && py >= 0 && py < SCREEN_HEIGHT)
                {
                    int index = (py * SCREEN_WIDTH + px) * CHANNELS;
                    _screen[index] = r;     // Red
                    _screen[index + 1] = g; // Green
                    _screen[index + 2] = b; // Blue
                }
            }
        }
    }
    
    private void DrawScore(int score, int x, int y)
    {
        string scoreStr = score.ToString();
        for (int i = 0; i < scoreStr.Length; i++)
        {
            DrawDigit(scoreStr[i], x + i * 6, y);
        }
    }
    
    private void DrawLives(int lives, int x, int y)
    {
        for (int i = 0; i < lives; i++)
        {
            DrawPlayer(x - i * 10, y);
        }
    }
    
    private void DrawDigit(char digit, int x, int y)
    {
        // Simple 5x7 pixel font for digits
        bool[,] patterns = digit switch
        {
            '0' => new bool[,] { {true,true,true}, {true,false,true}, {true,false,true}, {true,false,true}, {true,true,true} },
            '1' => new bool[,] { {false,true,false}, {true,true,false}, {false,true,false}, {false,true,false}, {true,true,true} },
            '2' => new bool[,] { {true,true,true}, {false,false,true}, {true,true,true}, {true,false,false}, {true,true,true} },
            '3' => new bool[,] { {true,true,true}, {false,false,true}, {true,true,true}, {false,false,true}, {true,true,true} },
            '4' => new bool[,] { {true,false,true}, {true,false,true}, {true,true,true}, {false,false,true}, {false,false,true} },
            '5' => new bool[,] { {true,true,true}, {true,false,false}, {true,true,true}, {false,false,true}, {true,true,true} },
            '6' => new bool[,] { {true,true,true}, {true,false,false}, {true,true,true}, {true,false,true}, {true,true,true} },
            '7' => new bool[,] { {true,true,true}, {false,false,true}, {false,false,true}, {false,false,true}, {false,false,true} },
            '8' => new bool[,] { {true,true,true}, {true,false,true}, {true,true,true}, {true,false,true}, {true,true,true} },
            '9' => new bool[,] { {true,true,true}, {true,false,true}, {true,true,true}, {false,false,true}, {true,true,true} },
            _ => new bool[,] { {false,false,false}, {false,false,false}, {false,false,false}, {false,false,false}, {false,false,false} }
        };
        
        for (int dy = 0; dy < 5; dy++)
        {
            for (int dx = 0; dx < 3; dx++)
            {
                if (patterns[dy, dx])
                {
                    DrawRectangle(x + dx, y + dy, 1, 1, 255, 255, 255);
                }
            }
        }
    }
    
    public override void Render(string mode = "human")
    {
        if (mode == "human")
        {
            ConsoleRenderer.RenderHeader("SpaceInvaders");
            Console.WriteLine($"Score: {_score} Lives: {_lives}");
            Console.WriteLine($"Player: ({_playerX:F1}) Aliens: {CountRemainingAliens()}");
            Console.WriteLine($"Bullets: Player={_playerBullets.Count}, Alien={_alienBullets.Count}");
        }
    }
    
    public override void Close()
    {
        // Nothing to clean up
    }
}
