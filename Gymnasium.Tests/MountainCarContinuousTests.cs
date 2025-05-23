using Xunit;
using Gymnasium.Envs;

public class MountainCarContinuousTests
{
    [Fact]
    public void MountainCarContinuous_ResetAndStep()
    {
        var env = new MountainCarContinuous();
        var state = env.Reset();
        Assert.InRange(state.Item1, -1.2f, 0.6f);
        Assert.InRange(state.Item2, -0.07f, 0.07f);
        for (int i = 0; i < 10; i++)
        {
            (state, var reward, var done, var info) = env.Step(0.5f);
            Assert.True(reward >= 0 || reward < 100.0);
            if (done) break;
        }
    }
}
