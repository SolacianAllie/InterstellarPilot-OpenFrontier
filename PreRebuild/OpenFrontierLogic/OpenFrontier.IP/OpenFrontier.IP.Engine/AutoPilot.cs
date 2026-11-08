using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class AutoPilot : MonoBehaviour
	{
		private NpcPilot controller;

		private void Awake()
		{
			controller = GetComponent<NpcPilot>();
		}
	}
}
