using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.EngineMusic;
using OpenFrontier.IP.MusicPlayer;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class EngineMusicPlayerController : MonoBehaviour
	{
		private Dictionary<string, int> trackPlayCounts = new Dictionary<string, int>();

		private void Update()
		{
			if (!EngineASX.LoadedAndReady || (EngineASX.Instance.World.ObjectiveState != WorldBase.ScenarioState.Playing && EngineASX.Instance.World.ObjectiveState != WorldBase.ScenarioState.Intro))
			{
				return;
			}
			SimpleMusicPlayer musicPlayer = GameController.Instance.MusicPlayer;
			if (musicPlayer != null)
			{
				if (IsMainMenuScenario())
				{
					PlayMenuMusic(musicPlayer);
				}
				else if (EngineASX.Instance.PlayerInCombat)
				{
					PlayWhenInCombat(musicPlayer);
				}
				else if (EngineASX.Instance.World.UseScenarioMusic)
				{
					PlayScenarioMusic(musicPlayer);
				}
			}
		}

		// Open Frontier: the main menu always plays Starbound - and Starbound
		// plays nowhere else. Detected via the world's ScenarioInfo matching
		// GameController.MainMenuScenario (reference or UniqueId).
		private const string MenuMusicTrackResourceName = "music_starbound";

		private static bool IsMainMenuScenario()
		{
			if (EngineASX.Instance == null || EngineASX.Instance.World == null || GameController.Instance == null)
			{
				return false;
			}
			ScenarioInfo scenarioInfo = EngineASX.Instance.World.ScenarioInfo;
			ScenarioInfo mainMenuScenario = GameController.Instance.MainMenuScenario;
			return scenarioInfo != null && mainMenuScenario != null && (scenarioInfo == mainMenuScenario || scenarioInfo.UniqueId == mainMenuScenario.UniqueId);
		}

		private static void PlayMenuMusic(SimpleMusicPlayer musicPlayer)
		{
			if (musicPlayer.PlayingTrack == null && musicPlayer.QueuedTracks.Count == 0)
			{
				Track track = new Track
				{
					FinishMode = TrackFinishMode.Discard,
					Loop = true,
					TrackName = MenuMusicTrackResourceName
				};
				musicPlayer.QueuedTracks.Clear();
				musicPlayer.EnqueueTrack(track);
			}
			else if (musicPlayer.PlayingTrack != null && musicPlayer.PlayingTrack.Track.TrackName != MenuMusicTrackResourceName)
			{
				musicPlayer.FadeOutIfPlaying();
			}
		}

		private void PlayScenarioMusic(SimpleMusicPlayer musicPlayer)
		{
			if (EngineASX.Instance.ActiveSector != null && EngineASX.Instance.ActiveSectorData != null && !string.IsNullOrEmpty(EngineASX.Instance.ActiveSectorData.BackgroundMusicResourceName))
			{
				string backgroundMusicResourceName = EngineASX.Instance.ActiveSectorData.BackgroundMusicResourceName;
				if (musicPlayer.PlayingTrack == null && musicPlayer.QueuedTracks.Count == 0)
				{
					SectorTrack track = new SectorTrack
					{
						FinishMode = TrackFinishMode.Discard,
						Loop = false,
						TrackName = backgroundMusicResourceName
					};
					RecordRecordPlay(backgroundMusicResourceName);
					musicPlayer.QueuedTracks.Clear();
					musicPlayer.EnqueueTrack(track);
				}
				else if (musicPlayer.PlayingTrack == null || !(musicPlayer.PlayingTrack.Track is SectorTrack) || musicPlayer.PlayingTrack.Track.TrackName != backgroundMusicResourceName)
				{
					musicPlayer.FadeOutIfPlaying();
				}
			}
			else if (musicPlayer.PlayingTrack == null && musicPlayer.QueuedTracks.Count == 0)
			{
				string randomJukeboxTrackToPlay = GetRandomJukeboxTrackToPlay();
				if (randomJukeboxTrackToPlay != null)
				{
					JukeboxTrack track2 = new JukeboxTrack
					{
						FinishMode = TrackFinishMode.Discard,
						Loop = false,
						TrackName = randomJukeboxTrackToPlay
					};
					RecordRecordPlay(randomJukeboxTrackToPlay);
					musicPlayer.QueuedTracks.Clear();
					musicPlayer.EnqueueTrack(track2);
				}
			}
			else if (musicPlayer.PlayingTrack == null || !(musicPlayer.PlayingTrack.Track is JukeboxTrack))
			{
				musicPlayer.FadeOutIfPlaying();
			}
		}

		private static void PlayWhenInCombat(SimpleMusicPlayer musicPlayer)
		{
			if (musicPlayer.PlayingTrack == null && musicPlayer.QueuedTracks.Count == 0)
			{
				string random = EngineASX.Instance.CombatAudioSourceNames.GetRandom();
				if (!string.IsNullOrEmpty(random))
				{
					CombatTrack track = new CombatTrack
					{
						FinishMode = TrackFinishMode.Discard,
						Loop = false,
						TrackName = random
					};
					musicPlayer.QueuedTracks.Clear();
					musicPlayer.EnqueueTrack(track);
				}
			}
			else if (musicPlayer.PlayingTrack == null || !(musicPlayer.PlayingTrack.Track is CombatTrack))
			{
				musicPlayer.FadeOutIfPlaying();
			}
		}

		private string GetRandomJukeboxTrackToPlay()
		{
			if (EngineASX.Instance.JukeboxAudioSourceNames.Length != 0)
			{
				var source = EngineASX.Instance.JukeboxAudioSourceNames.Select((string e) => new
				{
					TrackName = e,
					Count = GetPlaycountOfTrack(e)
				}).ToList();
				int minPlayCount = source.Min(e => e.Count);
				return source.Where(e => e.Count == minPlayCount).GetRandom().TrackName;
			}
			return null;
		}

		private int GetPlaycountOfTrack(string musicTrack)
		{
			int value = 0;
			if (trackPlayCounts.TryGetValue(musicTrack, out value))
			{
				return value;
			}
			return 0;
		}

		private void RecordRecordPlay(string musicTrack)
		{
			int value = 0;
			if (!trackPlayCounts.TryGetValue(musicTrack, out value))
			{
				trackPlayCounts[musicTrack] = 1;
			}
			else
			{
				trackPlayCounts[musicTrack] = value + 1;
			}
		}
	}
}
