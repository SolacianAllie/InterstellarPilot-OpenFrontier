using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI;

public class DockedScreen : EngineScreen
{
	public CurrentUnitUI ShipUI;

	public DockFacilitiesUI DockFacilitiesUI;

	private Unit playerCurrentUnit;

	protected override void refresh()
	{
		base.refresh();
		RefreshShipUIAvailable();
		RefreshDockUIAvailable();
		if (DockFacilitiesUI.isActiveAndEnabled)
		{
			DockFacilitiesUI.Refresh();
		}
	}

	protected override void update()
	{
		base.update();
		if (ShipUI.isActiveAndEnabled)
		{
			ShipUI.Refresh();
		}
		if (Eng.DockUI.PlayerCurrentUnit != playerCurrentUnit)
		{
			playerCurrentUnit = Eng.DockUI.PlayerCurrentUnit;
			Refresh();
		}
	}

	protected override void onMadeCurrentPanel(bool navigatedForward)
	{
		base.onMadeCurrentPanel(navigatedForward);
		EngineASX.Instance.CameraOrbitPlayerUnit();
		Refresh();
	}

	private bool ShouldShowCurrentUnitUI()
	{
		if (Eng.DockUI.PlayerCurrentUnit != null)
		{
			return Eng.DockUI.PlayerCurrentUnit.UnitType == UnitType.Ship;
		}
		return false;
	}

	private bool ShouldShowDockUI()
	{
		return true;
	}

	private void RefreshShipUIAvailable()
	{
		ShipUI.gameObject.SetActive(ShouldShowCurrentUnitUI());
	}

	private void RefreshDockUIAvailable()
	{
		DockFacilitiesUI.gameObject.SetActive(ShouldShowDockUI());
	}
}
