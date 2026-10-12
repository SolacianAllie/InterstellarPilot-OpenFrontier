using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class ComponentBay : MonoBehaviour
	{
		public ComponentBayType BayType;

		private bool hasInit;

		public int Id = -1;

		public ComponentClass InitialComponentClass;

		private ComponentBase installedComponent;

		public ShipHullType MaxClass = ShipHullType.Fighter;

		public bool OverrideUnitClassRating;

		private UnitComponentHolder unitComponents;

		public Vector3 WeaponArc = new Vector3(0f, 90f, 0f);

		public ComponentClass InstalledComponentClass
		{
			get
			{
				if (installedComponent != null)
				{
					return installedComponent.ComponentClass;
				}
				return null;
			}
		}

		public ComponentBase InstalledComponent
		{
			get
			{
				return installedComponent;
			}
			set
			{
				if (installedComponent != value)
				{
					ComponentBase componentBase = installedComponent;
					installedComponent = value;
					if (componentBase != null && componentBase.Bay == this)
					{
						componentBase.Bay = null;
					}
					if (installedComponent != null)
					{
						installedComponent.Bay = this;
					}
					if (LogWrapper.LogMsgs)
					{
						LogWrapper.Log($"{this}: InstalledComponent changed from {componentBase} to {installedComponent}", this, 3);
					}
				}
			}
		}

		public Unit Unit
		{
			get
			{
				if (unitComponents != null)
				{
					return unitComponents.Unit;
				}
				return null;
			}
		}

		public UnitComponentHolder UnitComponents
		{
			get
			{
				return unitComponents;
			}
			private set
			{
				if (!(unitComponents != value))
				{
					return;
				}
				UnitComponentHolder unitComponentHolder = unitComponents;
				if (installedComponent != null)
				{
					installedComponent.Bay = null;
				}
				unitComponents = value;
				if (unitComponentHolder != null)
				{
					unitComponentHolder.DeregisterComponentBay(this);
				}
				if (unitComponents != null)
				{
					unitComponents.RegisterComponentBay(this);
					if (installedComponent != null)
					{
						installedComponent.Bay = this;
					}
				}
			}
		}

		public bool IsModded
		{
			get
			{
				if (InitialComponentClass != null)
				{
					if (InstalledComponent == null || InstalledComponent.ComponentClass != InitialComponentClass)
					{
						return true;
					}
				}
				else if (InstalledComponent != null)
				{
					return true;
				}
				return false;
			}
		}

		public void Init(UnitComponentHolder unitComponentHolder)
		{
			if (!hasInit)
			{
				hasInit = true;
				UnitComponents = unitComponentHolder;
				if (unitComponents == null)
				{
					Debug.LogError("ComponentBay has not been parented to a Unit", this);
				}
				if (BayType == null)
				{
					Debug.LogWarning($"{this}: BayType has not been set", this);
				}
			}
		}

		public bool ComponentCanBeInstalled(ComponentBase component, bool ignoreExisting)
		{
			if (component.ComponentClass != null)
			{
				return ComponentCanBeInstalled(component.ComponentClass, ignoreExisting);
			}
			return false;
		}

		public bool ComponentCanBeInstalled(ComponentClass componentClass, bool ignoreExisting)
		{
			if (ignoreExisting || installedComponent == null)
			{
				if (IsComponentClassCompatible(componentClass))
				{
					return unitComponents.SupportsAdditionalComponentOfType(componentClass.ComponentType);
				}
				return false;
			}
			return false;
		}

		public void VerifyComponentClass(ComponentClass componentClass)
		{
			if (!(unitComponents != null) || !(componentClass != null))
			{
				return;
			}
			if (installedComponent == null)
			{
				if (IsComponentCorrectType(componentClass))
				{
					if (!IsComponentCorrectRating(componentClass))
					{
						Debug.LogWarning($"{this}: ComponentClass \"{componentClass}\" is not the correct rating", this);
					}
					if (!unitComponents.SupportsAdditionalComponentOfType(componentClass.ComponentType))
					{
						Debug.LogWarning($"{this}: Another component of type \"{componentClass.ComponentType}\" is not supported", this);
					}
				}
				else
				{
					Debug.LogWarning($"{this}: ComponentClass \"{componentClass}\" is not the correct type.", this);
				}
			}
			else
			{
				Debug.LogWarning($"{this}: Installing component in occupied bay.", this);
			}
		}

		public ComponentBase InstallComponent(ComponentClass componentClass)
		{
			DestroyInstalledComponent();
			GameObject gameObject = new GameObject(componentClass.GetFriendlyName());
			componentClass.CreateComponent(gameObject);
			gameObject.transform.SetParent(base.gameObject.transform);
			ComponentBase component = gameObject.GetComponent<ComponentBase>();
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.transform.localRotation = Quaternion.identity;
			component.Init(this);
			return component;
		}

		public void InstallInitialComponent()
		{
			if (InitialComponentClass != null)
			{
				InstallComponent(InitialComponentClass);
			}
		}

		public bool IsComponentClassCompatible(ComponentClass componentClass)
		{
			return IsComponentClassCompatible(componentClass, Unit.UnitClass.HullType);
		}

		public bool IsComponentClassCompatible(ComponentClass componentClass, ShipHullType unitHullType)
		{
			if (IsComponentCorrectType(componentClass))
			{
				return IsComponentCorrectRating(componentClass, unitHullType);
			}
			return false;
		}

		public bool IsComponentCorrectType(ComponentClass componentClass)
		{
			return componentClass.ComponentBayType == BayType;
		}

		public bool IsComponentCorrectRating(ComponentClass componentClass)
		{
			return IsComponentCorrectRating(componentClass, Unit.UnitClass.HullType);
		}

		public bool IsComponentCorrectRating(ComponentClass componentClass, ShipHullType unitHullType)
		{
			int minHullType = (int)componentClass.MinHullType;
			int maxHullType = (int)componentClass.MaxHullType;
			int num = (int)unitHullType;
			if (OverrideUnitClassRating)
			{
				num = (int)MaxClass;
			}
			if (num >= minHullType)
			{
				return num <= maxHullType;
			}
			return false;
		}

		public ShipHullType GetCompatibleHullType(ShipHullType installedHullType)
		{
			if (OverrideUnitClassRating)
			{
				return MaxClass;
			}
			return installedHullType;
		}

		public void DestroyInstalledComponent(bool immediate = false)
		{
			if (installedComponent != null)
			{
				if (immediate)
				{
					Object.DestroyImmediate(installedComponent.gameObject);
				}
				else
				{
					Object.Destroy(installedComponent.gameObject);
				}
				InstalledComponent = null;
			}
		}

		private void OnDestroy()
		{
			UnitComponents = null;
		}

		public string GetFriendlyName()
		{
			if (BayType.UseCustomBayNames)
			{
				return name;
			}
			return BayType.FriendlyName;
		}

		public string GetFriendlyNameAndIfModded()
		{
			string friendlyName = GetFriendlyName();
			if (IsModded)
			{
				return friendlyName + "+";
			}
			return friendlyName;
		}

		public bool GetIsRearFacingOnly()
		{
			if (WeaponArc.y >= 270f)
			{
				return false;
			}
			float y = transform.localEulerAngles.y;
			if (y > 90f)
			{
				return y < 180f;
			}
			return false;
		}

		public Sprite GetFiringArcSprite()
		{
			if (WeaponArc.y == 360f)
			{
				return EngineASX.Instance.WeaponArc360Sprite;
			}
			if (WeaponArc.y == 180f)
			{
				return EngineASX.Instance.WeaponArc180Sprite;
			}
			return EngineASX.Instance.WeaponArc90Sprite;
		}

		public bool ShouldShowFiringArcSpriteConsideringCurrentComponent(bool showIf360degrees = false)
		{
			if (installedComponent != null && !installedComponent.ShouldShowFiringArcSprite())
			{
				return false;
			}
			return ShouldShowFiringArcSprite(showIf360degrees);
		}

		public bool ShouldShowFiringArcSprite(bool showIf360degrees = false)
		{
			if (BayType.BayType == OpenFrontier.IP.Engine.UnitComponents.BayType.Turret)
			{
				return (WeaponArc.y < 360f) | showIf360degrees;
			}
			return false;
		}
	}
}
