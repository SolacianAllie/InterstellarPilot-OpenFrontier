using UnityEngine;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// The loading screen's ship lottery: every time the screen enables, ONE
	/// of the wired holo ships is shown and the rest are hidden - uniform
	/// random, re-rolled per enable, so every boot (and every return to a
	/// loading screen) rolls again.
	///
	/// Candidates are wired as transforms, not found by search: the ships sit
	/// next to the screen's own camera and GUI under the same root, so a
	/// child sweep would put those in the lottery too. Adding a ship: drag
	/// the HOLO prefab under the screen root, add it to the array (or re-run
	/// the landing scaffold, which rescans direct children named *HOLO).
	///
	/// Spin and shader clock are each ship's own (HoloSpinner +
	/// HologramShip, both unscaled), so the winner animates from frame one
	/// with nothing wired here.
	/// </summary>
	[DisallowMultipleComponent]
	public class LoadingShipPicker : MonoBehaviour
	{
		[Tooltip("The candidate holo ships. Exactly one is activated per enable; the rest are hidden.")]
		public Transform[] ships;

		private void OnEnable()
		{
			Pick();
		}

		private void Pick()
		{
			int count = ships == null ? 0 : ships.Length;

			// Compact the array's null holes first: an unwired slot would
			// otherwise win the roll and show nothing.
			int live = 0;

			for (int i = 0; i < count; i++)
			{
				if (ships[i] != null)
				{
					ships[live++] = ships[i];
				}
			}

			if (live == 0)
			{
				Debug.LogWarning($"[LoadingShipPicker] '{name}': no ships wired - leaving the current one visible.", this);
				return;
			}

			int chosen = Random.Range(0, live);

			for (int i = 0; i < live; i++)
			{
				ships[i].gameObject.SetActive(i == chosen);
			}
		}
	}
}
