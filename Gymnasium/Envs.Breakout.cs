using System;
using System.Collections.Generic;
using System.Linq;
using Gymnasium.Spaces;

namespace Gymnasium.Envs;

/// <summary>
/// Breakout environment: Classic Atari Breakout game implementation.
/// Player controls a paddle to bounce a ball and break bricks.
/// </summary>
public class Breakout : Env<byte[], int>
{
    // Screen dimensions (Atari standard)
    private const int SCREEN_WIDTH = 160;
    private const int SCREEN_HEIGHT = 210;
    private const int CHANNELS = 3; // RGB
    
    // Game constants
    private const double PADDLE_SPEED = 5.0;
    private const double BALL_SPEED = 3.0;
    private const int PADDLE_HEIGHT = 4;
    private const int PADDLE_WIDTH = 16;
    private const int BALL_SIZE = 2;
    private const int BRICK_WIDTH = 8;
    private const int BRICK_HEIGHT = 6;
    private const int BRICK_ROWS = 6;
    private const int BRICK_COLS = 18;
    private const int BRICK_START_Y = 60;
    
    // Game state
    private double _ballX, _ballY;
    private double _ballVelX, _ballVelY;
    private double _paddleX;
    private bool[,] _bricks;
    private byte[] _screen;
    private Random _random;
    private int _score;
    private int _lives;
    private int _steps;
    private bool _ballStuck;
    
    // Action space: 0=NOOP, 1=FIRE, 2=RIGHT, 3=LEFT
    public Discrete ActionSpace { get; } = new(4);
      // Observation space: RGB image (210, 160, 3)
    public Box ObservationSpace { get; } = new(
        new float[SCREEN_HEIGHT * SCREEN_WIDTH * CHANNELS], 
        Enumerable.Repeat(255f, SCREEN_HEIGHT * SCREEN_WIDTH * CHANNELS).ToArray());
    
    public override object? Spec => new { id = "Breakout-v4", max_episode_steps = 10000 };
    public override object? Metadata => new { render_modes = new[] { "human", "rgb_array" } };
    public override (double, double)? RewardRange => (0, 7); // Max 7 points per brick in top row
    
    public Breakout()
    {
        _screen = new byte[SCREEN_HEIGHT * SCREEN_WIDTH * CHANNELS];
        _bricks = new bool[BRICK_ROWS, BRICK_COLS];
        _random = new Random();
        Reset();
    }
    
    public override byte[] Reset()
    {
        // Initialize bricks
        for (int row = 0; row < BRICK_ROWS; row++)
        {
            for (int col = 0; col < BRICK_COLS; col++)
            {
                _bricks[row, col] = true;
            }
        }
        
        // Reset game state
        _paddleX = SCREEN_WIDTH / 2.0 - PADDLE_WIDTH / 2.0;
        _ballX = SCREEN_WIDTH / 2.0;
        _ballY = SCREEN_HEIGHT - 30;
        _ballVelX = 0;
        _ballVelY = 0;
        _ballStuck = true; // Ball starts attached to paddle
        
        _score = 0;
        _lives = 5;
        _steps = 0;
        
        RenderScreen();
        return _screen;
    }
    
    public override (byte[] state, double reward, bool done, IDictionary<string, object> info) Step(int action)
    {
        _steps++;
        double reward = 0.0;
        
        // Player actions
        switch (action)
        {
            case 1: // FIRE
                if (_ballStuck)
                {
                    _ballStuck = false;
                    _ballVelX = (_random.NextDouble() - 0.5) * 2.0 * BALL_SPEED;
                    _ballVelY = -BALL_SPEED;
                }
                break;
            case 2: // RIGHT
                _paddleX = Math.Min(SCREEN_WIDTH - PADDLE_WIDTH, _paddleX + PADDLE_SPEED);
                if (_ballStuck)
                    _ballX = _paddleX + PADDLE_WIDTH / 2.0;
                break;
            case 3: // LEFT
                _paddleX = Math.Max(0, _paddleX - PADDLE_SPEED);
                if (_ballStuck)
                    _ballX = _paddleX + PADDLE_WIDTH / 2.0;
                break;
        }
        
        if (!_ballStuck)
        {
            // Ball physics
            _ballX += _ballVelX;
            _ballY += _ballVelY;
            
            // Ball collision with walls
            if (_ballX <= 0 || _ballX >= SCREEN_WIDTH - BALL_SIZE)
            {
                _ballVelX = -_ballVelX;
                _ballX = Math.Max(0, Math.Min(SCREEN_WIDTH - BALL_SIZE, _ballX));
            }
            
            if (_ballY <= 0)
            {
                _ballVelY = -_ballVelY;
                _ballY = 0;
            }
            
            // Ball collision with paddle
            if (_ballY + BALL_SIZE >= SCREEN_HEIGHT - 20 && 
                _ballY + BALL_SIZE <= SCREEN_HEIGHT - 16 &&
                _ballX + BALL_SIZE >= _paddleX && 
                _ballX <= _paddleX + PADDLE_WIDTH)
            {
                _ballVelY = -Math.Abs(_ballVelY); // Ensure ball goes up
                
                // Add angle based on where ball hits paddle
                double relativeIntersectX = (_ballX + BALL_SIZE / 2.0) - (_paddleX + PADDLE_WIDTH / 2.0);
                double normalizedIntersectX = relativeIntersectX / (PADDLE_WIDTH / 2.0);
                _ballVelX = normalizedIntersectX * BALL_SPEED * 0.75;
            }
            
            // Ball collision with bricks
            int brickRow = (int)((_ballY - BRICK_START_Y) / BRICK_HEIGHT);
            int brickCol = (int)(_ballX / BRICK_WIDTH);
            
            if (brickRow >= 0 && brickRow < BRICK_ROWS && 
                brickCol >= 0 && brickCol < BRICK_COLS && 
                _bricks[brickRow, brickCol])
            {
                _bricks[brickRow, brickCol] = false;
                _ballVelY = -_ballVelY;
                
                // Scoring system: higher rows give more points
                reward = (BRICK_ROWS - brickRow) + 1;
                _score += (int)reward;
            }
            
            // Ball falls off bottom
            if (_ballY > SCREEN_HEIGHT)
            {
                _lives--;
                if (_lives > 0)
                {
                    // Reset ball position
                    _ballX = _paddleX + PADDLE_WIDTH / 2.0;
                    _ballY = SCREEN_HEIGHT - 30;
                    _ballVelX = 0;
                    _ballVelY = 0;
                    _ballStuck = true;
                }
            }
        }
        
        // Check win condition (all bricks destroyed)
        bool allBricksGone = true;
        for (int row = 0; row < BRICK_ROWS && allBricksGone; row++)
        {
            for (int col = 0; col < BRICK_COLS && allBricksGone; col++)
            {
                if (_bricks[row, col])
                    allBricksGone = false;
            }
        }
        
        if (allBricksGone)
        {
            reward += 100; // Bonus for clearing all bricks
        }
        
        // Game termination
        bool done = _lives <= 0 || allBricksGone || _steps >= 10000;
        
        RenderScreen();
        
        var info = new Dictionary<string, object>
        {
            ["score"] = _score,
            ["lives"] = _lives,
            ["ball_x"] = _ballX,
            ["ball_y"] = _ballY,
            ["bricks_remaining"] = CountRemainingBricks()
        };
        
        return (_screen, reward, done, info);
    }
    
    private int CountRemainingBricks()
    {
        int count = 0;
        for (int row = 0; row < BRICK_ROWS; row++)
        {
            for (int col = 0; col < BRICK_COLS; col++)
            {
                if (_bricks[row, col])
                    count++;
            }
        }
        return count;
    }
    
    private void RenderScreen()
    {
        // Clear screen (black background)
        Array.Fill(_screen, (byte)0);
        
        // Draw bricks with different colors for each row
        for (int row = 0; row < BRICK_ROWS; row++)
        {
            for (int col = 0; col < BRICK_COLS; col++)
            {
                if (_bricks[row, col])
                {
                    int x = col * BRICK_WIDTH;
                    int y = BRICK_START_Y + row * BRICK_HEIGHT;
                    
                    // Different colors for different rows
                    byte r, g, b;
                    switch (row)
                    {
                        case 0: r = 255; g = 0; b = 0; break;     // Red
                        case 1: r = 255; g = 165; b = 0; break;   // Orange
                        case 2: r = 255; g = 255; b = 0; break;   // Yellow
                        case 3: r = 0; g = 255; b = 0; break;     // Green
                        case 4: r = 0; g = 0; b = 255; break;     // Blue
                        case 5: r = 128; g = 0; b = 128; break;   // Purple
                        default: r = 255; g = 255; b = 255; break; // White
                    }
                    
                    DrawRectangle(x, y, BRICK_WIDTH - 1, BRICK_HEIGHT - 1, r, g, b);
                }
            }
        }
        
        // Draw paddle (white)
        DrawRectangle((int)_paddleX, SCREEN_HEIGHT - 20, PADDLE_WIDTH, PADDLE_HEIGHT, 255, 255, 255);
        
        // Draw ball (white)
        if (!_ballStuck)
        {
            DrawRectangle((int)_ballX, (int)_ballY, BALL_SIZE, BALL_SIZE, 255, 255, 255);
        }
        else
        {
            // Ball attached to paddle
            DrawRectangle((int)(_paddleX + PADDLE_WIDTH / 2.0 - BALL_SIZE / 2.0), 
                         SCREEN_HEIGHT - 30, BALL_SIZE, BALL_SIZE, 255, 255, 255);
        }
        
        // Draw score
        DrawScore(_score, 10, 10);
        
        // Draw lives
        for (int i = 0; i < _lives; i++)
        {
            DrawRectangle(10 + i * 8, 25, 6, 3, 255, 255, 255);
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
            Console.WriteLine($"Breakout - Score: {_score}, Lives: {_lives}");
            Console.WriteLine($"Ball: ({_ballX:F1}, {_ballY:F1}) Velocity: ({_ballVelX:F1}, {_ballVelY:F1})");
            Console.WriteLine($"Bricks remaining: {CountRemainingBricks()}");
        }
    }
    
    public override void Close()
    {
        // Nothing to clean up
    }
}
