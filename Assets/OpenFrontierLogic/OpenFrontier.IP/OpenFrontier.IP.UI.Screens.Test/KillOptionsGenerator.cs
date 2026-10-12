using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.MessageBox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class KillOptionsGenerator : MonoBehaviour
	{
		public Button ButtonPrefab;

		public Transform TargetTransform;

		private void Awake()
		{
			Generate();
		}

		public void Generate()
		{
			foreach (KillTargetType killType in Enum.GetValues(typeof(KillTargetType)))
			{
				Button button = UnityEngine.Object.Instantiate(ButtonPrefab, TargetTransform);
				button.gameObject.name = $"Button_{killType}";
				button.onClick.AddListener(() =>
				{
					OnButtonClick(killType);
				});
				button.GetComponentInChildren<TextMeshProUGUI>().text = TextUtils.FromTitleCase(Enum.GetName(typeof(KillTargetType), killType));
			}
		}

		private void OnButtonClick(KillTargetType killType)
		{
			try
			{
				int num = ExecuteKillType(killType);
				if (num > 0)
				{
					UIController.Instance.ShowMessageBox($"{num:N0} unit(s) were killed", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
				}
			}
			catch
			{
				UIController.Instance.ShowError("Error executing action");
			}
		}

		private int ExecuteKillType(KillTargetType KillTargetType)
		{
			switch (KillTargetType)
			{
			case KillTargetType.CurrentTarget:
				if (EngineASX.Instance.Hud.CurrentTarget.IsValidAndNotDestroyed && EngineASX.Instance.Hud.CurrentTarget.Destructable.KillAnonymously())
				{
					return 1;
				}
				return 0;
			case KillTargetType.CurrentTargetFactionUnits:
			{
				List<Unit> list = EngineASX.Instance.Hud.CurrentTarget.Faction.Units.ToList();
				int num = 0;
				{
					foreach (Unit item in list)
					{
						if (item != null && item.IsValidAndNotDestroyed && item.Destructable != null && item.Destructable.KillAnonymously())
						{
							num++;
						}
					}
					return num;
				}
			}
			case KillTargetType.DestroyAsteroids:
				UIController.Instance.ShowMessageBox("Are you sure?", MessageBoxButtons.OkCancel, (MessageBoxScreen screen, MessageBoxResult result) =>
				{
					if (result == MessageBoxResult.Ok)
					{
						int num2 = 0;
						foreach (Sector sector in EngineASX.Instance.Sectors)
						{
							List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Asteroid);
							if (unitsByType != null)
							{
								foreach (Unit item2 in unitsByType)
								{
									item2.Destructable.KillAnonymously();
									num2++;
								}
							}
						}
						if (num2 == 0)
						{
							UIController.Instance.ShowMessageBox("No asteroids found", MessageBoxButtons.Ok);
						}
						else
						{
							UIController.Instance.ShowMessageBox($"{num2} asteroids destroyed", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
						}
					}
				}, MessageBoxIcon.Question, "Destroy all asteroids");
				break;
			}
			return 0;
		}
	}
}
