using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens
{
	public class DamageFlashController : DamageFlashControllerBase
	{
		private float startTime;

		public bool RememberOriginalColor;

		private Color? originalColor;

		public Graphic TargetGraphic;

		private bool hasStarted;

		public DamageFlashSettings DamageFlashSettings => GameController.Instance.GameSettings.DamageFlashSettings;

		public override bool HasStarted => hasStarted;

		[ContextMenu("Start")]
		public override void StartFlash()
		{
			if (RememberOriginalColor && TargetGraphic != null)
			{
				originalColor = TargetGraphic.color;
			}
			startTime = Time.time;
			hasStarted = true;
		}

		[ContextMenu("Stop")]
		public override void StopFlash()
		{
			hasStarted = false;
			if (RememberOriginalColor && originalColor.HasValue)
			{
				TargetGraphic.color = originalColor.Value;
			}
		}

		private void LateUpdate()
		{
			if (hasStarted)
			{
				if (Time.time - startTime > DamageFlashSettings.FlashDuration)
				{
					StopFlash();
				}
				else if (Mathf.Sin((Time.time - startTime) * DamageFlashSettings.FlashRate) > DamageFlashSettings.FlashSineValue)
				{
					TargetGraphic.color = DamageFlashSettings.FlashColor;
				}
				else if (RememberOriginalColor && originalColor.HasValue)
				{
					TargetGraphic.color = originalColor.Value;
				}
			}
		}
	}
}
