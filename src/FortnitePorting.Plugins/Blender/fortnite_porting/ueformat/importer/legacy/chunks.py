from __future__ import annotations

from collections.abc import Iterator

from ..archive.reader import FArchiveReader


def iter_chunks(ar: FArchiveReader) -> Iterator[tuple[str, int, FArchiveReader]]:
    """Yield (name, array_size, archive) for each Name/ArraySize/ByteSize section.

    After the caller returns from a yield, the archive is seeked to the end of
    that section so leftover bytes cannot desync the next header.
    """
    while not ar.eof():
        name = ar.read_fstring()
        array_size = ar.read_int()
        byte_size = ar.read_int()
        pos = ar.data.tell()
        yield name, array_size, ar
        ar.data.seek(pos + byte_size, 0)
