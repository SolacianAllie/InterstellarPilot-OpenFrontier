using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class Explosion : MonoBehaviour
	{
		public ExplosionClass ExplosionClass;

		public bool IgnoreShields;

		private bool isReady;

		public Faction SourceFaction;

		public Unit SourceUnit;

		public void Init()
		{
			isReady = true;
		}

		private void Update()
		{
			if (isReady)
			{
				_ = ExplosionClass != null;
				Object.Destroy(this);
			}
		}
	}
}
