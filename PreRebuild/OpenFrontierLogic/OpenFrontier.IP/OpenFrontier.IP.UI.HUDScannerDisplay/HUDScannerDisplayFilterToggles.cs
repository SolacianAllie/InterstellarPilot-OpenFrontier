using UnityEngine;

namespace OpenFrontier.IP.UI.HUDScannerDisplay
{
	public class HUDScannerDisplayFilterToggles : MonoBehaviour
	{
		public HUDScannerFilterToggle AsteroidsToggle;

		public HUDScannerFilterToggle CargoToggle;

		public HUDScannerFilterToggle ShipsToggle;

		public HUDScannerFilterToggle StationsToggle;

		public HUDScannerFilterToggle WormholesToggle;

		public HUDScannerFilterToggle HostilesToggle;

		public bool IsShowingAll()
		{
			if (!AsteroidsToggle.Toggle.isOn && !CargoToggle.Toggle.isOn && !ShipsToggle.Toggle.isOn && !StationsToggle.Toggle.isOn && !WormholesToggle.Toggle.isOn)
			{
				return !HostilesToggle.Toggle.isOn;
			}
			return false;
		}

		public void RefreshToggles(HUDScannerScanFilterResult filterResult)
		{
			AsteroidsToggle.AvailableObject.SetActive(filterResult.Asteroids);
			StationsToggle.AvailableObject.SetActive(filterResult.Stations);
			CargoToggle.AvailableObject.SetActive(filterResult.Cargo);
			WormholesToggle.AvailableObject.SetActive(filterResult.Wormholes);
			HostilesToggle.AvailableObject.SetActive(filterResult.Hostiles);
			ShipsToggle.AvailableObject.SetActive(filterResult.Ships);
		}
	}
}
