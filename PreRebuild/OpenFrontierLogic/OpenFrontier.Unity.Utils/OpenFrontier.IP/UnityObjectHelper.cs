using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP
{
	public static class UnityObjectHelper
	{
		public static bool LayerMaskContainsLayer(ref LayerMask layerMask, int layer)
		{
			return (int)layerMask == ((int)layerMask | (1 << layer));
		}

		public static T FindComponent<T>() where T : Component
		{
			return UnityEngine.Object.FindObjectOfType(typeof(T)) as T;
		}

		public static IEnumerable<T> FindComponents<T>() where T : Component
		{
			return UnityEngine.Object.FindObjectsOfType(typeof(T)).Cast<T>();
		}

		public static T FindComponent<T>(string gameObjectName) where T : Component
		{
			GameObject gameObject = GameObject.Find(gameObjectName);
			if (gameObject != null)
			{
				return gameObject.GetComponent<T>();
			}
			return null;
		}

		public static IEnumerable<T> FindResources<T>()
		{
			return Resources.FindObjectsOfTypeAll(typeof(T)).Cast<T>();
		}

		public static IEnumerable<T> LoadAll<T>(string path)
		{
			return Resources.LoadAll(path, typeof(T)).Cast<T>();
		}

		public static GameObject NewGameObject(GameObject gameObject, Transform parent)
		{
			GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject);
			gameObject2.transform.SetParent(parent, worldPositionStays: true);
			return gameObject2;
		}

		public static T NewGameObject<T>() where T : Component
		{
			return new GameObject().AddComponent<T>();
		}

		public static T NewGameObject<T>(Component parent) where T : Component
		{
			return NewGameObject<T>((parent != null) ? parent.transform : null);
		}

		public static T NewGameObject<T>(Transform parent) where T : Component
		{
			GameObject gameObject = new GameObject();
			T result = gameObject.AddComponent<T>();
			if (parent != null)
			{
				gameObject.transform.SetParent(parent.transform);
				gameObject.transform.localPosition = Vector3.zero;
			}
			return result;
		}

		public static T NewGameObject<T>(Type type) where T : Component
		{
			return (T)new GameObject().AddComponent(type);
		}

		public static T FindInParent<T>(GameObject go) where T : Component
		{
			if (go == null)
			{
				return null;
			}
			Transform parent = go.transform.parent;
			if (parent != null)
			{
				return parent.GetComponent<T>();
			}
			return null;
		}

		public static T FindInParentsOrSelf<T>(GameObject go) where T : Component
		{
			if (go == null)
			{
				return null;
			}
			T component = go.GetComponent<T>();
			if (component == null)
			{
				Transform parent = go.transform.parent;
				while (parent != null && component == null)
				{
					component = parent.gameObject.GetComponent<T>();
					parent = parent.parent;
				}
			}
			return component;
		}

		public static T Load<T>(string path) where T : UnityEngine.Object
		{
			return (T)Resources.Load(path, typeof(T));
		}

		public static void DestroyChildren(Component component, bool destroyImmediate)
		{
			DestroyChildren(component.gameObject, destroyImmediate);
		}

		public static void DestroyChildren(GameObject gameObject, bool destroyImmediate)
		{
			GameObject[] array = new GameObject[gameObject.transform.childCount];
			for (int i = 0; i < gameObject.transform.childCount; i++)
			{
				array[i] = gameObject.transform.GetChild(i).gameObject;
			}
			for (int j = 0; j < array.Length; j++)
			{
				if (destroyImmediate)
				{
					UnityEngine.Object.DestroyImmediate(array[j]);
				}
				else
				{
					UnityEngine.Object.Destroy(array[j]);
				}
			}
		}

		public static T GetComponentInChildrenInactive<T>(GameObject g) where T : Component
		{
			for (int i = 0; i < g.transform.childCount; i++)
			{
				GameObject gameObject = g.transform.GetChild(i).gameObject;
				T component = gameObject.GetComponent<T>();
				if (component != null)
				{
					return component;
				}
				T componentInChildrenInactive = GetComponentInChildrenInactive<T>(gameObject);
				if (componentInChildrenInactive != null)
				{
					return componentInChildrenInactive;
				}
			}
			return null;
		}

		public static void ChangeLayerAndChildren(GameObject g, int layer)
		{
			g.layer = layer;
			ChangeLayerOfChildren(g.transform, layer);
		}

		public static void ChangeLayerOfChildren(Transform t, int layer)
		{
			for (int i = 0; i < t.childCount; i++)
			{
				ChangeLayerAndChildren(t.GetChild(i).gameObject, layer);
			}
		}

		public static T InstantiateAndGetComponent<T>(T componentPrefab, Transform parent = null) where T : Component
		{
			T val = null;
			val = ((!(parent != null)) ? UnityEngine.Object.Instantiate(componentPrefab) : UnityEngine.Object.Instantiate(componentPrefab, parent));
			val.transform.localPosition = Vector3.zero;
			return val;
		}
	}
}
