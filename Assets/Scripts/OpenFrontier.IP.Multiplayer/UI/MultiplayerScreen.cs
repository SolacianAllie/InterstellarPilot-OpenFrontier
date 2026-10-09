using System;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.Multiplayer.UI
{
	/// <summary>
	/// Main-menu screen for multiplayer: shows the mode, lets the player host a
	/// session (and read the join code to share), or paste a code to join one.
	///
	/// This is deliberately a *setup* screen, not a session screen. Hosting
	/// starts a listener and produces a code; joining opens a connection. What
	/// happens inside the session (world streaming, controlling a faction) is
	/// the business of later phases.
	///
	/// The screen owns nothing it did not create, and leaving the screen stops
	/// whatever it started - so backing out of this menu always leaves
	/// multiplayer as it was.
	/// </summary>
	public class MultiplayerScreen : ScreenBase
	{
		/// <summary>Mode buttons, in the same order as <see cref="MultiplayerMode"/>.</summary>
		public Button ModeOffButton;

		public Button ModeJoinButton;

		public Button ModeHostButton;

		/// <summary>Opens a listener and shows the resulting code.</summary>
		public Button HostButton;

		/// <summary>Closes the session this player is hosting.</summary>
		public Button StopHostingButton;

		/// <summary>The box a player pastes a join code into.</summary>
		public InputField JoinCodeField;

		/// <summary>Attempts to join using <see cref="JoinCodeField"/>.</summary>
		public Button JoinButton;

		/// <summary>Shows the generated code while hosting.</summary>
		public Text JoinCodeDisplay;

		/// <summary>Status and error messages.</summary>
		public Text StatusText;

		public Button BackButton;

		private HostSession host;
		private ClientSession client;

		protected override void awake()
		{
			base.awake();

			ModeOffButton?.onClick.AddListener(() => SetMode(MultiplayerMode.Off));
			ModeJoinButton?.onClick.AddListener(() => SetMode(MultiplayerMode.JoinOnly));
			ModeHostButton?.onClick.AddListener(() => SetMode(MultiplayerMode.HostOrJoin));

			HostButton?.onClick.AddListener(StartHosting);
			StopHostingButton?.onClick.AddListener(StopHosting);
			JoinButton?.onClick.AddListener(StartJoining);
			BackButton?.onClick.AddListener(NavigateBack);

			// The join box should never sit there with placeholder text that
			// looks like a real code.
			if (JoinCodeField != null)
			{
				JoinCodeField.onValueChanged.AddListener(_ => RefreshControls());
			}
		}

		protected override void start()
		{
			base.start();
			SetMode(MultiplayerSettings.Mode, announce: false);
		}

		protected override void refresh()
		{
			base.refresh();
			RefreshControls();
		}

		private void SetMode(MultiplayerMode mode, bool announce = true)
		{
			MultiplayerSettings.Mode = mode;

			// Switching mode ends anything the previous mode started. Hosting
			// with multiplayer then switched off would otherwise leave a live
			// listener behind, which is not what "off" should mean.
			if (mode == MultiplayerMode.Off)
			{
				StopHosting();
				Disconnect();
			}

			if (announce)
			{
				SetStatus(DescribeMode(mode));
			}

			RefreshControls();
		}

		private void StartHosting()
		{
			if (!MultiplayerSettings.CanHost)
			{
				SetStatus("Multiplayer is set to join-only. Switch it to Host or Join to host a session.");
				return;
			}

			StopHosting();

			// Listen on every local interface. Which one is reachable depends on
			// the network, and the address that ends up in the code is chosen by
			// the OS at connect time - see SessionCode's note on why the code
			// carries the address at all.
			IPAddressSelection.ListenAll(out System.Net.IPAddress address, out int port, out string problem);

			if (address == null)
			{
				SetStatus(problem ?? "Couldn't work out a network address to host on.");
				return;
			}

			host = HostSession.Start(address, port, SetStatus);

			if (host == null)
			{
				return;
			}

			// Hosting a world changes what a connected player may do: no god
			// mode, and only the host writes the shared save.
			ApplyHostPermissions();

			if (JoinCodeDisplay != null)
			{
				JoinCodeDisplay.text = host.JoinCode;
			}

			SetStatus("Hosting. Share this code with the players you want to invite.");
			RefreshControls();
		}

		/// <summary>
		/// Applies the restrictions a hosted session imposes. Uses the engine's
		/// existing permission flags rather than bespoke checks, the same way
		/// skirmish battles already disable god mode when launched from the
		/// battles screen.
		/// </summary>
		private static void ApplyHostPermissions()
		{
			EngineASX engine = EngineASX.Instance;

			if (engine?.World?.Permissions == null)
			{
				return;
			}

			engine.World.Permissions.AllowGodMode = false;
		}

		private void StopHosting()
		{
			if (host == null)
			{
				return;
			}

			host.Dispose();
			host = null;

			if (JoinCodeDisplay != null)
			{
				JoinCodeDisplay.text = string.Empty;
			}

			SetStatus("Stopped hosting.");
			RefreshControls();
		}

		private void StartJoining()
		{
			if (!MultiplayerSettings.CanJoin)
			{
				SetStatus("Multiplayer is switched off.");
				return;
			}

			string code = JoinCodeField != null ? JoinCodeField.text : string.Empty;

			Disconnect();

			client = new ClientSession();
			client.Failed += SetStatus;
			client.Connected += () => SetStatus("Connected.");
			client.Disconnected += SetStatus;

			// The join itself checks the version before opening a socket, so a
			// code from a different build is refused without any network traffic.
			client.Join(code);
		}

		private void Disconnect()
		{
			if (client == null)
			{
				return;
			}

			client.Dispose();
			client = null;
		}

		private static string DescribeMode(MultiplayerMode mode)
		{
			switch (mode)
			{
				case MultiplayerMode.Off:
					return "Multiplayer is off. Open Frontier will not listen for or connect to anyone.";

				case MultiplayerMode.JoinOnly:
					return "Join only. You can join a session someone else is hosting.";

				default:
					return "Host or join. You can host a session and join others'.";
			}
		}

		private void SetStatus(string message)
		{
			if (StatusText != null)
			{
				StatusText.text = message ?? string.Empty;
			}
		}

		private void RefreshControls()
		{
			MultiplayerMode mode = MultiplayerSettings.Mode;
			bool hosting = host != null;

			SetActive(ModeOffButton, mode == MultiplayerMode.Off);
			SetActive(ModeJoinButton, mode == MultiplayerMode.JoinOnly);
			SetActive(ModeHostButton, mode == MultiplayerMode.HostOrJoin);

			SetInteractable(HostButton, !hosting && mode == MultiplayerMode.HostOrJoin);
			SetInteractable(StopHostingButton, hosting);
			SetInteractable(JoinButton, HasJoinableCode() && mode != MultiplayerMode.Off);
		}

		private bool HasJoinableCode()
		{
			return JoinCodeField != null && !string.IsNullOrWhiteSpace(JoinCodeField.text);
		}

		private static void SetActive(Selectable selectable, bool active)
		{
			if (selectable != null)
			{
				selectable.gameObject.SetActive(active);
			}
		}

		private static void SetInteractable(Selectable selectable, bool interactable)
		{
			if (selectable != null)
			{
				selectable.interactable = interactable;
			}
		}

		protected override void onDestroy()
		{
			// Leaving the menu must not leave a listener running or a socket
			// open. Hosting is a deliberate act that ends when the player backs
			// out of the screen that started it.
			StopHosting();
			Disconnect();

			base.onDestroy();
		}
	}
}