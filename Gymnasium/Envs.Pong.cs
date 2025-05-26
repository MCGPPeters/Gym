using System;
using System.Collections.Generic;
using System.Linq;
using Gymnasium.Spaces;

namespace Gymnasium.Envs;

/// <summary>
/// Pong environment: Classic Atari Pong game implementation.
/// Two-player Pong game where the agent controls the right paddle.
/// </summary>
public class Pong : Env<byte[], int>
{
    // Screen dimensions (Atari standard)
    private const int SCREEN_WIDTH = 160;
    private const int SCREEN_HEIGHT = 210;
    private const int CHANNELS = 3; // RGB
    
    // Game constants
    private const double PADDLE_SPEED = 4.0;
    private const double BALL_SPEED = 3.0;
    private const int PADDLE_HEIGHT = 15;
    private const int PADDLE_WIDTH = 2;
    private const int BALL_SIZE = 2;
    
    // Game state
    private double _ballX, _ballY;
    private double _ballVelX, _ballVelY;
    private double _leftPaddleY, _rightPaddleY;
    private int _leftScore, _rightScore;
    private byte[] _screen;
    private Random _random;
    private int _steps;
    
    // Action space: 0=NOOP, 1=FIRE, 2=UP, 3=DOWN
    public Discrete ActionSpace { get; } = new(4);
    
    // Observation space: RGB image (210, 160, 3)
    public Box ObservationSpace { get; } = new(
        new float[SCREEN_HEIGHT * SCREEN_WIDTH * CHANNELS], 
        Enumerable.Repeat(255f, SCREEN_HEIGHT * SCREEN_WIDTH * CHANNELS).ToArray());
    
    public override object? Spec => new { id = "Pong-v4", max_episode_steps = 1000 };
    public override object? Metadata => new { render_modes = new[] { "human", "rgb_array" } };
    public override (double, double)? RewardRange => (-1, 1);
    
    public Pong()
    {
        _screen = new byte[SCREEN_HEIGHT * SCREEN_WIDTH * CHANNELS];
        _random = new Random();
        Reset();
    }
    
    public override byte[] Reset()
    {
        _ballX = SCREEN_WIDTH / 2.0;
        _ballY = SCREEN_HEIGHT / 2.0;
        _ballVelX = _random.NextDouble() > 0.5 ? BALL_SPEED : -BALL_SPEED;
        _ballVelY = (_random.NextDouble() - 0.5) * 2.0 * BALL_SPEED;
        
        _leftPaddleY = SCREEN_HEIGHT / 2.0 - PADDLE_HEIGHT / 2.0;
        _rightPaddleY = SCREEN_HEIGHT / 2.0 - PADDLE_HEIGHT / 2.0;
        
        _leftScore = 0;
        _rightScore = 0;
        _steps = 0;
        
        RenderScreen();
        return _screen;
    }
    
    public override (byte[] state, double reward, bool done, IDictionary<string, object> info) Step(int action)
    {
        _steps++;
        double reward = 0.0;
        
        // Player action (right paddle)
        switch (action)
        {
            case 2: // UP
                _rightPaddleY = Math.Max(0, _rightPaddleY - PADDLE_SPEED);
                break;
            case 3: // DOWN
                _rightPaddleY = Math.Min(SCREEN_HEIGHT - PADDLE_HEIGHT, _rightPaddleY + PADDLE_SPEED);
                break;
        }
        
        // AI opponent (left paddle) - simple AI
        double ballCenterY = _ballY + BALL_SIZE / 2.0;
        double leftPaddleCenterY = _leftPaddleY + PADDLE_HEIGHT / 2.0;
        
        if (ballCenterY < leftPaddleCenterY - 2)
            _leftPaddleY = Math.Max(0, _leftPaddleY - PADDLE_SPEED * 0.8); // Slightly slower AI
        else if (ballCenterY > leftPaddleCenterY + 2)
            _leftPaddleY = Math.Min(SCREEN_HEIGHT - PADDLE_HEIGHT, _leftPaddleY + PADDLE_SPEED * 0.8);
        
        // Ball physics
        _ballX += _ballVelX;
        _ballY += _ballVelY;
        
        // Ball collision with top/bottom walls
        if (_ballY <= 0 || _ballY >= SCREEN_HEIGHT - BALL_SIZE)
        {
            _ballVelY = -_ballVelY;
            _ballY = Math.Max(0, Math.Min(SCREEN_HEIGHT - BALL_SIZE, _ballY));
        }
        
        // Ball collision with paddles
        // Left paddle collision
        if (_ballX <= PADDLE_WIDTH && 
            _ballY + BALL_SIZE >= _leftPaddleY && 
            _ballY <= _leftPaddleY + PADDLE_HEIGHT)
        {
            _ballVelX = Math.Abs(_ballVelX); // Ensure ball goes right
            double relativeIntersectY = (_leftPaddleY + PADDLE_HEIGHT / 2.0) - (_ballY + BALL_SIZE / 2.0);
            _ballVelY = -relativeIntersectY * 0.1; // Add spin based on where ball hits paddle
        }
        
        // Right paddle collision
        if (_ballX + BALL_SIZE >= SCREEN_WIDTH - PADDLE_WIDTH && 
            _ballY + BALL_SIZE >= _rightPaddleY && 
            _ballY <= _rightPaddleY + PADDLE_HEIGHT)
        {
            _ballVelX = -Math.Abs(_ballVelX); // Ensure ball goes left
            double relativeIntersectY = (_rightPaddleY + PADDLE_HEIGHT / 2.0) - (_ballY + BALL_SIZE / 2.0);
            _ballVelY = -relativeIntersectY * 0.1; // Add spin based on where ball hits paddle
        }
        
        // Scoring
        if (_ballX < 0)
        {
            _rightScore++;
            reward = 1.0; // Agent scores
            ResetBall();
        }
        else if (_ballX > SCREEN_WIDTH)
        {
            _leftScore++;
            reward = -1.0; // Agent loses point
            ResetBall();
        }
        
        // Game termination
        bool done = _leftScore >= 21 || _rightScore >= 21 || _steps >= 10000;
        
        RenderScreen();
        
        var info = new Dictionary<string, object>
        {
            ["left_score"] = _leftScore,
            ["right_score"] = _rightScore,
            ["ball_x"] = _ballX,
            ["ball_y"] = _ballY
        };
        
        return (_screen, reward, done, info);
    }
    
    private void ResetBall()
    {
        _ballX = SCREEN_WIDTH / 2.0;
        _ballY = SCREEN_HEIGHT / 2.0;
        _ballVelX = _random.NextDouble() > 0.5 ? BALL_SPEED : -BALL_SPEED;
        _ballVelY = (_random.NextDouble() - 0.5) * 2.0 * BALL_SPEED;
    }
    
    private void RenderScreen()
    {
        // Clear screen (black background)
        Array.Fill(_screen, (byte)0);
        
        // Draw left paddle (white)
        DrawRectangle((int)0, (int)_leftPaddleY, PADDLE_WIDTH, PADDLE_HEIGHT, 255, 255, 255);
        
        // Draw right paddle (white)
        DrawRectangle(SCREEN_WIDTH - PADDLE_WIDTH, (int)_rightPaddleY, PADDLE_WIDTH, PADDLE_HEIGHT, 255, 255, 255);
        
        // Draw ball (white)
        DrawRectangle((int)_ballX, (int)_ballY, BALL_SIZE, BALL_SIZE, 255, 255, 255);
        
        // Draw center line (dashed)
        for (int y = 0; y < SCREEN_HEIGHT; y += 8)
        {
            DrawRectangle(SCREEN_WIDTH / 2 - 1, y, 2, 4, 255, 255, 255);
        }
        
        // Draw scores (simplified - just bright pixels)
        DrawScore(_leftScore, SCREEN_WIDTH / 4, 20);
        DrawScore(_rightScore, 3 * SCREEN_WIDTH / 4, 20);
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
    
    private void DrawScore(int score, int centerX, int y)
    {
        // Simple digit representation using pixels
        string scoreStr = score.ToString();
        int startX = centerX - (scoreStr.Length * 6) / 2;
        
        for (int i = 0; i < scoreStr.Length; i++)
        {
            DrawDigit(scoreStr[i], startX + i * 8, y);
        }
    }
    
    private void DrawDigit(char digit, int x, int y)
    {
        // Very simple 5x7 pixel font for digits
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
            Console.WriteLine($"Pong - Left: {_leftScore}, Right: {_rightScore}");
            Console.WriteLine($"Ball: ({_ballX:F1}, {_ballY:F1}) Velocity: ({_ballVelX:F1}, {_ballVelY:F1})");
        }
    }
    
    public override void Close()
    {
        // Nothing to clean up
    }
}
