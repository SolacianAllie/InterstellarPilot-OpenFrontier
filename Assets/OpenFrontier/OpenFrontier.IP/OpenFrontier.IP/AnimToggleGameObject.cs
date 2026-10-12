using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP
{
	public class AnimToggleGameObject : MonoBehaviour
	{
		public bool SetActiveOnDisable = true;

		public float Speed = 8f;

		public List<GameObject> TargetGameObjects = new List<GameObject>();

		public float VisibleThreshold = -0.5f;

		private void Update()
		{
			for (int i = 0; i < TargetGameObjects.Count; i++)
			{
				GameObject gameObject = TargetGameObjects[i];
				if (gameObject != null)
				{
					gameObject.SetActive(Mathf.Sin(Time.time * Speed) > VisibleThreshold);
				}
			}
		}

		private void OnDisable()
		{
			if (!SetActiveOnDisable)
			{
				return;
			}
			for (int i = 0; i < TargetGameObjects.Count; i++)
			{
				GameObject gameObject = TargetGameObjects[i];
				if (gameObject != null)
				{
					gameObject.SetActive(value: true);
				}
			}
		}
	}
}
