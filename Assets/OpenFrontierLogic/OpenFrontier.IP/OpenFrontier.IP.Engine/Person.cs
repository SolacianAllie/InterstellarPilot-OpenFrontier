using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenFrontier.IP.Avatars;
using OpenFrontier.IP.Engine.Comms;
using OpenFrontier.IP.Engine.Dialog;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Bounty;
using OpenFrontier.IP.Engine.PilotRankings;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Engine
{
	public class Person : MonoBehaviour
	{
		public delegate void FactionChangedHandler(Person sender, Faction oldFaction, Faction newFaction);

		public string CustomTitle;

		public int Seed = -1;

		public AvatarProfile AvatarProfile;

		private PilotRankingSystemRank rankingSystemRank;

		public PilotRank Rank;

		public int Deaths;

		[SerializeField]
		private int kills;

		private NpcPilot npcPilot;

		public DialogBase CommsHandler;

		private Unit currentUnit;

		public bool DestroyGameObjectOnKill;

		public DialogProfile DialogProfile;

		private EngineASX engine;

		[SerializeField]
		private Faction faction;

		private bool hasInit;

		public bool IsMale = true;

		public float Properness = 0.5f;

		public float Aggression = 0.5f;

		public float Greed = 0.5f;

		[FormerlySerializedAs("Name")]
		public string CustomName = string.Empty;

		public string CustomShortName = string.Empty;

		private bool hasGeneratedName;

		private int generatedFirstNameId = -1;

		private int generatedLastNameId = -1;

		public int UniqueId = -1;

		public bool IsAutoPilot;

		private static StringBuilder nameBuilder = new StringBuilder();

		public Unit CurrentUnit
		{
			get
			{
				return currentUnit;
			}
			set
			{
				if (currentUnit != value)
				{
					if (value != null && value.Components == null)
					{
						Debug.LogError("Cannot place a pilot on a unit without UnitComponentHolder component");
						return;
					}
					SetCurrentUnitInternal(value);
					SetParent();
				}
			}
		}

		public Unit RootUnit
		{
			get
			{
				_ = currentUnit;
				if (currentUnit != null)
				{
					return currentUnit.GetRootUnit();
				}
				return null;
			}
		}

		public bool IsLocalPlayer
		{
			get
			{
				if (engine != null && engine.LocalPlayer != null)
				{
					return engine.LocalPlayer.Person == this;
				}
				return false;
			}
		}

		public bool IsPilot
		{
			get
			{
				if (currentUnit != null && currentUnit.Components != null)
				{
					return currentUnit.Components.PilotPerson == this;
				}
				return false;
			}
			set
			{
				if (value)
				{
					if (currentUnit != null && currentUnit.Components != null)
					{
						currentUnit.Components.PilotPerson = this;
					}
					else
					{
						Debug.LogError("Cannot set pilot as no current unit or unit is not pilottable", this);
					}
				}
				else if (currentUnit != null && currentUnit.Components != null && currentUnit.Components.PilotPerson == this)
				{
					if (npcPilot != null)
					{
						npcPilot.CurrentUnit = null;
					}
					currentUnit.Components.PilotPerson = null;
				}
			}
		}

		public Faction Faction
		{
			get
			{
				return faction;
			}
			set
			{
				if (faction != value)
				{
					Faction oldFaction = faction;
					faction = value;
					OnFactionChanged(oldFaction);
				}
			}
		}

		public Sector Sector
		{
			get
			{
				if (currentUnit != null)
				{
					return currentUnit.Sector;
				}
				return null;
			}
		}

		public bool IsInActiveSector
		{
			get
			{
				if (Sector != null)
				{
					return Sector.IsActive;
				}
				return false;
			}
		}

		public bool HasGeneratedName
		{
			get
			{
				return hasGeneratedName;
			}
			set
			{
				hasGeneratedName = value;
			}
		}

		public int GeneratedFirstNameId
		{
			get
			{
				return generatedFirstNameId;
			}
			set
			{
				generatedFirstNameId = value;
			}
		}

		public int GeneratedLastNameId
		{
			get
			{
				return generatedLastNameId;
			}
			set
			{
				generatedLastNameId = value;
			}
		}

		public bool IsActiveInGame
		{
			get
			{
				if (Sector != null)
				{
					return currentUnit != null;
				}
				return false;
			}
		}

		public bool IsRootUnitSameFaction
		{
			get
			{
				if (currentUnit != null)
				{
					return currentUnit.GetRootUnit().Faction == Faction;
				}
				return false;
			}
		}

		public NpcPilot NpcPilot
		{
			get
			{
				return npcPilot;
			}
			set
			{
				npcPilot = value;
			}
		}

		public Unit PilottedUnit
		{
			get
			{
				if (IsPilot)
				{
					return currentUnit;
				}
				return null;
			}
		}

		public EngineASX Engine
		{
			get
			{
				return engine;
			}
			private set
			{
				if (!(engine != value))
				{
					return;
				}
				EngineASX engineASX = engine;
				engine = value;
				if (engineASX != null)
				{
					engineASX.DeregisterPerson(this);
				}
				if (engine != null)
				{
					if (UniqueId < 0)
					{
						UniqueId = engine.GetUniquePersonId();
					}
					engine.RegisterPerson(this);
				}
			}
		}

		public int Kills
		{
			get
			{
				return kills;
			}
			set
			{
				kills = value;
			}
		}

		public string Name { get; private set; }

		public string ShortName { get; private set; }

		public string ShortNameWithShortRank => GetNameAndRank();

		public string ShortNameWithFullRank => GetNameAndRank(shortName: true, shortRank: false);

		public string FullNameWithShortRank => GetNameAndRank(shortName: false);

		public string FullNameWithFullRank => GetNameAndRank(shortName: false, shortRank: false);

		public PilotRankingSystemRank RankingSystemRank => rankingSystemRank;

		public string Title
		{
			get
			{
				if (!string.IsNullOrEmpty(CustomTitle))
				{
					return CustomTitle;
				}
				if (IsLocalPlayer)
				{
					return GameController.Instance.DefaultPilotTitle;
				}
				if (IsMale)
				{
					return "Sir";
				}
				return "Maam";
			}
		}

		public event FactionChangedHandler FactionChanged;

		public int GetTotalBounty()
		{
			List<FactionBountyItem> bountiesOnPerson = engine.GetBountiesOnPerson(this);
			if (bountiesOnPerson != null)
			{
				int num = 0;
				{
					foreach (FactionBountyItem item in bountiesOnPerson)
					{
						if (item.IsValid)
						{
							num += item.Bounty;
						}
					}
					return num;
				}
			}
			return 0;
		}

		public string GetShortNameElseLong()
		{
			if (!string.IsNullOrWhiteSpace(ShortName))
			{
				return ShortName;
			}
			return Name;
		}

		public void SetFactionOnly(Faction newFaction)
		{
			faction = newFaction;
		}

		public void RandomizeNameAndPersonalityAndAssignDialog()
		{
			RandomizeGenderAndName(GameController.Instance.GameSettings.GameplaySettings.ProbabilityOfMaleNpc, engine.CharacterNames);
			RandomizePersonality();
			DialogProfile = engine.GetDialogProfileForPerson(this);
		}

		public void RandomizePersonality()
		{
			Properness = UnityEngine.Random.value;
			Aggression = UnityEngine.Random.value;
			Greed = UnityEngine.Random.value;
		}

		public void SyncPersonalityWithFaction(Faction faction)
		{
			Aggression = faction.Aggression;
			Greed = faction.Greed;
		}

		public void RandomizeGenderAndName(float probabilityOfMale, CharacterNames characterNames)
		{
			IsMale = UnityEngine.Random.value < probabilityOfMale;
			AutoAssignName(characterNames);
		}

		public void AssignRandomSeed()
		{
			Seed = UnityEngine.Random.Range(0, int.MaxValue);
		}

		public void Init()
		{
			if (!hasInit)
			{
				if (Seed < 0)
				{
					AssignRandomSeed();
				}
				FindNpcPilot();
				hasInit = true;
				Engine = EngineASX.Instance;
				if (npcPilot != null)
				{
					npcPilot.Init();
				}
				if (faction != null)
				{
					OnFactionChanged(null);
				}
				if (DialogProfile != null)
				{
					DialogProfile = engine.GetDialogProfileById(DialogProfile.UniqueId);
				}
			}
		}

		public void AutoAssignDialogProfileIfNone()
		{
			if (DialogProfile == null)
			{
				DialogProfile = EngineASX.Instance.GetDialogProfileForPerson(this);
			}
		}

		public void FindNpcPilot()
		{
			if (npcPilot == null)
			{
				npcPilot = GetComponent<NpcPilot>();
			}
		}

		public void AutoAssignName()
		{
			AutoAssignName(engine.CharacterNames);
		}

		public void AutoAssignName(CharacterNames characterNames)
		{
			characterNames.GenerateName(this);
		}

		public void NotifyPilotStatusChanged(UnitComponentHolder ship)
		{
			if (IsLocalPlayer)
			{
				if (IsPilot)
				{
					currentUnit.Faction = faction;
				}
				engine.NotifyLocalPlayerChangedPilotting(ship, ship.PilotPerson == this);
			}
			else if (NpcPilot != null)
			{
				NpcPilot.OnPilotStatusChanged(IsPilot);
			}
		}

		public bool TryKill()
		{
			if (engine.OnAboutToKillPerson(this))
			{
				Kill();
				return true;
			}
			return false;
		}

		public void Kill()
		{
			if (IsLocalPlayer)
			{
				engine.NotifyLocalPlayerKilled();
			}
			Deaths++;
			TryRemoveSpeedModelRequests();
			if (DestroyGameObjectOnKill)
			{
				SafeDestroy();
			}
		}

		public void SafeDestroy()
		{
			Cleanup();
			UnityEngine.Object.Destroy(gameObject);
		}

		public void TryRemoveSpeedModelRequests()
		{
			if (engine != null && engine.Hud != null && engine.Hud.SpeechModel != null)
			{
				engine.Hud.SpeechModel.RemoveRequestsFromPilotWhereNotDeathMessage(this);
			}
		}

		public void FindCurrentUnit()
		{
			CurrentUnit = UnityObjectHelper.FindInParentsOrSelf<Unit>(gameObject);
		}

		public void SetParent()
		{
			if ((bool)gameObject)
			{
				if (currentUnit != null)
				{
					gameObject.transform.SetParent(currentUnit.gameObject.transform);
					transform.localPosition = Vector3.zero;
					transform.localRotation = Quaternion.identity;
				}
				else
				{
					gameObject.transform.SetParent(null);
				}
			}
		}

		public void RaiseDialogEventRandomly(DialogEvent dialogEvent, float additionalDelay = 0f, DialogRequestArguments? args = null, float? probabilityMultiplier = null)
		{
			if (IsInActiveSector && UnityEngine.Random.value < dialogEvent.HandledProbability * (probabilityMultiplier ?? 1f))
			{
				RaiseDialogEvent(dialogEvent, additionalDelay, args);
			}
		}

		public void RaiseDialogEvent(DialogEvent dialogEvent, float additionalDelay = 0f, DialogRequestArguments? args = null)
		{
			if (!(DialogProfile != null) || !(currentUnit != null) || (!currentUnit.IsPlayerCurrentUnit && !currentUnit.IsScannedByPlayer()))
			{
				return;
			}
			CompiledDialogEventHandler dialogEventHandler = DialogProfile.GetDialogEventHandler(dialogEvent);
			if (dialogEventHandler != null)
			{
				SpeechModel.SpeechRequest speechRequest = engine.RequestSpeech(this, dialogEvent, dialogEventHandler, additionalDelay, args);
				if (speechRequest != null)
				{
					speechRequest.PlayWhenSourceNull = dialogEvent == EngineASX.Instance.DialogEvents.Destroyed;
				}
			}
		}

		private void SetCurrentUnitInternal(Unit value)
		{
			Unit unit = currentUnit;
			currentUnit = value;
			if (unit != null)
			{
				unit.Components.Crew.Remove(this);
			}
			if (IsLocalPlayer)
			{
				engine.NotifyLocalPlayerChangedCurrentUnit(unit);
			}
			if (currentUnit != null)
			{
				currentUnit.Components.Crew.Add(this);
				if (NpcPilot != null)
				{
					NpcPilot.CurrentUnitComponents = currentUnit.Components;
				}
				if (faction != null && faction.Intel != null)
				{
					faction.Intel.DiscoverUnit(currentUnit);
				}
			}
			else if (NpcPilot != null)
			{
				NpcPilot.CurrentUnitComponents = null;
			}
			if (unit != null && unit.Components.PilotPerson == this)
			{
				unit.Components.PilotPerson = null;
			}
		}

		private void Cleanup()
		{
			if (npcPilot != null)
			{
				npcPilot.SafeDestroy();
				npcPilot = null;
			}
			TryRemoveSpeedModelRequests();
			Faction = null;
			SetCurrentUnitInternal(null);
			Engine = null;
		}

		internal void OnPilotStatusChanging(UnitComponentHolder ship)
		{
			if (npcPilot != null)
			{
				npcPilot.OnPilotStatusChanging(ship);
			}
		}

		private void OnFactionChanged(Faction oldFaction)
		{
			if (oldFaction != null)
			{
				oldFaction.RemovePilot(this);
			}
			if (faction != null)
			{
				faction.AddPilot(this);
			}
			if (FactionChanged != null)
			{
				FactionChanged(this, oldFaction, faction);
			}
		}

		public void AutoNameGameObject()
		{
			string text = string.Format("Person: {0}_{1}_{2}", UniqueId, FullNameWithFullRank, IsLocalPlayer ? " (Player)" : string.Empty);
			gameObject.name = text;
		}

		public void ClearGeneratedName()
		{
			GeneratedFirstNameId = -1;
			GeneratedLastNameId = -1;
			hasGeneratedName = false;
		}

		public void AddKill(Unit killedUnit)
		{
			Kills++;
			if (npcPilot != null && npcPilot.Settings != null && npcPilot.Settings.CombatEfficiency < 1f)
			{
				npcPilot.Settings.CombatEfficiency = Mathf.Clamp01(npcPilot.Settings.CombatEfficiency + GameController.Instance.GameSettings.GameplaySettings.CombatEfficiencyChangePerKill);
			}
		}

		public string GetNameAndRank(bool shortName = true, bool shortRank = true)
		{
			if (Rank != null)
			{
				if (shortRank)
				{
					if (!string.IsNullOrWhiteSpace(Rank.ShortName))
					{
						return Rank.ShortName + ". " + ((shortName && !string.IsNullOrWhiteSpace(ShortName)) ? ShortName : Name);
					}
					if (!shortName || string.IsNullOrWhiteSpace(ShortName))
					{
						return Name;
					}
					return ShortName;
				}
				return Rank.Name + ". " + ((shortName && !string.IsNullOrWhiteSpace(ShortName)) ? ShortName : Name);
			}
			if (!shortName || string.IsNullOrWhiteSpace(ShortName))
			{
				return Name;
			}
			return ShortName;
		}

		public string GetNameAndTitle(bool shortName = true)
		{
			if (!string.IsNullOrWhiteSpace(CustomTitle))
			{
				if (!string.IsNullOrWhiteSpace(ShortName))
				{
					return CustomTitle + ". " + ShortName;
				}
				return CustomTitle + ". " + Name;
			}
			return Name;
		}

		public string GetNameAndTitleOrFullRank(bool shortName = true)
		{
			if (!string.IsNullOrWhiteSpace(CustomTitle))
			{
				if (!string.IsNullOrWhiteSpace(ShortName))
				{
					return CustomTitle + ". " + ShortName;
				}
				return CustomTitle + ". " + Name;
			}
			return GetNameAndRank(shortName, shortName);
		}

		public void RefreshName()
		{
			if (!string.IsNullOrWhiteSpace(CustomName))
			{
				Name = CustomName;
				ShortName = CustomShortName;
			}
			else if (hasGeneratedName)
			{
				Name = EngineASX.Instance.CharacterNames.GetName(generatedFirstNameId, generatedLastNameId);
				ShortName = ShortenName(Name);
			}
			else
			{
				Name = null;
				ShortName = null;
			}
		}

		public string ShortenName(string name)
		{
			if (name.Contains(" "))
			{
				nameBuilder.Length = 0;
				string[] array = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				if (array.Length > 1)
				{
					for (int i = 0; i < array.Length - 1; i++)
					{
						nameBuilder.Append(array[0].Substring(0, 1));
						nameBuilder.Append(".");
					}
					nameBuilder.Append(array[^1]);
					return nameBuilder.ToString();
				}
			}
			return null;
		}

		public void AssignFirstPilotRankIfNull()
		{
			if (!IsAutoPilot && Rank == null && faction.PilotRankingSystem != null && faction.PilotRankingSystem.Ranks.Count > 0)
			{
				ChangeRank(faction.PilotRankingSystem.Ranks[0].Rank);
			}
		}

		public void ChangeRank(PilotRank pilotRank)
		{
			if (pilotRank != Rank)
			{
				Rank = pilotRank;
				FindRankingSystemRank();
			}
		}

		public void FindRankingSystemRank()
		{
			rankingSystemRank = GetRankingSystemRank();
		}

		private PilotRankingSystemRank GetRankingSystemRank()
		{
			if (Rank == null || faction == null || faction.PilotRankingSystem == null)
			{
				return null;
			}
			return faction.PilotRankingSystem.Ranks.FirstOrDefault((PilotRankingSystemRank e) => e.Rank == Rank);
		}

		public void AutoAssignAvatarProfileFromFactionIfNone()
		{
			if (!(AvatarProfile != null) && faction != null)
			{
				if (faction.PersonAvatarProfiles.Count > 0)
				{
					AvatarProfile = faction.PersonAvatarProfiles.GetRandom();
				}
				else if (faction.Virtue < 0.25f)
				{
					AvatarProfile = GameController.Instance.AvatarController.HumanPunkAvatarProfiles.GetRandom();
				}
				else if (faction.Virtue < 0.5f)
				{
					AvatarProfile = GameController.Instance.AvatarController.HumanAvatarProfiles.GetRandom();
				}
				else
				{
					AvatarProfile = GameController.Instance.AvatarController.HumanCleanAvatarProfiles.GetRandom();
				}
			}
		}

		public bool OutRanks(Person pilotPerson)
		{
			if (faction != null && this == faction.LeaderPerson)
			{
				return true;
			}
			if (Rank != null)
			{
				if (!(pilotPerson.Rank == null))
				{
					return Rank.Superiority > pilotPerson.Rank.Superiority;
				}
				return true;
			}
			return false;
		}
	}
}
