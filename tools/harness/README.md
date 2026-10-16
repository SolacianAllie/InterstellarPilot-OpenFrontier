# Offline verification harness

Compiles the whole project and runs the checks that catch what a compiler
cannot, without opening the editor. Catches most issues in about 5 seconds.

```bash
bash tools/harness/check.sh            # build + check
bash tools/harness/check.sh --regen    # regenerate the csproj set first
HARNESS_JOBS=12 bash tools/harness/check.sh
```

## Why it is in the repo

It used to live in `/tmp/opencode/harness`, and was lost twice — to a power
cut on 2026-10-10, and to a `/tmp` clear. `/tmp` is not durable storage and
AGENTS.md already said so. Only `gen.py` and `check.sh` had to be rewritten,
because everything they need survives in the project: Unity's managed DLLs
under `~/Unity/Hub/Editor/6000.6.4f1/Editor/Data/Managed`, the package DLLs
under `Library/ScriptAssemblies`, and the vendored ones under
`Assets/InterstellarPilot2/Plugins`.

## Layout

| path | what it does |
|---|---|
| `gen.py` | generates one `.csproj` per `.asmdef` into `build/` |
| `check.sh` | generates, builds in parallel, runs the checks, prints a verdict |
| `checks/monoscript_check.py` | one MonoBehaviour per file, filename == class name |
| `checks/asmdef_refs.py` | a script must *declare* every project assembly it uses |
| `checks/prefab_yaml_check.py` | prefab structure: anchors, internal references, missing scripts |
| `checks/prefab_null_check.py` | advisory: null fields the script dereferences unguarded |

## How it is put together

**One csproj per asmdef, with that asmdef's real references.** Compiling
everything into everything cannot see the two error classes that have
actually bitten: a script using a type from an assembly it does not
reference, and a call that exists in a stub but not in the real engine
(`Resolution.refreshRateRatio.value` is a `double`).

**Unity's own response files are the reference list.** `gen.py` reads
`Library/Bee/artifacts/*/<assembly>.rsp`, which is exactly what Unity passes
to its compiler — including the `-define:` constants, without which `#if
UNITY_ANDROID` branches compile the wrong way.

Two details in there are not optional:

- **The editor's rsp, not a player's.** There are several per assembly, one
  per build graph. The Android ones carry `UNITY_ANDROID` and no
  `UNITY_EDITOR`, which includes `#if UNITY_ANDROID` blocks the editor never
  compiles and reports errors the editor does not have.
- **`.ref.dll` resolves on its stem.** Unity passes its own reference
  assemblies. Matching by basename misses nearly all of them, silently — a
  dropped reference just becomes a CS0246 on a type the editor compiles fine.

**References are unioned, not intersected.** The rsp can be older than the
source, so it is unioned with an ambient set (UnityEngine.UI, TextMeshPro,
InputSystem, URP) and the asmdef's own declarations. A csproj that is too
broad costs milliseconds; one that is too narrow fails to compile code the
editor compiles fine.

**There is no `ProjectReference` anywhere.** Each project references Unity's
DLLs out of `Library/ScriptAssemblies` rather than each other, so the
parallel build has no ordering to respect and `xargs -P 6` is safe.

## Known limit: a new type is invisible until Unity rebuilds

`Library/ScriptAssemblies/OpenFrontier.dll` is **Unity's** build, not the
harness's. Adding a new file to `OpenFrontier` and using it from
`OpenFrontier.IP` fails here with `CS0103` **even when the asmdef reference
is correct and the editor will compile it**. The DLL is stale.

Confirm before working around it — do not inline the shared code back into
the caller:

- is the association in the generated csproj at all? (`grep -oE '<Reference Include="X">' -A 3`)
- is the `HintPath` DLL older than the new file? (`ls -la`)
- `strings` that DLL for the type — is the *older sibling* type in it while the new one is absent? That is the signature of staleness.
- is there already code in the consumer assembly using that assembly? If yes, the direction is proven.

The editor rebuild clears it. `grep -c "error CS" Logs/Editor.log` is the
real confirmation.

## What it cannot catch

- **HLSL.** No shader compiles here. After an editor reimport:
  `grep "Shader error" Logs/Editor.log`.
- **A loop that never terminates.** `while (num > 0)` with nothing in the
  body changing `num` is valid C#. This has frozen the editor before the
  main menu. When editing a loop, put the counter next to the read, before
  any `continue`/`break`, and re-read the whole body afterwards.
- **Symbols the editor defines but we do not.** A clean run is necessary but
  not sufficient — check `grep -c "error CS" Logs/Editor.log` after the last
  import line.
- **Whether `_Lights.a` survives at runtime**, and every other asset-import
  question. Only the editor and a device answer those.

## Guards

`gen.py --check` runs first and fails loudly if either of these trips,
because a build that is trusted and wrong is worse than one that is broken
and ignored:

1. **the generated project count must match the `.asmdef` files on disk**
   (plus `Assembly-CSharp` and `Assembly-CSharp-Editor`). `gen.main()`
   *writes* the csproj files, so wrong paths regenerate projects that point
   at nothing, find fewer assemblies, and iterate over almost nothing. That
   happened: the harness reported ALL CHECKS PASSED for several commits
   having compiled **one project of fourteen**.
2. **every `.cs` under `Assets/` must be in some csproj.** A file in none was
   never compiled by anything — which is how a `Gizmos.DrawTriangle` (a
   method that does not exist) reached the editor.

Both were verified by breaking `gen.py` on purpose and watching them fail.
**If either fires, do not work around it** — the honest reading is that the
harness has stopped seeing your code.

## Checks are only worth what their false-positive rate allows

Every check here was verified by injecting a known failure and watching it
fail, because a check that cannot fail is indistinguishable from a check
that does not work. Three that did not, on the first attempt:

- `monoscript_check.py` bailed at `base not in bases` before reaching the
  `ROOTS` test, so every base chain stopped at `MonoBehaviour` and it found
  nothing.
- `asmdef_refs.py` attributed *parent* namespaces, so `using UnityEngine;`
  matched `namespace UnityEngine.UI.Extensions` and it reported **1173**
  findings led by "uses namespace UnityEngine from UnityUIExtensions". Its
  `using` regex also captured only the first dotted segment and then
  required `;`, so it matched almost nothing. And `owner()` required a
  trailing separator, skipping every `.cs` sitting directly in an asmdef
  root — 17 files.
- `prefab_yaml_check.py` stored the messages but a warm run `continue`d past
  them without printing, so a **failing** check reported "0 failures" on the
  second run. That is precisely the inversion the cache design warns about.

`prefab_null_check.py` is **advisory, not a gate** (exit 0; `--strict` to
gate). It reports 8 candidates. The reason it cannot gate: a field wired on
some *other* prefab of the same class is optional by design, and a field
assigned at runtime (`SectorMapForm.Sector` is a property with a setter, set
when the screen opens) is fine at null in a prefab. Judging that needs real
dataflow. Reporting it anyway and gating on it would train everyone to
ignore it, which is how the deleted `prefab-field-check.py` ended up crying
wolf about 1867 findings across 2084 prefabs and then catching nothing.

## Performance

About 5s warm, 15s cold. If it reads 70s again, one of these has been lost:

1. `yaml.load_all(text, Loader=CSafeLoader)` — same parser, compiled. 6.7s → 0.9s over 200 prefabs.
2. the prefab caches, keyed on `(mtime_ns, size)`: 12.9s → 142ms when nothing under `Assets/` was touched.
3. parallel builds, `xargs -P 6`, capped because the editor is usually open and starving it to save three seconds is a bad trade.
4. `--no-restore` on every build. NuGet restore runs whether anything changed or not: 853ms against 521ms.

## Rebuilding a check

`checks/*.py` are standalone and take no framework. Two rules learned the
hard way:

- **Test that a check can fail.** Inject the failure, watch it fire, remove
  it, watch it go quiet. A check that reports nothing is indistinguishable
  from a check that is not looking.
- **Never trust a count.** Read the full diff and confirm each finding
  against the file. Every finding in these checks was hand-verified, and the
  first draft of each had a bug that only showed up when the finding was
  checked against the actual prefab or script.