from __future__ import annotations

import io
import struct
import numpy as np
import numpy.typing as npt
from typing import TYPE_CHECKING, BinaryIO, Literal, TypeVar, overload

from ..importer.utils import bytes_to_str
from ..importer.classes import EUEFormatVersion

if TYPE_CHECKING:
    from collections.abc import Callable
    from types import TracebackType

R = TypeVar("R")


class FArchiveReader:
    def __init__(self, data: bytes) -> None:
        self.data: BinaryIO = io.BytesIO(data)
        self.size = len(data)
        self.data.seek(0)
        self.file_version = EUEFormatVersion.BeforeCustomVersionWasAdded

    def __enter__(self) -> FArchiveReader:
        self.data.seek(0)
        return self

    def __exit__(
        self,
        exc_type: type[BaseException] | None,
        exc_value: BaseException | None,
        traceback: TracebackType | None,
    ) -> None:
        self.data.close()

    def eof(self) -> bool:
        return self.data.tell() >= self.size

    def read(self, size: int) -> bytes:
        return self.data.read(size)

    def read_to_end(self) -> bytes:
        return self.data.read(self.size - self.data.tell())

    def read_bool(self) -> bool:
        return struct.unpack("?", self.data.read(1))[0]

    def read_string(self, size: int) -> str:
        string = self.data.read(size)
        return bytes_to_str(string)

    def read_fstring(self) -> str:
        (size,) = struct.unpack("i", self.data.read(4))
        string = self.data.read(size)
        return bytes_to_str(string)

    def read_int(self) -> int:
        return struct.unpack("i", self.data.read(4))[0]

    def read_int_vector(self, size: int) -> tuple[int, ...]:
        if size <= 0:
            return ()
        return struct.unpack(str(size) + "I", self.data.read(size * 4))

    def read_short(self) -> int:
        return struct.unpack("h", self.data.read(2))[0]

    def read_byte(self) -> bytes:
        return struct.unpack("c", self.data.read(1))[0]

    def read_float(self) -> float:
        return struct.unpack("f", self.data.read(4))[0]

    @overload
    def read_float_vector(self, size: Literal[1]) -> tuple[float]: ...
    @overload
    def read_float_vector(self, size: Literal[2]) -> tuple[float, float]: ...
    @overload
    def read_float_vector(self, size: Literal[3]) -> tuple[float, float, float]: ...
    @overload
    def read_float_vector(self, size: Literal[4]) -> tuple[float, float, float, float]: ...
    @overload
    def read_float_vector(self, size: int) -> tuple[float, ...]: ...

    def read_float_vector(self, size: int) -> npt.NDArray[float]:
        return np.array(struct.unpack(str(size) + "f", self.data.read(size * 4)))

    def read_byte_vector(self, size: int) -> tuple[int, ...]:
        return struct.unpack(str(size) + "B", self.data.read(size))

    def skip(self, size: int) -> None:
        self.data.seek(size, 1)

    def read_bulk_array(self, predicate: Callable[[FArchiveReader], R]) -> list[R]:
        count = self.read_int()
        return self.read_array(count, predicate)

    def read_array(
        self,
        count: int,
        predicate: Callable[[FArchiveReader], R],
    ) -> list[R]:
        return [predicate(self) for _ in range(count)]

    def chunk(self, size: int) -> FArchiveReader:
        new_reader = FArchiveReader(self.read(size))
        new_reader.file_version = self.file_version
        
        return new_reader
