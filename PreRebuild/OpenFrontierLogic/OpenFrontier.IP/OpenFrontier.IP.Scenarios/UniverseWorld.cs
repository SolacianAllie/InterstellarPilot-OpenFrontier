using System;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.Scenarios
{
	public class UniverseWorld : WorldAdvanced
	{
		public UniverseScenarioData DefaultLoadData;

		public TractorTurretClass ScavengerModeTractorTurretClass;

		public UniverseScenarioData GetUniverseScenarioData()
		{
			if (lastLoadedData != null && !(lastLoadedData is UniverseScenarioData))
			{
				throw new Exception("Ooops.. looks like you are trying to start a new universe game but there is no valid scenario data.");
			}
			return (UniverseScenarioData)lastLoadedData;
		}

		protected override void OnInit()
		{
			base.OnInit();
			if (Engine.EconomySettings.TrimTraderCargo)
			{
				TrimTraderCargos();
			}
		}

		private void TrimTraderCargos()
		{
			foreach (CargoTrader trader in Engine.Traders)
			{
				if (trader.Unit.Faction != Engine.LocalFaction)
				{
					TraderCargoTrimmer.Trim(trader);
				}
			}
		}

		protected override ScenarioLoadData CreateDefaultLoadData()
		{
			return DefaultLoadData;
		}
	}
}
