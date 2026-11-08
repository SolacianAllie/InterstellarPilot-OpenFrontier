using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class FlickerPanelsAlpha : MonoBehaviour
	{
		public bool AllowFlickering = true;

		private float currentAlphaRestoreRate;

		public float MaxAlphaRestoreRate = 1f;

		public float MaxFlickerAlpha = 0.2f;

		public float MaxRestoreDelay = 0.3f;

		public float MaxTimeBeforeFlicker = 1f;

		public float MinAlphaRestoreRate = 0.5f;

		public float MinFlickerAlpha;

		public float MinRestoreDelay;

		public float MinTimeBeforeFlicker = 0.2f;

		private float nextFlickerTime;

		private float nextRestoreTime;

		public float PanelDesiredAlpha = 1f;

		public bool RestoreAlphaOnDisable = true;

		public List<CanvasGroup> TargetPanels = new List<CanvasGroup>();

		public void Update()
		{
			if (Time.time > nextFlickerTime)
			{
				nextFlickerTime = Time.time + Random.Range(MinTimeBeforeFlicker, MaxTimeBeforeFlicker);
				ApplyFlicker();
			}
			else if (Time.time > nextRestoreTime)
			{
				float delta = currentAlphaRestoreRate * Time.deltaTime;
				RestorePanelsAlpha(delta);
			}
		}

		public void RestoreAlpha(float alpha)
		{
			for (int i = 0; i < TargetPanels.Count; i++)
			{
				CanvasGroup canvasGroup = TargetPanels[i];
				if (canvasGroup != null)
				{
					canvasGroup.alpha = alpha;
				}
			}
		}

		private void Awake()
		{
		}

		private void ApplyFlicker()
		{
			float panelsAlpha = Random.Range(MinFlickerAlpha, MaxFlickerAlpha);
			SetPanelsAlpha(panelsAlpha);
			currentAlphaRestoreRate = Random.Range(MinAlphaRestoreRate, MaxAlphaRestoreRate);
			nextRestoreTime = Time.time + Random.Range(MinRestoreDelay, MaxRestoreDelay);
		}

		private void SetPanelsAlpha(float targetAlpha)
		{
			for (int i = 0; i < TargetPanels.Count; i++)
			{
				CanvasGroup canvasGroup = TargetPanels[i];
				if (canvasGroup != null)
				{
					canvasGroup.alpha = targetAlpha;
				}
			}
		}

		private void RestorePanelsAlpha(float delta)
		{
			for (int i = 0; i < TargetPanels.Count; i++)
			{
				CanvasGroup canvasGroup = TargetPanels[i];
				if (canvasGroup != null && canvasGroup.alpha < PanelDesiredAlpha)
				{
					canvasGroup.alpha += delta;
				}
			}
		}

		private void OnDisable()
		{
			if (RestoreAlphaOnDisable)
			{
				SetPanelsAlpha(PanelDesiredAlpha);
			}
		}
	}
}
