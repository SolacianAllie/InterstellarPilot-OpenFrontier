# Assets — what is here, and what must not move

Three things live here, and the top level says which is which:

```
Assets/
  InterstellarPilot2/    the original game, exactly as it came out of extraction
  OpenFrontier/          this project's twelve assemblies
  NewAssets/             assets written or commissioned to replace the above
```

`InterstellarPilot2/` is the extraction dump. The tool wrote everything by
*asset type* rather than by what it is, which is why it has folders called
`Texture2D/`, `Mesh/` and `Sprite/` instead of `ships/`, `stations/` and
`music/`. `Scripts/Assembly-CSharp` inside it is the original game's own loose
scripts; the twelve assemblies beside `OpenFrontier/` are this project's.

That split is the point. A file is either the old game's or ours, and never
ambiguously both.

---

## Two folders must never be moved or renamed

This is the part worth knowing before anyone tidies up.

**`Assets/InterstellarPilot2/Resources/`** — 2,541 files, loaded **by path**, not by
GUID. Move anything inside it and the `Resources.Load` call that names it
stops finding it, silently, at runtime rather than at compile time. Seven call
sites, including sector data:

| Call site | Path it loads |
|---|---|
| `EngineResources.cs:37` | `Textures/Backgrounds/<name>` |
| `MainMenuWorldController.cs:256` | `Prefabs/WorldData/AsteroidTypes` |
| `EngineDialogEvents.cs:67` | `GameController.DialogEventsPath` |
| `SpaceConstructor.cs:56,67` | `SpaceUnity/Prefabs/NebulaAssets`, `StarsAssets` |
| `NebulaConstructor.cs:20`, `StarsConstructor.cs:14` | materials and textures by name |
| `RandomShipImage.cs:64` | `ThumbnailsPath` |
| `AvatarResourceLoader.cs:100`, `UnityObjectHelper.cs:43,115` | avatars and misc |

**`Assets/InterstellarPilot2/Scenes/`** — referenced **by path** in
`ProjectSettings/EditorBuildSettings.asset`. That file lists
`Assets/Scenes/ui/menu/LandingScene.unity` and its siblings as text, so moving
a scene drops it out of the build with no error anywhere.

---

## Everything else *can* be reorganised, with one rule

The type-named folders inside `InterstellarPilot2/` are referenced **only by
GUID**. Nothing loads them by name.

Unity identifies an asset by the GUID inside its `.meta` file, and keeps that
relationship wherever the file sits. So a folder can move and every reference
survives — **provided the `.meta` moves with it in the same breath.**

If the asset moves and its `.meta` does not, Unity notices a new file, invents
a new GUID, and every prefab, scene and material that pointed at the old one
now points at nothing. It fails **silently**: the inspector shows *Missing*,
and a prefab edited by hand will quietly drop a field. That has happened here
before — see `AGENTS.md` on prefabs.

So the rule for any future move:

```
git mv path/to/asset.ext path/to/newplace/asset.ext
git mv path/to/asset.ext.meta path/to/newplace/asset.ext.meta
```

Together, always, and then `bash /tmp/opencode/harness/check.sh`, which runs
the prefab null-reference check.

---

## Why the type-named folders were left alone

They could be regrouped by meaning — `Ships/`, `Stations/`, `World/`, `Music/`,
`UI/`, `VFX/` — and the split by old-versus-new is now clear enough to do it
inside `InterstellarPilot2/` without crossing the line.

It was not done because the filenames do not support it. `128mm_shell.png` is a
weapon, `AlienAshyGreenPlanetIcon.asset` is world data, `ArchiveIcon.asset` is
UI, and `Allie Logo.png` is branding — all in the same folder, all different
things. Anything that sorted those automatically would be guessing, and a
folder that is confidently wrong is worse than one that is merely full.

Per-file decisions, or a Unity tool that knows what each prefab references,
would do it properly. Until then the type-named layout stands on its own.

---

## Also here, and not assets

- `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/` — Unity's own working
  directories. Generated; never edit, never commit. They are in `.gitignore`.
- `Docs/` — plans and documentation. Outside `Assets/` on purpose, so Unity
  neither imports it nor writes `.meta` files next to it.

---

## The offline harness follows this layout

`/tmp/opencode/harness` compiles each assembly separately against the editor's
real DLLs, and it used to hardcode `Assets/Scripts/`. It now points at
`Assets/OpenFrontier/` and `Assets/InterstellarPilot2/Scripts/`, and the codec
tests' `sync.py` links from the new paths too. If the layout changes again,
those two files are what will silently report "0 problems across 0 files" —
which looks exactly like passing.