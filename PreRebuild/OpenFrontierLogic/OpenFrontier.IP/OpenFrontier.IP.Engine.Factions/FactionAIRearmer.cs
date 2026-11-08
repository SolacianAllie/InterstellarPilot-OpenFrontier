using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionAIRearmer
	{
		private const float CheckForRearmFrequency = 2f;

		private float nextCheckForRearmTime;

		private RearmAtNearestSearchOperation currentRearmSearchOperation;

		private Fleet currentRearmFleet;

		private float currentRearmFleetSearchPriority;

		private FactionAIBase factionAI;

		private Faction faction;

		private int rearmFleetIndex = -1;

		public FactionAIRearmer(FactionAIBase factionAI)
		{
			this.factionAI = factionAI;
			faction = this.factionAI.Faction;
		}

		public void Update(float elapsedTime)
		{
			if (currentRearmSearchOperation != null)
			{
				if (currentRearmSearchOperation.HasFinished)
				{
					if (currentRearmSearchOperation.Result != null && CanRearmFleet(currentRearmFleet, currentRearmFleetSearchPriority))
					{
						OrderFleetToRearm(currentRearmFleet, currentRearmSearchOperation.Result, factionAI);
						EngineASX.Instance.DebugInfo.NumTimesFleetSendForRearming++;
					}
					currentRearmSearchOperation = null;
					currentRearmFleet = null;
				}
				else
				{
					currentRearmSearchOperation.Process(elapsedTime);
				}
			}
			else if (faction.Fleets.Count != 0 && Time.time > nextCheckForRearmTime && faction.IsAffordableConsideringReserve(2000))
			{
				nextCheckForRearmTime = Time.time + Random.Range(2f, 4f);
				rearmFleetIndex++;
				if (rearmFleetIndex >= faction.Fleets.Count)
				{
					rearmFleetIndex = 0;
				}
				Fleet fleet = faction.Fleets[rearmFleetIndex];
				if (!(fleet == null) && fleet.IsValid)
				{
					TryRearmFleet(fleet);
				}
			}
		}

		private void StartRearmLocationSearch(Fleet fleet, float priority)
		{
			currentRearmSearchOperation = new RearmAtNearestSearchOperation();
			currentRearmSearchOperation.ShipHullTypes = fleet.NpcShipHullTypes;
			currentRearmSearchOperation.LocalFleet = fleet;
			currentRearmSearchOperation.InitialiseAndStartSearch(fleet.Sector, fleet.GetHomeSectorOrCurrent(), fleet.SectorPosition, fleet.Faction, fleet.Settings.MaxJumpDistance);
			currentRearmFleet = fleet;
			currentRearmFleetSearchPriority = priority;
		}

		public bool TryRearmFleet(Fleet fleet)
		{
			if (CanRearmFleet(fleet, 0f) && EvaluateFleetNeedsRearm(faction, fleet, out var priority) && factionAI.CanOverrideFleetOrder(fleet, priority))
			{
				StartRearmLocationSearch(fleet, priority);
				return true;
			}
			return false;
		}

		public bool CanRearmFleet(Fleet fleet, float priority)
		{
			if (FactionAIBase.CanOrderFleet(fleet) && !factionAI.IsGroupActiveMercenaryGroup(fleet) && factionAI.CanRearmFleet(fleet))
			{
				return factionAI.CanOverrideFleetOrder(fleet, priority);
			}
			return false;
		}

		public static bool EvaluateFleetNeedsRearm(Faction faction, Fleet fleet, out float priority)
		{
			priority = 0f;
			if (!fleet.IsHomeBaseValid)
			{
				return false;
			}
			if (!faction.FactionAI.DoesFleetHaveRearmOrder(fleet))
			{
				float fleetEquipmentUsageWhenRearming = GetFleetEquipmentUsageWhenRearming(fleet);
				if (fleetEquipmentUsageWhenRearming > 0f)
				{
					float num = 0f;
					float num2 = 0f;
					float num3 = 0f;
					foreach (NpcPilot npcPilot in fleet.NpcPilots)
					{
						Unit currentUnit = npcPilot.CurrentUnit;
						if (currentUnit != null && currentUnit.IsValidAndNotDestroyed && currentUnit.Components.AnyTurretUsesAmmo && currentUnit.Components.CargoBayComponent != null)
						{
							float num4 = Mathf.Min(currentUnit.Components.CargoBayComponent.FreeSpace, fleetEquipmentUsageWhenRearming * currentUnit.Components.CargoBayComponent.Capacity - currentUnit.CargoBayComponent.EquipmentLoad);
							num += fleetEquipmentUsageWhenRearming;
							if (num4 > 2f && currentUnit.Components.CargoBayComponent.EquipmentLoad01 < fleetEquipmentUsageWhenRearming * GameController.Instance.GameSettings.RearmSettings.EquipmentRequiredThreshold)
							{
								num3 += (fleetEquipmentUsageWhenRearming - currentUnit.Components.CargoBayComponent.EquipmentLoad01) * currentUnit.Components.CargoBayComponent.Capacity * GameController.Instance.GameSettings.RearmSettings.EstimatedCostPerCargoUnit;
								num2 += currentUnit.Components.CargoBayComponent.EquipmentLoad01;
							}
							else
							{
								num2 += fleetEquipmentUsageWhenRearming;
							}
						}
					}
					if ((double)num <= 0.0)
					{
						return false;
					}
					if (!faction.IsAffordableConsideringReserve((int)num3))
					{
						return false;
					}
					priority = Mathf.Clamp01((1f - num2 / num) * 1.1f) * GameController.Instance.GameSettings.RearmSettings.MaxRearmPriority;
					return priority > GameController.Instance.GameSettings.RearmSettings.RearmPriorityThreshold;
				}
			}
			return false;
		}

		public static float GetFleetEquipmentUsageWhenRearming(Fleet fleet)
		{
			float preference = 0.5f;
			if (fleet.Faction.AISettings != null)
			{
				preference = fleet.Faction.AISettings.PreferenceToHaveAmmo;
			}
			return GetFleetEquipmentUsageWhenRearming(fleet, preference);
		}

		public static float GetFleetEquipmentUsageWhenRearming(Fleet fleet, float preference)
		{
			switch (fleet.FleetStrategy)
			{
			case FactionStrategy.Scavenge:
				return Mathf.Lerp(0.1f, 0.2f, preference);
			case FactionStrategy.Trade:
			case FactionStrategy.Mine:
			case FactionStrategy.PassengerTransport:
				return Mathf.Lerp(0.1f, 0.3f, preference);
			case FactionStrategy.BountyHunt:
				return Mathf.Lerp(0.5f, 0.9f, preference);
			case FactionStrategy.War:
			case FactionStrategy.Escort:
				return Mathf.Lerp(0.15f, 0.8f, preference);
			case FactionStrategy.DealEquipment:
				return 0f;
			default:
				return Mathf.Lerp(0.1f, 0.3f, preference);
			}
		}

		public static void OrderFleetToRearm(Fleet fleet, Unit location, FactionAIBase factionAI)
		{
			fleet.ClearOrders();
			float fleetEquipmentUsageWhenRearming = GetFleetEquipmentUsageWhenRearming(fleet);
			OrderRearmAtLocation(fleet, location, fleetEquipmentUsageWhenRearming);
			fleet.AssignNextOrder();
			factionAI.OnFleetOrdered(fleet);
		}

		private static void OrderRearmAtLocation(Fleet fleet, Unit location, float equipmentUsage)
		{
			ManualRearmOrder manualRearmOrder = UnityObjectHelper.NewGameObject<ManualRearmOrder>();
			manualRearmOrder.AllowCombatInterception = true;
			manualRearmOrder.PreferCloak = FleetOrderCloakPreference.OnlyIfAllCanCloak;
			manualRearmOrder.Priority = 0.8f;
			manualRearmOrder.EquipmentUsage = equipmentUsage;
			manualRearmOrder.SpecificRearmLocation = location;
			manualRearmOrder.MaxDuration = manualRearmOrder.AIDefaultMaxDuration;
			fleet.EnqueueOrder(manualRearmOrder);
		}
	}
}
