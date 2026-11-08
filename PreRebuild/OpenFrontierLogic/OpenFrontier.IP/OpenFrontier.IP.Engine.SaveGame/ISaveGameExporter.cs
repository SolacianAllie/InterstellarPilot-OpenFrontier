using OpenFrontier.IP.Common;

namespace OpenFrontier.IP.Engine.SaveGame
{
	public interface ISaveGameExporter
	{
		ISavedGame Export(EngineASX engine);
	}
}
