using OpenFrontier.IP.UI.Screens;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class StoryUI : ScreenBase
	{
		public MainMenuScreen MainMenuUI;

		public Button NextTextButton;

		public SimpleLabelCycler PageCycler;

		public Button PreviousTextButton;

		protected override void awake()
		{
			base.awake();
			NextTextButton.onClick.AddListener(NextText);
			PreviousTextButton.onClick.AddListener(PreviousText);
		}

		protected override void start()
		{
			base.start();
			UpdateCycleButtonStates();
		}

		private void PreviousText()
		{
			PageCycler.CycleLabel(-1);
			UpdateCycleButtonStates();
		}

		private void NextText()
		{
			PageCycler.CycleLabel(1);
			UpdateCycleButtonStates();
		}

		private void UpdateCycleButtonStates()
		{
			NextTextButton.gameObject.SetActive(PageCycler.CurIndex < PageCycler.Labels.Length - 1);
			PreviousTextButton.gameObject.SetActive(PageCycler.CurIndex > 0);
		}
	}
}
