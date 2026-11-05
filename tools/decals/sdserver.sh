#!/usr/bin/env bash
# sd-server with a cgroup memory ceiling (Steam Deck hardening).
#
# Without this, a big-window generation (1024²-ish) can exhaust the 16 GB
# shared APU memory while the browser/Unity hold their own share, and the
# kernel OOM-killer freezes the whole desktop session (happened 2026-11-05:
# "Failed to allocate pinned memory" → SIGKILL → frozen UI).
# Inside this user-scope, a runaway run aborts the SERVER process only —
# the desktop and everything else keep living.
#
# Caps sized for the Deck working set: model ~1.8 GB resident +
# 512² working set ≈ 3 GB; soft throttle 4.5 GB; hard kill 5.25 GB;
# zram use for the unit capped at 512 MB so even thrashing stays local.
#
# Usage: tools/decals/sdserver.sh [extra sd-server args]
#   model dir, port (default 1234) live under ~/sdcpp/models & sd-server.
#   Kill & restart freely; the UI at http://127.0.0.1:1234 reconnects.
set -eu
DIR="${HOME}/sdcpp"

exec systemd-run --user --collect --scope \
  -p "Description=sd-server decal painter" \
  -p "MemoryHigh=4608M" \
  -p "MemoryMax=5256M" \
  -p "MemorySwapMax=512M" \
  "$DIR/sd-server" --model-dir "$DIR/models" "$@"
