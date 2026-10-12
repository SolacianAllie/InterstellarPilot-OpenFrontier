using System;
using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI.Extensions;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class CreditsOptions : MonoBehaviour
	{
		public List<int> CreditValues = new List<int>();

		public Transform ButtonsTransform;

		public Button ButtonPrefab;

		public Toggle FactionTargetPlayerToggle;

		public Toggle FactionTargetCurrentTargetToggle;

		public FactionTarget FactionTarget
		{
			get
			{
				if (FactionTargetCurrentTargetToggle.isOn)
				{
					return FactionTarget.CurrentTargetFaction;
				}
				return FactionTarget.PlayerFaction;
			}
		}

		private void Awake()
		{
			CreateButtons();
		}

		private void CreateButtons()
		{
			CreateButton(TestCreditsOperation.Set, 0);
			foreach (TestCreditsOperation value in Enum.GetValues(typeof(TestCreditsOperation)))
			{
				foreach (int creditValue in CreditValues)
				{
					CreateButton(value, creditValue);
				}
			}
		}

		private void CreateButton(TestCreditsOperation operation, int creditsValue)
		{
			CreateButtonForCargoClass(operation, creditsValue).onClick.AddListener(() =>
			{
				Faction faction = GetFaction();
				if (faction != null)
				{
					ApplyToFaction(operation, creditsValue, faction);
				}
				else
				{
					UIController.Instance.ShowError("Unable to apply credits change");
				}
			});
		}

		private void ApplyToFaction(TestCreditsOperation operation, int creditsValue, Faction faction)
		{
			int credits = faction.Credits;
			switch (operation)
			{
			case TestCreditsOperation.Add:
				faction.ApplyTransaction(creditsValue, FactionTransactionType.Gift);
				break;
			case TestCreditsOperation.Remove:
				faction.ApplyTransaction(-creditsValue, FactionTransactionType.Gift);
				break;
			case TestCreditsOperation.Set:
			{
				int creditsValue2 = creditsValue - faction.Credits;
				faction.ApplyTransaction(creditsValue2, FactionTransactionType.Gift);
				break;
			}
			}
			int num = faction.Credits - credits;
			if (num != 0)
			{
				UIController.Instance.ShowMessageBox(string.Format("{0:N0} credits {1} faction {2}", Mathf.Abs(num), (num > 0) ? "added to" : "removed from", faction.GetLongNameElseShort()));
			}
		}

		private Faction GetFaction()
		{
			switch (FactionTarget)
			{
			case FactionTarget.PlayerFaction:
				return EngineASX.Instance.LocalFaction;
			case FactionTarget.CurrentTargetFaction:
				if (EngineASX.Instance.Hud != null && EngineASX.Instance.Hud.CurrentTarget != null && EngineASX.Instance.Hud.CurrentTarget.Faction != null)
				{
					return EngineASX.Instance.Hud.CurrentTarget.Faction;
				}
				throw new Exception("No current target faction");
			default:
				throw new Exception("Unknown target faction");
			}
		}

		private Button CreateButtonForCargoClass(TestCreditsOperation creditsOperation, int value)
		{
			Button button = UnityEngine.Object.Instantiate(ButtonPrefab);
			button.transform.SetParent(ButtonsTransform);
			button.transform.localScale = Vector3.one;
			if (creditsOperation == TestCreditsOperation.Set && value == 0)
			{
				button.SetText("Remove All");
			}
			else
			{
				button.SetText($"{creditsOperation} {value:N0}");
			}
			return button;
		}
	}
}
