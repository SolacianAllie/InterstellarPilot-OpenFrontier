# M0 execution plan — assembly skeleton

*Written 2026-11-12 (execution session, after the design session that pinned
the architecture in `docs/engine-plan.md`). This file is the M0 HOW-TO and
the handover record for a fresh agent session: every decision below was
either pinned in the plan or agreed with the user on 2026-11-12. Companion
docs: `docs/engine-plan.md` (the spec — M0 bullet + re-homing map; do not
re-litigate), `docs/gamemode-modding-design.md`, `docs/new-ui-guide.md`,
`AGENTS.md` (traps — auto-loads).*

**Repo:** `/home/deck/Documents/InterstellarPilot-OpenFrontier` (fresh agent
sessions start in `/home/deck` — cd first). Branch `Dev`, up to date with
`origin/Dev`. Working tree at handover: clean except untracked `Thry/`
(repo root, `preset_cache.txt` — Thry Editor preset cache, editor debris;
stays untracked, never staged).

## Verified ground truth (do not re-derive; re-verify cheaply if paranoid)

- **Baseline harness run is GREEN**: `bash tools/harness/check.sh --regen` →
  2 projects (`Assembly-CSharp` 31 .cs, `Assembly-CSharp-Editor` 3 .cs),
  34 .cs covered, `ALL CHECKS PASSED`, `grep -c "error CS" Logs/Editor.log`
  = 0. An earlier red run this day was **stale generated set only**: the
  csproj dated 2026-11-11 20:19 predates 9 scripts (ActiveInput,
  InputSchemeTracker + 7 landing-boot batch files), so MenuScreen's
  `InputScheme` read CS0246 in the harness while the editor compiled clean.
  Lesson restated: `--regen` before trusting anything.
- **31 loose scripts** (the plan's "30" is stale by one): `MainMenu/` 20,
  `Common/` 4, `Gamemodes/` 2, `Input/` 2, `SpaceUnity/` 2, `NavLight.cs` 1.
  The per-folder re-homing map in the plan is exact.
- **Namespaces (untouched in M0)**: `OpenFrontier` (Input ×2 — ActiveInput,
  InputSchemeTracker; `InputScheme` enum is top-level in it),
  `OpenFrontier.Gamemodes` (×2), `OpenFrontier.IP.Common` (×4),
  `OpenFrontier.IP.Engine` (NavLight ×1), `OpenFrontier.IP.SpaceUnity.Nebulas`
  (NebulaAsset), `OpenFrontier.IP.SpaceUnity.Stars` (StarsAsset),
  `OpenFrontier.MainMenu` (×20).
- **Cross-assembly usage, proven by grep**: 5 MainMenu files
  (`LandingBoot`, `GamemodeList`, `GamemodeRow`, `GamemodeCard`,
  `StartGamemodeButton`) carry `using OpenFrontier.Gamemodes;`;
  `MenuScreen.cs` uses `ActiveInput`/`InputScheme` bare (parent-namespace
  resolution, no using). Nothing in `Input/`, `Common/`, `SpaceUnity/`,
  `Gamemodes/`, `NavLight.cs` references any MainMenu type. NebulaAsset +
  StarsAsset `using OpenFrontier.IP.Common;` — same assembly post-move, no
  reference needed. Pinned dependency direction holds empirically:
  **Content → nothing; Engine → Content; Menu → Engine + Content.**
- **Zero path-based references** to the moved scripts anywhere under
  `Assets/` (incl. the three `Assets/Editor` installers) or in the harness —
  `gen.py` discovers asmdefs by glob; checks are glob/namespace-based. No
  harness edits needed for M0.
- **`codec-test` / `sync.py` does not exist** (tree, all git history, disk).
  The working-agreement clause "regenerate codec-test's sync.py before
  trusting green" is a pre-blank-slate-migration leftover. **AGREED: skip
  it; name the skip in the commit/report.**
- Harness wording corrections for the record: the "LOUD-IDLE" state described
  in the plan predates the loose-script compile state — since the menu
  scripts landed, the harness has been compiling them as `Assembly-CSharp`
  (2-project strict-ish). M0's real flip is: one lump `Assembly-CSharp` →
  per-asmdef boundaries with `asmdef-refs.py`'s direct-reference gate active,
  and the loose `Assembly-CSharp` project ceases to exist (no loose scripts
  remain; the build-gate-idle branch is dead forever after).
- Editor scripts stay loose under `Assets/Editor/` (platform-callback trap —
  no `Editor/` subfolder inside any asmdef).

## The landing (Phase C) — final layout

```
Assets/OpenFrontier/Scripts/
  Engine/                       OpenFrontier.Engine.asmdef   (refs: Content)
    Input/                      ActiveInput.cs, InputSchemeTracker.cs
  Content/                      OpenFrontier.Content.asmdef  (refs: none)
    Gamemodes/                  GamemodeDefinition.cs, GamemodeRegistry.cs
    SpaceUnity/                 NebulaAsset.cs, StarsAsset.cs
    Common/                     NebulaBrightness, NebulaColour, NebulaStyle, StarsCount
    NavLight.cs                 (content component; awaits SetVisible driver)
  Menu/                         OpenFrontier.Menu.asmdef     (refs: Engine + Content)
    (the 20 MainMenu scripts, flat)
```

User decision on record: layout was left to the agent ("rebuild and format
everything as you need it"); this uniform three-root tree is the choice —
source subfolder identities preserved, `Menu/` flat, `NavLight.cs` at
Content root, namespaces untouched (zero-churn), no prefab/scene YAML
touched (bindings survive by GUID — `.meta` files travel with every move).

### Exact move sequence

```bash
cd /home/deck/Documents/InterstellarPilot-OpenFrontier/Assets/OpenFrontier/Scripts

# assembly roots (new folders; metas authored below)
mkdir Engine Content Menu

# wholesale folder moves — folder + sibling folder-meta both move (GUIDs kept)
git mv Input Engine/Input            && git mv Input.meta Engine/Input.meta
git mv Gamemodes Content/Gamemodes   && git mv Gamemodes.meta Content/Gamemodes.meta
git mv SpaceUnity Content/SpaceUnity && git mv SpaceUnity.meta Content/SpaceUnity.meta
git mv Common Content/Common         && git mv Common.meta Content/Common.meta

# NavLight to Content root
git mv NavLight.cs Content/NavLight.cs
git mv NavLight.cs.meta Content/NavLight.cs.meta

# the 20 MainMenu scripts, flat into Menu/
for f in ButtonSounds GamemodeCard GamemodeList GamemodeRow HologramShip \
         HoloSpinner LandingBoot LoadingShipPicker LoadingSpinner LoadingTips \
         MenuMusic MenuScreen OpenScreenButton QuitGameButton SafeAreaFitter \
         SceneFade SelectableImageColor SelectableTextColor StartGamemodeButton \
         VersionLabel; do
  git mv "MainMenu/$f.cs" "Menu/$f.cs"
  git mv "MainMenu/$f.cs.meta" "Menu/$f.cs.meta"
done
git rm -q MainMenu.meta        # the emptied MainMenu folder + its meta
```

### New files to author (9)

Three asmdefs at the assembly roots + one meta each + three folder metas
(`Engine.meta`, `Content.meta`, `Menu.meta`). House JSON shape (matches the
PreRebuild asmdefs; `rootNamespace: ""` — namespaces are untouched, so no
new-file template is implied):

```json
{
    "name": "OpenFrontier.Content",
    "rootNamespace": "",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- `OpenFrontier.Content.asmdef`: exactly the body above.
- `OpenFrontier.Engine.asmdef`: same body, `"references": ["OpenFrontier.Content"]`.
- `OpenFrontier.Menu.asmdef`: same body,
  `"references": ["OpenFrontier.Engine", "OpenFrontier.Content"]`.

Meta templates (fresh GUID per file — `python3 -c "import uuid;print(uuid.uuid4().hex)"`;
6 GUIDs total):

Folder meta (e.g. `Engine.meta`):

```
fileFormatVersion: 2
guid: <fresh>
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

`.asmdef` meta: the same `DefaultImporter` block without the
`folderAsset: yes` line. (Trailing spaces after `userData:` etc. are
Unity-canonical. If Unity rejects any authored meta on the restart import:
delete that meta and let Unity regenerate — safe for NEW files only,
nothing references their GUIDs yet. NEVER regenerate a meta of a MOVED
script.)

## Harness expectations — Phase D (first `--regen`, BEFORE the editor restart)

`bash tools/harness/check.sh --regen` → generates **4 projects**:

```
    OpenFrontier.Content                             9 .cs
    OpenFrontier.Engine                              2 .cs
    OpenFrontier.Menu                               20 .cs
    Assembly-CSharp-Editor                            3 .cs
```

Expected verdicts — 2 PASS, 2 FAIL, and the FAILs are EXPECTED staleness,
documented in AGENTS.md; do NOT chase them:

- `PASS OpenFrontier.Content` (no refs), `PASS OpenFrontier.Engine` (its
  Content reference is declared but Input code uses no Content types).
- `FAIL OpenFrontier.Menu` — CS0246 on `using OpenFrontier.Gamemodes;`:
  `Library/ScriptAssemblies/OpenFrontier.Content.dll` does not exist until
  Unity builds it (the stale-DLL class; the csproj was generated without
  the reference because `resolve_reference` found no DLL).
- `FAIL Assembly-CSharp-Editor` — same class: it needs
  `OpenFrontier.Menu.dll` for `MenuScreen` & co.
- `gen.py: WARNING unresolved asmdef reference: ...` lines are the same
  story in warning form.

This red is the signal to proceed to the editor step, NOT a bug. The
distinction that matters: a real error persists after the editor rebuilds;
staleness clears. (Same-day corollary learned at baseline: a stale
GENERATED SET also produces red — the fix there was this same `--regen`,
which is why the baseline is taken fresh before Phase C.)

## User gates (the only editor-side steps)

1. **Before Phase C: Unity editor CLOSED.** No live import may race the
   moves. (Fresh session: confirm with the user before landing anything.)
2. **Phase E: full Unity restart** after the tree is landed and Phase D is
   recorded. AGENTS.md precedent for large re-integration; the
   focus-in-while-importing segfault trap is exactly this shape. After the
   import settles the user eyeballs the Main Menu scene: no missing-script
   entries, screens intact (script GUIDs survived, so this should be a
   formality — but it is a formality that must be performed, not assumed).

## Phase F — green-strict verification

After the user confirms the editor is up and idle:

```bash
bash tools/harness/check.sh --regen   # regen AGAIN: picks up the now-real DLL refs
```

Green-strict means ALL of:

- `PASS` ×4 (`OpenFrontier.Content`, `OpenFrontier.Engine`,
  `OpenFrontier.Menu`, `Assembly-CSharp-Editor`), `4 projects, 34 .cs files`.
- Guard line `4 projects, 34 .cs files all covered` (count guard +
  every-`.cs`-covered guard).
- `asmdef-refs: 31 .cs, every project namespace used is declared` (the
  direct-reference gate now ACTIVE — it was idle pre-M0).
- `ALL CHECKS PASSED` (exit 0).
- `grep -c "error CS" Logs/Editor.log` → **0** (the user's agreed gate;
  Editor.log is the fresh log of the new editor session — run after import
  settles).
- Cheap sanity: `grep "Shader error" Logs/Editor.log` → empty (no shaders
  touched in M0; the grep is the standing rule).

If `Menu`/`Assembly-CSharp-Editor` are STILL red after the restart +
re-regen: that is no longer staleness — diagnose per AGENTS.md (defining
assembly compiles? same-namespace siblings resolve? missing `using` vs
stale DLL). Known blind spot: parent-namespace-resolution usage
(`MenuScreen` → `ActiveInput`) is invisible to `asmdef-refs.py` (it only
reads `using` statements) — the pinned Menu→Engine reference is what makes
it compile anyway; do not remove that reference because a check doesn't
demand it.

## Phase G — commit + push

- Stage `Assets/` paths only: `git add Assets/` (moves are already staged by
  `git mv`; this picks up the new asmdefs/metas). Sanity-check
  `git status --short`: `Thry/` must remain untracked (`?? Thry/`).
- Commit message draft (repo style — names what's whose):

```
M0 — assembly skeleton: three asmdefs + all scripts re-homed per the pinned map

Mine: OpenFrontier.{Engine,Content,Menu}.asmdef (+ metas, fresh GUIDs) under
Scripts/{Engine,Content,Menu}/; 31 scripts + metas re-homed (Input -> Engine/Input;
Gamemodes + SpaceUnity + Common + NavLight -> Content; MainMenu -> Menu flat;
folder metas moved with their folders — script GUIDs intact, prefab/scene
bindings survive). Namespaces untouched; Editor stays loose under Assets/Editor.
Harness: strict forever (first asmdefs landed); loose Assembly-CSharp project
retired; codec-test/sync.py does not exist anywhere (tree/history/disk) — the
"regenerate before green" clause is a pre-migration leftover, skipped.
Yours: nothing (tree was clean at handover; Thry/ preset cache stays untracked).
Verified: check.sh --regen 4/4 green-strict, asmdef-refs direct-gate active+green,
grep -c "error CS" Logs/Editor.log = 0 after your restart.
```

- `git push origin Dev` — never bare `git push`.

## Explicit don'ts

- Do not re-litigate the design — `docs/engine-plan.md`'s [2026-11-12] tags
  are the spec.
- Do not regenerate/edit any MOVED file's `.meta`; do not touch prefab or
  scene YAML; do not rename namespaces.
- Do not chase the Phase D staleness red; do not treat a Phase F red as
  staleness without the three-point diagnosis.
- Do not sweep `Thry/` into any commit; do not bare-push.
- Do not put platform build callbacks in an asmdef `Editor/` subfolder.
- Do not set Fixed Timestep now — that is an M1 user step (0.0333333 when
  M1 needs it).

## M1 pointer (next milestone — EngineCore; detail after M0 green)

Per the pinned plan: `EngineCore` persistent singleton spawned AT BOOT via
the LandingBoot seam; registries; net-stable 64-bit kind-tagged ID
allocation; event bus; pools; fixed-step 1/30 s scheduler with the tier
machine (FullFrame / SystemTraffic / PoiAmbient + resolver seam; NO
SystemSlow tier); four phases (Commands → Simulate → AI → Housekeeping);
per-render-frame RenderSync as the only gameplay transform writer;
timeScale protocol (0 = loading/paused, 1 = ready); double-precision sim
state + pinned-player architecture; sim/render snapshot decoupling; launch
payload (`GamemodeDefinition` → payload → scene boot); `StartGamemodeButton`
wired to it; dev gamemode `core.devtest` boots the bare stub scene (empty
skybox; engine boots; timeScale 0→1; proven by log/unscaled marker — zero
content authoring). User gates already known: Project Settings → Time →
Fixed Timestep 0.0333333; stub scene authoring + build-settings scene;
`core.devtest` Gamemode Definition asset (editor-authored); the
StartGamemodeButton wiring clicks; editor restarts at each compile gate.
