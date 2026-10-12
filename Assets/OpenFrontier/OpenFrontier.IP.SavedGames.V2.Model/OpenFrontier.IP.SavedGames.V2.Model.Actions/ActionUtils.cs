using System;
using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Actions
{
	public class ActionUtils
	{
		public static ModelAction CreateModelAction(ActionType actionType)
		{
			return actionType switch
			{
				ActionType.Dialog_Activate => (ModelAction)new ModelAction_Dialog_Activate(), 
				ActionType.Dialog_ChangeStage => new ModelAction_Dialog_ChangeStage(), 
				ActionType.Dialog_SetInitialStage => new ModelAction_Dialog_SetInitialStage(), 
				ActionType.Faction_SetNeutralityWith => new ModelAction_Faction_SetNeutralityWith(), 
				ActionType.Faction_SetOpinionWith => new ModelAction_Faction_SetOpinionWith(), 
				ActionType.Faction_DiscoverFactionUnits => new ModelAction_Faction_DiscoverFactionUnits(), 
				ActionType.Faction_SetRpMultiplier => new ModelAction_Faction_SetRpMultiplier(), 
				ActionType.Fleet_Activate => new ModelAction_Fleet_Activate(), 
				ActionType.Fleet_EnqueueOrder => new ModelAction_Fleet_EnqueueOrder(), 
				ActionType.Fleet_SetOrder => new ModelAction_Fleet_SetOrder(), 
				ActionType.Mission_Activate => new ModelAction_Mission_Activate(), 
				ActionType.Mission_ActivateObjective => new ModelAction_Mission_ActivateObjective(), 
				ActionType.Mission_ChangeStage => new ModelAction_Mission_ChangeStage(), 
				ActionType.Mission_CompleteObjective => new ModelAction_Mission_CompleteObjective(), 
				ActionType.Player_ChangeCargo => new ModelAction_Player_ChangeCargo(), 
				ActionType.Player_ChangeCredits => new ModelAction_Player_ChangeCredits(), 
				ActionType.Player_DiscoverFactionUnits => new ModelAction_Player_DiscoverFactionUnits(), 
				ActionType.Player_DiscoverStaticUnitsInSectors => new ModelAction_Player_DiscoverStaticUnitsInSectors(), 
				ActionType.Player_NewMessage => new ModelAction_Player_NewMessage(), 
				ActionType.Player_NewMessageSimple => new ModelAction_Player_NewMessageSimple(), 
				ActionType.TriggerGroup_Activate => new ModelAction_TriggerGroup_Activate(), 
				ActionType.Unit_Activate => new ModelAction_Unit_Activate(), 
				ActionType.Unit_AllowDestruction => new ModelAction_Unit_AllowDestruction(), 
				ActionType.Unit_ChangeCargo => new ModelAction_Unit_ChangeCargo(), 
				ActionType.Unit_ChangeCargos => new ModelAction_Unit_ChangeCargos(), 
				ActionType.Unit_ChangeFleet => new ModelAction_Unit_ChangeFleet(), 
				ActionType.Unit_Dock => new ModelAction_Unit_Dock(), 
				ActionType.Unit_LegacyInstallComponent => new ModelAction_Unit_LegacyInstallComponent(), 
				ActionType.Unit_LegacyUninstallComponent => new ModelAction_Unit_LegacyUninstallComponent(), 
				ActionType.Unit_PerformScan => new ModelAction_Unit_PerformScan(), 
				ActionType.Unit_PositionRelativeToUnit => new ModelAction_Unit_PositionRelativeToUnit(), 
				ActionType.Spawn_BanditHorde => new ModelAction_Spawn_BanditHorde(), 
				_ => throw new Exception($"Unknown action type: {actionType}"), 
			};
		}
	}
}
