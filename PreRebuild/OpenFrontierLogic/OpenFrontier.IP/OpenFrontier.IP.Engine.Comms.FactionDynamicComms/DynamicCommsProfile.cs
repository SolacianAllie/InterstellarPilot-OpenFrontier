using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms
{
	public class DynamicCommsProfile : MonoBehaviour
	{
		public Neutrality Neutrality;

		public DynamicCommsProfileOpinion Opinion;

		public List<string> Messages = new List<string>();
	}
}
