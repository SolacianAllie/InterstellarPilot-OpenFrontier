using OpenFrontier.IP.Common.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionAIPatroller : FactionAIBase
	{
		public override FactionAIType AIType => FactionAIType.Patroller;

		protected override void onNewGame()
		{
			base.onNewGame();
			Debug.LogError("This faction AI type (patroller) is obsolete", faction);
		}
	}
}
