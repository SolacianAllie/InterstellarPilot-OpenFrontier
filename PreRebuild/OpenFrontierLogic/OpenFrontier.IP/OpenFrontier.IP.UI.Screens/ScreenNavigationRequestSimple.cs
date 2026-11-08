using System;

namespace OpenFrontier.IP.UI.Screens
{
	public class ScreenNavigationRequestSimple : IScreenNavigationRequest
	{
		public bool IgnoreIfExistingRequest { get; set; }

		public bool LoadOnly { get; set; }

		public ScreenBase ScreenResult { get; set; }

		public Type ScreenType { get; set; }

		public ReuseScreenMode ReuseLoadedExistingScreen { get; set; }
	}
}
