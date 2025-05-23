using Xunit;
using Gymnasium.Envs;

public class AcrobotTests
{
    [Fact]
    public void Acrobot_ResetAndStep()
    {
        var env = new Acrobot();
        var state = env.Reset();
        Assert.Equal(6, state.Length);
        for (int i = 0; i < 10; i++)
        {
            (state, var reward, var done, var info) = env.Step(1);
            Assert.True(reward <= 0);
            if (done) break;
        }
    }
}
