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

## Shell mesh deletion (automated, done)

The 138 shell GameObjects are deleted from all 25 field-based prefabs by a
script driven by the units' own serialized shell lists (not GO names):
GO+transform+filter+renderer blocks removed, parents' children trimmed,
Thunder's LODGroup trimmed of its shell renderers (the other 16 groups
referenced only shells and were removed whole), shells dropped from
DrawRangeObjects, and `Cloak Renderer(s)` reset.

Editor re-save will lighten the files further (Unity keeps serialized
`CloakRenderers: []` lines until the prefab is touched; they are inert).

## Final code prune (done)

`ActiveUnit` reads no shell state any more: the `CloakRenderer` /
`CloakRenderers` fields, `FoldMaterialFromRenderer` and the shell-disabling
loops are gone; `FoldMaterialForSlot` is the hand-assigned `CloakMaterial`
(slot 0, `_CloakAmount`-gated) or nothing. `ShipDecals` paints hulls only.
The harness check now FAILS when a unit has no fold source, since none
besides the field exists. The per-ship `[CloakProbe]` line remains the
runtime witness.
