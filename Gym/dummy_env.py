import numpy as np
from .env import Env
from typing import Any, Tuple

class DummyEnv(Env):
    """A simple dummy environment for demonstration purposes."""
    def __init__(self):
        self.state = 0

    def reset(self) -> int:
        self.state = 0
        return self.state

    def step(self, action: int) -> Tuple[int, float, bool, dict]:
        self.state += action
        reward = float(self.state)
        done = self.state >= 10
        info = {}
        return self.state, reward, done, info

    def render(self, mode: str = "human") -> None:
        print(f"State: {self.state}")

    def close(self) -> None:
        pass
