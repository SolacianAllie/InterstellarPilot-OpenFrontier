using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelFleetSettings
	{
		public bool AllowCombatInterception = true;

		public bool AllowAttack = true;

		public float Aggression = 13f;

		public float TargetInterceptionLowerDistance = 1300f;

		public float TargetInterceptionUpperDistance = 1500f;

		public bool PreferCloak;

		public DockedPreference PreferToDock;

		public int MaxJumpDistance = 99;

		public bool DestroyWhenNoPilots;

		public float FormationTightness = 0.9f;

		public FleetCargoCollectionPreference CargoCollectionPreference = FleetCargoCollectionPreference.CompatibleEquipment;

		public ModelPlayerFleetSettings PlayerFleetSettings;

		public bool AreSameAs(ModelFleetSettings modelFleetSettings)
		{
			if (PlayerFleetSettings == null != (modelFleetSettings.PlayerFleetSettings == null))
			{
				return false;
			}
			if (PlayerFleetSettings != null && !PlayerFleetSettings.AreSameAs(modelFleetSettings.PlayerFleetSettings))
			{
				return false;
			}
			if (AllowAttack != modelFleetSettings.AllowAttack)
			{
				return false;
			}
			if (AllowCombatInterception != modelFleetSettings.AllowCombatInterception)
			{
				return false;
			}
			if (Aggression != modelFleetSettings.Aggression)
			{
				return false;
			}
			if (TargetInterceptionLowerDistance != modelFleetSettings.TargetInterceptionLowerDistance)
			{
				return false;
			}
			if (TargetInterceptionUpperDistance != modelFleetSettings.TargetInterceptionUpperDistance)
			{
				return false;
			}
			if (PreferCloak != modelFleetSettings.PreferCloak)
			{
				return false;
			}
			if (PreferToDock != modelFleetSettings.PreferToDock)
			{
				return false;
			}
			if (MaxJumpDistance != modelFleetSettings.MaxJumpDistance)
			{
				return false;
			}
			if (DestroyWhenNoPilots != modelFleetSettings.DestroyWhenNoPilots)
			{
				return false;
			}
			if (FormationTightness != modelFleetSettings.FormationTightness)
			{
				return false;
			}
			if (CargoCollectionPreference != modelFleetSettings.CargoCollectionPreference)
			{
				return false;
			}
			return true;
		}
	}
}
