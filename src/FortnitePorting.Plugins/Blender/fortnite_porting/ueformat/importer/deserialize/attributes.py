from __future__ import annotations

from collections.abc import Iterator

from ..archive.reader import FArchiveReader


def iter_attributes(ar: FArchiveReader) -> Iterator[tuple[str, FArchiveReader]]:
    count = ar.read_int()
    for _ in range(count):
        name = ar.read_fstring()
        byte_size = ar.read_int()
        yield name, ar.chunk(byte_size)
