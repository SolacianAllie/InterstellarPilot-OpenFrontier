using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.EngineMusic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenFrontier.IP.UI
{
	public class ScenarioLoaderUI : MonoBehaviour
	{
		public delegate void LoadingScenarioHandler(ScenarioLoaderUI sender, ScenarioLoadData loadData);

		private ScenarioLoadData queuedLoadData;

		public float LoadScenarioDelay = 1f;

		private float loadScenarioTime;

		private AsyncOperation loadOperation;

		public bool IsLoading
		{
			get
			{
				if (queuedLoadData == null)
				{
					if (loadOperation != null)
					{
						return !loadOperation.isDone;
					}
					return false;
				}
				return true;
			}
		}

		public event LoadingScenarioHandler LoadingScenario;

		public void Update()
		{
			if (queuedLoadData != null && Time.realtimeSinceStartup > loadScenarioTime)
			{
				WorldBase.LoadData = queuedLoadData;
				StartAsyncLoadOperation();
				queuedLoadData = null;
			}
		}

		private void StartAsyncLoadOperation()
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Start async load of new scene \"" + queuedLoadData.ScenarioInfo.TargetSceneName + "\"", this, 1);
			}
			loadOperation = SceneManager.LoadSceneAsync(queuedLoadData.ScenarioInfo.TargetSceneName, LoadSceneMode.Single);
			loadOperation.allowSceneActivation = true;
		}

		public bool TryLoadScenario(EngineSaveGameHeader header)
		{
			ScenarioInfo scenarioInfoById = GameController.Instance.GetScenarioInfoById(header.ScenarioInfoId);
			return TryLoadScenario(new ScenarioLoadData
			{
				FullSaveGamePath = header.FullPath,
				ScenarioInfo = scenarioInfoById,
				SaveVersion = header.SaveVersion
			});
		}

		public bool TryLoadScenario(ScenarioInfo scenario)
		{
			return TryLoadScenario(new ScenarioLoadData
			{
				FullSaveGamePath = null,
				ScenarioInfo = scenario
			});
		}

		public bool TryLoadScenario(ScenarioLoadData loadData)
		{
			if (loadData.ScenarioInfo != null)
			{
				if (loadOperation == null || loadOperation.isDone)
				{
					Time.timeScale = 0f;
					Debug.Log($"Scenario loading Scene: {loadData.ScenarioInfo.TargetSceneName} SaveGame: \"{loadData.FullSaveGamePath}\"");

					// The music needs to know the moment the menu is over, and
					// this is the only place that knows. Watching the engine
					// for teardown does not work - it is not destroyed, the
					// same instance carries the menu world and then the galaxy
					// one - and watching for a scene change would cut the music
					// every time a submenu was opened. Same assembly, so there
					// is no reference to add and no event to subscribe to.
					MusicDirector.ScenarioLoadCommitted();
					if (!string.IsNullOrEmpty(loadData.ScenarioInfo.TargetSceneName))
					{
						QueueSceneLoad(loadData);
						return true;
					}
					Debug.LogError("UnitySceneName is not assigned");
				}
			}
			else
			{
				Debug.LogError("Unable to LoadScenario. Null ScenarioInfo specified", this);
			}
			return false;
		}

		private void QueueSceneLoad(ScenarioLoadData loadData)
		{
			// No fade here. This faded the OLD player, and the equivalent for
			// this system is ScenarioLoadCommitted, called from TryLoadScenario
			// above - the same call, on the right system, at the moment the
			// load is actually committed. Two fades for one event is how they
			// ended up disagreeing about which one the player heard.
			if (LoadingScenario != null)
			{
				LoadingScenario(this, queuedLoadData);
			}
			loadScenarioTime = Time.realtimeSinceStartup + LoadScenarioDelay;
			queuedLoadData = loadData;
			UIController.Instance.QuickMsg.ClearMessages();
			UIController.Instance.ScreenNavigator.RemoveAllInStack(null);
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Showing loading screen before new scenario load", this, 1);
			}
			if (LoadingScreenUI.Instance == null)
			{
				SceneManager.LoadSceneAsync(ScreenNames.LoadingSceneName, LoadSceneMode.Additive);
			}
			if (EngineASX.Instance != null)
			{
				EngineASX.Instance.ActiveSector = null;
			}
		}
	}
}
