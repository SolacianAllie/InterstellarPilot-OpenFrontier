using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.UI.Screens.BuildMode;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Fleets.ActiveOrders
{
	public class ActiveBuildStationOrder : ActiveFleetOrder
	{
		public BuildStationOrder BuildStationOrder;

		public override bool IsValid
		{
			get
			{
				if (BuildStationOrder.UnitClass != null && BuildStationOrder.Sector != null)
				{
					return BuildStationOrder != null;
				}
				return false;
			}
		}

		protected override void resetTargetPosition()
		{
			base.resetTargetPosition();
			if (BuildStationOrder.Sector != null)
			{
				fleet.SetTargetToSectorPosition(this, BuildStationOrder.Sector, BuildStationOrder.SectorPosition);
				fleet.NavTarget.ArrivalThreshold = BuildStationOrder.UnitClass.ShieldRingRadius + 50f;
			}
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			if (fleet.Sector == BuildStationOrder.Sector && Vector3.Distance(fleet.SectorPosition, BuildStationOrder.SectorPosition) < 3000f)
			{
				if (!ValidateCanBuildAtSectorPosition(out var errorMessage))
				{
					OnInvalid(errorMessage);
					return;
				}
				UnitClass unitClass = BuildStationOrder.UnitClass;
				if (IsAffordableConsideringReserve(unitClass.SaleCost))
				{
					Unit unit = WorldHelper.SpawnUnitAndInstallComponents(unitClass.UnitPrefab, BuildStationOrder.Sector, BuildStationOrder.SectorPosition, 10f, addCargoLoadout: false, unitClass.UnitType != UnitType.Ship);
					unit.transform.localRotation = Geometry.RandomYRotation();
					unit.Faction = fleet.Faction;
					Faction faction = fleet.Faction;
					int creditsValue = -unitClass.SaleCost;
					UnitClass unitClass2 = BuildStationOrder.UnitClass;
					ICreditsSource creditsSource = CreditsSource;
					faction.ApplyTransaction(creditsValue, FactionTransactionType.StationBuild, null, null, null, unitClass2, FactionTransactionTaxType.None, null, creditsSource);
					EngineASX.Instance.NotifyNewStationConstructionStarted(unit);
					OnComplete();
				}
				else
				{
					switch (BuildStationOrder.InsufficientCreditsMode)
					{
					case InsufficientCreditsMode.Wait:
						OnIdle();
						break;
					case InsufficientCreditsMode.Abort:
						InvalidateFromInsufficientCredits();
						break;
					}
				}
			}
			else
			{
				ResetTargetPosition();
			}
		}

		public bool ValidateCanBuildAtSectorPosition(out string errorMessage)
		{
			errorMessage = null;
			if (!IsValid)
			{
				return false;
			}
			return BuildStationValidator.CanBuild(BuildStationOrder.UnitClass, BuildStationOrder.Sector, BuildStationOrder.SectorPosition, out errorMessage);
		}
	}
}
