using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Screens.Hud.Components;
using OpenFrontier.IP.UI.Screens.Hud.CurrentTargetUI.WeaponsReadyIndicator;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class TurretGridItemUI : ScrollListItem<TurretComponent>
	{
		public TextMeshProUGUI ComponentShortIconName;

		public Graphic PoweredDownIcon;

		public TurretComponentUI ShipComponentUI;

		public Slider ChargeSlider;

		public WeaponsReadyIndicatorItem WeaponsReadyIndicatorItem;

		public Image ReadyToFireIndicatorImage;

		private const float checkAmmoStuffInterval = 0.05f;

		public Image ActivatedSprite;

		public TextMeshProUGUI AmmoLabel;

		public Color ChargedColor = Color.green;

		public Color ChargingColor = Color.yellow;

		public Toggle FireToggle;

		public Image FiringArcSprite;

		private int lastAmmoCount = -1;

		private float nextCheckAmmoStuff;

		public Image NextProjectileSprite;

		public Button SwitchProjectileButton;

		public Graphic ChargeGraphic;

		public Image TurretIconSprite;

		public TurretGridUI TurretGrid => (TurretGridUI)ParentList;

		private void Awake()
		{
			FireToggle.onValueChanged.AddListener(TryToggleActivated);
		}

		public override void Refresh()
		{
			base.Refresh();
			if (Item != null)
			{
				RefreshAmmoLabel();
				TryAutoLoadTurret();
				RefreshSprite();
				RefreshFiringArcSprite();
				ShipComponentUI.Component = Item;
			}
		}

		public void ToggleTurretActivated()
		{
			((TurretGridUI)ParentList).ToggleTurretActivated(Item);
		}

		public void RefreshVolatile()
		{
			WeaponsReadyIndicatorItem.Refresh(Item);
			if (ActivatedSprite != null)
			{
				ActivatedSprite.gameObject.SetActive(Item.IsFiring);
			}
			if (FireToggle != null)
			{
				FireToggle.interactable = TurretGridUI.CanToggleTurretActivated(Item, EngineASX.Instance.Hud.CurrentTarget);
			}
			bool flag = Item.CanChargeTurret();
			bool flag2 = (Item.IsPoweredAndEnergySupplied & flag) && !Item.HasFullEnergyCharge;
			ChargeSlider.gameObject.SetActive(flag2);
			if (flag2)
			{
				ChargeSlider.value = Item.ChargedEnergyNormalized;
				ChargeGraphic.color = (Item.HasMinimumEnergyCharge ? ChargedColor : ChargingColor);
			}
			RefreshAmmoLabel();
			if (Time.time > nextCheckAmmoStuff)
			{
				nextCheckAmmoStuff = Time.time + 0.05f;
				ProjectileClass nextProjectileClass = GetNextProjectileClass();
				if (nextProjectileClass != null && nextProjectileClass.AmmoClass != null)
				{
					NextProjectileSprite.sprite = EngineASX.Instance.EngineResources.GetCargoSpriteOrDefault(nextProjectileClass.AmmoClass);
				}
				bool active = nextProjectileClass != null;
				SwitchProjectileButton.gameObject.SetActive(active);
				if (!Item.IsFiring)
				{
					TryAutoLoadTurret();
				}
			}
		}

		private ProjectileClass GetNextProjectileClass()
		{
			ProjectileTurretComponent projectileTurretComponent = Item as ProjectileTurretComponent;
			ProjectileClass result = null;
			if (projectileTurretComponent != null)
			{
				result = projectileTurretComponent.GetNextProjectileClass(ignoreAmmo: false, conventionalWeaponsOnly: false);
			}
			return result;
		}

		private void Start()
		{
			SwitchProjectileButton.onClick.AddListener(SwitchAmmo);
		}

		private void TryToggleActivated(bool value)
		{
			if (TurretGridUI.CanToggleTurretActivated(Item, EngineASX.Instance.Hud.CurrentTarget))
			{
				TurretGrid.ToggleTurretActivated(Item);
			}
		}

		private void SwitchAmmo()
		{
			if (!Item.IsFiring)
			{
				ProjectileTurretComponent projectileTurretComponent = Item as ProjectileTurretComponent;
				if (projectileTurretComponent != null)
				{
					projectileTurretComponent.TrySelectNextProjectileClass(ignoreAmmo: false, conventionalWeaponsOnly: false);
					RefreshSprite();
				}
			}
		}

		private void TryAutoLoadTurret()
		{
			ProjectileTurretComponent projectileTurretComponent = Item as ProjectileTurretComponent;
			if (projectileTurretComponent != null)
			{
				AutoLoadTurret(projectileTurretComponent);
			}
		}

		private void RefreshFiringArcSprite()
		{
			bool flag = Item.ShouldShowFiringArcSprite();
			FiringArcSprite.enabled = flag;
			if (flag)
			{
				FiringArcSprite.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f - Item.Bay.transform.localRotation.eulerAngles.y));
				FiringArcSprite.sprite = Item.Bay.GetFiringArcSprite();
			}
		}

		private void RefreshSprite()
		{
			TurretIconSprite.sprite = EngineASX.Instance.EngineResources.GetBayComponentOrAmmoSprite(Item.Bay);
			string shortIconName = Item.TurretClass.ShortIconName;
			ComponentShortIconName.enabled = !string.IsNullOrWhiteSpace(shortIconName);
			if (ComponentShortIconName.enabled)
			{
				ComponentShortIconName.text = shortIconName;
			}
			if (Item.TurretClass is LaserTurretClass)
			{
				TurretIconSprite.rectTransform.SetRight(0f);
				TurretIconSprite.rectTransform.SetBottom(0f);
			}
			else
			{
				TurretIconSprite.rectTransform.SetRight(10f);
				TurretIconSprite.rectTransform.SetBottom(10f);
			}
		}

		private void RefreshAmmoLabel()
		{
			AmmoLabel.enabled = Item.UsesAmmo;
			if (!Item.UsesAmmo)
			{
				return;
			}
			int getAvailableAmmoCount = Item.GetAvailableAmmoCount;
			if (getAvailableAmmoCount != lastAmmoCount)
			{
				lastAmmoCount = getAvailableAmmoCount;
				if (getAvailableAmmoCount > 0)
				{
					AmmoLabel.text = getAvailableAmmoCount.ToString();
				}
				else
				{
					AmmoLabel.text = "-";
				}
			}
		}

		private void Update()
		{
			if (Item != null && Item.UnitComponents != null)
			{
				RefreshVolatile();
			}
		}

		private void AutoLoadTurret(ProjectileTurretComponent p)
		{
			ProjectileClass curProjectileClass = p.CurProjectileClass;
			if ((p.CurProjectileClass == null || (p.UsesAmmo && !p.HasRequiredAmmo)) && !p.TrySelectNextProjectileClass(ignoreAmmo: false, conventionalWeaponsOnly: false))
			{
				p.SelectFirstProjectileClass(conventionalWeaponsOnly: false);
			}
			if (p.CurProjectileClass != curProjectileClass)
			{
				RefreshSprite();
			}
		}

		public void OnPlayerUnitChanged()
		{
			ShipComponentUI.ClearFlashState();
		}

		public override void CleanupOnDisable()
		{
			ShipComponentUI.ClearFlashState();
		}
	}
}
