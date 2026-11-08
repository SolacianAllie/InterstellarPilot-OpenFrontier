using UnityEngine;

namespace OpenFrontier.IP.Assets.Scripts.Rendering
{
	public class CameraIgnoreFog : MonoBehaviour
	{
		private bool revertFogState;

		private void OnPreRender()
		{
			revertFogState = RenderSettings.fog;
			RenderSettings.fog = false;
		}

		private void OnPostRender()
		{
			RenderSettings.fog = revertFogState;
		}
	}
}
