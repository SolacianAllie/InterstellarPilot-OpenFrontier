using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class FactionContextButton : MonoBehaviour
	{
		public FactionContextButtonNoFactionMode HideWhenNoFactionMode;

		public Button Button;

		public Faction Faction;

		public bool Interactable = true;

		private TextMeshProUGUI label;

		public bool ShortName = true;

		private void Awake()
		{
			Button.onClick.AddListener(ButtonClick);
			label = Button.GetComponentInChildren<TextMeshProUGUI>();
		}

		private void Start()
		{
			Refresh();
		}

		private void Update()
		{
			RefreshIsInteractable();
		}

		public void RefreshIsInteractable()
		{
			if (Button.gameObject.activeSelf)
			{
				Button.interactable = Interactable && Faction != null && Faction.IsValidInGame;
			}
		}

		public void Refresh()
		{
			if (Button == null)
			{
				Debug.LogError("Missing button", this);
				return;
			}
			if (Faction != null)
			{
				Button.gameObject.SetActive(value: true);
				Button.interactable = true;
				if (label != null)
				{
					label.text = Faction.GetDescriptiveFactionNameIncludingPilotName(ShortName);
					label.color = EngineASX.Instance.GetFactionHostilityColorForPlayerTarget(Faction);
				}
			}
			else
			{
				switch (HideWhenNoFactionMode)
				{
				case FactionContextButtonNoFactionMode.Hide:
					Button.gameObject.SetActive(value: false);
					break;
				case FactionContextButtonNoFactionMode.ShowNoFactionText:
					if (label != null)
					{
						label.text = "[None]";
						label.color = EngineASX.Instance.AttitudeNeutralColor;
						Button.interactable = false;
					}
					break;
				}
			}
			RefreshIsInteractable();
		}

		public void SetFaction(Faction faction)
		{
			Faction = faction;
			Refresh();
		}

		private void ButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowFactionsScreen(Faction);
		}
	}
}
