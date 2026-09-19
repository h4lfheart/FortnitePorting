import time
from typing import ClassVar


class Log:
    INFO = "\u001b[36m"
    WARN = "\u001b[33m"
    ERROR = "\u001b[31m"
    RESET = "\u001b[0m"

    NoLog: bool = False

    timers: ClassVar[dict[str, float]] = {}

    @classmethod
    def info(cls, message: str) -> None:
        if not cls.NoLog:
            print(f"{cls.INFO}[UEFORMAT] {cls.RESET}{message}")  # noqa: T201

    @classmethod
    def warn(cls, message: str) -> None:
        if not cls.NoLog:
            print(f"{cls.WARN}[UEFORMAT] {cls.RESET}{message}")  # noqa: T201

    @classmethod
    def error(cls, message: str) -> None:
        if not cls.NoLog:
            print(f"{cls.ERROR}[UEFORMAT] {cls.RESET}{message}")  # noqa: T201

    @classmethod
    def time_start(cls, name: str) -> None:
        if not cls.NoLog:
            cls.timers[name] = time.time()

    @classmethod
    def time_end(cls, name: str) -> None:
        if cls.NoLog:
            return

        start_time = cls.timers.pop(name, None)

        if start_time is None:
            cls.error(f"Timer {name} does not exist")
        else:
            cls.info(f"{name} took {time.time() - start_time} seconds")
