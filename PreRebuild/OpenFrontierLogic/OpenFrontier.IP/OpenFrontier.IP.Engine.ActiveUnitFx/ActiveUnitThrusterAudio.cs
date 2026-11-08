using UnityEngine;

namespace OpenFrontier.IP.Engine.ActiveUnitFx
{
	public class ActiveUnitThrusterAudio : MonoBehaviour
	{
		private ActiveUnit activeUnit;

		private AudioSource audiosource;

		public void Init(ActiveUnit activeUnit)
		{
			this.activeUnit = activeUnit;
			if (this.activeUnit == null)
			{
				Debug.LogError("Expecting active unit parent", this);
			}
			audiosource = GetComponent<AudioSource>();
			if (audiosource == null)
			{
				Debug.LogError("Expecting audio source", this);
			}
		}

		private void Update()
		{
			if (!(activeUnit != null) || !(activeUnit.Unit != null) || !activeUnit.Unit.IsValidAndNotDestroyed || !(activeUnit.Unit.Components != null))
			{
				return;
			}
			if (activeUnit.IsInDrawRange)
			{
				float engineThrottleValue = GetEngineThrottleValue();
				float num = CalculateVolumeFromEngineThrottle(engineThrottleValue);
				if (activeUnit.Unit.IsSameRootUnitAsPlayer)
				{
					num *= EngineASX.Instance.GameSettings.AudioSettings.PlayerThrusterVolumeMultiplier;
				}
				audiosource.volume = num;
			}
			else
			{
				audiosource.volume = 0f;
			}
		}

		private float CalculateVolumeFromEngineThrottle(float engineThrottle)
		{
			float minThrusterVolume = EngineASX.Instance.GameSettings.AudioSettings.MinThrusterVolume;
			return minThrusterVolume + engineThrottle * (1f - minThrusterVolume);
		}

		private float GetEngineThrottleValue()
		{
			return activeUnit.Unit.Components.EngineThrottle;
		}
	}
}
