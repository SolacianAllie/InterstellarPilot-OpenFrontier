using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.SaveGame;
using OpenFrontier.IP.UI.Screens.LoadGame;

namespace OpenFrontier.IP.UI
{
	public class LoadGameItemsUI : ScrollList<EngineSaveGameHeader>
	{
		public LoadGameScreen LoadGameUI;

		protected override void OnRefreshing()
		{
			SetItems(from e in SaveGameUtilities.FindSavedGameHeaders(includeIncompatibleVersions: true)
				orderby e.TimeStamp descending
				select e);
		}
	}
}
