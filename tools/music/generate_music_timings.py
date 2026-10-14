#!/usr/bin/env python3
"""
Measures every music track and writes the timing table the game plays by.

Run:  python3 Tools/music/generate_music_timings.py
Then: the editor compiles the regenerated .cs and the numbers are live.

WHY THIS IS A SCRIPT AND NOT RUNTIME CODE
-----------------------------------------
The tracks have endings that were composed, not bolted on: Alioth drops hard
at about 1:05, settles, rises again, and only then fades to nothing. Asking
"where does this fade start" of a piece of music is a musical question, and
the only honest way to answer it for 86 files is to measure them all once and
write the answers down.

THE DETECTOR, AND WHY IT IS PEAK-RELATIVE
------------------------------------------
Three attempts, and the first two were wrong:

  1. Last bucket above 20% of the track's *median* level. Got Alioth's ending
     at 86s instead of 68s, because the ending dips, swells, and dips again.
  2. First index whose remaining mean exceeds a threshold. Returned 0.0s for
     68 of 70 tracks, because these tracks are dense and their average sits
     near their peak.
  3. This one. Smooth the envelope, take the peak as the median of the top
     10% of the smoothed values, threshold at half of that, and take the LAST
     index at or above it.

Referencing the *peak* rather than the *median* is the whole fix. An ending
re-enters the median range all the time - Alioth's late swell does exactly
that - but it never comes back to the peak. So "the last time this was as loud
as this track gets loud" is a question the music can answer, and 68s is the
answer.

Checked against a human: Alioth detects at 67.5s, and the person who owns
these tracks said it starts coming down at about 1:05. That agreement is the
only validation that matters, and it is why the numbers are checked by eye
once rather than trusted.

The generator also writes a plot of every track's envelope with the detected
point marked, so a wrong number is visible rather than silent.
"""

import os
import struct
import subprocess
import sys
import tempfile
import wave
import statistics as st

# Tools/music/generate_music_timings.py -> up three is the project root.
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
MUSIC = os.path.join(ROOT, "Assets", "OpenFrontier", "Resources", "Music")
OUT = os.path.join(
    ROOT,
    "Assets/OpenFrontierLogic/OpenFrontier.IP/OpenFrontier.IP.Engine.EngineMusic",
    "MusicTimingTable.g.cs")
PLOTS = os.path.join(ROOT, "music", "plots")

BUCKETS = 300
SMOOTH = 5
PEAK_FRACTION = 0.90      # top 10% of buckets define "as loud as this track gets"
THRESHOLD = 0.5          # of that peak

# The main menu track. Named, not inferred: it is the only mp3 in the tree,
# it sits loose in the music folder rather than in a category of its own, and
# the game already knew it as the menu track - EngineMusicPlayerController
# hardcoded "music_starbound" for the menu and played nothing else there.
# It LOOPS. The menu is not a place where a track ends and another begins.
MENU_TRACK_NAME = "Starbound"

# folder -> (C# category, family). Order matters only for readability.
CATEGORIES = {
    "Allied Space": ("Allied", "Allied"),
    "Anarchic Space": ("Anarchic", "Anarchic"),
    "Federal Space": ("Federal", "Federal"),
    "Imperial Space": ("Imperial", "Imperial"),
    "Neutral": ("Neutral", "Neutral"),
    "Superliminal": ("Hypersleep", "Hypersleep"),
    "Combat/Threat": ("Threat", "Combat"),
    "Combat/Engagement": ("Engagement", "Combat"),
}


def decode_to_wav(path):
    """Decode an mp3 to a temp wav so one envelope code can measure both.

    ffmpeg is used rather than a bundled decoder because Python has no mp3 in
    its standard library, and Starbound - the one track the menu must play -
    is an mp3. Measuring it by a different method than the other 85 would make
    its numbers incomparable, so it is decoded to wav first and measured by
    exactly the same code.
    """
    out = tempfile.NamedTemporaryFile(suffix=".wav", delete=False)
    out.close()
    subprocess.run(
        ["ffmpeg", "-y", "-v", "quiet", "-i", path, "-ac", "2", "-ar", "44100", out.name],
        check=True)
    return out.name


def envelope(path, buckets=BUCKETS):
    """Peak amplitude per bucket, normalised."""
    temporary = None

    if path.lower().endswith(".mp3"):
        temporary = decode_to_wav(path)
        path = temporary

    w = wave.open(path, "rb")
    n, sw = w.getnframes(), w.getsampwidth()
    step = max(1, n // buckets)
    out = []
    for b in range(buckets):
        w.setpos(b * step)
        raw = w.readframes(min(step, n - b * step))
        if not raw or sw != 2:
            out.append(0.0)
            continue
        v = struct.unpack("<%dh" % (len(raw) // 2), raw)
        out.append(max(abs(x) for x in v) / 32768.0)
    rate = w.getframerate()
    w.close()

    if temporary:
        os.unlink(temporary)

    m = max(out) or 1.0
    return [x / m for x in out], n / rate


def smooth_max(e, k):
    out, h = [], k // 2
    for i in range(len(e)):
        out.append(max(e[max(0, i - h):i + h + 1]))
    return out


def ending_onset(e, frac=THRESHOLD, smooth=SMOOTH):
    s = smooth_max(e, smooth)
    peak = st.median(sorted(s)[int(len(s) * PEAK_FRACTION):]) or max(s) or 1.0
    best = 0
    for i, v in enumerate(s):
        if v >= peak * frac:
            best = i
    return best / len(s)


def leading_silence(e, silence=0.012):
    for i, v in enumerate(e):
        if v > silence:
            return i / len(e)
    return 1.0


def classify(rel):
    """(category, family, resourcePath, isCombatSection, loops)"""
    parts = rel.split(os.sep)
    folder = "/".join(parts[:-1])
    name = parts[-1].rsplit(".", 1)[0]
    resource = "Music/" + rel.replace(os.sep, "/").rsplit(".", 1)[0]

    # The menu track is identified by name, before anything else, so it cannot
    # be reclassified by a folder it happens to sit in.
    if name == MENU_TRACK_NAME:
        return "Menu", "Menu", resource, False, True

    # Starport before folder. The starport tracks live INSIDE the space
    # folders - "Anarchic Space/Anarchic Starport.wav" - so testing the
    # folder first classified all four of them as ordinary space tracks,
    # and there was nothing left for the starport rule to find.
    #
    # Matched by exact name, not by prefix: the folder is "Anarchic Space"
    # and the file is "Anarchic Starport", so a startswith on the folder
    # name never fires and all four starports fell through to the folder.
    if name.endswith(" Starport"):
        for key, (_, fam) in CATEGORIES.items():
            if name == key.replace(" Space", " Starport"):
                return "Starport", fam, resource, False, False

    if folder in CATEGORIES:
        cat, fam = CATEGORIES[folder]
        return cat, fam, resource, cat in ("Threat", "Engagement"), False

    # Loose in the music folder root and not the menu track. Measured and kept,
    # but under a category nothing plays: picking at random from "Menu" would
    # put these on the title screen, which is Starbound's job and was never
    # right. Unassigned says they are waiting to be given a job rather than
    # pretending they already have one.
    return "Unassigned", "Unassigned", resource, False, False


def collect():
    rows = []
    for dirpath, _, files in os.walk(MUSIC):
        for f in sorted(files):
            if not f.lower().endswith((".wav", ".mp3")):
                continue
            full = os.path.join(dirpath, f)
            rel = os.path.relpath(full, MUSIC)
            env, dur = envelope(full)
            on = ending_onset(env)
            cat, fam, res, combat, loops = classify(rel)
            rows.append({
                "name": f[:-4],
                "rel": rel.replace(os.sep, "/"),
                "resource": res,
                "category": cat,
                "family": fam,
                "duration": dur,
                "onset": on * dur,
                "lead": leading_silence(env) * dur,
                "combat": combat,
                "loops": loops,
                "env": env,
                "thr": THRESHOLD,
            })
    rows.sort(key=lambda r: (r["category"], r["name"]))
    return rows


def write_table(rows):
    lines = []
    a = lines.append
    a("// GENERATED by Tools/music/generate_music_timings.py - do not edit by hand.")
    a("//")
    a("// Regenerate when the music changes. The numbers are measured from the")
    a("// audio: duration, and where each track's composed ending begins.")
    a("//")
    a("// The ending onset is the pinned transition point. When a track reaches it")
    a("// the music hands over to whatever the state machine says is current, and")
    a("// the outgoing track plays its own ending underneath. Nothing is cut.")
    a("//")
    a("// If a track is interrupted before that point - you undock halfway through")
    a("// a starport track - it is faded to silence over a set duration instead,")
    a("// because waiting out a 49 second ending is worse than cutting it.")
    a("")
    a("using System.Collections.Generic;")
    a("")
    a("namespace OpenFrontier.IP.Engine.EngineMusic")
    a("{")
    a("\tpublic static class MusicTimingTable")
    a("\t{")
    a("\t\t/// <summary>One measured track.</summary>")
    a("\t\tpublic sealed class Entry")
    a("\t\t{")
    a("\t\t\tpublic string Name;")
    a("\t\t\tpublic string ResourcePath;")
    a("\t\t\tpublic string Category;")
    a("\t\t\tpublic string Family;")
    a("\t\t\tpublic float Duration;")
    a("\t\t\tpublic float EndingOnset;")
    a("\t\t\tpublic float LeadingSilence;")
    a("\t\t\tpublic bool IsCombatSection;")
    a("\t\t\tpublic bool Loops;")
    a("\t\t}")
    a("")
    a(f"\t\t/// <summary>{len(rows)} tracks, measured from the audio.</summary>")
    a("\t\tpublic static readonly List<Entry> Tracks = new List<Entry>")
    a("\t\t{")

    def esc(s):
        return s.replace('"', '\\"')

    for i, r in enumerate(rows):
        a("\t\t\tnew Entry")
        a("\t\t\t{")
        a(f'\t\t\t\tName = "{esc(r["name"])}",')
        a(f'\t\t\t\tResourcePath = "{esc(r["resource"])}",')
        a(f'\t\t\t\tCategory = "{esc(r["category"])}",')
        a(f'\t\t\t\tFamily = "{esc(r["family"])}",')
        a(f'\t\t\t\tDuration = {r["duration"]:.3f}f,')
        a(f'\t\t\t\tEndingOnset = {r["onset"]:.3f}f,')
        a(f'\t\t\t\tLeadingSilence = {r["lead"]:.3f}f,')
        a(f'\t\t\t\tIsCombatSection = {"true" if r["combat"] else "false"},')
        a(f'\t\t\t\tLoops = {"true" if r["loops"] else "false"},')
        a("\t\t\t}" + ("," if i < len(rows) - 1 else ""))
    a("\t\t};")
    a("\t}")
    a("}")
    with open(OUT, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")


def write_plots(rows):
    os.makedirs(PLOTS, exist_ok=True)
    for r in rows:
        e = r["env"]
        bar = "".join(
            "#" if v >= r["thr"] else ("+" if v >= r["thr"] / 2 else ".")
            for v in e)
        mark = min(len(e) - 1, int(r["onset"] / r["duration"] * len(e)))
        marked = bar[:mark] + "^" + bar[mark + 1:]
        with open(os.path.join(PLOTS, r["name"].replace(" ", "_") + ".txt"), "w") as f:
            f.write(f"{r['rel']}\n")
            f.write(f"duration {r['duration']:.1f}s   ending onset {r['onset']:.1f}s"
                    f"   ({r['duration'] - r['onset']:.1f}s of ending)\n")
            f.write(f"threshold {r['thr']:.2f}   ^ = detected transition point\n\n")
            for i in range(0, len(marked), 100):
                f.write(f"{i / len(e) * r['duration']:6.1f}s |{marked[i:i + 100]}|\n")
            f.write("\n")


def main():
    if not os.path.isdir(MUSIC):
        print(f"no music folder at {MUSIC}")
        return 1
    rows = collect()
    if not rows:
        print("no tracks found")
        return 1
    write_table(rows)
    write_plots(rows)
    onsets = [r["onset"] for r in rows]
    endings = [r["duration"] - r["onset"] for r in rows]
    print(f"wrote {len(rows)} entries to")
    print("  " + os.path.relpath(OUT, ROOT))
    print(f"  {len(rows)} envelope plots to")
    print("  " + os.path.relpath(PLOTS, ROOT))
    print(f"\nending onset : min {min(onsets):6.1f}s  median {st.median(onsets):6.1f}s"
          f"  max {max(onsets):6.1f}s")
    print(f"ending length: min {min(endings):6.1f}s  median {st.median(endings):6.1f}s"
          f"  max {max(endings):6.1f}s")
    bad = [r for r in rows if r["onset"] < 5.0]
    if bad:
        print(f"\nWARNING: {len(bad)} track(s) have an ending onset under 5s, which"
              f" usually means the detector failed:")
        for r in bad:
            print(f"  {r['rel']}  onset {r['onset']:.1f}s of {r['duration']:.1f}s")
    return 0


if __name__ == "__main__":
    sys.exit(main())