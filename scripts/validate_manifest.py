#!/usr/bin/env python3
"""Validate Thunderstore metadata and keep runtime requirements minimal."""

import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "thunderstore" / "manifest.json"
SOURCE_DIR = ROOT / "GiveEliteItem"
ENTRYPOINT = SOURCE_DIR / "GiveEliteItem.cs"


def validate():
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    source = ENTRYPOINT.read_text(encoding="utf-8")
    all_sources = "\n".join(
        path.read_text(encoding="utf-8") for path in sorted(SOURCE_DIR.glob("*.cs"))
    )
    project = (SOURCE_DIR / "GiveEliteItem.csproj").read_text(encoding="utf-8")
    errors = []

    match = re.search(r'const\s+string\s+PluginVersion\s*=\s*"([^"]+)"', source)
    version = match.group(1) if match else None

    if manifest.get("name") != "PlayWithEliteItem":
        errors.append("Keep the published package name PlayWithEliteItem.")
    if manifest.get("version_number") != version:
        errors.append(f"Manifest version must match PluginVersion ({version}).")
    if not isinstance(manifest.get("description"), str) or not (
        1 <= len(manifest["description"]) <= 250
    ):
        errors.append("Description must have 1–250 characters.")
    if not str(manifest.get("website_url", "")).startswith("https://"):
        errors.append("An HTTPS website_url is required.")

    dependencies = manifest.get("dependencies")
    if not isinstance(dependencies, list):
        errors.append("Dependencies must be an array.")
        dependencies = []

    if len(dependencies) != len(set(dependencies)):
        errors.append("Duplicate dependencies.")
    if (
        len(dependencies) != 1
        or not re.fullmatch(
            r"bbepis-BepInExPack-\d+\.\d+\.\d+",
            dependencies[0] if dependencies else "",
        )
    ):
        errors.append("Declare only bbepis-BepInExPack as a direct runtime dependency.")

    if re.search(r"\busing\s+R2API\b|\bR2API\.", all_sources):
        errors.append("R2API code detected; review the runtime dependency list.")
    if re.search(r'<PackageReference\s+Include="R2API', project):
        errors.append("R2API package reference detected; review the dependency list.")
    if any("R2API" in dependency for dependency in dependencies):
        errors.append("R2API is not an allowed direct dependency.")

    if errors:
        for error in errors:
            print("ERROR:", error, file=sys.stderr)
        return 1

    print(f"PASS: version {version}; one direct dependency (BepInExPack).")
    print("Note: BepInExPack still has transitive requirements.")
    return 0


if __name__ == "__main__":
    sys.exit(validate())
