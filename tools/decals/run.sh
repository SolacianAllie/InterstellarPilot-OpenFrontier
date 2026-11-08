#!/usr/bin/env bash
# Runs a decals tool with the project's python venv if it exists (Pillow+
# numpy live there and the host system python is PEP-668 locked).
PY="$HOME/.venvs/decals/bin/python"
if [ -x "$PY" ]; then exec "$PY" "$@"; fi
exec python3 "$@"
