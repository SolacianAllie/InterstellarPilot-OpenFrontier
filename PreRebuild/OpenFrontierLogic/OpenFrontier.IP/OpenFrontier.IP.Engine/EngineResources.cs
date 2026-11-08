using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class EngineResources : MonoBehaviour
	{
		private Dictionary<string, GameObject> loadedActiveUnits = new Dictionary<string, GameObject>();

		private Dictionary<int, Sprite> loadedUnitThumbnailSprites = new Dictionary<int, Sprite>();

		private Dictionary<int, Sprite> loadedUnitRenderSprites = new Dictionary<int, Sprite>();

		private Dictionary<int, Sprite> loadedUnitIconSprites = new Dictionary<int, Sprite>();

		private Dictionary<int, Sprite> loadedCargoIcons = new Dictionary<int, Sprite>();

		private Dictionary<int, Sprite> loadedComponentClassIcons = new Dictionary<int, Sprite>();

		private Dictionary<int, Sprite> loadedComponentBayTypeIcons = new Dictionary<int, Sprite>();

		public Sprite DefaultCargoIconSprite;

		public Sprite DefaultComponentIconSprite;

		public Sprite DefaultUnitClassThumbnailSprite;

		public Sprite DefaultUnitClassRenderSprite;

		public Sprite DefaultComponentBayTypeSprite;

		public static Texture LoadSkyTexture(string textureName)
		{
			return Resources.Load<Texture>("Textures/Backgrounds/" + textureName);
		}

		public void Load(EngineASX engine)
		{
			LoadCargoSprites(engine);
			LoadComponentTypeSprites(engine);
			LoadComponentClassSprites(engine);
			LoadUnitThumbnailSprites(engine);
			LoadUnitRenderSprites(engine);
			LoadUnitIconSprites(engine);
		}

		private void LoadActiveUnits(EngineASX engine)
		{
			IEnumerable<GameObject> enumerable = UnityObjectHelper.LoadAll<GameObject>("Prefabs/WorldData/ActiveUnits/");
			loadedActiveUnits.Clear();
			foreach (GameObject item in enumerable)
			{
				loadedActiveUnits[item.name] = item;
			}
		}

		public GameObject GetActiveUnit(string className)
		{
			GameObject value = null;
			if (loadedActiveUnits.TryGetValue(className, out value))
			{
				return value;
			}
			return null;
		}

		private void LoadUnitRenderSprites(EngineASX engine)
		{
			IEnumerable<Sprite> enumerable = UnityObjectHelper.LoadAll<Sprite>("Textures/UI/UnitRenders");
			loadedUnitRenderSprites.Clear();
			foreach (Sprite sprite in enumerable)
			{
				foreach (UnitClass item in engine.UnitClasses.Where((UnitClass e) => e.DisplayData != null && (string.Equals(sprite.name, e.DisplayData.RenderIconSpriteName, StringComparison.InvariantCultureIgnoreCase) || string.Equals(e.DisplayData.gameObject.name.Replace("DisplayData", string.Empty), sprite.name, StringComparison.InvariantCultureIgnoreCase))))
				{
					loadedUnitRenderSprites[item.UniqueID] = sprite;
				}
			}
		}

		private void LoadUnitIconSprites(EngineASX engine)
		{
			IEnumerable<Sprite> enumerable = UnityObjectHelper.LoadAll<Sprite>("Textures/UI/UnitIcons");
			loadedUnitIconSprites.Clear();
			foreach (Sprite sprite in enumerable)
			{
				foreach (UnitClass item in engine.UnitClasses.Where((UnitClass e) => e.DisplayData != null && (string.Equals(sprite.name, e.DisplayData.RenderIconSpriteName, StringComparison.InvariantCultureIgnoreCase) || string.Equals(e.DisplayData.gameObject.name.Replace("DisplayData", string.Empty), sprite.name, StringComparison.InvariantCultureIgnoreCase))))
				{
					loadedUnitIconSprites[item.UniqueID] = sprite;
				}
			}
		}

		private void LoadUnitThumbnailSprites(EngineASX engine)
		{
			IEnumerable<Sprite> enumerable = UnityObjectHelper.LoadAll<Sprite>("Textures/UI/UnitThumbnails");
			loadedUnitThumbnailSprites.Clear();
			foreach (Sprite sprite in enumerable)
			{
				foreach (UnitClass item in engine.UnitClasses.Where((UnitClass e) => e.DisplayData != null && string.Equals(e.DisplayData.ThumbnailSpriteName, sprite.name, StringComparison.InvariantCultureIgnoreCase)))
				{
					loadedUnitThumbnailSprites[item.UniqueID] = sprite;
				}
			}
		}

		private void LoadComponentTypeSprites(EngineASX engine)
		{
			IEnumerable<Sprite> enumerable = UnityObjectHelper.LoadAll<Sprite>("Textures/UI/ComponentBayTypeIcons");
			loadedComponentBayTypeIcons.Clear();
			foreach (Sprite sprite in enumerable)
			{
				foreach (ComponentBayType item in engine.ComponentBayTypes.Where((ComponentBayType e) => e.GetIconSpriteName() == sprite.name))
				{
					loadedComponentBayTypeIcons[item.UniqueId] = sprite;
				}
			}
		}

		private void LoadComponentClassSprites(EngineASX engine)
		{
			IEnumerable<Sprite> enumerable = UnityObjectHelper.LoadAll<Sprite>("Textures/UI/ComponentIcons");
			loadedComponentClassIcons.Clear();
			foreach (Sprite sprite in enumerable)
			{
				foreach (ComponentClass item in engine.ComponentClasses.Where((ComponentClass e) => e.GetIconSpriteName() == sprite.name))
				{
					loadedComponentClassIcons[item.UniqueId] = sprite;
				}
			}
		}

		private void LoadCargoSprites(EngineASX engine)
		{
			IEnumerable<Sprite> enumerable = UnityObjectHelper.LoadAll<Sprite>("Textures/UI/CargoIcons");
			loadedCargoIcons.Clear();
			foreach (Sprite sprite in enumerable)
			{
				CargoClass cargoClass = engine.CargoClasses.FirstOrDefault((CargoClass e) => e.ClassName == sprite.name || e.SpriteName == sprite.name);
				if (cargoClass != null)
				{
					loadedCargoIcons[cargoClass.UniqueId] = sprite;
				}
			}
		}

		public Sprite GetCargoSpriteOrDefault(CargoClass cargoClass)
		{
			Sprite sprite = GetCargoSprite(cargoClass);
			if (sprite == null)
			{
				sprite = DefaultCargoIconSprite;
			}
			return sprite;
		}

		public Sprite GetComponentBayTypeSprite(ComponentBayType componentBayType)
		{
			if (componentBayType != null)
			{
				Sprite value = null;
				if (loadedComponentBayTypeIcons.TryGetValue(componentBayType.UniqueId, out value))
				{
					return value;
				}
			}
			return null;
		}

		public Sprite GetComponentBayTypeSpriteOrDefault(ComponentBayType componentBayType)
		{
			Sprite componentBayTypeSprite = GetComponentBayTypeSprite(componentBayType);
			if (componentBayTypeSprite == null)
			{
				return DefaultComponentBayTypeSprite;
			}
			return componentBayTypeSprite;
		}

		public Sprite GetCargoSprite(CargoClass cargoClass)
		{
			if (cargoClass != null)
			{
				Sprite value = null;
				if (loadedCargoIcons.TryGetValue(cargoClass.UniqueId, out value))
				{
					return value;
				}
			}
			return null;
		}

		public Sprite GetComponentSpriteOrDefault(ComponentBase component)
		{
			if (component != null)
			{
				return GetComponentClassSpriteOrDefault(component.ComponentClass);
			}
			return DefaultComponentIconSprite;
		}

		public Sprite GetBayComponentOrAmmoSprite(ComponentBay componentBay, ComponentClass installedComponentClass)
		{
			if (componentBay != null)
			{
				if (componentBay != null && installedComponentClass != null)
				{
					Sprite componentClassSprite = GetComponentClassSprite(installedComponentClass);
					if (componentClassSprite != null)
					{
						return componentClassSprite;
					}
				}
				return GetComponentBayTypeSpriteOrDefault(componentBay.BayType);
			}
			return DefaultComponentBayTypeSprite;
		}

		public Sprite GetBayComponentOrAmmoSprite(ComponentBay componentBay)
		{
			if (componentBay != null)
			{
				if (componentBay != null && componentBay.InstalledComponent != null)
				{
					ProjectileTurretComponent projectileTurretComponent = componentBay.InstalledComponent as ProjectileTurretComponent;
					if (projectileTurretComponent != null && projectileTurretComponent.CurProjectileClass != null && projectileTurretComponent.CurProjectileClass.AmmoClass != null)
					{
						Sprite cargoSprite = GetCargoSprite(projectileTurretComponent.CurProjectileClass.AmmoClass);
						if (cargoSprite != null)
						{
							return cargoSprite;
						}
					}
					Sprite componentClassSprite = GetComponentClassSprite(componentBay.InstalledComponent.ComponentClass);
					if (componentClassSprite != null)
					{
						return componentClassSprite;
					}
				}
				return GetComponentBayTypeSpriteOrDefault(componentBay.BayType);
			}
			return DefaultComponentBayTypeSprite;
		}

		public Sprite GetUnitClassThumbnailIconSpriteOrDefault(UnitClass unitClass)
		{
			Sprite value = null;
			if (loadedUnitThumbnailSprites.TryGetValue(unitClass.UniqueID, out value))
			{
				return value;
			}
			return DefaultUnitClassThumbnailSprite;
		}

		public Sprite GetUnitClassRenderSpriteOrDefault(UnitClass unitClass)
		{
			Sprite value = null;
			if (loadedUnitRenderSprites.TryGetValue(unitClass.UniqueID, out value))
			{
				return value;
			}
			return DefaultUnitClassRenderSprite;
		}

		public Sprite GetUnitClassIconSpriteOrDefault(UnitClass unitClass)
		{
			Sprite value = null;
			if (loadedUnitIconSprites.TryGetValue(unitClass.UniqueID, out value))
			{
				return value;
			}
			return DefaultUnitClassRenderSprite;
		}

		public Sprite GetComponentClassSprite(ComponentClass componentClass)
		{
			Sprite value = null;
			if (loadedComponentClassIcons.TryGetValue(componentClass.UniqueId, out value))
			{
				return value;
			}
			return null;
		}

		public Sprite GetComponentClassOrBaySpriteOrDefault(ComponentClass componentClass)
		{
			Sprite componentClassSprite = GetComponentClassSprite(componentClass);
			if (componentClassSprite != null)
			{
				return componentClassSprite;
			}
			return GetComponentBayTypeSpriteOrDefault(componentClass.ComponentBayType);
		}

		public Sprite GetComponentClassSpriteOrDefault(ComponentClass componentClass)
		{
			Sprite componentClassSprite = GetComponentClassSprite(componentClass);
			if (componentClassSprite == null)
			{
				return DefaultComponentIconSprite;
			}
			return componentClassSprite;
		}

		public Sprite GetUnitThumbnailSpriteOrCargoClassSprite(Unit unit)
		{
			if (unit.CargoComponent != null && unit.CargoComponent.CargoClass != null)
			{
				return GetCargoSpriteOrDefault(unit.CargoComponent.CargoClass);
			}
			return GetUnitClassThumbnailIconSpriteOrDefault(unit.UnitClass);
		}
	}
}
