using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class MissileLockButtonsController : MonoBehaviour
	{
		public SpriteSineColor SpriteSineColor;

		private List<Missile> locksCache = new List<Missile>();

		public List<MissileLockButton> Buttons = new List<MissileLockButton>();

		public MissileLockButton MainMissileLockButton;

		private void Awake()
		{
			foreach (MissileLockButton button in Buttons)
			{
				button.gameObject.SetActive(value: false);
			}
		}

		private void Update()
		{
			if (EngineASX.LoadedAndReady)
			{
				Unit playerUnit = EngineASX.Instance.PlayerUnit;
				RefreshMissileLocks(playerUnit);
			}
		}

		private void RefreshMissileLocks(Unit playerUnit)
		{
			PopulateMissileLockCache(playerUnit);
			for (int i = 0; i < Buttons.Count; i++)
			{
				if (i < locksCache.Count)
				{
					Buttons[i].Missile = locksCache[i];
				}
				Buttons[i].gameObject.SetActive(i < locksCache.Count);
			}
			MainMissileLockButton.Missile = ((locksCache.Count > 0) ? locksCache[0] : null);
			MainMissileLockButton.gameObject.SetActive(locksCache.Count > 0);
		}

		private void PopulateMissileLockCache(Unit playerUnit)
		{
			locksCache.Clear();
			if (!(playerUnit != null) || !(playerUnit.ActiveUnit != null))
			{
				return;
			}
			List<Missile> missileLocks = playerUnit.Engine.MissileLockController.GetMissileLocks(playerUnit);
			if (missileLocks != null && missileLocks.Count > 0)
			{
				int num = Mathf.Min(Buttons.Count, missileLocks.Count);
				for (int i = 0; i < num; i++)
				{
					locksCache.Add(missileLocks[i]);
				}
			}
		}
	}
}
