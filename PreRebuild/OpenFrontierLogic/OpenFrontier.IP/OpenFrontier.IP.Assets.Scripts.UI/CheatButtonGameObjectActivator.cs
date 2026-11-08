using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.Assets.Scripts.UI
{
	public class CheatButtonGameObjectActivator : MonoBehaviour
	{
		private int clickCount;

		public List<GameObject> TargetGameObjects;

		private Button button;

		private float lastClickTime;

		public int RequiredClicks = 10;

		public float ResetSeconds = 3f;

		public bool FireAndDisable = true;

		private void Awake()
		{
			button = GetComponent<Button>();
			if (button != null)
			{
				button.onClick.AddListener(OnClick);
			}
			else
			{
				Debug.LogError("Missing button component", this);
			}
		}

		private void Update()
		{
			if (clickCount > 0 && Time.realtimeSinceStartup > lastClickTime + ResetSeconds)
			{
				ResetClicks();
			}
		}

		private void ResetClicks()
		{
			clickCount = 0;
			lastClickTime = 0f;
		}

		private void OnClick()
		{
			if (!enabled)
			{
				return;
			}
			lastClickTime = Time.realtimeSinceStartup;
			clickCount++;
			if (clickCount >= RequiredClicks)
			{
				Fire();
				if (FireAndDisable)
				{
					enabled = false;
				}
			}
		}

		private void Fire()
		{
			if (TargetGameObjects != null)
			{
				foreach (GameObject targetGameObject in TargetGameObjects)
				{
					if (targetGameObject != null)
					{
						targetGameObject.SetActive(value: true);
					}
				}
			}
			ResetClicks();
		}
	}
}
