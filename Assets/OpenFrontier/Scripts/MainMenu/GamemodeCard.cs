using OpenFrontier.Gamemodes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// The NewGame preview card: mirrors the list's selection into
	/// Title / Description / Image. Sits on the "Gamemode Card" object; wire
	/// the three child visuals plus the list (the Content object carrying
	/// GamemodeList) and it is done - no per-frame work, refreshes only on
	/// change.
	///
	/// Subscribes to SelectedChanged AND pulls the current selection in
	/// OnEnable. The pull makes component enable order irrelevant: whichever
	/// of card/list enables first, the card ends up showing the right
	/// selection (the push covers card-enables-first, the pull covers
	/// list-enables-first).
	///
	/// With nothing selected every wired label reads empty and the image
	/// hides - an authored placeholder can live underneath it if wanted.
	/// </summary>
	[DisallowMultipleComponent]
	public class GamemodeCard : MonoBehaviour
	{
		[Tooltip("The list to mirror - drag the NewGame Content object carrying GamemodeList.")]
		public GamemodeList List;

		[Tooltip("Card title label.")]
		public TMP_Text Title;

		[Tooltip("Card description label.")]
		public TMP_Text Description;

		[Tooltip("Card preview image. Hidden while the selection has no image; leave unwired for a text-only card.")]
		public Image Image;

		private void OnEnable()
		{
			if (List != null)
			{
				List.SelectedChanged += Refresh;
			}

			Refresh();
		}

		private void OnDisable()
		{
			if (List != null)
			{
				List.SelectedChanged -= Refresh;
			}
		}

		private void Refresh()
		{
			GamemodeDefinition selected = List != null ? List.Selected : null;

			if (Title != null)
			{
				Title.text = selected != null ? selected.Title : string.Empty;
			}

			if (Description != null)
			{
				Description.text = selected != null ? selected.Description : string.Empty;
			}

			if (Image != null)
			{
				bool hasImage = selected != null && selected.PreviewImage != null;
				Image.gameObject.SetActive(hasImage);

				if (hasImage)
				{
					Image.sprite = selected.PreviewImage;
				}
			}
		}
	}
}
