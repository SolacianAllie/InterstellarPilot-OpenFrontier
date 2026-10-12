using System;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Testing;
using OpenFrontier.IP.Testing.FactionControl;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class GodModeGeneralOptions : MonoBehaviour
	{
		public Button ForceEnterWormholeTargetButton;

		public Button ForceTargetFactionToRetireButton;

		public Button ForceShipsToUndockFromTargetButton;

		public Button MakePlayerFactionVisibleToNpcButton;

		public Button MakePlayerFactionInvisibleToNpcButton;

		public Button MakePlayerUnitInvulnerableButton;

		public Button MakePlayerUnitVulnerableButton;

		public Button EnablePermadeathButton;

		public Button DisablePermadeathButton;

		public Button ParalyseCurrentShipButton;

		public Button ForceEntryButton;

		public Button ForceDockWithTargetButton;

		public Button MakeCurrentUnitFasterButton;

		public Button MakeCurrentUnitSlowerButton;

		public Button TakeControlOfFactionAbandonExisting;

		public Button TakeControlOfFactionAutomateExisting;

		public Button LeaveFactionButton;

		public Button TakeControlOfAllSectorsButton;

		public Button TakeControlOfAllUnclaimedSectorsButton;

		private void Awake()
		{
			MakePlayerFactionVisibleToNpcButton.onClick.AddListener(MakePlayerFactionVisibleToNpcButtonClick);
			MakePlayerFactionInvisibleToNpcButton.onClick.AddListener(MakePlayerFactionInvisibleToNpcButtonClick);
			MakePlayerUnitInvulnerableButton.onClick.AddListener(MakePlayerUnitInvulnerableButtonClick);
			MakePlayerUnitVulnerableButton.onClick.AddListener(MakePlayerUnitVulnerableButtonClick);
			EnablePermadeathButton.onClick.AddListener(() =>
			{
				EngineASX.Instance.World.ScenarioOptions.Permadeath = true;
			});
			DisablePermadeathButton.onClick.AddListener(() =>
			{
				EngineASX.Instance.World.ScenarioOptions.Permadeath = false;
			});
			ParalyseCurrentShipButton.onClick.AddListener(() =>
			{
				UnitMiscUtils.ParalyseUnit(EngineASX.Instance.LocalUnit);
				UIController.Instance.ShowMessageBox("Current unit engine and power generator damaged.");
			});
			ForceEntryButton.onClick.AddListener(ForceEntryButtonClick);
			ForceDockWithTargetButton.onClick.AddListener(ForceDockWithTargetButtonClick);
			MakeCurrentUnitFasterButton.onClick.AddListener(MakeCurrentUnitFasterButtonClick);
			MakeCurrentUnitSlowerButton.onClick.AddListener(MakeCurrentUnitSlowerButtonClick);
			TakeControlOfFactionAbandonExisting.onClick.AddListener(() =>
			{
				TakeControlOfFactionClick(abandonExisting: true);
			});
			TakeControlOfFactionAutomateExisting.onClick.AddListener(() =>
			{
				TakeControlOfFactionClick(abandonExisting: false);
			});
			LeaveFactionButton.onClick.AddListener(LeaveFactionButtonClick);
			ForceShipsToUndockFromTargetButton.onClick.AddListener(ForceShipsToUndockFromTargetButtonClick);
			ForceTargetFactionToRetireButton.onClick.AddListener(ForceTargetFactionToRetireButtonClick);
			ForceEnterWormholeTargetButton.onClick.AddListener(ForceEnterWormholeTargetButtonClick);
			TakeControlOfAllSectorsButton.onClick.AddListener(TakeControlOfAllSectorsButtonClick);
			TakeControlOfAllUnclaimedSectorsButton.onClick.AddListener(TakeControlOfAllUnclaimedSectorsButtonClick);
		}

		private void Update()
		{
			ForceEnterWormholeTargetButton.interactable = EngineASX.Instance.Hud.CurrentTarget != null && EngineASX.Instance.Hud.CurrentTarget.UnitType == UnitType.Wormhole;
			ForceDockWithTargetButton.interactable = EngineASX.Instance.Hud.CurrentTarget != null && EngineASX.Instance.Hud.CurrentTarget.GetRootUnit() != EngineASX.Instance.PlayerRootUnit && EngineASX.Instance.Hud.CurrentTarget.IsDockable;
			ForceTargetFactionToRetireButton.interactable = EngineASX.Instance.Hud.CurrentTargetFaction != null && !EngineASX.Instance.Hud.CurrentTargetFaction.IsPlayerFaction;
			TakeControlOfFactionAbandonExisting.interactable = EngineASX.Instance.Hud.CurrentTargetFaction != null && EngineASX.Instance.Hud.CurrentTargetFaction != EngineASX.Instance.LocalFaction;
			TakeControlOfFactionAutomateExisting.interactable = EngineASX.Instance.Hud.CurrentTargetFaction != null && EngineASX.Instance.Hud.CurrentTargetFaction != EngineASX.Instance.LocalFaction;
			EnablePermadeathButton.interactable = !EngineASX.Instance.World.ScenarioOptions.Permadeath;
			DisablePermadeathButton.interactable = EngineASX.Instance.World.ScenarioOptions.Permadeath;
			ParalyseCurrentShipButton.interactable = EngineASX.Instance.LocalUnit != null && EngineASX.Instance.LocalUnit.IsShip();
			ForceEntryButton.interactable = EngineASX.Instance.Hud.CurrentTarget != null && EngineASX.Instance.Hud.CurrentTarget.Components != null && !EngineASX.Instance.Hud.CurrentTarget.IsPlayerCurrentUnit;
			MakePlayerUnitInvulnerableButton.interactable = EngineASX.Instance.LocalUnit != null && EngineASX.Instance.LocalUnit.Destructable != null && !EngineASX.Instance.LocalUnit.Destructable.IsInvulnerable;
			MakePlayerUnitVulnerableButton.interactable = EngineASX.Instance.LocalUnit != null && EngineASX.Instance.LocalUnit.Destructable != null && EngineASX.Instance.LocalUnit.Destructable.IsInvulnerable;
			MakePlayerFactionVisibleToNpcButton.interactable = EngineASX.Instance.LocalFaction.IsIgnoredByAI;
			MakePlayerFactionInvisibleToNpcButton.interactable = !EngineASX.Instance.LocalFaction.IsIgnoredByAI;
			Button makeCurrentUnitFasterButton = MakeCurrentUnitFasterButton;
			bool interactable = (MakeCurrentUnitSlowerButton.interactable = EngineASX.Instance.LocalUnit != null && EngineASX.Instance.LocalUnit.IsNormalShip());
			makeCurrentUnitFasterButton.interactable = interactable;
			ForceShipsToUndockFromTargetButton.interactable = EngineASX.Instance.Hud.CurrentTarget != null && EngineASX.Instance.Hud.CurrentTarget.IsDockable;
		}

		private void ForceEnterWormholeTargetButtonClick()
		{
			if (!EngineASX.Instance.TryEnterWormhole(EngineASX.Instance.Hud.CurrentTarget.WormholeComponent))
			{
				UIController.Instance.ShowMessageBox("Failed to enter target wormhole", MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
			}
		}

		private void ForceTargetFactionToRetireButtonClick()
		{
			Faction currentTargetFaction = EngineASX.Instance.Hud.CurrentTargetFaction;
			try
			{
				FactionRetirer.RetireFaction(currentTargetFaction);
				UIController.Instance.ShowMessageBox("Faction was retired", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
			}
			catch
			{
				UIController.Instance.ShowMessageBox("An error occured executing action", MessageBoxButtons.Ok, null, MessageBoxIcon.Error, "Retire faction");
			}
		}

		private void ForceShipsToUndockFromTargetButtonClick()
		{
			Unit currentTarget = EngineASX.Instance.Hud.CurrentTarget;
			if (currentTarget != null && currentTarget.IsDockable)
			{
				int num = currentTarget.Components.UndockAllDockedUnits();
				if (num > 0)
				{
					UIController.Instance.ShowMessageBox($"{num} ship(s) were undocked");
				}
				else
				{
					UIController.Instance.ShowMessageBox("No docked ships were found");
				}
			}
		}

		private void MakePlayerFactionVisibleToNpcButtonClick()
		{
			EngineASX.Instance.LocalFaction.IsIgnoredByAI = false;
		}

		private void MakePlayerFactionInvisibleToNpcButtonClick()
		{
			EngineASX.Instance.LocalFaction.IsIgnoredByAI = true;
		}

		private void MakePlayerUnitInvulnerableButtonClick()
		{
			EngineASX.Instance.PlayerUnit.Destructable.IsInvulnerable = true;
		}

		private void MakePlayerUnitVulnerableButtonClick()
		{
			EngineASX.Instance.PlayerUnit.Destructable.IsInvulnerable = false;
		}

		private void ForceEntryButtonClick()
		{
			if (EngineASX.Instance.Hud.CurrentTarget != null && EngineASX.Instance.Hud.CurrentTarget != EngineASX.Instance.LocalUnit)
			{
				EngineASX.Instance.ChangePlayerUnit(EngineASX.Instance.Hud.CurrentTarget);
				EngineASX.Instance.PlayChangeShipAudio();
			}
		}

		private void ForceDockWithTargetButtonClick()
		{
			if (!EngineASX.Instance.LocalUnit.CanDock)
			{
				UIController.Instance.ShowMessageBox("Current ship is not capable of docking");
			}
			else if (EngineASX.Instance.Hud.CurrentTarget != null && EngineASX.Instance.Hud.CurrentTarget != EngineASX.Instance.LocalUnit && EngineASX.Instance.Hud.CurrentTarget.IsDockable)
			{
				UnitHangarBay bestUnoccupiedBay = EngineASX.Instance.Hud.CurrentTarget.GetHangar().GetBestUnoccupiedBay(ShipHullType.Fighter);
				if (bestUnoccupiedBay != null)
				{
					EngineASX.Instance.LocalUnit.Components.DockedInHangarBay = bestUnoccupiedBay;
				}
				else
				{
					UIController.Instance.ShowMessageBox("Unable to find free docking bay at target");
				}
			}
		}

		private void MakeCurrentUnitFasterButtonClick()
		{
			float mass = EngineASX.Instance.LocalUnit.Mass;
			UnitMassUtils.ChangeUnitMassWithMultiplier(EngineASX.Instance.LocalUnit, 0.75f);
			UpdatedMassMessage(mass);
		}

		private static void UpdatedMassMessage(float previousMass)
		{
			if (previousMass != EngineASX.Instance.LocalUnit.Mass)
			{
				UIController.Instance.ShowMessageBox($"{EngineASX.Instance.LocalUnit.GetFriendlyName()} mass is now {EngineASX.Instance.LocalUnit.Mass:N2}", MessageBoxButtons.Ok);
			}
			else
			{
				UIController.Instance.ShowMessageBox("It is not possible to change unit mass further", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning, "Change unit mass");
			}
		}

		private void MakeCurrentUnitSlowerButtonClick()
		{
			float mass = EngineASX.Instance.LocalUnit.Mass;
			UnitMassUtils.ChangeUnitMassWithMultiplier(EngineASX.Instance.LocalUnit, 1.25f);
			UpdatedMassMessage(mass);
		}

		private void TakeControlOfFactionClick(bool abandonExisting)
		{
			if (EngineASX.Instance.Hud.CurrentTarget == null || EngineASX.Instance.Hud.CurrentTarget.Faction == null)
			{
				UIController.Instance.ShowError("No target faction found. Select a target in the HUD first.");
				return;
			}
			if (EngineASX.Instance.Hud.CurrentTarget.Faction == EngineASX.Instance.LocalFaction)
			{
				UIController.Instance.ShowError("Cannot switch control to current faction. Select a different target in the HUD.");
				return;
			}
			try
			{
				if (abandonExisting)
				{
					FactionControlUtil.SwitchLocalPlayerFactionToAndDestroyExisting(EngineASX.Instance.Hud.CurrentTarget.Faction);
				}
				else
				{
					FactionControlUtil.SwitchLocalPlayerFactionToAndAutomateExisting(EngineASX.Instance.Hud.CurrentTarget.Faction);
				}
				UIController.Instance.ShowMessageBox("Switched control to " + EngineASX.Instance.LocalFaction.GetLongNameElseShort(), MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
				UIController.Instance.ShowError("An error occured taking control of the target faction.");
			}
		}

		private void LeaveFactionButtonClick()
		{
			if (EngineASX.Instance.LocalFaction.GetValidShipAndStationCount() < 2)
			{
				UIController.Instance.ShowMessageBox("Leaving a faction requires more than one item of property", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
				return;
			}
			try
			{
				FactionControlUtil.LeaveCurrentPlayerFactionAndAutomate(out var oldPlayerFaction, out var _);
				UIController.Instance.ShowMessageBox("Successfully left faction. Old faction is now known as \"" + oldPlayerFaction.GetLongNameElseShort() + "\"", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
				UIController.Instance.ShowError("An error occured attempting to leave the current faction.");
			}
		}

		private void TakeControlOfAllSectorsButtonClick()
		{
			TakeControlOfSectors((Sector sector) => true, EngineASX.Instance.LocalFaction);
		}

		private void TakeControlOfAllUnclaimedSectorsButtonClick()
		{
			TakeControlOfSectors((Sector sector) => sector.ControllingFaction == null, EngineASX.Instance.LocalFaction);
		}

		private int TakeControlOfSectors(Func<Sector, bool> predicate, Faction faction)
		{
			int num = 0;
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				if (predicate(sector))
				{
					SectorControlUtils.ForceControlOfSector(sector, faction);
					num++;
				}
			}
			if (num > 0)
			{
				UIController.Instance.ShowMessageBox($"{num} sector(s) were claimed", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
			}
			else
			{
				UIController.Instance.ShowMessageBox("No sectors found to claim", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
			}
			return num;
		}
	}
}
