using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.MissionObjectives;

namespace OpenFrontier.IP.Engine.Missions
{
	public class DeliverShipMission : Mission
	{
		public MissionObjective DeliverShipObjective;

		public Unit Destination;

		public UnitClass UnitClass;

		public static string UnitClassEnvVar = "#UnitClass#";

		public override MissionType MissionType => MissionType.DeliverShip;

		public override bool ManualCompletionEnabled => true;

		public override bool IsValid
		{
			get
			{
				if (!base.IsValid)
				{
					return false;
				}
				if (Destination == null || !Destination.IsValidAndNotDestroyed)
				{
					return false;
				}
				return true;
			}
		}

		protected override void OnInit()
		{
			base.OnInit();
			if (OwnerFaction != null && Destination != null)
			{
				OwnerFaction.Intel.DiscoverUnit(Destination);
			}
		}

		public override bool CanPlayerManualComplete()
		{
			Unit currentUnit = Engine.LocalPlayer.Person.CurrentUnit;
			if (currentUnit != null && currentUnit.UnitClass == UnitClass)
			{
				return currentUnit.GetRootUnit() == Destination;
			}
			return false;
		}

		public override void ManualComplete()
		{
			Unit currentUnit = Engine.LocalPlayer.Person.CurrentUnit;
			MovePlayerToDock(currentUnit);
			GiveCreditsForShipsSale(currentUnit);
			currentUnit.SafeDestroy();
			base.ManualComplete();
			EngineASX.Instance.SetUIFromPlayerStatus();
		}

		private void MovePlayerToDock(Unit deliveredShip)
		{
			Engine.LocalPlayer.Person.CurrentUnit = deliveredShip.Components.DockUnit;
		}

		private void GiveCreditsForShipsSale(Unit deliveredShip)
		{
			int credits = CalculateShipRevenue(deliveredShip);
			Engine.AddCreditsToPlayerFactionWithMsg(credits, FactionTransactionType.ShipPurchase, Destination.Faction, Destination);
		}

		private static int CalculateShipRevenue(Unit deliveredShip)
		{
			return Maths.RoundUpToInt((float)deliveredShip.CalculateCurrentMoneyValue() * (1f + EngineASX.Instance.EconomySettings.TraderMarkupUpper), EngineASX.Instance.EconomySettings.ShipSaleRounding);
		}

		public override string GetMissionObjectiveText(MissionObjective objective)
		{
			if (objective == DeliverShipObjective)
			{
				return $"Deliver a {UnityRichTextHelper.Color(UnitClass.GetClassAndSeriesName(), GameController.Instance.GameSettings.TextUnitNameColor)} to {UnityRichTextHelper.Color(Destination.GetFriendlyName(), GameController.Instance.GameSettings.TextSceneColor)} in the {UnityRichTextHelper.Color(Destination.Sector.Name, GameController.Instance.GameSettings.TextSceneColor)} sector";
			}
			return base.GetMissionObjectiveText(objective);
		}

		public override string CalculateTitle()
		{
			return Title.Replace(UnitClassEnvVar, UnitClass.GetClassAndSeriesName());
		}

		public override void AddWaypointsToList(List<PlayerWaypoint> waypoints)
		{
			if (Destination != null && Destination.IsValidAndNotDestroyed)
			{
				waypoints.Add(PlayerWaypoint.FromUnit(Destination));
			}
		}
	}
}
