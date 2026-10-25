#!/usr/bin/env python3
"""Validate Thunderstore dependencies against actual runtime requirements."""
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "thunderstore" / "manifest.json"
PLUGIN = ROOT / "GiveEliteItem" / "GiveEliteItem.cs"

def validate():
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    source = PLUGIN.read_text(encoding="utf-8")
    errors = []
    match = re.search(r'const\s+string\s+PluginVersion\s*=\s*"([^"]+)"', source)
    version = match.group(1) if match else None
    if manifest.get("name") != "PlayWithEliteItem":
        errors.append("Keep the existing Thunderstore package name PlayWithEliteItem.")
    if manifest.get("version_number") != version:
        errors.append("Manifest version must match PluginVersion: " + str(version))
    if not isinstance(manifest.get("description"), str) or not (1 <= len(manifest["description"]) <= 250):
        errors.append("Description must be 1-250 characters.")
    if not str(manifest.get("website_url", "")).startswith("https://"):
        errors.append("HTTPS website_url required.")
    deps = manifest.get("dependencies")
    if not isinstance(deps, list):
        errors.append("dependencies must be an array.")
        deps = []
    if len(deps) != len(set(deps)):
        errors.append("Duplicate dependencies found.")
    if len(deps) != 1 or not re.fullmatch(r"bbepis-BepInExPack-\d+\.\d+\.\d+", deps[0] if deps else ""):
        errors.append("Expected only bbepis-BepInExPack as a direct dependency.")
    if any("R2API" in d for d in deps):
        errors.append("Unnecessary R2API dependency found.")
    if "R2API" in source:
        errors.append("Source now uses R2API; review runtime dependencies before release.")
    if errors:
        for err in errors:
            print("ERROR:", err, file=sys.stderr)
        return 1
    print("PASS: only one direct dependency (BepInExPack).")
    print("Note: BepInExPack installs its own requirements transitively.")
    print("Current local package candidate:", manifest["version_number"])
    return 0

if __name__ == "__main__":
    sys.exit(validate())
