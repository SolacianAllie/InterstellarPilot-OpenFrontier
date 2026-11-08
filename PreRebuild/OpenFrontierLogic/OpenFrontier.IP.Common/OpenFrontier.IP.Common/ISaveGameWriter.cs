using System;
using System.IO;

namespace OpenFrontier.IP.Common
{
	public interface ISaveGameWriter
	{
		void Write(BinaryWriter writer, ISavedGame savedGame, Action<string> logger);
	}
}
