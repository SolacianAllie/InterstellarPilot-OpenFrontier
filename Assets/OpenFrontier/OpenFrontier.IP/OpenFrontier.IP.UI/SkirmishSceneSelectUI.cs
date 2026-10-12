using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.Skirmish;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class SkirmishSceneSelectUI : ScreenBase
	{
		private Sector currentSector;

		public ScrollRect DescriptionScrollRect;

		public Button NextSceneButton;

		public Button PlayButton;

		public Button PreviousSceneButton;

		public Text SceneDescriptionLabel;

		public Text SceneNameLabel;

		public RawImage ScenePreviewImage;

		private SkirmishSetupScreen skirmishUI;

		public SkirmishSetupScreen SkirmishUI
		{
			get
			{
				return skirmishUI;
			}
			set
			{
				skirmishUI = value;
			}
		}

		public List<Sector> AvailableSectors => GameController.Instance.AllSectors.ToList();

		public Sector CurrentSector
		{
			get
			{
				return currentSector;
			}
			set
			{
				if (currentSector != value)
				{
					currentSector = value;
					if (currentSector != null)
					{
						ScenePreviewImage.texture = EngineASX.LoadSceneTexture(currentSector);
						SceneNameLabel.text = currentSector.Name;
						SceneDescriptionLabel.text = currentSector.Description;
						DescriptionScrollRect.ResetScrollPosition();
					}
				}
			}
		}

		protected override void awake()
		{
			base.awake();
			NextSceneButton.onClick.AddListener(NextScene);
			PreviousSceneButton.onClick.AddListener(PreviousScene);
			PlayButton.onClick.AddListener(Play);
		}

		protected override void start()
		{
			base.start();
			skirmishUI = UnityObjectHelper.FindComponent<SkirmishSetupScreen>();
			CurrentSector = AvailableSectors.First();
		}

		private void Play()
		{
			SkirmishUI.SpecificSceneToLoad = CurrentSector;
			SkirmishUI.Play();
		}

		private void PreviousScene()
		{
			SwitchScene(-1);
		}

		private void NextScene()
		{
			SwitchScene(1);
		}

		private void SwitchScene(int change)
		{
			int num = AvailableSectors.IndexOf(currentSector);
			num = Maths.WrapValue(num + change, 0, AvailableSectors.Count);
			CurrentSector = AvailableSectors[num];
		}
	}
}
