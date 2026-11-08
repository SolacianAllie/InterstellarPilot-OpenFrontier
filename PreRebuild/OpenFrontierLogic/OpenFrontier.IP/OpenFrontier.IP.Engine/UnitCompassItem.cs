using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.Engine
{
	public class UnitCompassItem : MonoBehaviour
	{
		public Image SpeechImage;

		public Image UnitTypeImage;

		public Image WaypointTypeImage;

		public Image SelectedImage;

		public Image MissileLockImage;

		public void Init()
		{
			Deactivate();
		}

		public void Deactivate()
		{
			SpeechImage.enabled = false;
			UnitTypeImage.enabled = false;
			WaypointTypeImage.enabled = false;
			SelectedImage.enabled = false;
			MissileLockImage.enabled = false;
		}

		private void Update()
		{
			if (MissileLockImage.enabled && EngineASX.LoadedAndReady)
			{
				MissileLockImage.color = EngineASX.Instance.Hud.MissileLockButtonsController.SpriteSineColor.GetColor();
			}
		}
	}
}
