using OpenFrontier.IP.Common.Triggers;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Misc_ActivateGameObject : EngineAction
	{
		public bool Active = true;

		public GameObject Target;

		public override ActionType Type => ActionType.Misc_ActivateGameObject;

		public override void Execute()
		{
			if (Target != null)
			{
				Target.SetActive(Active);
				Unit component = Target.GetComponent<Unit>();
				if (component != null)
				{
					component.RefreshIsValidAndNotDestroyed();
				}
			}
			else
			{
				NullTargetErr();
			}
			base.Execute();
		}

		protected override void validate()
		{
			if (EngineASX.Instance.World.Permissions.AllowSaving)
			{
				Debug.LogError(name + ": This action should no longer be used as it cannot be persisted", this);
			}
			if (Target == null)
			{
				NullTargetErr();
			}
		}

		private void NullTargetErr()
		{
			Debug.LogError($"{gameObject}: Target is null. Is target game object persistent?", this);
		}
	}
}
