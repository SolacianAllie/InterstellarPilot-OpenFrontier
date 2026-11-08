using System.IO;

namespace OpenFrontier.IP.Common
{
	public interface ISaveGameReader
	{
		ISavedGame Read(BinaryReader reader);
	}
}
