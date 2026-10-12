using OpenFrontier.IP.Common;

namespace OpenFrontier.IP.Engine.SaveGame
{
	public interface ISaveGameImporter
	{
		void Import(ISavedGame savedGame, EngineASX engine);
	}
}
