using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms
{
	public class DynamicCommsSettings : MonoBehaviour
	{
		public DynamicCommsSubstitutionSettings DynamicCommsSubstitutionSettings;

		public List<DynamicCommsProfile> WelcomeMessageProfiles = new List<DynamicCommsProfile>();

		public List<DynamicCommsProfile> RepeatWelcomeMessageProfiles = new List<DynamicCommsProfile>();
	}
}
