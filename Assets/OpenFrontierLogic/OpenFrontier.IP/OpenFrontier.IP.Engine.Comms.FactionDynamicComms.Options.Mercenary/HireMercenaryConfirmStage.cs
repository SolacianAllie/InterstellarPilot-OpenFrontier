using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Mercenaries;
using OpenFrontier.IP.UI;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Mercenary
{
	public class HireMercenaryConfirmStage : DynamicCommsStage
	{
		private Dictionary<ICommsStageOption, MercenaryHireDurationSetting> optionRateSettings = new Dictionary<ICommsStageOption, MercenaryHireDurationSetting>();

		public override void Show(ICommsController commsController)
		{
			base.Show(commsController);
			DynamicCommsStage.stageOptionCache.Clear();
			SetMessageText();
			AddNevermindOption();
			AddRateOptions();
		}

		private void SetMessageText()
		{
			DynamicCommsMessage dynamicCommsMessage = new DynamicCommsMessage();
			dynamicCommsMessage.MessageText = GetHireMessage();
			Message = dynamicCommsMessage;
		}

		private string GetHireMessage()
		{
			return $"You need protection? I'll follow your current ship around and take out your enemies for as long as you need me. My rate is {TextFormattingHelper.FormatCredits(GetHourlyRate())} credits per hour payable in advance. So, what do you say?";
		}

		public int GetHourlyRate()
		{
			Fleet fleet = DynamicCommsHandler.OwnerPilot.GetFleet();
			if (fleet != null)
			{
				FactionAIMercenary factionAIMercenary = fleet.Faction.FactionAI as FactionAIMercenary;
				if (factionAIMercenary != null)
				{
					return factionAIMercenary.GetHourlyRate(fleet, EngineASX.Instance.LocalFaction);
				}
			}
			return 1000;
		}

		private void AddRateOptions()
		{
			optionRateSettings.Clear();
			MercenarySettings mercenarySettings = GameController.Instance.GameSettings.MercenarySettings;
			int hourlyRate = GetHourlyRate();
			foreach (MercenaryHireDurationSetting hireDurationSetting in mercenarySettings.HireDurationSettings)
			{
				CommsStageEventOption commsStageEventOption = new CommsStageEventOption
				{
					OptionText = GetDurationSettingDisplayText(hireDurationSetting, hourlyRate)
				};
				optionRateSettings.Add(commsStageEventOption, hireDurationSetting);
				commsStageEventOption.Selected += ConfirmOption_Selected;
				DynamicCommsStage.stageOptionCache.Add(commsStageEventOption);
			}
		}

		private string GetDurationSettingDisplayText(MercenaryHireDurationSetting setting, int hourlyRate)
		{
			int durationCost = GetDurationCost(setting, hourlyRate);
			return CommsHelper.FormatMessageWithCreditsCost(setting.DisplayText, durationCost);
		}

		private static int GetDurationCost(MercenaryHireDurationSetting setting, int hourlyRate)
		{
			return Mathf.RoundToInt((float)hourlyRate * setting.NumHours);
		}

		private int GetDurationCost(MercenaryHireDurationSetting durationSetting)
		{
			int hourlyRate = GetHourlyRate();
			return GetDurationCost(durationSetting, hourlyRate);
		}

		private void ConfirmOption_Selected(CommsStageEventOption option, ICommsController commsController)
		{
			MercenaryHireDurationSetting durationSetting = optionRateSettings[option];
			if (CanPlayerAffordDuration(durationSetting))
			{
				HireMercenary(durationSetting);
				commsController.GoToHandlerDefaultStage();
			}
			else
			{
				UIController.Instance.ShowInsufficientCreditsMessageBox();
			}
		}

		private void HireMercenary(MercenaryHireDurationSetting durationSetting)
		{
			Fleet fleet = DynamicCommsHandler.OwnerPilot.GetFleet();
			if (fleet != null)
			{
				FactionAIMercenary factionAIMercenary = fleet.Faction.FactionAI as FactionAIMercenary;
				if (factionAIMercenary != null && factionAIMercenary.TryAssignFleetToProtectHiringPerson(fleet, EngineASX.Instance.LocalPlayer.Person))
				{
					factionAIMercenary.OnHired(fleet, durationSetting.NumHours, EngineASX.Instance.LocalFaction);
				}
			}
		}

		private bool CanPlayerAffordDuration(MercenaryHireDurationSetting durationSetting)
		{
			int durationCost = GetDurationCost(durationSetting);
			return EngineASX.Instance.LocalFaction.Credits >= durationCost;
		}
	}
}
