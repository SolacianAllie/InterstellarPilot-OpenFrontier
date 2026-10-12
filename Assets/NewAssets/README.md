# NewAssets — replacements for the extracted assets

Everything else under `Assets/` came out of the original game. This folder is
the other side of that line: **assets written or commissioned for this project**,
put here so a replacement can be dropped in and swapped deliberately instead of
being edited on top of an extracted file in place.

## Why a separate folder rather than editing in place

An extracted asset has no history. Overwriting `Assets/Texture2D/hull_a.png`
with a new version loses the only copy of the original, and when the new one
turns out to be wrong there is nothing to go back to. Keeping both is one extra
file and it is the difference between "revert" and "start again".

## The subfolders

They mirror the type-named folders the extraction produced, because that is
where a replacement is most likely to be looked for:

| Folder | For |
|---|---|
| `Sprite/` | UI icons and images |
| `Texture2D/` | diffuse, normal, emission maps |
| `Material/` | materials that reference the textures above |
| `Mesh/` | meshes |
| `AudioClip/` | sound effects and music |
| `Font/` | fonts |
| `Cubemap/` | skyboxes |
| `PhysicMaterial/` | collision materials |
| `MonoBehaviour/` | ScriptableObjects and settings |

`Prefab/` and `Audio/` were considered and left out: a prefab is a composition
of the above, and there was no separate audio concept in the extracted layout
to mirror.

## How to use one

1. Drop the new asset in the matching folder. Unity writes its `.meta` and a
   GUID.
2. Open whatever referenced the old one and drag the new one in. The reference
   is by GUID, so nothing needs renaming and no code changes.
3. Once it is clearly better, the old asset can go. Until then, leave it.

## When the folder is empty, that is fine

An empty replacement folder with a README explaining why it exists is worth more
than one full of experiments. The point is that the *next* asset has somewhere
obvious to go.