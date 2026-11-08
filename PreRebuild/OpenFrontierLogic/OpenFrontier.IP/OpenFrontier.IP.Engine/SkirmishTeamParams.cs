using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class SkirmishTeamParams
	{
		public List<SkirmishTeamParamsItem> ShipItems;

		public Color TeamColor = Color.white;

		public float GetCombatRating()
		{
			float num = 0f;
			foreach (SkirmishTeamParamsItem shipItem in ShipItems)
			{
				num += shipItem.GetCombatRating();
			}
			return num;
		}
	}
}
