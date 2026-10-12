using System.IO;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Comms
{
	public class DialogMessage : MonoBehaviour, ICommsMessage
	{
		public int UniqueId = -1;

		[TextArea(3, 8)]
		public string Text;

		public int TimesConfirmed;

		public string MessageText => Text;

		public virtual void WriteBinary(BinaryWriter writer, EngineASX engine)
		{
			writer.Write(TimesConfirmed);
		}

		public virtual void ReadBinary(BinaryReader reader, EngineASX engine)
		{
			TimesConfirmed = reader.ReadInt32();
		}

		public void Confirm()
		{
			TimesConfirmed++;
		}
	}
}
