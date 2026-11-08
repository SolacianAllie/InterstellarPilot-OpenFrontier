using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class TimedAutosaver : MonoBehaviour
	{
		private void Update()
		{
			if (EngineASX.LoadedAndReady && GameController.Instance.AutoSaveSettings.AutoSaveAfterDuration && EngineASX.Instance.World.ObjectiveState == WorldBase.ScenarioState.Playing && EngineASX.Instance.ScenarioElapsedTime - EngineASX.Instance.TimeOfLastAutoSave.GetValueOrDefault() > (double)GameController.Instance.AutoSaveSettings.AutoSaveAfterDurationMinutes * 60.0)
			{
				EngineASX.Instance.AutoSaveIfPossible();
			}
		}
	}
}
