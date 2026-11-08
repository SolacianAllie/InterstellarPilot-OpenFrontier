using System.IO;

namespace OpenFrontier.IP.Common
{
	public interface ISavedGameHeaderReader
	{
		ISavedGameHeader Read(BinaryReader binaryReader);
	}
}
