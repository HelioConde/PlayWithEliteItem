#!/usr/bin/env python3
"""Package a candidate release; never automatically upload to Thunderstore.

Reuse the owner's original public Thunderstore icon when no local copy exists.
"""
import argparse
import io
import json
import pathlib
import struct
import sys
import urllib.request
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "thunderstore" / "manifest.json"
DEFAULT_DLL = ROOT / "GiveEliteItem" / "bin" / "Release" / "netstandard2.1" / "GiveEliteItem.dll"
ICON = ROOT / "thunderstore" / "icon.png"
ORIGINAL_RELEASE = "https://thunderstore.io/package/download/CONDE/PlayWithEliteItem/1.0.1/"


def get_icon():
    if ICON.exists():
        icon = ICON.read_bytes()
    else:
        print("Using icon from the author's original PlayWithEliteItem 1.0.1 release...")
        try:
            request = urllib.request.Request(
                ORIGINAL_RELEASE,
                headers={"User-Agent": "PlayWithEliteItem-release-packager/1.0"},
            )
            with urllib.request.urlopen(request, timeout=30) as response:
                archive_bytes = response.read(5_000_000)
            with zipfile.ZipFile(io.BytesIO(archive_bytes)) as archived:
                names = [name for name in archived.namelist() if name.lower().endswith("/icon.png") or name.lower() == "icon.png"]
                if not names:
                    raise ValueError("The original package has no icon.png")
                icon = archived.read(sorted(names, key=lambda name: name.count("/"))[0])
        except Exception as error:
            raise RuntimeError(
                "Cannot retrieve the original icon. Copy icon.png from the old "
                "Thunderstore package into thunderstore/icon.png and retry."
            ) from error
    if not icon.startswith(b"\x89PNG\r\n\x1a\n"):
        raise ValueError("Icon must be PNG")
    width, height = struct.unpack(">II", icon[16:24])
    if width != 256 or height != 256:
        raise ValueError(f"Thunderstore icon must be 256x256 PNG; got {width}x{height}")
    return icon


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dll", type=pathlib.Path, default=DEFAULT_DLL)
    parser.add_argument("--output-dir", type=pathlib.Path, default=ROOT / "dist")
    args = parser.parse_args()

    if not args.dll.is_file():
        parser.error(f"Compiled DLL not found: {args.dll}")
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    version = manifest["version_number"]
    args.output_dir.mkdir(parents=True, exist_ok=True)
    target = args.output_dir / f"CONDE-PlayWithEliteItem-{version}-candidate.zip"
    assets = {
        "manifest.json": MANIFEST.read_bytes(),
        "README.md": (ROOT / "README.md").read_bytes(),
        "CHANGELOG.md": (ROOT / "thunderstore" / "CHANGELOG.md").read_bytes(),
        "icon.png": get_icon(),
        "GiveEliteItem.dll": args.dll.read_bytes(),
    }
    with zipfile.ZipFile(target, "w", zipfile.ZIP_DEFLATED) as out:
        for filename, payload in assets.items():
            out.writestr(filename, payload)
    print(f"Candidate package ready: {target}")
    print("NOT uploaded to Thunderstore; manual gameplay testing required.")


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        print(f"PACKAGING ERROR: {error}", file=sys.stderr)
        sys.exit(1)
