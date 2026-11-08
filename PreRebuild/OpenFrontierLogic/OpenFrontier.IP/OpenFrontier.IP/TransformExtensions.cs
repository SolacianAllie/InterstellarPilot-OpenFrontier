using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP
{
	public static class TransformExtensions
	{
		public static void SetRandomLocalYRotation(this Transform transform)
		{
			float y = Random.Range(0f, 360f);
			transform.localEulerAngles = new Vector3(0f, y, 0f);
		}

		public static void RemoveTranslationY(this Transform transform)
		{
			Vector3 position = transform.position;
			position.y = 0f;
			transform.position = position;
		}

		public static List<T> GetComponentsInImmediateChildren<T>(this Transform transform, bool includeInactive = true) where T : Component
		{
			List<T> list = new List<T>();
			for (int i = 0; i < transform.childCount; i++)
			{
				T component = transform.GetChild(i).GetComponent<T>();
				if (component != null && (includeInactive || component.gameObject.activeSelf))
				{
					list.Add(component);
				}
			}
			return list;
		}

		public static T GetComponentInImmediateChildren<T>(this Transform transform) where T : Component
		{
			new List<T>();
			for (int i = 0; i < transform.childCount; i++)
			{
				T component = transform.GetChild(i).GetComponent<T>();
				if (component != null)
				{
					return component;
				}
			}
			return null;
		}

		public static void AddComponentsInChildrenToList<T>(this Transform transform, List<T> list) where T : Component
		{
			for (int i = 0; i < transform.childCount; i++)
			{
				Transform child = transform.GetChild(i);
				T component = child.GetComponent<T>();
				if (component != null)
				{
					list.Add(component);
				}
				child.AddComponentsInChildrenToList(list);
			}
		}
	}
}
