#!/usr/bin/env bash
# Compile and run the engine-independent game-logic tests using the Mono
# toolchain bundled with the Unity editor. Requires NO Unity license.
set -euo pipefail

UNITY_ROOT="${UNITY_ROOT:-/opt/unity/6000.0.33f1/Editor}"
MONO_BIN="$UNITY_ROOT/Data/MonoBleedingEdge/bin"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

OUT_DIR="$(mktemp -d)"
OUT_EXE="$OUT_DIR/LogicTests.exe"

# Core sources are shared verbatim with the Unity project (no UnityEngine deps).
"$MONO_BIN/mcs" \
  -out:"$OUT_EXE" \
  "$REPO_ROOT"/Assets/Scripts/Core/*.cs \
  "$REPO_ROOT"/tools/LogicTests/Program.cs

"$MONO_BIN/mono" "$OUT_EXE"
