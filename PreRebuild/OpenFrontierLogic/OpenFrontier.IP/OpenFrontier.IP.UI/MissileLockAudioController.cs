using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class MissileLockAudioController : MonoBehaviour
	{
		private float nextMissileLockPlay;

		private float lastMissileLockPlay;

		public AudioClip missileLockAudioClip;

		public float MissileLockSoundMaxRepeatTime = 1.5f;

		public float MissileLockSoundMinRepeatTime = 0.05f;

		public float MissileLockSoundRefDist = 2000f;

		private void Update()
		{
			if (EngineASX.LoadedAndReady && EngineASX.Instance.World.ObjectiveState == WorldBase.ScenarioState.Playing && GameController.Instance.PlayerOptions.Audio_MissileLockSound)
			{
				EngineASX instance = EngineASX.Instance;
				Unit playerUnit = EngineASX.Instance.PlayerUnit;
				if (playerUnit != null && playerUnit.IsActiveInEngine && playerUnit.ActiveUnit != null && playerUnit.IsOwnedByPlayer && instance.MissileLockController.HasMissileLock(playerUnit))
				{
					UpdateMissileLockCue(playerUnit);
				}
			}
		}

		private void UpdateMissileLockCue(Unit unit)
		{
			nextMissileLockPlay = lastMissileLockPlay + CalculateMissileRepeatTime(unit);
			if (Time.time > nextMissileLockPlay)
			{
				AudioHelper.PlaySound(missileLockAudioClip);
				lastMissileLockPlay = Time.time;
			}
		}

		private float CalculateMissileRepeatTime(Unit unit)
		{
			float nearestDist = 0f;
			if (unit.GetNearestMissileLock(out nearestDist) != null)
			{
				if (nearestDist > MissileLockSoundRefDist)
				{
					nearestDist = MissileLockSoundRefDist;
				}
				return MissileLockSoundMinRepeatTime + (MissileLockSoundMaxRepeatTime - MissileLockSoundMinRepeatTime) * nearestDist / MissileLockSoundRefDist;
			}
			return MissileLockSoundMaxRepeatTime;
		}
	}
}
