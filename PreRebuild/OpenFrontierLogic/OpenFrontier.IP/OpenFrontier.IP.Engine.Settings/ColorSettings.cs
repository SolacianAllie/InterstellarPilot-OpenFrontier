using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class ColorSettings : MonoBehaviour
	{
		public Color UniverseMapUnclaimedSectorLabelColor = Color.grey;

		public Color UniverseMapClaimedSectorLabelColor = Color.white;

		public Color UniverseMapHostileSectorColor = Color.red;

		public List<Color> SecurityColors = new List<Color>();

		public Color WeaponReadyColor = Color.green;

		public Color WeaponNotReadyColor = Color.red;

		public Color WeaponAutoFireReadyColor = Color.grey;

		public Color WeaponAutoFireNotReadyColor = Color.grey;

		public Color PoweredDownComponentColor = Color.red;

		public Color PoweredUpComponentColor = Color.green;

		public Color WaypointMissionColor = Color.white;

		public Color WaypointCustomColor = Color.white;
	}
}
