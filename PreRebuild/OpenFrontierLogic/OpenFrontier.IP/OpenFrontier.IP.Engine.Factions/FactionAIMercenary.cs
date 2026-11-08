using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionAIMercenary : FactionAIBase
	{
		private static List<Faction> tempFactionCache = new List<Faction>();

		private UnitSearchOperation searchForFriendlyStationOperation;

		private float lastTimeRaisedSalesPitchDialog;

		public const int DefaultHireHours = 3;

		public override FactionAIType AIType => FactionAIType.Mercenary;

		protected override void AssignOrdersToWarFleet(Fleet fleet)
		{
			if (MercenaryHireInfo != null)
			{
				Faction hiringFaction = MercenaryHireInfo.HiringFaction;
				if (hiringFaction != null && !TryAssignFleetToProtectHiringPerson(fleet, hiringFaction.LeaderPerson) && hiringFaction.Fleets.Count > 0)
				{
					Fleet random = hiringFaction.Fleets.GetRandom();
					if (random != null && random.IsMobile && random.Ships.Count > 0)
					{
						TryAssignFleetToProtectHiringPerson(fleet, random.Ships[0].PilotPerson);
					}
				}
			}
			else if (searchForFriendlyStationOperation == null)
			{
				if (Random.value < 0.5f)
				{
					StartSearchForFriendlyStation(fleet);
					return;
				}
				WaitOrder waitOrder = UnityObjectHelper.NewGameObject<WaitOrder>();
				waitOrder.WaitTime = Random.Range(600f, 1200f);
				fleet.EnqueueOrder(waitOrder);
				fleet.AssignNextOrder();
				OnFleetOrdered(fleet);
			}
		}

		public override void SetFleetSettings(Fleet fleet)
		{
			base.SetFleetSettings(fleet);
			if (MercenaryHireInfo == null)
			{
				fleet.Settings.PreferCloak = false;
			}
		}

		protected override bool CanClaimUnit(Unit scanned)
		{
			if (MercenaryHireInfo != null)
			{
				return false;
			}
			return true;
		}

		public bool TryAssignFleetToProtectHiringPerson(Fleet mercenaryFleet, Person hiringPerson)
		{
			if (hiringPerson == null || hiringPerson.CurrentUnit == null || hiringPerson.Faction == faction || hiringPerson.CurrentUnit.Faction != hiringPerson.Faction)
			{
				return false;
			}
			mercenaryFleet.ClearOrders();
			ProtectOrder protectOrder = UnityObjectHelper.NewGameObject<ProtectOrder>();
			protectOrder.MaxJumpDistance = -1;
			protectOrder.AllowTimeout = false;
			protectOrder.Target = new SectorTarget
			{
				TargetUnit = hiringPerson.CurrentUnit
			};
			mercenaryFleet.EnqueueOrder(protectOrder);
			return true;
		}

		protected override void updateHeavy()
		{
			base.updateHeavy();
			if (MercenaryHireInfo != null)
			{
				UpdateWhenHired();
				return;
			}
			float value = Random.value;
			if (value < 0.05f)
			{
				try
				{
					Fleet firstValidOrderableFleet = GetFirstValidOrderableFleet();
					if (firstValidOrderableFleet != null && CheckForNearbyMercenaries(firstValidOrderableFleet))
					{
						EngineASX.Instance.DebugInfo.NumTimesMercenaryTriedToAvoidOtherMercenaries++;
						StartSearchForFriendlyStation(firstValidOrderableFleet);
					}
					return;
				}
				catch
				{
					Debug.LogError("Mercenary failed to check for nearby mercenaries", this);
					return;
				}
			}
			if (!(value > 0.6f))
			{
				return;
			}
			try
			{
				Fleet firstValidOrderableFleet2 = GetFirstValidOrderableFleet();
				if (firstValidOrderableFleet2 != null && !firstValidOrderableFleet2.InCombat && TryOfferServices(firstValidOrderableFleet2, out var hiringPerson) && TryAssignFleetToProtectHiringPerson(firstValidOrderableFleet2, hiringPerson))
				{
					int hiredMercenaryHours = hiringPerson.Faction.FactionAI.GetHiredMercenaryHours(firstValidOrderableFleet2, this);
					if (hiredMercenaryHours > 0)
					{
						OnHired(firstValidOrderableFleet2, hiredMercenaryHours, hiringPerson.Faction);
						EngineASX.Instance.DebugInfo.NumTimesMercenaryStartedWorkForOtherFaction++;
					}
				}
			}
			catch
			{
				Debug.LogError("Mercenary failed to offer services", this);
			}
		}

		public void OnHired(Fleet mercenaryFleet, float numHours, Faction hiringFaction)
		{
			float num = numHours * 60f * 60f;
			MercenaryHireInfo = new MercenaryHireInfo
			{
				HiringFaction = hiringFaction,
				HireTimeExpiry = EngineASX.Instance.ScenarioElapsedTime + (double)(num / GameController.Instance.GameSettings.GameTimeToRealTimeConversion)
			};
			int num2 = Mathf.RoundToInt((float)GetHourlyRate(mercenaryFleet, hiringFaction) * numHours);
			if (hiringFaction.IsPlayerFaction)
			{
				EngineASX.Instance.AddCreditsToPlayerFactionWithMsg(-num2, FactionTransactionType.MiscPurchase, faction);
			}
			else
			{
				hiringFaction.ApplyTransaction(-num2, FactionTransactionType.MiscPurchase, faction);
			}
			faction.GetOrCreateAttitude(hiringFaction);
			hiringFaction.GetOrCreateAttitude(faction);
			faction.ChangeOpinion(hiringFaction, Random.Range(0.05f, 0.2f));
			faction.RemoveRecentDamageFrom(hiringFaction);
			hiringFaction.RemoveRecentDamageFrom(faction);
			faction.SetNeutralityWith(hiringFaction, Neutrality.Allied);
			hiringFaction.SetNeutralityWith(faction, Neutrality.Allied);
			if (mercenaryFleet.IsInActiveSector)
			{
				Unit leaderUnit = mercenaryFleet.LeaderUnit;
				if (leaderUnit != null)
				{
					leaderUnit.GetPilot().RaiseDialogEvent(EngineASX.Instance.DialogEvents.MercenaryHired, 1.5f);
				}
			}
		}

		private Fleet GetFirstValidOrderableFleet()
		{
			if (faction.Fleets.Count > 0)
			{
				Fleet fleet = faction.Fleets[0];
				if (fleet != null && FactionAIBase.CanOrderFleet(fleet))
				{
					return fleet;
				}
			}
			return null;
		}

		protected override void UpdateLight()
		{
			base.UpdateLight();
			if (searchForFriendlyStationOperation == null)
			{
				return;
			}
			if (!searchForFriendlyStationOperation.HasFinished)
			{
				searchForFriendlyStationOperation.Process(Time.deltaTime);
				return;
			}
			if (MercenaryHireInfo == null && searchForFriendlyStationOperation.Result != null)
			{
				foreach (Fleet fleet in faction.Fleets)
				{
					if (FactionAIBase.CanOrderFleet(fleet))
					{
						MoveToOrder moveToOrder = UnityObjectHelper.NewGameObject<MoveToOrder>();
						moveToOrder.Target = SectorTarget.FromUnit(searchForFriendlyStationOperation.Result);
						moveToOrder.CompleteOnReachTarget = true;
						fleet.EnqueueOrder(moveToOrder);
						fleet.AssignNextOrder();
						OnFleetOrdered(fleet);
					}
				}
			}
			searchForFriendlyStationOperation = null;
		}

		private void StartSearchForFriendlyStation(Fleet fleet)
		{
			searchForFriendlyStationOperation = new UnitSearchOperation();
			searchForFriendlyStationOperation.SearchAllSectors = true;
			searchForFriendlyStationOperation.CustomScorer = (Unit unit, float distance) =>
			{
				if (unit.IsDockable && !unit.IsHostileToOrAlwaysHostileToTwoWay(faction))
				{
					float opinion = faction.GetOpinion(unit.Faction);
					if (opinion > -0.5f)
					{
						float num = 0f;
						switch (unit.UnitClass.StationPurpose)
						{
						case StationPurpose.Refinery:
							num += 0.2f;
							break;
						case StationPurpose.TradeStation:
							num += 0.1f;
							break;
						}
						return num + Random.value + Mathf.Min(opinion, 0.2f);
					}
				}
				return (float?)null;
			};
			searchForFriendlyStationOperation.InitialiseAndStartSearch(fleet.Sector, fleet.HomeSector, fleet.SectorPosition, faction, Mathf.Min(3, fleet.Settings.MaxJumpDistance));
		}

		private bool CanMercenaryBeHiredByNpcFaction(Fleet ourFleet, Fleet targetFleet, Faction faction)
		{
			if (!faction.IsAIFactionType)
			{
				return false;
			}
			return faction.FactionAI.RequestHireMercenary(ourFleet, targetFleet, this);
		}

		private bool TryOfferServices(Fleet ourFleet, out Person hiringPerson)
		{
			hiringPerson = null;
			int num = Physics.OverlapSphereNonAlloc(ourFleet.transform.position, 600f, EngineASX.ColliderCache, GameController.Instance.ShipsMask, QueryTriggerInteraction.Collide);
			if (num > 1)
			{
				Collider collider = EngineASX.ColliderCache[Random.Range(0, num)];
				if (collider != null)
				{
					Unit component = collider.GetComponent<Unit>();
					if (component != null)
					{
						Person pilot = component.GetPilot();
						if (pilot != null && pilot.Faction != null && pilot.Faction != faction && !pilot.Faction.IsHostileTo(faction))
						{
							Fleet fleet = pilot.GetFleet();
							if (ourFleet.IsInActiveSector && Time.time > lastTimeRaisedSalesPitchDialog + 30f)
							{
								Unit leaderUnit = ourFleet.LeaderUnit;
								if (leaderUnit != null)
								{
									leaderUnit.GetPilot().RaiseDialogEvent(EngineASX.Instance.DialogEvents.MercenarySalesPitch, 0.25f);
									lastTimeRaisedSalesPitchDialog = Time.time;
								}
							}
							if (fleet != null && fleet.GetCachedMinMoveSpeed() < ourFleet.GetCachedMinMoveSpeed() * 0.9f && fleet != ourFleet && CanFleetBeHiredAsMercenaryByPerson(this, ourFleet, pilot) && CanMercenaryBeHiredByNpcFaction(ourFleet, fleet, pilot.Faction))
							{
								hiringPerson = pilot;
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		private bool CheckForNearbyMercenaries(Fleet fleet)
		{
			int num = Physics.OverlapSphereNonAlloc(fleet.transform.position, 300f, EngineASX.ColliderCache, GameController.Instance.ShipsMask, QueryTriggerInteraction.Collide);
			for (int i = 0; i < num; i++)
			{
				Unit component = EngineASX.ColliderCache[i].GetComponent<Unit>();
				if (component != null && component.Sector == fleet.Sector && component.GetFleet() != fleet && component.IsMercenary() && component.HasNoFleetOrders())
				{
					return true;
				}
			}
			return false;
		}

		private void UpdateWhenHired()
		{
			if (MercenaryHireInfo.HiringFaction == null)
			{
				ClearMercenaryHireInfo(this);
				OrderMercenaryAfterHire(this);
				return;
			}
			if (IsHostileWithMercenaryHirer(this))
			{
				ClearMercenaryHireInfo(this);
				OrderMercenaryAfterHire(this);
				return;
			}
			if (MercenaryHireTimeExpired(this))
			{
				if (CanTimeoutMercenaryHire(this))
				{
					OnMercenaryHireTimeExpired();
				}
				return;
			}
			foreach (FactionAttitude relation in MercenaryHireInfo.HiringFaction.Relations)
			{
				if (relation.TargetFaction != null && relation.Neutrality == Neutrality.Hostile)
				{
					Faction.SetNeutralityWith(relation.TargetFaction, Neutrality.Hostile);
				}
			}
		}

		private static bool IsHostileWithMercenaryHirer(FactionAIBase factionAI)
		{
			return factionAI.Faction.IsHostileTo(factionAI.MercenaryHireInfo.HiringFaction);
		}

		private static bool MercenaryHireTimeExpired(FactionAIBase factionAI)
		{
			return EngineASX.Instance.ScenarioElapsedTime > factionAI.MercenaryHireInfo.HireTimeExpiry;
		}

		public static void ClearMercenaryHireInfo(FactionAIBase factionAI)
		{
			factionAI.MercenaryHireInfo = null;
			OnMercenaryHireFactionChanged(factionAI);
		}

		public static void OrderMercenaryAfterHire(FactionAIBase factionAI)
		{
			foreach (Fleet fleet in factionAI.Faction.Fleets)
			{
				if (FactionAIBase.CanOrderFleet(fleet))
				{
					if (FactionAIRepairer.EvaluateFleetNeedsRepair(factionAI.Faction, fleet))
					{
						FactionAIRepairer.RepairFleet(fleet, factionAI);
						continue;
					}
					fleet.ClearOrders();
					MoveToNearestFriendlyStationOrder order = UnityObjectHelper.NewGameObject<MoveToNearestFriendlyStationOrder>();
					fleet.EnqueueOrder(order);
					fleet.AssignNextOrder();
					factionAI.OnFleetOrdered(fleet);
				}
			}
		}

		public static void OnMercenaryHireFactionChanged(FactionAIBase factionAI)
		{
			factionAI.BreakAllAlliances();
			ForceEnemiesToMakePeace(factionAI);
			MakePeaceWithAIEnemiesOneWay(factionAI);
			InvalidateProtectObjectives(factionAI);
		}

		public static void MakePeaceWithAIEnemiesOneWay(FactionAIBase factionAI)
		{
			tempFactionCache.Clear();
			foreach (FactionAttitude relation in factionAI.Faction.Relations)
			{
				if (relation.TargetFaction != null && relation.Neutrality == Neutrality.Hostile && relation.TargetFaction.FactionAI != null && !relation.TargetFaction.FactionAI.IsAlwaysAtWarWithFaction(factionAI.Faction))
				{
					tempFactionCache.Add(relation.TargetFaction);
				}
			}
			foreach (Faction item in tempFactionCache)
			{
				factionAI.Faction.MakePeace(item);
			}
		}

		private static void InvalidateProtectObjectives(FactionAIBase factionAI)
		{
			foreach (Fleet fleet in factionAI.Faction.Fleets)
			{
				if (FactionAIBase.CanUseFleet(fleet))
				{
					ActiveProtectOrder activeProtectOrder = fleet.ActiveOrder as ActiveProtectOrder;
					if (activeProtectOrder != null)
					{
						activeProtectOrder.OnInvalid();
					}
				}
			}
		}

		private static void ForceEnemiesToMakePeace(FactionAIBase factionAI)
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction != factionAI.Faction && faction.FactionAI != null && faction.IsHostileTo(factionAI.Faction) && !faction.FactionAI.IsAlwaysAtWarWithFaction(factionAI.Faction))
				{
					faction.MakePeace(factionAI.Faction);
				}
			}
		}

		private static bool CanTimeoutMercenaryHire(FactionAIBase factionAI)
		{
			Fleet firstUsableGroup = GetFirstUsableGroup(factionAI);
			if (firstUsableGroup != null && firstUsableGroup.InCombat)
			{
				return factionAI.Faction.Greed > 0.7f;
			}
			return true;
		}

		private static Fleet GetFirstUsableGroup(FactionAIBase factionAI)
		{
			foreach (Fleet fleet in factionAI.Faction.Fleets)
			{
				if (FactionAIBase.CanOrderFleet(fleet))
				{
					return fleet;
				}
			}
			return null;
		}

		private void OnMercenaryHireTimeExpired()
		{
			if (Faction.LeaderPerson != null && MercenaryHireInfo != null && MercenaryHireInfo.HiringFaction != null && MercenaryHireInfo.HiringFaction.IsPlayerFaction)
			{
				RaiseMercenaryHiredByPlayerExpired();
			}
			ClearMercenaryHireInfo(this);
			OrderMercenaryAfterHire(this);
		}

		private void RaiseMercenaryHiredByPlayerExpired()
		{
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage
			{
				SubjectText = "Hired mercenary moving on",
				FromText = Faction.LeaderPerson.FullNameWithFullRank,
				MessageText = "The time you hired me for has passed. You can hire me again if you have the credits. Nice doing business with you.",
				AllowDelete = true
			};
			playerActiveMessage.SetSenderUnitAndPosition(Faction.LeaderPerson.CurrentUnit);
			EngineASX.Instance.LocalPlayer.AddMessage(playerActiveMessage);
		}

		public static bool CanFleetBeHiredAsMercenaryByPerson(FactionAIBase factionAI, Fleet mercenaryGroup, Person person)
		{
			if (person.CurrentUnit == null || !person.CurrentUnit.IsNormalShip())
			{
				return false;
			}
			if (mercenaryGroup == null || person.Faction == null || person.Faction == factionAI.Faction || person.Sector != mercenaryGroup.Sector || person.CurrentUnit.Faction != person.Faction)
			{
				return false;
			}
			if (!factionAI.Faction.IsHostileTo(person) && person.Faction != null && person.Faction.LeaderPerson != null && factionAI.FactionTypeInfo.FactionType == FactionType.Mercenary && factionAI.MercenaryHireInfo == null)
			{
				if (FactionAIBase.CanUseFleet(mercenaryGroup) && (mercenaryGroup.IdleAndNoObjectives || mercenaryGroup.ActiveOrder is ActiveWaitOrder || mercenaryGroup.ActiveOrder is ActiveMoveToNearestFriendlyStationOrder || mercenaryGroup.ActiveOrder is ActiveMoveToOrder))
				{
					return mercenaryGroup.NpcPilots.Count > 0;
				}
				return false;
			}
			return false;
		}

		public int GetHourlyRate(Fleet mercenaryFleet, Faction hiringFaction)
		{
			if (mercenaryFleet != null)
			{
				float price = GameController.Instance.GameSettings.MercenarySettings.CombatRatingToHourRateConversion * mercenaryFleet.GetCachedSimpleCombatRating();
				price = faction.GetMarkedUpPriceAfterOpinionChange(TradeType.Sell, price, hiringFaction);
				return Maths.RoundUpToInt(price, EngineASX.Instance.EconomySettings.GeneralRounding);
			}
			return 1000;
		}

		public override bool RequestCapture(Unit unit)
		{
			if (MercenaryHireInfo != null)
			{
				return false;
			}
			return base.RequestCapture(unit);
		}

		public override bool RequestStealCargo(Faction ownerFaction, CargoOwnership cargoOwnership)
		{
			if (cargoOwnership != CargoOwnership.OwnedByHostile && faction.GetEffectiveOpinionOrNull(ownerFaction) > -0.5f)
			{
				return false;
			}
			return base.RequestStealCargo(ownerFaction, cargoOwnership);
		}
	}
}
