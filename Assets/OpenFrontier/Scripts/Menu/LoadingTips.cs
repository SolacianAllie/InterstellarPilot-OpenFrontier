using TMPro;
using UnityEngine;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// The loading screen's tip line: every enable picks ONE tip uniformly at
	/// random and fills the Title / Description labels with it. Labels are
	/// wired by child name, exactly like GamemodeCard's visuals.
	///
	/// Tips are authored data, not code: add or edit entries in the Inspector
	/// on this component. With no tips in the pool the labels keep their
	/// authored text and the editor console names the gap (compiled out of
	/// release builds - they simply show whatever the labels hold).
	///
	/// Fires once per enable, never per frame - no clock concerns.
	/// </summary>
	[DisallowMultipleComponent]
	public class LoadingTips : MonoBehaviour
	{
		[System.Serializable]
		public struct Tip
		{
			[TextArea(1, 2)] public string Title;

			[TextArea(2, 6)] public string Description;
		}

		[Tooltip("The tip pool. Exactly one is shown per enable; add or edit entries in the Inspector.")]
		public Tip[] tips;

		[Tooltip("The tip's headline label (wired by child name 'Title' if left empty).")]
		public TMP_Text titleLabel;

		[Tooltip("The tip's body label (wired by child name 'Description' if left empty).")]
		public TMP_Text descriptionLabel;

		private void OnEnable()
		{
			// Self-healing wiring, same shape as SafeAreaFitter's rect lookup:
			// OnEnable can run before any tool or hand wiring landed.
			if (titleLabel == null || descriptionLabel == null)
			{
				WireLabelsByChildName();
			}

			ShowRandomTip();
		}

		private void WireLabelsByChildName()
		{
			TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);

			for (int i = 0; i < labels.Length; i++)
			{
				TMP_Text label = labels[i];

				if (label.name == "Title" && titleLabel == null)
				{
					titleLabel = label;
				}
				else if (label.name == "Description" && descriptionLabel == null)
				{
					descriptionLabel = label;
				}
			}
		}

		private void ShowRandomTip()
		{
			int count = tips == null ? 0 : tips.Length;

			// Compact the array's blank holes first: an empty tip would
			// otherwise win the roll and blank the line.
			int live = 0;

			for (int i = 0; i < count; i++)
			{
				if (!string.IsNullOrWhiteSpace(tips[i].Title) || !string.IsNullOrWhiteSpace(tips[i].Description))
				{
					tips[live++] = tips[i];
				}
			}

			if (live == 0)
			{
#if UNITY_EDITOR
				Debug.LogWarning($"[LoadingTips] '{name}': no tips authored - the labels keep their current text.", this);
#endif
				return;
			}

			Tip chosen = tips[Random.Range(0, live)];

			if (titleLabel != null)
			{
				titleLabel.text = chosen.Title;
			}

			if (descriptionLabel != null)
			{
				descriptionLabel.text = chosen.Description;
			}
		}
	}
}
