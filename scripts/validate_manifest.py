#!/usr/bin/env python3
"""Guard minimal runtime requirements for the fixed physical elite slot mod."""
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
SOURCE = ROOT / "GiveEliteItem"
manifest = json.loads((ROOT / "thunderstore/manifest.json").read_text(encoding="utf-8"))
entry = (SOURCE / "GiveEliteItem.cs").read_text(encoding="utf-8")
project = (SOURCE / "GiveEliteItem.csproj").read_text(encoding="utf-8")
sources = "\n".join(x.read_text(encoding="utf-8") for x in SOURCE.glob("*.cs"))
errors = []

version_match = re.search(r'const\s+string\s+PluginVersion\s*=\s*"([^"]+)"', entry)
version = version_match.group(1) if version_match else None
if manifest.get("name") != "PlayWithEliteItem":
    errors.append("Package name changed.")
if manifest.get("version_number") != version:
    errors.append("Manifest and code versions differ.")
if manifest.get("dependencies") != ["bbepis-BepInExPack-5.4.2122"]:
    errors.append("This version requires only BepInExPack directly.")
if not str(manifest.get("website_url", "")).startswith("https://"):
    errors.append("An HTTPS website is required.")
if not isinstance(manifest.get("description"), str) or not (1 <= len(manifest["description"]) <= 250):
    errors.append("Invalid Thunderstore description.")
if re.search(r"\bOn\.RoR2\.|\bIL\.RoR2\.", sources):
    errors.append("Equipment hooks reintroduced; re-evaluate HookGen dependency.")
if "MMHOOK.RoR2" in project:
    errors.append("Remove MMHOOK compilation reference when no hooks are used.")
if re.search(r"\busing\s+R2API\b|\bR2API\.", sources):
    errors.append("Unexpected R2API dependency.")
if errors:
    for error in errors:
        print("ERROR:", error, file=sys.stderr)
    sys.exit(1)
print(f"PASS: v{version}, physical slot 1, no equipment hooks or R2API.")
