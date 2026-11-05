#!/usr/bin/env bash
# PARKED — kept only as documentation of an experiment that lost.
#
# Story (2026-11-05): ComfyUI + ROCm torch was installed (~/ComfyUI/{repo,.venv}),
# models downloaded, HIP init fine, memory blits fine — but ALL compute kernels
# silently produced garbage (2x2 ones-matmul = 0.0). Reproduced identically on
# host and in the box, across torch-rocm 7.2, 6.4 AND AMD's native-gfx1033
# TheRock nightly. Suspected SteamOS-kernel kfd scheduling, unfixable from
# userland. The AI engine is now stable-diffusion.cpp's Vulkan backend instead
# (~/sdcpp, see README.md) — ComfyUI is only needed if the Krita AI Diffusion
# plugin is ever wanted (it speaks ComfyUI only), pending an AMD kernel fix.
#
# The APU is gfx1033; these two exports are what make ROCm run on it.
set -e
BOX=Arch
export HSA_OVERRIDE_GFX_VERSION=10.3.0
export HSA_ENABLE_SDMA=0
exec distrobox enter "$BOX" -- "$HOME/ComfyUI/.venv/bin/python" \
     "$HOME/ComfyUI/repo/main.py" "$@" 2>&1 | grep -v -i "warning"
