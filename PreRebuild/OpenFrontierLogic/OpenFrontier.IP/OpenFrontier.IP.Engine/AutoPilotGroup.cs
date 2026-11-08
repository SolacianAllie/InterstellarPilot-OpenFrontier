using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	[RequireComponent(typeof(Fleet))]
	public class AutoPilotGroup : MonoBehaviour
	{
		private Fleet fleet;

		private void Awake()
		{
			fleet = GetComponent<Fleet>();
		}
	}
}
