using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Skirmish
{
	public class SkirmishTeam
	{
		public List<SkirmishShipItem> Ships = new List<SkirmishShipItem>();

		public string TeamName { get; set; }

		public int TeamIndex { get; set; }

		public Color TeamColor { get; set; }
	}
}
