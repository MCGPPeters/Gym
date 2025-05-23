import sys
import os
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../Gym')))

from Gym.dummy_env import DummyEnv

def test_dummy_env():
    env = DummyEnv()
    state = env.reset()
    assert state == 0
    state, reward, done, info = env.step(1)
    assert state == 1
    assert reward == 1.0
    assert not done
    for _ in range(9):
        state, reward, done, info = env.step(1)
    assert done

def test_render():
    env = DummyEnv()
    env.reset()
    env.render()

def test_close():
    env = DummyEnv()
    env.close()
