import abc
from typing import Any, Tuple

class Env(abc.ABC):
    """Abstract base class for all environments."""

    @abc.abstractmethod
    def reset(self) -> Any:
        pass

    @abc.abstractmethod
    def step(self, action: Any) -> Tuple[Any, float, bool, dict]:
        pass

    @abc.abstractmethod
    def render(self, mode: str = "human") -> Any:
        pass

    @abc.abstractmethod
    def close(self) -> None:
        pass
