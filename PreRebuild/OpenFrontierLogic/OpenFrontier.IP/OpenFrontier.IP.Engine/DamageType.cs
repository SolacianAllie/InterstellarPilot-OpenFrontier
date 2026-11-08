using System;
using OpenFrontier.IP.Common;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class DamageType
	{
		public float Damage;

		public float MiningDamage;

		public ShieldDamageType ShieldDamageType;

		public float ComponentDamageMultiplier = 1f;
	}
}
