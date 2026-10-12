using System.IO;
using OpenFrontier.IP.Common.Triggers;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class EngineAction : MonoBehaviour
	{
		public virtual ActionType Type => ActionType.Unspecified;

		protected EngineASX engine => EngineASX.Instance;

		public void Validate()
		{
			validate();
		}

		protected virtual void validate()
		{
		}

		public virtual void Execute()
		{
		}

		public virtual void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
		}

		public virtual void ReadBinary(BinaryReader reader, EngineASX engine)
		{
		}
	}
}
