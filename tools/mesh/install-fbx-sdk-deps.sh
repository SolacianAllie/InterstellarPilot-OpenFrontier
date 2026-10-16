#!/usr/bin/env bash
#
# Install the shared libraries Unity's FBX exporter needs on Arch Linux.
#
#   sudo bash tools/mesh/install-fbx-sdk-deps.sh
#
# WHY
#
# Unity's FBX exporter loads Library/PackageCache/com.autodesk.fbx@*/Editor/
# Plugins/UnityFbxSdkNative.so, which has:
#
#     NEEDED  libxml2.so.2
#
# Arch ships libxml2.so.16 (2.13+). The exporter therefore dies at load with
# DllNotFoundException: libxml2.so.2 -- visible in Logs/Editor.log at
# Autodesk.Fbx.NativeMethods..cctor -> ModelExporter.ExportObjects.
#
# WHY NOT JUST SYMLINK libxml2.so.2 -> libxml2.so.16
#
# Measured, and it does not work:
#
#   * the .so loads, and Arch's library exports all 31 xml* symbols the SDK
#     imports, so the naive check passes;
#   * but the SDK requires VERSIONED symbols (LIBXML2_2.4.30, LIBXML2_2.6.0)
#     and Arch's libxml2.so.16 carries no version definitions at all, so the
#     loader reports "no version information available" and those bindings
#     fail at first call. A load-time pass is not a working exporter.
#
# So this installs the genuine 2.9-era libxml2, whose version tags match, plus
# the ICU 60 it links against. Both are from the Ubuntu bionic archive, which
# is just a convenient source for old builds on a machine that has no dpkg.
#
# WHAT IT INSTALLS, INTO /usr/local/lib
#
#   libxml2.so.2.9.4   (soname libxml2.so.2, tags through LIBXML2_2.9.1)
#   libicuuc.so.60.2, libicui18n.so.60.2, libicudata.so.60.2
#
# Nothing already on the system binds libxml2.so.2 or libicu*.so.60, so
# adding these cannot shadow a library anything else is using.
#
# TO UNDO
#
#   sudo rm -f /usr/local/lib/libxml2.so.2* /usr/local/lib/libicu*.so.60*
#   sudo rm -f /etc/ld.so.conf.d/unity-fbx-deps.conf && sudo ldconfig

set -euo pipefail

PREFIX=/usr/local/lib
CONF=/etc/ld.so.conf.d/unity-fbx-deps.conf
SDK="$(ls -d "$PWD"/Library/PackageCache/com.autodesk.fbx@*/Editor/Plugins 2>/dev/null | head -1)/UnityFbxSdkNative.so"

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

red()   { printf '\033[31m%s\033[0m\n' "$*"; }
green() { printf '\033[32m%s\033[0m\n' "$*"; }
step()  { printf '\n\033[1m%s\033[0m\n' "$*"; }

[ "$(id -u)" = 0 ] || { red "run this with sudo"; exit 1; }
[ -f "$SDK" ] || { red "cannot find UnityFbxSdkNative.so under Library/PackageCache"; exit 1; }

step "fetching"
LIBXML2="http://archive.ubuntu.com/ubuntu/pool/main/libx/libxml2/libxml2_2.9.4+dfsg1-6.1ubuntu1_amd64.deb"
ICU="http://archive.ubuntu.com/ubuntu/pool/main/i/icu/libicu60_60.2-3ubuntu3.2_amd64.deb"
for url in "$LIBXML2" "$ICU"; do
  f="$TMP/$(basename "$url")"
  curl -sfL -o "$f" "$url" || { red "download failed: $url"; exit 1; }
  echo "  got $(basename "$f") ($(stat -c %s "$f") bytes)"
done

# Arch has no dpkg, but a .deb is just an ar archive containing a tarball.
step "extracting"
cd "$TMP"
for f in *.deb; do
  d="${f%.deb}"
  mkdir -p "$d" && (cd "$d" && ar x "../$f" && tar -xf data.tar.* )
  echo "  extracted $f"
done

step "installing into $PREFIX"
mkdir -p "$PREFIX"
# Copied explicitly rather than `cp -a <dir>/.` so nothing else from the
# package lands in the loader path.
cp -L "$TMP"/libxml2*/usr/lib/x86_64-linux-gnu/libxml2.so.2.9.4 "$PREFIX/"
ln -sf libxml2.so.2.9.4 "$PREFIX/libxml2.so.2"
for n in uc i18n data; do
  cp -L "$TMP"/libicu60*/usr/lib/x86_64-linux-gnu/libicu${n}.so.60.2 "$PREFIX/"
  ln -sf "libicu${n}.so.60.2" "$PREFIX/libicu${n}.so.60"
done
echo "  libxml2.so.2, libicuuc/libicui18n/libicudata .so.60"

# /etc/ld.so.conf on this box only has `include` lines, so /usr/local/lib is
# not searched unless it is named explicitly.
step "registering with the loader"
echo "$PREFIX" > "$CONF"
ldconfig
echo "  wrote $CONF"

step "verifying"
MISSING="$(ldd "$SDK" 2>/dev/null | grep -c 'not found' || true)"
WARN="$(ldd -v "$SDK" 2>&1 | grep -ci 'no version information' || true)"

if [ "$MISSING" != 0 ]; then
  ldd "$SDK" 2>/dev/null | grep 'not found' | sed 's/^/  /'
  red "  $MISSING dependency still unresolved"
  exit 1
fi
if [ "$WARN" != 0 ]; then
  red "  $WARN version-binding warning(s) -- the SDK would crash on first use"
  exit 1
fi

ldd "$SDK" 2>/dev/null | grep -E 'xml2|icu' | sed 's/^/  /'
green "  all dependencies resolve, no version warnings"
echo
green "Unity's FBX exporter will now load."
echo
echo "  ONE THING LEFT: Unity's exporter writes ASCII FBX BY DEFAULT, and"
echo "  Blender 5.x refuses ASCII ('ASCII FBX files are not supported')."
echo "  In the export window, switch Format from 'ASCII' to 'Binary'."
echo "  That setting persists per project."
echo
echo "  Verified against $(basename "$SDK")."