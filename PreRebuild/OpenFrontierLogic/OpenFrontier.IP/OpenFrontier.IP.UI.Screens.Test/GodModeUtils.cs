using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public static class GodModeUtils
	{
		public static void OnGodModeActionExecuted()
		{
			EngineASX.Instance.World.HasCheated = true;
		}
	}
}
