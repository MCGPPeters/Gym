using Xunit;
using Gymnasium;
using Gymnasium.Envs;

public class CartPoleTests
{
    [Fact]
    public void CartPole_ResetAndStep()
    {
        var env = new CartPole();
        var state = env.Reset();
        Assert.IsType<(float, float, float, float)>(state);
        for (int i = 0; i < 10; i++)
        {
            (state, var reward, var done, var info) = env.Step(1);
            Assert.InRange(reward, 0.0, 1.0);
            if (done) break;
        }
    }
}
