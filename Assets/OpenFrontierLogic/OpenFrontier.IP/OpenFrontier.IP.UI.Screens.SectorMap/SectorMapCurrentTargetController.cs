using System;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI.Controls;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.IP.UI.Screens.Fleets;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.SectorMap
{
	public class SectorMapCurrentTargetController : MonoBehaviour
	{
		public delegate void SelectedObjectChangedHandler(SectorMapCurrentTargetController sender, SectorMapSelectionItem? newItem);

		public Sprite SelectedImageUnitSprite;

		public Sprite SelectedImagePositionSprite;

		public FactionContextButton SelectedFactionContextButton;

		public UnitNameAndIcon UnitDisplayButton;

		public UnitConditionControllerUI SelectedUnitConditionControllerUI;

		public SectorPositionContextButton SelectedSectorPositionContextButton;

		public SectorPositionPopupMenu SectorPositionPopupMenu;

		public FleetPopupMenu FleetPopupMenu;

		public UnitPopupMenu UnitPopupMenu;

		public Text SelectedUnitLabel;

		public GameObject SelectedUnitFactionRoot;

		public Text SelectedUnitFactionLabel;

		[FormerlySerializedAs("SelectedUnitSprite")]
		public Image SelectionImage;

		public SectorMap SectorMap;

		public Sector Sector;

		// Open Frontier: played when a unit is selected on the map (same clip
		// the 3D view's HudTargetChangeSoundPlay uses; the HUD screen is
		// deactivated while the map is open, so its watcher can't cover this).
		public AudioClip SelectUnitSound;

		private const string NoTargetText = "[None]";

		public Func<SectorMapSelectionItem, bool> OnSelecting;

		public bool EnabledSelectedUnitContextMenu = true;

		public bool SyncSelectionWithHud = true;

		public SectorMapSelectionItem? SelectedObject { get; set; }

		public Unit SelectedUnit
		{
			get
			{
				if (SelectedObject.HasValue)
				{
					return SelectedObject.Value.Unit;
				}
				return null;
			}
			set
			{
				if (value != null)
				{
					SelectedObject = SectorMapSelectionItem.FromUnit(value);
				}
				else
				{
					SelectedObject = null;
				}
			}
		}

		public Faction SelectedFaction
		{
			get
			{
				Unit selectedUnit = SelectedUnit;
				if (selectedUnit != null)
				{
					return selectedUnit.Faction;
				}
				return null;
			}
		}

		public EngineASX Eng => EngineASX.Instance;

		public bool IsSelectedObjectValid
		{
			get
			{
				if (!SelectedObject.HasValue)
				{
					return false;
				}
				if (SelectedObject.Value.HadUnit && !IsUnitValidForSelection(SelectedUnit))
				{
					return false;
				}
				return true;
			}
		}

		public bool IsSelectedSectorPosition
		{
			get
			{
				if (SelectedObject.HasValue)
				{
					return SelectedUnit == null;
				}
				return false;
			}
		}

		public event SelectedObjectChangedHandler SelectedObjectChanged;

		internal Fleet SelectedFleet()
		{
			Unit selectedUnit = SelectedUnit;
			if (selectedUnit != null)
			{
				return selectedUnit.GetFleet();
			}
			return null;
		}

		public bool TrySelect(SectorMapSelectionItem item)
		{
			if (OnSelecting != null && !OnSelecting(item))
			{
				return false;
			}
			SectorMapSelectionItem? selectedObject = SelectedObject;
			SelectedObject = item;
			RefreshContextButtons();
			RefreshSelectedConditionControllerUI();
			if (SyncSelectionWithHud && SelectedUnit != null && Sector.IsActive)
			{
				EngineASX.Instance.Hud.CurrentTarget = SelectedUnit;
			}
			// Open Frontier: target-select beep, matching the 3D view's
			// HudTargetChangeSoundPlay. The HUD screen is deactivated while
			// the map is open, so its watcher never covers map selections -
			// play unconditionally on an actual selection change, like the
			// HUD's watcher does for 3D-view target changes.
			if (SelectedUnit != null && SelectUnitSound != null && GameController.Instance.PlayButtonSounds && (!selectedObject.HasValue || selectedObject.Value.Unit != item.Unit || selectedObject.Value.SectorPosition != item.SectorPosition))
			{
				AudioHelper.PlaySound(SelectUnitSound);
			}
			Refresh();
			if (SelectedObjectChanged != null)
			{
				SelectedObjectChanged(this, SelectedObject);
			}
			return true;
		}

		private void RefreshContextButtons()
		{
			FleetPopupMenu.Fleet = ((SelectedUnit != null && SelectedUnit.IsOwnedByPlayer) ? SelectedUnit.GetFleet() : null);
			FleetPopupMenu.gameObject.SetActive(EnabledSelectedUnitContextMenu && FleetPopupMenu.Fleet != null && FleetsHelper.ShouldShowFleetContextMenu(SelectedUnit));
			FleetPopupMenu.Refresh();
			UnitPopupMenu.Unit = SelectedUnit;
			if (EnabledSelectedUnitContextMenu)
			{
				UnitDisplayButton.SetUnit(SelectedUnit);
			}
			else
			{
				UnitDisplayButton.SetUnit(null);
			}
			UnitPopupMenu.gameObject.SetActive(EnabledSelectedUnitContextMenu && SelectedUnit != null);
			Faction selectedFaction = SelectedFaction;
			SelectedFactionContextButton.SetFaction((EnabledSelectedUnitContextMenu && selectedFaction != null && !selectedFaction.IsPlayerFaction) ? selectedFaction : null);
			RefreshSelectedSectorTargetButton();
		}

		private void RefreshSelectedSectorTargetButton()
		{
			if (EnabledSelectedUnitContextMenu && IsSelectedSectorPosition)
			{
				SectorPositionPopupMenu.SectorTarget = SelectedObject.Value.ToSectorTargetPosition();
				SelectedSectorPositionContextButton.SetTarget(SelectedObject.Value.ToSectorTargetPosition());
			}
			else
			{
				SectorPositionPopupMenu.SectorTarget = null;
				SelectedSectorPositionContextButton.SetTarget(null);
			}
		}

		public void AutopickHudTarget()
		{
			if (Eng != null && Sector != null)
			{
				if (Eng.Hud != null && Eng.IsPlayerPilot && Sector.IsActive && IsUnitValidForSelection(Eng.Hud.CurrentTarget))
				{
					SelectedUnit = Eng.Hud.CurrentTarget;
				}
				Refresh();
			}
		}

		private bool IsUnitValidForSelection(Unit unit)
		{
			if (unit != null && unit.IsValidAndNotDestroyed && unit.IsRootUnit() && unit.Sector == Sector && SectorMap.ShouldShowUnitOnMap(unit, Sector))
			{
				return SectorMap.CanSelectUnit(unit);
			}
			return false;
		}

		public void Tick()
		{
			AutoDeselectObject();
			if (Sector != null)
			{
				UpdateSelectionImage();
				RefreshFactionRootEnabled();
			}
		}

		private void RefreshFactionRootEnabled()
		{
			SelectedUnitFactionRoot.SetActive(SelectedUnit != null && SelectedUnit.ShowWithOwnershipColours);
		}

		public void AutoDeselectObject()
		{
			if (SelectedObject.HasValue && !IsSelectedObjectValid)
			{
				SelectedObject = null;
				Refresh();
			}
		}

		private void UpdateSelectionImage()
		{
			if (!(SelectionImage == null))
			{
				bool flag = SelectedObject.HasValue && (SelectedUnit == null || SectorMap.ShowUnitAtCurrentZoomLevel(SelectedUnit));
				SelectionImage.enabled = flag;
				if (flag)
				{
					SelectionImage.sprite = ((SelectedUnit != null) ? SelectedImageUnitSprite : SelectedImagePositionSprite);
					UpdateSelectedUnitSpritePosition();
					UpdateSelectedUnitSpriteScale();
				}
			}
		}

		private void UpdateSelectedUnitSpriteScale()
		{
			float unitBracketsDrawSize = SectorMap.GetUnitBracketsDrawSize(SelectedUnit);
			SelectionImage.rectTransform.SetSize(new Vector2(unitBracketsDrawSize, unitBracketsDrawSize));
		}

		private void UpdateSelectedUnitSpritePosition()
		{
			SelectionImage.transform.localPosition = SectorMap.ConvertWorldToScreen(SelectedObject.Value.ActualWorldPosition);
		}

		public void Refresh()
		{
			UpdateSelectionImage();
			RefreshFactionRootEnabled();
			RefreshSelectedUnitLabel();
			RefreshSelectedFactionLabel();
			RefreshContextButtons();
			RefreshSelectedConditionControllerUI();
		}

		private void RefreshSelectedConditionControllerUI()
		{
			if (SelectedUnitConditionControllerUI != null)
			{
				bool flag = SelectedUnit != null && SelectedUnit.Destructable != null && SelectedUnit.UnitType != UnitType.Asteroid;
				SelectedUnitConditionControllerUI.gameObject.SetActive(flag);
				if (flag)
				{
					SelectedUnitConditionControllerUI.LocalUnit = SelectedUnit;
				}
			}
		}

		private void RefreshSelectedFactionLabel()
		{
			if (SelectedUnit != null)
			{
				if (SelectedUnit.Faction != null)
				{
					SelectedUnitFactionLabel.text = SelectedUnit.Faction.GetDescriptiveFactionNameIncludingPilotName(shortName: false);
				}
				else
				{
					SelectedUnitFactionLabel.text = "[None]";
				}
			}
			else
			{
				SelectedUnitFactionLabel.text = string.Empty;
			}
		}

		public void RefreshSelectedUnitLabel()
		{
			if (SelectedObject.HasValue)
			{
				if (SelectedUnit != null)
				{
					if (SelectedUnit.UnitType == UnitType.Ship)
					{
						SelectedUnitLabel.text = SelectedUnit.GetFriendlyNameForLocalFaction();
					}
					else
					{
						SelectedUnitLabel.text = SelectedUnit.GetFriendlyNameForLocalFaction();
					}
				}
				else
				{
					SelectedUnitLabel.text = UnitExtensions.GetSectorLocationText(SelectedObject.Value.SectorPosition);
				}
			}
			else
			{
				ShowNoTargetText();
			}
		}

		private void ShowNoTargetText()
		{
			SelectedUnitLabel.text = "[None]";
		}

		public void ClearSelection()
		{
			SelectedObject = null;
			Refresh();
		}
	}
}
