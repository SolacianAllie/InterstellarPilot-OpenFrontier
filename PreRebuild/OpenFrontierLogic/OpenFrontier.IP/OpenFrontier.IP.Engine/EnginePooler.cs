using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class EnginePooler : MonoBehaviour
	{
		private Dictionary<EntityId, GameObject> activePoolObjects = new Dictionary<EntityId, GameObject>();

		private Dictionary<EntityId, List<GameObject>> pooledObjects = new Dictionary<EntityId, List<GameObject>>();

		private Dictionary<EntityId, EntityId> poolInstanceToPrefabMap = new Dictionary<EntityId, EntityId>();

		private List<AudioSource> fadeOutAudioSources = new List<AudioSource>();

		public void CreatePool(string path, int instanceCount)
		{
			foreach (GameObject item in UnityObjectHelper.LoadAll<GameObject>(path))
			{
				EntityId instanceID = item.GetEntityId();
				List<GameObject> value = new List<GameObject>(20);
				pooledObjects.Add(instanceID, value);
				for (int i = 0; i < instanceCount; i++)
				{
					AddNewToPoolFromPrefab(item, keepPooled: true);
				}
			}
		}

		private void Update()
		{
			if (fadeOutAudioSources.Count <= 0)
			{
				return;
			}
			for (int i = 0; i < fadeOutAudioSources.Count; i++)
			{
				AudioSource audioSource = fadeOutAudioSources[i];
				if (audioSource != null)
				{
					float volume = audioSource.volume;
					if (volume > 0f)
					{
						volume -= RealTime.deltaTime * 2f;
						if (volume < 0f)
						{
							volume = 0f;
						}
						audioSource.volume = volume;
					}
					else
					{
						RecyclePoolObject(audioSource.gameObject);
						fadeOutAudioSources.RemoveAt(i);
						i--;
					}
				}
				else
				{
					fadeOutAudioSources.RemoveAt(i);
					i--;
				}
			}
		}

		public void RecyclePoolObject(GameObject g)
		{
			if (!(g != null))
			{
				return;
			}
			EntityId instanceID = g.GetEntityId();
			EntityId value = default;
			if (g.TryGetComponent<AudioSource>(out var component) && component.volume > 0f)
			{
				StartAudioFadeOut(g, component);
				return;
			}
			g.SetActive(value: false);
			if (poolInstanceToPrefabMap.TryGetValue(instanceID, out value))
			{
				List<GameObject> value2 = null;
				if (pooledObjects.TryGetValue(value, out value2))
				{
					ReturnObjectToPool(value2, g);
				}
				else
				{
					Debug.LogWarning("Cannot recycle object. Unknown prefab id." + g);
				}
			}
			else
			{
				Debug.LogWarning("Cannot recycle object. Unknown instance id." + g);
			}
		}

		private void StartAudioFadeOut(GameObject g, AudioSource audioSource)
		{
			g.transform.SetParent(gameObject.transform);
			if (!fadeOutAudioSources.Contains(audioSource))
			{
				fadeOutAudioSources.Add(audioSource);
			}
		}

		public GameObject GetPooledObjectOrCreate(GameObject prefab)
		{
			if (pooledObjects.ContainsKey(prefab.GetEntityId()))
			{
				GameObject gameObject = GetPooledObject(prefab);
				if (gameObject == null)
				{
					gameObject = AddNewToPoolFromPrefab(prefab, keepPooled: false);
				}
				activePoolObjects[gameObject.GetEntityId()] = gameObject;
				return gameObject;
			}
			Debug.LogWarning("There is no pool for prefab: " + prefab, this);
			return null;
		}

		public GameObject GetPooledObject(GameObject prefab)
		{
			EntityId instanceID = prefab.GetEntityId();
			List<GameObject> value = null;
			if (pooledObjects.TryGetValue(instanceID, out value) && value.Count > 0)
			{
				GameObject result = value[value.Count - 1];
				value.RemoveAt(value.Count - 1);
				return result;
			}
			return null;
		}

		public void ReturnAllObjectsToPool()
		{
			GameObject[] array = activePoolObjects.Values.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				RecyclePoolObject(array[i]);
			}
		}

		private GameObject AddNewToPoolFromPrefab(GameObject prefab, bool keepPooled)
		{
			GameObject gameObject = Object.Instantiate(prefab);
			List<GameObject> pool = pooledObjects[prefab.GetEntityId()];
			EntityId instanceID = gameObject.GetEntityId();
			poolInstanceToPrefabMap.Add(instanceID, prefab.GetEntityId());
			if (keepPooled)
			{
				ReturnObjectToPool(pool, gameObject);
			}
			return gameObject;
		}

		private void ReturnObjectToPool(List<GameObject> pool, GameObject g)
		{
			if (g != null)
			{
				if (g.TryGetComponent<Unit>(out var component) && component.UniqueId >= 0)
				{
					component.CleanupUnit();
				}
				activePoolObjects.Remove(g.GetEntityId());
				g.transform.SetParent(gameObject.transform);
				g.transform.localPosition = Vector3.zero;
				g.SetActive(value: false);
				if (!pool.Contains(g))
				{
					pool.Add(g);
				}
			}
		}
	}
}
