namespace OpenFrontier.IP.Common.Triggers
{
	public enum TriggerType
	{
		Unspecified = -1,
		Always = 0,
		Player_InSector = 10000,
		Player_HasMissileLock = 11000,
		Player_NearSectorTarget = 12000,
		Player_NearUnit = 12100,
		Player_IsPilotting = 13000,
		Player_DockedAtUnit = 14000,
		Player_TimePilotting = 15000,
		Player_CurrentHudTarget = 16000,
		Player_NoHostileFactions = 17000,
		Player_HasOpenedMessage = 18000,
		Fleet_InSector = 20000,
		Fleet_InState = 21000,
		Fleet_HostileTargets = 22000,
		Fleet_DestroyedOrNoPilots = 23000,
		Scenario_TimeElapsed = 100,
		Scenario_TimeSinceDialogShown = 200,
		Scenario_AllowDialog = 300,
		Unit_ReceivedDamage = 30000,
		Unit_TotalDamageReceived = 31000,
		Unit_DockedAtUnit = 32000,
		Unit_InSector = 33000,
		Unit_NearSectorTarget = 34000,
		Unit_HasCargo = 35000,
		Units_Destroyed = 36000,
		Dialog_MessageConfirmed = 50000,
		Dialog_NumTimesStageShown = 51000,
		Dialog_OptionConfirmed = 52000,
		Dialog_StageFinished = 53000
	}
}
