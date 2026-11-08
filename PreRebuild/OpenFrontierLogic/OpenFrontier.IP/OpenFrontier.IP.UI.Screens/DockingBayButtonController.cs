using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens
{
	public class DockingBayButtonController : MonoBehaviour
	{
		public ScreenDeactivateType ButtonDeactivateType;

		private int oldDockedShipsCount = -1;

		public Button Button;

		public Unit Unit;

		[FormerlySerializedAs("DockedShipsLabel")]
		public Text DockedShipsLabelObsolete;

		public TextMeshProUGUI DockedShipsLabel;

		private void Awake()
		{
			if (Button == null)
			{
				Button = GetComponent<Button>();
			}
		}

		private void Start()
		{
			if (Button != null)
			{
				Button.onClick.AddListener(ButtonClick);
			}
		}

		private void ButtonClick()
		{
			UnitHangar hangar = Unit.GetHangar();
			if (hangar != null)
			{
				UIController.Instance.ScreenNavigator.ShowDockedShipsScreen(hangar);
			}
		}

		private void Update()
		{
			Refresh();
		}

		public void Refresh()
		{
			RefreshButtonInteractable();
			RefreshCountLabel();
		}

		private void RefreshCountLabel()
		{
			if (Unit != null && Unit.IsDockable)
			{
				int dockedUnitCount = Unit.Components.HangarComponent.DockedUnitCount;
				if (dockedUnitCount == oldDockedShipsCount)
				{
					return;
				}
				if (dockedUnitCount > 0)
				{
					if (DockedShipsLabel != null)
					{
						DockedShipsLabel.color = Color.white;
						DockedShipsLabel.text = $"Hangar ({dockedUnitCount})";
					}
					else
					{
						DockedShipsLabelObsolete.color = Color.white;
						DockedShipsLabelObsolete.text = $"Hangar ({dockedUnitCount})";
					}
				}
				else
				{
					if (DockedShipsLabel != null)
					{
						DockedShipsLabel.color = GameController.Instance.GameSettings.UIButtonColors.NoItemsTextColor;
					}
					else
					{
						DockedShipsLabelObsolete.color = GameController.Instance.GameSettings.UIButtonColors.NoItemsTextColor;
					}
					SetDefaultLabelText();
				}
				oldDockedShipsCount = dockedUnitCount;
			}
			else
			{
				Reset();
			}
		}

		private void Reset()
		{
			SetDefaultLabelText();
			oldDockedShipsCount = 0;
		}

		private void SetDefaultLabelText()
		{
			if (DockedShipsLabel != null)
			{
				DockedShipsLabel.text = "Hangar";
			}
			else
			{
				DockedShipsLabelObsolete.text = "Hangar";
			}
		}

		private void RefreshButtonInteractable()
		{
			if (Button != null)
			{
				if (ButtonDeactivateType == ScreenDeactivateType.Disable)
				{
					Button.interactable = ShouldButtonBeEnabled();
				}
				else
				{
					Button.gameObject.SetActive(ShouldButtonBeEnabled());
				}
			}
		}

		public bool ShouldButtonBeEnabled()
		{
			if (Unit != null && Unit.IsValidAndNotDestroyed && Unit.Engine.World.Permissions.AllowDockUIDockedShips && Unit.GetHangar() != null)
			{
				if (EngineASX.Instance.LocalFaction != null)
				{
					if (!Unit.HasSameRootUnitAsPlayer())
					{
						return EngineASX.Instance.LocalFaction.Intel.IsUnitDiscoveredOrOwned(Unit);
					}
					return true;
				}
				return false;
			}
			return false;
		}
	}
}
