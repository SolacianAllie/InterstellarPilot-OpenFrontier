# Cloak material migration (one mesh set per ship)

Every unit now cloaks through **material state alone**. The legacy shell-toggle
routing is removed from `ActiveUnit.cs`; a hull is the only clothed mesh a ship
has. Two fold sources remain, checked in this order at init:

1. the hull's own authored material (already carries `_CloakAmount`), or
2. the hand-assigned **Cloak Material** on the ActiveUnit component (new
   serialized field; assigned below for every unit that needs it).

A hull that answers neither simply does not cloak. The authored shell meshes
inside the prefabs are now only a *material source* (read once at init, in the
matching-slot-count order) plus duplicated draw weight — safe to delete after
the field is filled, which is done for all 24 units.

## Assigned fields (shell-derived guids, verified against each prefab's own shells)

| Prefab | Fold material | Unsigned location |
|---|---|---|
| ActiveUnitAres | MaterialAresCloaked | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitBar2 | space_station_4_cloaked | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitCreon | MaterialCreonCloaked | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitDrake | MaterialDrakeCloaked | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitFactory | space_station_4_cloaked | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitFlyer | MaterialFlyerCloaked | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitHauler | MaterialHaulerCloaked | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitHornet | HornetCloakedMaterial | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitMagnus | MaterialMagnusCloaked | **`OpenFrontier/Material/`** |
| ActiveUnitMagnusPirateBlack | MaterialMagnusCloakedPirateBlack | **`OpenFrontier/Material/`** |
| ActiveUnitOverlord | MaterialOverlordCloaked | **`OpenFrontier/Material/`** |
| ActiveUnitPioneer | MaterialPioneerCloaked | **`OpenFrontier/Material/`** |
| ActiveUnitRanger | MaterialRangerCloaked | **`OpenFrontier/Material/`** |
| ActiveUnitRaptor | MaterialRaptorCloaked | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitRefinery | refinery_cloaked | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitShipyard | Shipyard_cloaked | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitShuttle | MaterialShuttleCloaked | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitSmallOutpost | space_station_tower_2_cloaked | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitSmallSatellite | MaterialSmallSatelliteCloaked | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitStorageLocker | ResourceContainerCloaked | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitThunder | MaterialThunderCloaked | **`OpenFrontier/Material/`** |
| ActiveUnitTradeStation | trade_station_cloaked | `InterstellarPilot2/Extracted/Material/` |
| ActiveUnitVenture | MaterialVentureCloaked | **`OpenFrontier/Material/`** |
| ActiveUnitWeaponsPlatform | MaterialWeaponsPlatformCloaked | `InterstellarPilot2/Extracted/Material/` |

## The name-twin trap

Several material names exist in BOTH trees as different assets (different
guids): `MaterialMagnusCloaked`, `MaterialMagnusCloakedPirateBlack`,
`MaterialMagnusStandard`, `MaterialMagnusStandardPirateBlack`,
`MaterialOverlord`, `MaterialOverlordCloaked`, `Moon1`–`Moon4`. The
OpenFrontier copy is the migrated/current one. Never fill the Cloak Material
field by picking a material from a Project-window search** — it can return the
IP2 twin. The census enforces the safe rule:

    python3 tools/harness/checks/cloak_fields.py

and runs automatically inside `tools/harness/check.sh` (exit 1 on any
misassigned field or non-fold field material). Every unit must come out
0 misassigned. Re-run it
after moving any prefab or material between trees — as long as `.meta` files
move together, guids survive the move and the field stays valid.

## Remaining prefab cleanup (editor, per ship)

1. Delete the duplicate `*Cloaked*` / `*Fade*` shell meshes (children plus their
   renderers).
2. Clear `Cloak Renderer(s)` on the ActiveUnit component (no dangling refs).
3. Keep `Cloak Material` filled — it now carries the fold going forward.

Then watch one play session's `[CloakProbe]` lines (editor-only): each unit
prints its hull renderers **with the material every slot now wears**, so a
silent adoption failure turns into an obvious `transposed False`.

### Known exceptions

- **Drake LOD5** was authored with the fold on *slot 1* of a two-slot far-LOD
  shell pair. The field covers slot 0 only; if Drakes show an unfolded hull at
  extreme range while cloaked, the LOD5 shell stays, or a second slot-1 field
  is added. The probe lines will say.
- **Orion** is now wired to its authored `orion_cloak` (`OpenFrontier/Material/`)
  as the hand-assigned field — its shells' `orion_transparent` carries an
  imported shader we cannot verify from source, so the field (never the shell)
  is Orion's fold source. Orion folds for the first time; verify the hull
  still looks right at amount 0 on the next play session.
- `ActiveUnitRangerTest.prefab` is an empty stub: nothing to do.

## Final code prune (after prefab cleanup)

Once every shell mesh is deleted and the lists cleared, one more commit
removes: the `CloakRenderer`/`CloakRenderers` serialized fields, the shell
reading in `FoldMaterialForSlot` (slot > 0 path), and ShipDecals' shell
painting. Until then that code stays: it is the fold source for hulls whose
field alone cannot cover every slot.
