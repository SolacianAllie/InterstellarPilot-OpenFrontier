using OpenFrontier.IP.Common;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelDamageType
	{
		public float Damage { get; set; }

		public float MiningDamage { get; set; }

		public ShieldDamageType ShieldDamageType { get; set; }
	}
}
