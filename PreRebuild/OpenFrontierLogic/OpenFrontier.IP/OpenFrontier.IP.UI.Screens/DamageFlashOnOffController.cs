using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens
{
	public class DamageFlashOnOffController : DamageFlashControllerBase
	{
		private float startTime;

		public Graphic TargetGraphic;

		private bool hasStarted;

		public DamageFlashSettings DamageFlashSettings => GameController.Instance.GameSettings.DamageFlashSettings;

		public override bool HasStarted => hasStarted;

		private void Awake()
		{
			TargetGraphic.enabled = false;
		}

		[ContextMenu("Start")]
		public override void StartFlash()
		{
			startTime = Time.time;
			hasStarted = true;
			TargetGraphic.color = DamageFlashSettings.FlashColor;
		}

		[ContextMenu("Stop")]
		public override void StopFlash()
		{
			hasStarted = false;
			TargetGraphic.enabled = false;
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
					TargetGraphic.enabled = true;
				}
				else
				{
					TargetGraphic.enabled = false;
				}
			}
		}
	}
}
