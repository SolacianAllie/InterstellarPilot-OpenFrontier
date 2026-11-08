using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class UnitContextButton : MonoBehaviour
	{
		public bool ShowRenderSpriteInIconImage;

		public Image UnitIconImage;

		public Image CargoIconImage;

		public bool HideWhenNoUnit = true;

		public Button Button;

		public Unit Unit;

		public bool Interactable = true;

		private TextMeshProUGUI label;

		public bool ShortName = true;

		public bool HandleClick = true;

		private void Awake()
		{
			Button.onClick.AddListener(ButtonClick);
			label = Button.GetComponentInChildren<TextMeshProUGUI>();
			if (CargoIconImage != null)
			{
				CargoIconImage.enabled = false;
			}
		}

		private void Start()
		{
			Refresh();
		}

		private void Update()
		{
			RefreshIsInteractable();
			if (Unit != null)
			{
				RefreshLabelColor();
			}
		}

		private void RefreshLabelColor()
		{
			if (label != null)
			{
				label.color = EngineASX.Instance.GetFactionHostilityColorForPlayerTarget(Unit.Faction);
			}
		}

		public void RefreshIsInteractable()
		{
			if (Button.gameObject.activeSelf)
			{
				Button.interactable = Interactable && WorldHelper.CanShowUnitContext(Unit);
			}
		}

		public void Refresh()
		{
			if (Button == null)
			{
				Debug.LogError("Missing button", this);
				return;
			}
			if (CargoIconImage != null)
			{
				CargoIconImage.enabled = Unit != null && Unit.CargoComponent != null && Unit.CargoComponent.CargoClass != null && UnitIconImage != null && ShowRenderSpriteInIconImage;
				if (CargoIconImage.enabled)
				{
					CargoIconImage.sprite = Unit.CargoComponent.CargoClass.GetCargoSpriteOrDefault();
				}
			}
			if (Unit != null && Unit.IsValidAndNotDestroyed && WorldHelper.CanShowUnitContext(Unit))
			{
				Button.gameObject.SetActive(value: true);
				if (label != null)
				{
					label.text = Unit.GetFriendlyNameForFaction(EngineASX.Instance.LocalFaction, ShortName);
				}
				RefreshLabelColor();
				if (UnitIconImage != null)
				{
					if (ShowRenderSpriteInIconImage)
					{
						UnitIconImage.sprite = Unit.UnitClass.GetRenderSprite();
					}
					else
					{
						UnitIconImage.sprite = Unit.UnitClass.GetThumbnailIconSprite();
					}
				}
			}
			else if (HideWhenNoUnit)
			{
				Button.gameObject.SetActive(value: false);
			}
			else if (UnitIconImage != null)
			{
				UnitIconImage.sprite = GameController.Instance.ContextOptionsSprite;
			}
			RefreshIsInteractable();
		}

		public void SetUnit(Unit unit)
		{
			Unit = unit;
			Refresh();
		}

		private void ButtonClick()
		{
			if (HandleClick)
			{
				UIController.Instance.ScreenNavigator.ShowUnitContextMenuScreen(Unit);
			}
		}
	}
}
