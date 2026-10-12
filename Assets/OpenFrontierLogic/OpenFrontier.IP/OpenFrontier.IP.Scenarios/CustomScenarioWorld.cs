using System;
using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.Scenarios
{
	public class CustomScenarioWorld : WorldBase
	{
		protected override void OnNewGame()
		{
			throw new Exception("Not supported");
		}

		protected override void OnLoadGame()
		{
			base.OnLoadGame();
			SeedWorldOnNewGame();
			CanLoadLastSave = true;
		}
	}
}
