using System.Text;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.UniverseMap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class DockUIHeader : MonoBehaviour
	{
		private float lastTimeRefreshedTitleLabel = -100f;

		// Frame rate shown at the head of the location label. A rolling average over
		// the last second of real frames, kept in a fixed ring so there is no
		// allocation per frame.
		private const int FpsSampleCount = 128;

		private readonly float[] fpsSamples = new float[FpsSampleCount];

		private int fpsSampleHead;

		private int fpsSampleCount;

		private float displayFps;

		public TextMeshProUGUI CreditsText;

		public Image PausedActivatorImage;

		public Button NewMessagesButton;

		private string lastKnownShipName;

		private string lastKnownUnitName;

		private string lastKnownRootUnitName;

		private Sector lastPlayerScene;

		private Unit lastPlayerUnit;

		private Unit lastPlayerRootUnit;

		public TextMeshProUGUI LocationText;

		public Button LogButton;

		public Button MenuButton;

		private int oldPlayerCredits = -1;

		public Button PauseButton;

		public Button SectorMapButton;

		public Button UniverseMapButton;

		public Button PropertyButton;

		public Button FleetsButton;

		private static StringBuilder stringBuilder = new StringBuilder();

		public EngineASX Engine => EngineASX.Instance;

		public DockUI DockUI
		{
			get
			{
				if (Engine != null)
				{
					return Engine.DockUI;
				}
				return null;
			}
		}

		public void StorePlayerCreditsValue()
		{
			if (Engine != null)
			{
				oldPlayerCredits = Engine.CreditsAnimation.AnimatedValue;
				RefreshCreditsLabel();
			}
		}

		public void RefreshCreditsLabel()
		{
			CreditsText.text = TextFormattingHelper.FormatCredits(Engine.CreditsAnimation.AnimatedValue, includeSuffix: true);
		}

		public void UpdateTitleLabels()
		{
			LocationText.text = GetLocationText();
			lastTimeRefreshedTitleLabel = Time.realtimeSinceStartup;
		}

		private void Awake()
		{
			if (LogButton != null)
			{
				LogButton.onClick.AddListener(LogButton_Activated);
			}
			if (MenuButton != null)
			{
				MenuButton.onClick.AddListener(MenuButton_Activated);
			}
			PauseButton.onClick.AddListener(PauseButton_Activated);
			NewMessagesButton.onClick.AddListener(NewMessagesButtonClick);
			PropertyButton.onClick.AddListener(PropertyButtonClick);
			FleetsButton.onClick.AddListener(FleetsButtonClick);
			SectorMapButton.onClick.AddListener(SectorMapButtonClick);
			UniverseMapButton.onClick.AddListener(UniverseMapButtonClick);
			PausedActivatorImage.enabled = false;
			CreditsText.text = string.Empty;
		}

		private void PropertyButtonClick()
		{
			UIController.Instance.ScreenNavigator.TogglePropertyScreen();
		}

		private void FleetsButtonClick()
		{
			UIController.Instance.ScreenNavigator.ToggleFleetsScreen();
		}

		private void SectorMapButtonClick()
		{
			UIController.Instance.ScreenNavigator.ToggleSectorMapScreenWhenPilotting();
		}

		private void UniverseMapButtonClick()
		{
			ToggleUniverseMap();
		}

		public static void ToggleUniverseMap()
		{
			UIController.Instance.ScreenNavigator.ToggleCustomUniverseMapScreen((UniverseMapScreen screen) =>
			{
				screen.AutoSelectPlayerSector();
				screen.CenterOnPlayerSector();
			});
		}

		private void NewMessagesButtonClick()
		{
			UIController.Instance.ScreenNavigator.ToggleMessagesScreen();
		}

		private void PauseButton_Activated()
		{
			Engine.IsPaused = !Engine.IsPaused;
			if (Engine.IsPaused)
			{
				UIController.Instance.QuickMsg.AddMessage("Paused");
			}
		}

		private void MenuButton_Activated()
		{
			UIController.Instance.ScreenNavigator.ShowPauseMenu(Engine.IsPaused);
		}

		private void LogButton_Activated()
		{
			UIController.Instance.ScreenNavigator.ToggleLogScreen();
		}

		private void Start()
		{
			StorePlayerCreditsValue();
		}

		private void Update()
		{
			// Unscaled, so the readout stays honest while a PauseWhenShown screen
			// has Time.timeScale at 0 - which is exactly when it is least obvious
			// whether something is holding the framerate up.
			//
			// An average over the last second of frames, not the instantaneous
			// rate and not an exponential smoothing. Both of those were tried and
			// both read badly: raw per-frame alternates by a frame or two for
			// reasons that do not matter, and a smoothing factor leans too far
			// towards responsiveness - the number moves every frame, which is
			// precisely what makes it hard to read.
			//
			// Samples are taken newest-first and the window is cut at one second
			// of accumulated time rather than at a fixed frame count, so the
			// average covers the same span at any framerate. The ring is 128 deep
			// and preallocated; nothing here allocates.
			float frameDelta = Time.unscaledDeltaTime;
			if (frameDelta > 0f)
			{
				fpsSamples[fpsSampleHead] = frameDelta;
				fpsSampleHead = (fpsSampleHead + 1) % FpsSampleCount;
				if (fpsSampleCount < FpsSampleCount)
				{
					fpsSampleCount++;
				}
				float windowTime = 0f;
				int windowFrames = 0;
				int index = fpsSampleHead;
				while (windowFrames < fpsSampleCount)
				{
					index = (index - 1 + FpsSampleCount) % FpsSampleCount;
					float sample = fpsSamples[index];
					if (windowTime + sample > 1f && windowFrames > 0)
					{
						break;
					}
					windowTime += sample;
					windowFrames++;
				}
				if (windowTime > 0f)
				{
					displayFps = windowFrames / windowTime;
				}
			}

			// Location label, refreshed every frame so the framerate reading moves
			// with the display rather than jumping once a second. This used to be
			// driven entirely by the change-detection block below, which fired
			// when the unit, ship name or sector changed or every 2s; the framerate
			// was averaged over 1s and sampled into that. Two things are given up
			// for live updating: a StringBuilder.ToString() per frame, and a TMP
			// mesh rebuild of this one label per frame. Both are small, and the
			// feature is player-facing, so it is worth it - but if this ever needs
			// to be cheaper, split the framerate into its own TMP element rather
			// than rebuilding the whole label, so only the digits change.
			if (!(Engine != null))
			{
				return;
			}
			Button sectorMapButton = SectorMapButton;
			bool interactable = (UniverseMapButton.interactable = Engine.World.Permissions.AllowDockUIMap);
			sectorMapButton.interactable = interactable;
			NewMessagesButton.interactable = Engine.World.Permissions.AllowDockUIMessages;
			PropertyButton.interactable = Engine.World.Permissions.AllowPropertyScreen;
			NewMessagesButton.interactable = Engine.AllowUiMessagesNavigation();
			PausedActivatorImage.enabled = Engine.IsPaused;
			if (!(Engine.LocalPlayer != null))
			{
				return;
			}
			Unit playerUnit = Engine.PlayerUnit;
			if (playerUnit != null)
			{
				if (playerUnit.IsValidAndNotDestroyed)
				{
					UpdateTitleLabels();
				}
				Unit playerRootUnit = Engine.PlayerRootUnit;
				if (lastPlayerUnit != playerUnit || lastPlayerRootUnit != playerRootUnit || lastKnownUnitName != playerUnit.UnitName || lastKnownRootUnitName != playerRootUnit.UnitName || lastPlayerScene != Engine.ActiveSector || Time.realtimeSinceStartup > lastTimeRefreshedTitleLabel + 2f || (playerUnit != null && playerUnit.Components != null && playerUnit.Components.ShipName != lastKnownShipName))
				{
					// No UpdateTitleLabels() here any more - the label is now rebuilt
					// every frame above, so calling it from here too would do the
					// string build and the mesh rebuild twice on every change frame.
					// This block stays for the caching it does.
					if (playerUnit.IsValidAndNotDestroyed)
					{
						lastTimeRefreshedTitleLabel = Time.realtimeSinceStartup;
					}
					lastKnownUnitName = playerUnit.UnitName;
					lastKnownRootUnitName = playerRootUnit.UnitName;
					CacheLastKnownShipName(playerUnit);
					lastPlayerUnit = playerUnit;
					lastPlayerScene = Engine.ActiveSector;
					lastPlayerRootUnit = playerRootUnit;
				}
			}
			UpdateCreditsLabel();
			UpdateLocationLabelVisibility();
		}

		private void CacheLastKnownShipName(Unit playerUnit)
		{
			if (playerUnit != null && playerUnit.Components != null)
			{
				lastKnownShipName = playerUnit.Components.ShipName;
			}
			else
			{
				lastKnownShipName = null;
			}
		}

		private void UpdateLocationLabelVisibility()
		{
			bool active = UIController.Instance.QuickMsg.ShownMessageCount == 0 && UIController.Instance.QuickMsg.QueuedMessageCount == 0;
			LocationText.gameObject.SetActive(active);
		}

		private void OnEnable()
		{
			StorePlayerCreditsValue();
		}

		private void UpdateCreditsLabel()
		{
			if (oldPlayerCredits != Engine.CreditsAnimation.AnimatedValue)
			{
				oldPlayerCredits = Engine.CreditsAnimation.AnimatedValue;
				RefreshCreditsLabel();
			}
		}

		private void StopCreditsAnimation(int playerCredits)
		{
			oldPlayerCredits = playerCredits;
			RefreshCreditsLabel();
		}

		private static string GetUnitName(Unit unit, bool shortName = true)
		{
			string text = unit.GetFriendlyName(shortName);
			if (unit.Faction != null && !unit.Faction.IsPlayerFaction)
			{
				string shortNameElseLong = unit.Faction.GetShortNameElseLong();
				if (shortNameElseLong != unit.UnitName)
				{
					text += $" ({shortNameElseLong})";
				}
			}
			return text;
		}

		private string GetLocationText()
		{
			stringBuilder.Length = 0;
			Unit playerCurrentUnit = DockUI.PlayerCurrentUnit;
			if (playerCurrentUnit != null)
			{
				// Frame rate prefix, e.g. "60/V-Sync - Shuttle-A "Aether", Abocleus".
				// Measured from unscaled time so it stays true while a pause screen
				// has stopped Time.timeScale.
				//
				// The denominator is whatever cap was chosen: the word V-Sync
				// when that is the option, since the display decides and there is
				// no number to show, and the frame rate limit row when one was
				// picked. 0 or less on that row is the "Uncapped" option.
				stringBuilder.Append(displayFps.ToString("0"));
				stringBuilder.Append('/');
				GameController gameController = GameController.Instance;
				if (gameController != null && PlayerPrefs.GetInt(gameController.PlayerOptionConstants.Video_VSyncKey, 1) > 0)
				{
					stringBuilder.Append("V-Sync");
				}
				else if (gameController != null && gameController.UserTargetFrameRate > 0)
				{
					stringBuilder.Append(gameController.UserTargetFrameRate);
				}
				else
				{
					stringBuilder.Append("Uncapped");
				}
				stringBuilder.Append(" - ");
				GetLocationTextNonAlloc(playerCurrentUnit, stringBuilder);
				return stringBuilder.ToString();
			}
			return null;
		}

		public static void GetLocationTextNonAlloc(Unit currentUnit, StringBuilder stringBuilder)
		{
			stringBuilder.Append(GetUnitName(currentUnit));
			if (currentUnit.IsDocked)
			{
				stringBuilder.Append(", docked in " + GetUnitName(currentUnit.GetDockUnit()));
			}
			stringBuilder.Append(", " + currentUnit.Sector.Name);
		}
	}
}
