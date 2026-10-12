# Assets — what is here, and what must not move

This folder is the product of two things living together: assets **extracted
from the original game**, and code and assets **written for this project**. The
extraction tool dumped everything by *asset type* — a folder of every texture, a
folder of every mesh — which is why the top level looks like
`Texture2D/`, `Mesh/`, `Sprite/` rather than `ships/`, `stations/`, `music/`.

`Assets/NewAssets/` is the replacement side of that line. See its README.

---

## The line

| | |
|---|---|
| **Extracted from the original game** | everything except `Scripts/OpenFrontier*` and `NewAssets/` |
| **Written for this project** | `Scripts/OpenFrontier*`, `NewAssets/`, `Editor/`, `Plugins/` |

The code tells the same story: `Scripts/OpenFrontier*` is this project's
decompiled-then-renamed assemblies, and `Scripts/Assembly-CSharp` is the
original game's loose scripts, untouched.

---

## Two folders must never be moved or renamed

This is the part worth knowing before anyone tidies up.

**`Assets/Resources/`** — 2,541 files, and it is loaded **by path**, not by
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

**`Assets/Scenes/`** — referenced **by path** in
`ProjectSettings/EditorBuildSettings.asset`. That file lists
`Assets/Scenes/ui/menu/LandingScene.unity` and its siblings as text, so moving
a scene drops it out of the build with no error anywhere.

---

## Everything else *can* be reorganised, with one rule

`Texture2D/`, `Mesh/`, `Sprite/`, `Material/`, `AudioClip/`, `Cubemap/`,
`Font/`, `MonoBehaviour/`, `PhysicMaterial/`, `TextAsset/`, `GameObject/` and
`LightingSettings/` are referenced **only by GUID**. Nothing loads them by
name.

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

## Suggested shape, if it is ever done

Grouping by what a thing *is* rather than what type it is, which is what the
original game's own scenes would have used:

```
Assets/Ships/         hulls, turrets, modules
Assets/Stations/      station pieces, hangar bays
Assets/World/         sectors, asteroids, nebulae, starfields
Assets/Music/  Assets/Sound/
Assets/UI/            sprites, fonts, icons
Assets/VFX/           effects, materials, shaders
```

Nothing about this is required for the game to work. It is only about finding
things, and it is a large diff for a large diff's sake — which is why it is
written down here rather than done in passing.

---

## Also here, and not assets

- `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/` — Unity's own working
  directories. Generated; never edit, never commit. They are in `.gitignore`.
- `Docs/` — plans and documentation. Outside `Assets/` on purpose, so Unity
  neither imports it nor writes `.meta` files next to it.