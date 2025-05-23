using Xunit;
using Gymnasium.Envs;

public class PendulumTests
{
    [Fact]
    public void Pendulum_ResetAndStep()
    {
        var env = new Pendulum();
        var state = env.Reset();
        Assert.Equal(3, state.Length);
        for (int i = 0; i < 10; i++)
        {
            (state, var reward, var done, var info) = env.Step(0.0f);
            Assert.True(reward <= 0);
            if (done) break;
        }
    }
}
