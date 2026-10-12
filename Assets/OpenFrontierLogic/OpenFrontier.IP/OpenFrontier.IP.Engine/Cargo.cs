using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	[RequireComponent(typeof(Unit))]
	public class Cargo : MonoBehaviour
	{
		public enum CargoCollectMode
		{
			None,
			Partial,
			All
		}

		[SerializeField]
		private CargoClass cargoClass;

		private EngineASX engine;

		public bool Expires = true;

		private double spawnTime;

		public int Quantity = 1;

		private Unit unit;

		public Unit Unit
		{
			get
			{
				return unit;
			}
			private set
			{
				if (unit != value)
				{
					unit = value;
				}
			}
		}

		public CargoClass CargoClass
		{
			get
			{
				return cargoClass;
			}
			set
			{
				cargoClass = value;
			}
		}

		public double SpawnTime
		{
			get
			{
				return spawnTime;
			}
			set
			{
				spawnTime = value;
			}
		}

		public int CreditsValue
		{
			get
			{
				if (cargoClass == null)
				{
					return 0;
				}
				return Quantity * cargoClass.BasePrice;
			}
		}

		public float Volume
		{
			get
			{
				if (cargoClass == null)
				{
					return 0f;
				}
				return (float)Quantity * cargoClass.Volume;
			}
		}

		public void SetHealthBasedOnVolume()
		{
			unit.Destructable.CurrentHealth = GetHealthBasedOnVolume();
		}

		public float GetHealthBasedOnVolume()
		{
			return Mathf.Clamp(GameController.Instance.GameSettings.GameplaySettings.CargoBaseHealth + Volume * GameController.Instance.GameSettings.GameplaySettings.CargoHealthPerVolumeUnit, 1f, GameController.Instance.GameSettings.GameplaySettings.CargoMaxHealth);
		}

		public void Init(Unit unit)
		{
			engine = EngineASX.Instance;
			if (engine == null)
			{
				Debug.LogError("Engine null");
			}
			this.unit = unit;
		}

		public void SetSpawnTime()
		{
			spawnTime = EngineASX.Instance.ScenarioElapsedTime;
		}

		private float GetLifetime()
		{
			if (cargoClass != null && cargoClass.IsOre)
			{
				return engine.GameSettings.GameplaySettings.CargoOreLifetime;
			}
			return engine.GameSettings.GameplaySettings.CargoLifetime;
		}

		public void UpdateExpiry()
		{
			if (unit != null && Expires && engine.ScenarioElapsedTime > spawnTime + (double)GetLifetime() && (!unit.IsInActiveSector || (!unit.IsOwnedByPlayer && (unit.ActiveUnit == null || unit.ActiveUnit.LastDistanceFromCamera > GameController.Instance.GameSettings.ExpireCargoInActiveSceneDistanceFromCamera))))
			{
				EngineASX.Instance.DebugInfo.NumCargoExpired++;
				unit.SafeDestroy();
			}
		}

		public int Collect(CargoBayComponent target)
		{
			int num = 0;
			if (target != null && Quantity > 0 && CargoClass != null)
			{
				num = Mathf.Min(Quantity, target.GetFreeSpaceFor(CargoClass));
				if (num > 0)
				{
					target.AddToCargo(CargoClass, num, ignoreCapacity: true);
					Quantity -= num;
				}
			}
			return num;
		}

		public int CollectAndDestroyIfEmpty(CargoBayComponent target)
		{
			int result = Collect(target);
			DestroyIfEmpty();
			return result;
		}

		public void DestroyIfEmpty()
		{
			if (Quantity <= 0 || CargoClass == null)
			{
				unit.SafeDestroy();
			}
		}

		public string GetFriendlyName(bool useShortName = true)
		{
			if (cargoClass != null && Quantity > 0)
			{
				string arg = cargoClass.ClassName;
				if (useShortName && !string.IsNullOrEmpty(cargoClass.ShortName))
				{
					arg = cargoClass.ShortName;
				}
				return $"{arg} ({Quantity})";
			}
			return "Cargo";
		}

		public void AutoNameGameObject()
		{
			gameObject.name = GetEditorName();
		}

		public string GetEditorName()
		{
			string arg = ((cargoClass != null) ? cargoClass.ClassName : "NoClass");
			string arg2 = ((unit.Faction != null) ? unit.Faction.GetShortNameElseLong() : "Abandoned");
			return $"Cargo_{arg}_{Quantity}_{arg2}";
		}
	}
}
