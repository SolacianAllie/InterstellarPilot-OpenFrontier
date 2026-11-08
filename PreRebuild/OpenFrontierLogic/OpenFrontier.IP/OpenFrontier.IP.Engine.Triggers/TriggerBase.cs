using System.IO;
using OpenFrontier.IP.Common.Triggers;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Triggers
{
	[RequireComponent(typeof(TriggerGroup))]
	public class TriggerBase : MonoBehaviour
	{
		public bool Invert;

		protected TriggerGroup triggerGroup;

		public virtual TriggerType Type => TriggerType.Unspecified;

		public bool Evaluate(EngineASX engine)
		{
			if (Invert)
			{
				return !evaluate(engine);
			}
			return evaluate(engine);
		}

		public virtual void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
		}

		public virtual void ReadBinary(BinaryReader reader, EngineASX engine)
		{
		}

		protected virtual bool evaluate(EngineASX engine)
		{
			return false;
		}

		private void Awake()
		{
			triggerGroup = GetComponent<TriggerGroup>();
		}
	}
}
