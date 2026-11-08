using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionSetup : MonoBehaviour
	{
		public bool ApplyNow;

		public List<FactionAttitudeSetupSpecific> Attitudes = new List<FactionAttitudeSetupSpecific>();

		public bool ForceTwoWay;

		public List<FactionAttitudeSetupGlobal> GlobalAttitudes = new List<FactionAttitudeSetupGlobal>();

		[SerializeField]
		private Faction sourceFaction;

		public void ApplyAttitudes()
		{
			EngineASX instance = EngineASX.Instance;
			if (instance != null)
			{
				int num;
				for (num = 0; num < Attitudes.Count; num++)
				{
					Attitudes[0].Apply(instance, sourceFaction, ForceTwoWay);
					Attitudes.RemoveAt(0);
					num--;
				}
				int num2;
				for (num2 = 0; num2 < GlobalAttitudes.Count; num2++)
				{
					GlobalAttitudes[0].Apply(instance, sourceFaction, ForceTwoWay);
					GlobalAttitudes.RemoveAt(0);
					num2--;
				}
			}
			else
			{
				Debug.LogWarning("FactionSetup: could not apply faction attitudes because can't find the engine", this);
			}
			ApplyNow = false;
		}

		private void Awake()
		{
			if (sourceFaction == null)
			{
				sourceFaction = GetComponent<Faction>();
			}
		}

		private void Update()
		{
			if (ApplyNow)
			{
				ApplyAttitudes();
			}
		}
	}
}
