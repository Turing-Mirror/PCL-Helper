"""Independently validate downloaded Minecraft assets and Modrinth pack files."""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile


def verify(path, expected):
    if not path.is_file():
        raise AssertionError(f"Missing file: {path}")
    with path.open("rb") as stream:
        actual = hashlib.file_digest(stream, "sha1").hexdigest()
    if actual.lower() != expected.lower():
        raise AssertionError(f"SHA1 mismatch: {path}")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--minecraft", type=Path, required=True)
    parser.add_argument("--instance", required=True)
    parser.add_argument("--modpack", type=Path)
    args = parser.parse_args()
    instance = args.minecraft / "versions" / args.instance
    manifest = json.loads((instance / f"{args.instance}.json").read_text(encoding="utf-8-sig"))
    verify(instance / f"{args.instance}.jar", manifest["downloads"]["client"]["sha1"])
    index_id = manifest["assetIndex"]["id"]
    index = json.loads((args.minecraft / "assets" / "indexes" / f"{index_id}.json").read_text(encoding="utf-8-sig"))
    hashes = {entry["hash"] for entry in index["objects"].values()}
    for digest in hashes:
        verify(args.minecraft / "assets" / "objects" / digest[:2] / digest, digest)
    pack_files = 0
    if args.modpack:
        with zipfile.ZipFile(args.modpack) as archive:
            pack = json.loads(archive.read("modrinth.index.json"))
        for entry in pack["files"]:
            if entry.get("env", {}).get("client") == "unsupported":
                continue
            verify(instance / entry["path"], entry["hashes"]["sha1"])
            pack_files += 1
    print(f"PASS independent SHA1 validation: instance={args.instance}, client=1, assets={len(hashes)}, modpackFiles={pack_files}")


if __name__ == "__main__":
    main()
