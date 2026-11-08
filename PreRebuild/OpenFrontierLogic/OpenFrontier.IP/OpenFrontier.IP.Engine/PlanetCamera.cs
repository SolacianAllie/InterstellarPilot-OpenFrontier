using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class PlanetCamera : MonoBehaviour
	{
		private Color revertAmbientLightColor;

		public bool ForceSectorAmbientLighting = true;

		private void OnPreRender()
		{
			revertAmbientLightColor = RenderSettings.ambientLight;
			if (EngineASX.LoadedAndReady && ForceSectorAmbientLighting && EngineASX.Instance.ActiveSector != null)
			{
				RenderSettings.ambientLight = EngineASX.Instance.ActiveSector.AmbientLightColor;
			}
		}

		private void OnPostRender()
		{
			RenderSettings.ambientLight = revertAmbientLightColor;
		}
	}
}
