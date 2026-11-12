using UnityEngine;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// The simple loading screen's spinning shield segment - a faithful
	/// rebuild of the legacy LoadingWidgetUI
	/// (PreRebuild/OpenFrontierLogic/OpenFrontier.IP/OpenFrontier.IP.UI/LoadingWidgetUI.cs):
	/// the pivot rotates in fixed steps on UNSCALED time while timeScale
	/// sits frozen under the loading overlay (Update itself is not gated by
	/// timeScale, and the step accumulator runs on the unscaled clock, so
	/// the orbit neither stalls nor races with frozen gameplay).
	///
	/// Stepped, not smooth - the legacy values were -60 degrees every
	/// 0.1 seconds, and that cadence is the delivery: a mechanical orbit
	/// that reads as instrumentation rather than decoration. Place this
	/// on the PIVOT node whose child holds the shield image, offset from
	/// the centre; the ship image stays static beside it.
	/// </summary>
	[DisallowMultipleComponent]
	public class LoadingSpinner : MonoBehaviour
	{
		[Tooltip("Degrees the pivot turns per step. The legacy value was -60 (clockwise).")]
		public float DegreesPerStep = -60f;

		[Tooltip("Seconds between steps, unscaled. The legacy value was 0.1.")]
		[Min(0.01f)]
		public float SecondsPerStep = 0.1f;

		private float elapsed;

		private void Update()
		{
			elapsed += Time.unscaledDeltaTime;
			while (elapsed >= SecondsPerStep)
			{
				transform.Rotate(0f, 0f, DegreesPerStep, Space.Self);
				elapsed -= SecondsPerStep;
			}
		}
	}
}
