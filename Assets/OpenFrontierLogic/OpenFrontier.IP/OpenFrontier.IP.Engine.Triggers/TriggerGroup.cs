using System.Collections.Generic;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.EngineActions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Triggers
{
	public class TriggerGroup : MonoBehaviour
	{
		public bool EvaluateDuringIntroState;

		private EngineASX engine;

		public float EvaluateFrequency = 1f;

		public bool FireAndDisable = true;

		private bool hasFired;

		private double nextEvaluationTime;

		private List<TriggerBase> triggers = new List<TriggerBase>();

		public int UniqueId;

		private bool hasInit;

		private int fireCount;

		[SerializeField]
		private int maxFireCount = 1;

		public TriggerMaxFiredAction MaxFiredAction = TriggerMaxFiredAction.Destroy;

		public int MaxFireCount
		{
			get
			{
				return maxFireCount;
			}
			set
			{
				maxFireCount = value;
			}
		}

		public int FireCount
		{
			get
			{
				return fireCount;
			}
			set
			{
				fireCount = value;
			}
		}

		public double NextEvaluationTime
		{
			get
			{
				return nextEvaluationTime;
			}
			set
			{
				nextEvaluationTime = value;
			}
		}

		public bool HasFired => fireCount > 0;

		public IEnumerable<TriggerBase> Triggers => triggers;

		public void Init()
		{
			if (!hasInit)
			{
				engine = EngineASX.Instance;
				engine.RegisterTriggerGroup(this);
				hasInit = true;
			}
		}

		public void FindTriggers()
		{
			triggers.Clear();
			triggers.AddRange(GetComponents<TriggerBase>());
		}

		public void Evaluate()
		{
			if (engine != null)
			{
				if (!engine.World.HasInitialised || (hasFired && FireAndDisable) || !(engine.ScenarioElapsedTime >= nextEvaluationTime))
				{
					return;
				}
				bool flag = true;
				for (int i = 0; i < triggers.Count; i++)
				{
					if (!triggers[i].Evaluate(engine))
					{
						flag = false;
					}
				}
				if (flag)
				{
					OnFired();
				}
				nextEvaluationTime = engine.ScenarioElapsedTime + (double)EvaluateFrequency;
			}
			else if (EngineASX.LoadedAndReady)
			{
				engine = EngineASX.Instance;
			}
		}

		private void OnFired()
		{
			fireCount++;
			Execute();
			if (FireAndDisable)
			{
				gameObject.SetActive(value: false);
			}
			if (maxFireCount >= 1 && fireCount >= maxFireCount)
			{
				OnMaxFired();
			}
		}

		private void OnMaxFired()
		{
			switch (MaxFiredAction)
			{
			case TriggerMaxFiredAction.Deactivate:
				gameObject.SetActive(value: false);
				break;
			case TriggerMaxFiredAction.Destroy:
				Object.Destroy(gameObject);
				break;
			}
		}

		public EngineAction[] GetActions()
		{
			return GetComponents<EngineAction>();
		}

		public void Execute()
		{
			EngineAction[] actions = GetActions();
			if (actions.Length != 0)
			{
				EngineAction[] array = actions;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Execute();
				}
			}
		}

		private void Update()
		{
			if (CanEvaluate())
			{
				Evaluate();
			}
		}

		public bool CanEvaluate()
		{
			if (!hasInit)
			{
				return false;
			}
			if (!EngineASX.LoadedAndReady)
			{
				return false;
			}
			return EngineASX.Instance.World.ObjectiveState switch
			{
				WorldBase.ScenarioState.Playing => true, 
				WorldBase.ScenarioState.Intro => EvaluateDuringIntroState, 
				_ => false, 
			};
		}

		public void AutoNameGameObject()
		{
			gameObject.name = GetGameObjectName();
		}

		public string GetGameObjectName()
		{
			if (triggers.Count > 0)
			{
				EngineAction[] actions = GetActions();
				if (actions.Length != 0)
				{
					TriggerBase triggerBase = triggers[0];
					return $"Trigger_{UniqueId}_{triggerBase.Type}_then_{actions[0].Type}";
				}
			}
			return $"Trigger_{UniqueId}";
		}
	}
}
