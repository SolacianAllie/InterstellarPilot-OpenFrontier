using System;

namespace OpenFrontier.IP.UI.Screens
{
	public class ScreenNavigationRequest<T> : IScreenNavigationRequest where T : ScreenBase
	{
		private bool loadOnly;

		private bool ignoreIfExisingRequest = true;

		public Type ScreenType => typeof(T);

		public T ResultConcrete => (T)ScreenResult;

		public ScreenBase ScreenResult { get; set; }

		public bool LoadOnly
		{
			get
			{
				return loadOnly;
			}
			set
			{
				loadOnly = value;
			}
		}

		public bool IgnoreIfExistingRequest
		{
			get
			{
				return ignoreIfExisingRequest;
			}
			set
			{
				ignoreIfExisingRequest = value;
			}
		}

		public ReuseScreenMode ReuseLoadedExistingScreen { get; set; }
	}
}
