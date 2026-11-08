using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.UI
{
	public class EngineScreen : ScreenBase
	{
		private EngineASX engine;

		[FormerlySerializedAs("PauseWhenShown")]
		public bool LegacyPauseWhenShown;

		public bool ShowDockHeader = true;

		public EngineASX Eng
		{
			get
			{
				if (engine == null)
				{
					engine = EngineASX.Instance;
				}
				return engine;
			}
			set
			{
				engine = value;
			}
		}

		public DockUI DockUI
		{
			get
			{
				if (Eng != null)
				{
					return Eng.DockUI;
				}
				return null;
			}
		}

		protected override void awake()
		{
			base.awake();
			engine = EngineASX.Instance;
			if (LegacyPauseWhenShown)
			{
				Debug.LogError(name + ": \"LegacyPauseWhenShown\" is no longer used. Use PauseWhenShown instead");
			}
		}

		public virtual bool ShouldShowDockHeader()
		{
			return ShowDockHeader;
		}
	}
}
