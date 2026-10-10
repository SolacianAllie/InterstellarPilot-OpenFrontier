using System.Linq;
using OpenFrontier.IP.UI;

namespace OpenFrontier.IP.Multiplayer.UI
{
	/// <summary>
	/// The scroll list of saved multiplayer hosts.
	///
	/// Mirrors <see cref="LoadGameItemsUI"/> - same generic list, same
	/// refresh contract - but sourced from the host store instead of the save
	/// game folder. That is what lets the rest of this screen reuse the existing
	/// selection, scrolling and multi-select machinery unchanged.
	/// </summary>
	///
	/// The row class lives in SavedHostItemUI.cs rather than here: Unity
	/// registers one MonoScript per file, named after the file, so a second
	/// MonoBehaviour sharing this one has no MonoScript and cannot be attached
	/// to a prefab at all.
	public class SavedHostItemsUI : ScrollList<SavedHost>
	{
		protected override void OnRefreshing()
		{
			// Most recently added first, matching the save-game list's ordering
			// so the two screens feel the same.
			SetItems(SavedHostStore.Hosts.OrderByDescending(h => h.AddedUtc).ToList());
		}
	}
}
