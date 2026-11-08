using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Testing.Spawning;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.IP.UI.Screens.MessageBox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class SpawnCargoContainerOptions : MonoBehaviour
	{
		public float SliderPower = 4f;

		public int MinQuantity = 1;

		public int MaxQuantity = 1000;

		public Toggle SpawnOwnershipPlayerToggle;

		public Toggle SpawnOwnershipBanditsToggle;

		public Toggle SpawnOwnershipAbandonedToggle;

		public Toggle SpawnOwnershipCurrentTargetToggle;

		public Transform ButtonsTransform;

		public Button ButtonPrefab;

		public Slider QuantitySlider;

		public TextMeshProUGUI CountSliderText;

		public SpawnOwnership Ownership
		{
			get
			{
				if (SpawnOwnershipAbandonedToggle.isOn)
				{
					return SpawnOwnership.Abandoned;
				}
				if (SpawnOwnershipPlayerToggle.isOn)
				{
					return SpawnOwnership.Player;
				}
				if (SpawnOwnershipCurrentTargetToggle.isOn)
				{
					return SpawnOwnership.CurrentTarget;
				}
				if (SpawnOwnershipBanditsToggle.isOn)
				{
					return SpawnOwnership.Bandits;
				}
				return SpawnOwnership.Unknown;
			}
		}

		public int SpawnQuantity => Mathf.CeilToInt(Mathf.Lerp(MinQuantity, MaxQuantity, Mathf.Pow(QuantitySlider.value, SliderPower)));

		private void Awake()
		{
			CreateButtons();
			RefreshQuantitySldierText();
			QuantitySlider.onValueChanged.AddListener((float value) =>
			{
				RefreshQuantitySldierText();
			});
		}

		private void RefreshQuantitySldierText()
		{
			CountSliderText.text = SpawnQuantity.ToString();
		}

		private void CreateButtons()
		{
			foreach (CargoClass cargoClass in GetSpawnableCargoClasses())
			{
				CreateButtonForCargoClass(cargoClass).onClick.AddListener(() =>
				{
					Unit unit = TrySpawnCargoContainer(cargoClass, SpawnQuantity, out var spawnFaction);
					if (unit != null)
					{
						string text = null;
						text = ((!(spawnFaction == null)) ? $"{unit.GetFriendlyName()} spawned in current sector for {spawnFaction.GetShortNameElseLong()} ({spawnFaction.UniqueId})" : ("Abandoned " + unit.GetFriendlyName() + " spawned"));
						UIController.Instance.ShowMessageBox(text, MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
					}
					else
					{
						UIController.Instance.ShowError("Failed to spawn any units");
					}
				});
			}
		}

		private Unit TrySpawnCargoContainer(CargoClass cargoClass, int quantity, out Faction spawnFaction)
		{
			spawnFaction = null;
			try
			{
				if (EngineASX.Instance.LocalUnit == null || EngineASX.Instance.ActiveSector == null)
				{
					throw new Exception("Cannot determine location to spawn");
				}
				if (!EngineASX.Instance.CanSpawnUnitsInSector(EngineASX.Instance.LocalUnitSector, UnitType.Cargo, 1))
				{
					throw new Exception("Cannnot spawn: too many units in sector");
				}
				spawnFaction = GetSpawnFaction();
				if (SpawnQuantity > 0)
				{
					Unit unit = SpawnUtils.SpawnCargoContainerNearLocalUnit(cargoClass, SpawnQuantity);
					unit.Faction = spawnFaction;
					return unit;
				}
			}
			catch (Exception ex)
			{
				UIController.Instance.ShowMessageBox(ex.Message, MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
				Debug.LogException(ex);
			}
			return null;
		}

		public Faction GetSpawnFaction()
		{
			switch (Ownership)
			{
			case SpawnOwnership.Abandoned:
				return null;
			case SpawnOwnership.Player:
				if (EngineASX.Instance.LocalFaction == null)
				{
					throw new Exception("No player faction");
				}
				return EngineASX.Instance.LocalFaction;
			case SpawnOwnership.Bandits:
			{
				Faction orCreateHostileWithAllBanditFaction = SpawnUtils.GetOrCreateHostileWithAllBanditFaction();
				if (orCreateHostileWithAllBanditFaction == null)
				{
					throw new Exception("Failed to get bandit faction");
				}
				return orCreateHostileWithAllBanditFaction;
			}
			case SpawnOwnership.CurrentTarget:
				if (EngineASX.Instance.Hud == null || EngineASX.Instance.Hud.CurrentTarget == null || EngineASX.Instance.Hud.CurrentTarget.Faction == null)
				{
					throw new Exception("No current target with faction");
				}
				return EngineASX.Instance.Hud.CurrentTarget.Faction;
			default:
				throw new Exception("Internal error: Unknown spawn ownership type");
			}
		}

		private Button CreateButtonForCargoClass(CargoClass cargoClass)
		{
			Button button = UnityEngine.Object.Instantiate(ButtonPrefab);
			button.transform.SetParent(ButtonsTransform);
			button.transform.localScale = Vector3.one;
			button.SetText($"{cargoClass.ClassName} (ID: {cargoClass.UniqueId})");
			return button;
		}

		private IEnumerable<CargoClass> GetSpawnableCargoClasses()
		{
			return from e in EngineASX.Instance.CargoClasses
				where !e.IsReserved
				orderby e.ClassName
				select e;
		}
	}
}
