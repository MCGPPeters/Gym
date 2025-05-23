using Xunit;
using Gymnasium.Envs;

public class MountainCarTests
{
    [Fact]
    public void MountainCar_ResetAndStep()
    {
        var env = new MountainCar();
        var state = env.Reset();
        Assert.InRange(state.Item1, -1.2f, 0.6f);
        Assert.InRange(state.Item2, -0.07f, 0.07f);
        for (int i = 0; i < 10; i++)
        {
            (state, var reward, var done, var info) = env.Step(1);
            Assert.True(reward <= 0);
            if (done) break;
        }
    }
}
