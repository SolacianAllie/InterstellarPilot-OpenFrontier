using System;

namespace OpenFrontier.IP.UI.Screens
{
	public interface IScreenNavigationRequest
	{
		Type ScreenType { get; }

		ScreenBase ScreenResult { get; set; }

		bool LoadOnly { get; set; }

		bool IgnoreIfExistingRequest { get; set; }

		ReuseScreenMode ReuseLoadedExistingScreen { get; set; }
	}
}
