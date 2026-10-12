using System;
using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public static class TriggerUtils
	{
		public static ModelTrigger CreateModelTrigger(TriggerType triggerType)
		{
			return triggerType switch
			{
				TriggerType.Always => (ModelTrigger)new ModelTrigger_Always(), 
				TriggerType.Dialog_MessageConfirmed => new ModelTrigger_Dialog_MessageConfirmed(), 
				TriggerType.Dialog_NumTimesStageShown => new ModelTrigger_Dialog_NumTimesStageShown(), 
				TriggerType.Dialog_OptionConfirmed => new ModelTrigger_Dialog_OptionConfirmed(), 
				TriggerType.Dialog_StageFinished => new ModelTrigger_Dialog_StageFinished(), 
				TriggerType.Fleet_DestroyedOrNoPilots => new ModelTrigger_Fleet_DestroyedOrNoPilots(), 
				TriggerType.Fleet_HostileTargets => new ModelTrigger_Fleet_HostileTargets(), 
				TriggerType.Fleet_InSector => new ModelTrigger_Fleet_InSector(), 
				TriggerType.Fleet_InState => new ModelTrigger_Fleet_InState(), 
				TriggerType.Player_CurrentHudTarget => new ModelTrigger_Player_CurrentHudTarget(), 
				TriggerType.Player_DockedAtUnit => new ModelTrigger_Player_DockedAtUnit(), 
				TriggerType.Player_HasMissileLock => new ModelTrigger_Player_HasMissileLock(), 
				TriggerType.Player_HasOpenedMessage => new ModelTrigger_Player_HasOpenedMessage(), 
				TriggerType.Player_InSector => new ModelTrigger_Player_InSector(), 
				TriggerType.Player_IsPilotting => new ModelTrigger_Player_IsPilotting(), 
				TriggerType.Player_NearSectorTarget => new ModelTrigger_Player_NearSectorTarget(), 
				TriggerType.Player_NearUnit => new ModelTrigger_Player_NearUnit(), 
				TriggerType.Player_NoHostileFactions => new ModelTrigger_Player_NoHostileFactions(), 
				TriggerType.Player_TimePilotting => new ModelTrigger_Player_TimePilotting(), 
				TriggerType.Scenario_AllowDialog => new ModelTrigger_Scenario_AllowDialog(), 
				TriggerType.Scenario_TimeElapsed => new ModelTrigger_Scenario_TimeElapsed(), 
				TriggerType.Scenario_TimeSinceDialogShown => new ModelTrigger_Scenario_TimeSinceDialogShown(), 
				TriggerType.Unit_HasCargo => new ModelTrigger_Unit_HasCargo(), 
				TriggerType.Unit_NearSectorTarget => new ModelTrigger_Unit_NearSectorTarget(), 
				TriggerType.Unit_ReceivedDamage => new ModelTrigger_Unit_ReceivedDamage(), 
				TriggerType.Unit_TotalDamageReceived => new ModelTrigger_Unit_TotalDamageReceivedd(), 
				TriggerType.Units_Destroyed => new ModelTrigger_Units_Destroyed(), 
				_ => throw new Exception($"Unknown trigger type: {triggerType}"), 
			};
		}
	}
}
