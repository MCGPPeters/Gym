using Xunit;
using Gymnasium;

public class DummyEnvTests
{
    [Fact]
    public void DummyEnv_BasicStepAndReset()
    {
        var env = new DummyEnv();
        var state = env.Reset();
        Assert.Equal(0, state);
        (state, var reward, var done, var info) = env.Step(1);
        Assert.Equal(1, state);
        Assert.Equal(1.0, reward);
        Assert.False(done);
        for (int i = 0; i < 9; i++)
        {
            (state, reward, done, info) = env.Step(1);
        }
        Assert.True(done);
    }

    [Fact]
    public void DummyEnv_RenderAndClose()
    {
        var env = new DummyEnv();
        env.Reset();
        env.Render();
        env.Close();
    }
}
