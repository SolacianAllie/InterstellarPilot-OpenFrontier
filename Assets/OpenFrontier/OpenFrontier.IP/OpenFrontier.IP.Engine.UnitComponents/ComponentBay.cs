using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class ComponentBay : MonoBehaviour
	{
		public ComponentBayType BayType;

		private bool hasInit;

		public int Id = -1;

		public ComponentClass InitialComponentClass;

		private ComponentBase installedComponent;

		/// <summary>
		/// The ship class this bay evaluates against, when
		/// <see cref="OverrideUnitClassRating"/> is set.
		/// </summary>
		/// <remarks>
		/// Renamed from MaxClass, and the rename is not cosmetic. "MaxClass"
		/// said nothing about ship, and there is now a Class on this bay that
		/// means something else entirely - the module class a component must be
		/// to fit. Two fields both called a class, on the same inspector row,
		/// one a hull rating and one a part number, is how the wrong one gets
		/// edited.
		///
		/// FormerlySerializedAs is not optional here. Unity matches serialized
		/// data to fields by name, so a plain rename resets every value in
		/// every prefab to the field initializer - Fighter - and this field is
		/// live on all 1,237 bays with six values in use, on 330 of which
		/// OverrideUnitClassRating is set and this number is what the bay
		/// compares against. That reset would be silent: no missing-reference
		/// error, no warning, just 330 slots quietly rating every ship as a
		/// fighter and accepting or refusing the wrong components.
		///
		/// It is genuinely a maximum. IsComponentCorrectRating does
		/// `num <= maxHullType` against this value, so it bounds the top of the
		/// range the component declares.
		/// </remarks>
		[FormerlySerializedAs("MaxClass")]
		public ShipHullType MaxShipClass = ShipHullType.Fighter;

		/// <summary>
		/// The largest class of component this slot takes. Anything equal to or
		/// below it fits; anything above is refused.
		/// </summary>
		/// <remarks>
		/// Pairs with ComponentClass.Class. It is a ceiling, not a pin: a
		/// Class8 bay takes Class1 through Class8, and a Class2 bay takes Class1
		/// and Class2 only. A slot never accepts something bigger than itself,
		/// which is the whole point of having a ceiling at all.
		///
		/// RequireExactFit is what turns a ceiling into a pin, for a slot that
		/// is meant to hold one specific part.
		///
		/// Class1 by default, and Class1 is what all 141 components are, because
		/// a new field reads as zero from every prefab that predates it and
		/// ModuleClass.Class1 is zero. So a Class1 bay accepts every component
		/// in the game today, exactly as before this field existed. Order of
		/// operations matters here: label the parts first, then raise the
		/// ceilings, or the ceilings start refusing parts nobody has named yet.
		/// </remarks>
		public ModuleClass Class = ModuleClass.Class1;

		/// <summary>
		/// Pin this slot to one exact size and one exact class, instead of
		/// accepting anything at or below them.
		/// </summary>
		/// <remarks>
		/// Off by default, so nothing changes until it is switched on per bay.
		/// The loose rule - small fits big, low class fits high class - is right
		/// for a weapon hardpoint, where a large mount carrying a light cannon is
		/// a sensible thing to want, and wrong for a slot meant to take one
		/// specific part, which is what this is for.
		///
		/// Governs size and class together because they answer the same
		/// question - how big a part belongs in this - and a slot that is pinned
		/// is pinned in both. Splitting them would allow a Core Internal to be
		/// pinned by class while still quietly taking anything that fits, which
		/// is not a pin.
		///
		/// Same zero-default reasoning as everything else added here: a new
		/// field on a MonoBehaviour reads as false from every prefab that
		/// predates it, and false is the behaviour that already ships.
		/// </remarks>
		public bool RequireExactFit;

		public bool OverrideUnitClassRating;

		private UnitComponentHolder unitComponents;

		public Vector3 WeaponArc = new Vector3(0f, 90f, 0f);

		/// <summary>
		/// How big this slot is. A component that does not fit will not install.
		/// </summary>
		/// <remarks>
		/// On the bay rather than on the bay *type*, deliberately: a ship has
		/// small hardpoints and large ones at the same time, which is the entire
		/// point of having sizes. Putting it on ComponentBayType would give
		/// every turret slot on every ship the same size.
		///
		/// Small by default, so every existing ship is unchanged until
		/// something is deliberately made bigger.
		/// </remarks>
		public HardpointSize Size = HardpointSize.Small;

		/// <summary>
		/// Which family of hardware this slot is: weapon, ship system, or a
		/// mount. Decides what may go in it and what the trade screen groups it
		/// under.
		/// </summary>
		/// <remarks>
		/// On the bay rather than on the bay type, for the same reason Size is:
		/// a ship can have weapon slots and system slots at once, and two ships
		/// of the same class can disagree about what a given slot is for. The
		/// bay type says what the slot IS - shield, engine - and this says what
		/// the rules around it are.
		///
		/// Legacy by default, so every existing bay lands somewhere real and
		/// the trade screen opens on a category that shows all of them. See
		/// ComponentCategory for why zero is a named value rather than the first family.
		/// </remarks>
		public ComponentCategory Category = ComponentCategory.Legacy;

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

		/// <summary>
		/// Draws this slot's size, and what it can shoot.
		/// </summary>
		/// <remarks>
		/// Here rather than on the turret component because here is where the
		/// numbers are: WeaponArc and Size are both ComponentBay fields, and
		/// this is the object you select to change them. Gizmos only draw for
		/// the selected object, so hanging the arc off the turret meant that
		/// selecting a bay - the thing you are actually editing - showed
		/// nothing at all.
		///
		/// Drawn on a prefab as well as in a game, since a ship is edited as a
		/// prefab and has no installed component until it is instantiated.
		/// </remarks>
		private void OnDrawGizmosSelected()
		{
			ComponentGizmos.DrawBay(this);
		}

		public bool IsComponentCorrectType(ComponentClass componentClass)
		{
			if (componentClass.ComponentBayType != BayType)
			{
				return false;
			}

			// Category. Strict, with no exemption for Legacy.
			//
			// An earlier version skipped this check while either side was still
			// Legacy, so that a half-finished pass could not lock every ship in
			// the game. That was the wrong instinct here: the legacy parts are
			// being retired, not migrated, so legacy fitting only legacy is the
			// point. A bay given a real category takes only parts of that
			// category, and stays empty until some exist.
			//
			// ComponentBayType cannot carry this. It says what shape the slot is
			// - an engine mount, a shield mount - not what the hardware is for,
			// and it cannot express a rule that spans two bay types both of
			// which happen to accept, say, shields.
			//
			// Nothing in the game is refused today: all 1,237 bays and all 141
			// components are Legacy.
			if (componentClass.Category != Category)
			{
				return false;
			}

			// Size and class are the same kind of question and get the same answer, so
			// they are checked together.
			//
			// Both are maxima by default. A slot takes a part of its own size or
			// smaller, and a part of its own class or lower, never higher. A
			// Class8 hardpoint therefore takes anything from Class1 up, which is
			// what a big mount is for, and a Class2 bay refuses a Class5 module
			// rather than pretending to hold it.
			//
			// RequireExactFit turns both into equality, for a slot meant to take
			// one specific part. It then refuses a smaller size as well, which is
			// the point of it: a Core Internal set to class 5 that would accept a
			// class 3 module is not pinned to anything.
			//
			// Nothing is refused today. Every bay is Small and Class1, every
			// component is Small and Class1, and the flag is off on all 1,237
			// bays, so both comparisons hold for everything in the game.
			if (RequireExactFit)
			{
				return componentClass.Size == Size && componentClass.Class == Class;
			}

			return componentClass.Size <= Size && componentClass.Class <= Class;
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
				num = (int)MaxShipClass;
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
				return MaxShipClass;
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
