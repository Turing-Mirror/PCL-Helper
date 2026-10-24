"""Compare native icon, version and manifest resources without executing either EXE."""
import argparse
import struct

import pefile


def resources(path):
    result = {}
    with pefile.PE(path) as pe:
        for kind in pe.DIRECTORY_ENTRY_RESOURCE.entries:
            if kind.id not in (3, 14, 16, 24):
                continue
            for name in kind.directory.entries:
                for language in name.directory.entries:
                    entry = language.data.struct
                    key = (kind.id, str(name.name) if name.name else name.id, language.id)
                    result[key] = pe.get_data(entry.OffsetToData, entry.Size)
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--original", required=True)
    parser.add_argument("--patched", required=True)
    args = parser.parse_args()
    original, patched = resources(args.original), resources(args.patched)
    if not any(key[0] == 14 for key in original):
        raise AssertionError("Original has no native group icon")
    if original != patched:
        raise AssertionError("Native icon/version/manifest resources changed")
    sizes = set()
    for key, data in original.items():
        if key[0] == 14:
            count = struct.unpack_from("<H", data, 4)[0]
            for index in range(count):
                width, height = struct.unpack_from("BB", data, 6 + 14 * index)
                sizes.add((width or 256, height or 256))
    print(f"PASS native resources byte-for-byte: {len(original)} entries, icon sizes={sorted(sizes)}")


if __name__ == "__main__":
    main()
