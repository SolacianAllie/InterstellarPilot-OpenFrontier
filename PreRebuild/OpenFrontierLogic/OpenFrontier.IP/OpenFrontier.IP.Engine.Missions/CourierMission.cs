using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.MissionObjectives;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.MessageBox;

namespace OpenFrontier.IP.Engine.Missions
{
	public class CourierMission : Mission
	{
		public CargoBayItem CargoItem;

		public MissionObjective DeliverObjective;

		public Unit DestinationUnit;

		private bool hasPlayerPickedUpCargo;

		public MissionObjective PickupObjective;

		public Unit PickupUnit;

		public override MissionType MissionType => MissionType.Courier;

		public override bool ManualCompletionEnabled => true;

		public bool HasPlayerPickedUpCargo
		{
			get
			{
				return hasPlayerPickedUpCargo;
			}
			set
			{
				hasPlayerPickedUpCargo = value;
			}
		}

		public override bool IsValid
		{
			get
			{
				if (!base.IsValid)
				{
					return false;
				}
				if (!PickupObjective.IsComplete)
				{
					if (InvalidPickupUnit() || InvalidDestinationUnit())
					{
						return false;
					}
				}
				else if (InvalidDestinationUnit())
				{
					return false;
				}
				return true;
			}
		}

		public override void GetMissionOptions(List<MissionOption> options)
		{
			base.GetMissionOptions(options);
			if (!hasPlayerPickedUpCargo && Engine.PlayerRootUnit == PickupUnit && Engine.LocalUnit != PickupUnit)
			{
				options.Add(new MissionOption("Pickup Cargo", "MsgPickupCargo"));
			}
		}

		protected override void OnInit()
		{
			base.OnInit();
			if (OwnerFaction != null)
			{
				if (PickupUnit != null)
				{
					OwnerFaction.Intel.DiscoverUnit(PickupUnit);
				}
				if (DestinationUnit != null)
				{
					OwnerFaction.Intel.DiscoverUnit(DestinationUnit);
				}
			}
		}

		public override string CalculateTitle()
		{
			return $"{Title} - {CargoItem.Quantity}x {CargoItem.CargoClass.ClassName}";
		}

		public override bool CanPlayerManualComplete()
		{
			if (Engine.PlayerRootUnit == DestinationUnit && Engine.LocalUnit.Components.CargoBayComponent.HasCargo(CargoItem.CargoClass, CargoItem.Quantity))
			{
				return true;
			}
			return false;
		}

		public override void ManualComplete()
		{
			RemoveCargoFromPlayer();
			EngineASX.Instance.PlayCollectCargoAudio(EngineASX.Instance.LocalUnit.transform.position);
			DeliverObjective.Complete(success: true);
			base.ManualComplete();
		}

		private void RemoveCargoFromPlayer()
		{
			Engine.TransferToPlayerCargoWithMsg(CargoItem.CargoClass, -CargoItem.Quantity, ignoreCapacity: true);
		}

		private bool InvalidDestinationUnit()
		{
			if (!(DestinationUnit == null))
			{
				return !DestinationUnit.IsValidAndNotDestroyed;
			}
			return true;
		}

		private bool InvalidPickupUnit()
		{
			if (!(PickupUnit == null))
			{
				return !PickupUnit.IsValidAndNotDestroyed;
			}
			return true;
		}

		public override void AddWaypointsToList(List<PlayerWaypoint> waypoints)
		{
			if (hasPlayerPickedUpCargo)
			{
				if (DestinationUnit != null && DestinationUnit.IsValidAndNotDestroyed)
				{
					waypoints.Add(PlayerWaypoint.FromUnit(DestinationUnit));
				}
			}
			else if (PickupUnit != null && PickupUnit.IsValidAndNotDestroyed)
			{
				waypoints.Add(PlayerWaypoint.FromUnit(PickupUnit));
			}
		}

		public override string GetMissionObjectiveText(MissionObjective objective)
		{
			if (objective == PickupObjective)
			{
				return $"Pickup {TextFormattingHelper.FormatCargoAmount(CargoItem.Quantity)}x {CargoItem.CargoClass.GetShortNameElseLong()} at {GetFormattedUnitName(PickupUnit)} in the {UnityRichTextHelper.Color(PickupUnit.Sector.Name, GameController.Instance.GameSettings.TextSceneColor)} sector";
			}
			if (objective == DeliverObjective)
			{
				return $"Deliver {TextFormattingHelper.FormatCargoAmount(CargoItem.Quantity)}x {CargoItem.CargoClass.GetShortNameElseLong()} to {GetFormattedUnitName(DestinationUnit)} in the {UnityRichTextHelper.Color(DestinationUnit.Sector.Name, GameController.Instance.GameSettings.TextSceneColor)} sector";
			}
			return base.GetMissionObjectiveText(objective);
		}

		private string GetFormattedUnitName(Unit unit)
		{
			string text = UnityRichTextHelper.Color(unit.GetFriendlyName(), GameController.Instance.GameSettings.TextUnitNameColor);
			if (unit.Faction != null)
			{
				text = $"{text} ({unit.Faction.GetShortNameElseLong()})";
			}
			return text;
		}

		public void MsgPickupCargo()
		{
			if (Engine.LocalUnit.CargoBayComponent.GetFreeSpaceFor(CargoItem.CargoClass) >= CargoItem.Quantity)
			{
				hasPlayerPickedUpCargo = true;
				Engine.TransferToPlayerCargoWithMsg(CargoItem, ignoreCapacity: true);
				PickupObjective.Complete(success: true);
				EngineASX.Instance.PlayCollectCargoAudio(EngineASX.Instance.LocalUnit.transform.position);
			}
			else
			{
				UIController.Instance.ShowMessageBox("Insufficient cargo space", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
			}
		}
	}
}
