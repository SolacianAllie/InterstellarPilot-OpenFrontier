using OpenFrontier.IP.Engine;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class UIActiveSceneLoader : MonoBehaviour
	{
		private ActiveSectorData loadedSceneData;

		public bool LoadOnAwake = true;

		public bool RandomCameraRot;

		public string[] SceneResourceNames;

		public ActiveSectorData LoadedSceneData => loadedSceneData;

		public void Awake()
		{
			if (LoadOnAwake)
			{
				LoadBg();
				if (RandomCameraRot)
				{
					GameController.Instance.MainCamera.transform.rotation = Random.rotation;
				}
			}
		}

		public void LoadBg()
		{
			if (SceneResourceNames != null)
			{
				string random = SceneResourceNames.GetRandom();
				Debug.Log("UIActiveSceneLoader: Loading active scene resource: " + random);
				loadedSceneData = EngineASX.LoadAndCreateActiveScene(random);
			}
		}
	}
}
