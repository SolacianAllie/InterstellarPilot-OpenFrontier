using System.Collections.Generic;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Hud_FlashAndEnableComponent : EngineAction
	{
		public List<HudComponent> Components = new List<HudComponent>();

		public float Duration = 5f;

		public bool RemoveExistingFlashComponents = true;

		public override ActionType Type => ActionType.Hud_FlashAndEnableComponent;

		public override void Execute()
		{
			base.Execute();
			if (RemoveExistingFlashComponents)
			{
				foreach (AnimToggleGameObject item in UnityObjectHelper.FindComponents<AnimToggleGameObject>())
				{
					Object.Destroy(item.gameObject);
				}
			}
			GameObject gameObject = new GameObject("Hud_ComponentFlasher");
			AnimToggleGameObject animToggleGameObject = gameObject.AddComponent<AnimToggleGameObject>();
			gameObject.gameObject.AddComponent<DestroyOnScreenTouch>();
			Object.Destroy(animToggleGameObject.gameObject, Duration);
			foreach (HudComponent component in Components)
			{
				engine.Hud.SetHudComponentVisible(component, visible: true);
				animToggleGameObject.TargetGameObjects.Add(engine.Hud.GetHudComponent(component));
			}
		}
	}
}
