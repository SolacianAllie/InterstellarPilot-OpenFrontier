using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.MainMenu
{
	/// <summary>
	/// Stage tags shown next to the version number, combinable: the inspector
	/// renders a [Flags] enum as checkboxes, so Alpha, Beta and Pre Release can
	/// be combined in any mixture. Display order is fixed (Development first,
	/// Stable last) whatever the box order.
	/// </summary>
	[System.Flags]
	public enum VersionStage
	{
		Development = 1 << 0,

		Alpha = 1 << 1,

		Beta = 1 << 2,

		PreRelease = 1 << 3,

		Stable = 1 << 4,
	}

	/// <summary>
	/// Builds and shows the build version plus toggleable, combinable extras.
	///
	/// Version source: <see cref="Application.version"/> - Project Settings ->
	/// Player -> Other Settings -> Version. One number to bump, same rule the
	/// PreRebuild <c>Versioning</c> class follows (it parses the same string for
	/// save-game and multiplayer checks), so the menu, saves and sessions all
	/// agree with the build. When that class is restored, its
	/// <c>StageTag</c> booleans are a precedence-picked single tag (Alpha beats
	/// Beta), while this label's checkboxes combine - a deliberate divergence,
	/// this is presentation only.
	///
	/// Segments, each optional, joined with the separator in this order:
	/// number block ("Version 0.1.1 Beta"), the update name, the short build
	/// code. Example with everything on:
	/// <c>Version 0.1.1 Beta PreRelease - Fleet Carriers Update - FID: b3h1g7m</c>
	///
	/// Set <see cref="Separator"/> to a newline for one segment per line.
	///
	/// The text refreshes on Awake, on inspector edits (edit-mode preview, so
	/// the label can be laid out with real content; Awake overwrites in play
	/// and in builds), and on demand from the context menu. Event-driven only -
	/// nothing runs per frame.
	/// </summary>
	public class VersionLabel : MonoBehaviour
	{
		[Tooltip("The text that shows the version, legacy Text. Empty finds the first Text child of this object.")]
		public Text Target;

		[Tooltip("The text that shows the version, TextMesh Pro. Takes priority over Target when both are set.")]
		public TMP_Text TargetTMP;

		[Header("Core")]
		[Tooltip("Word in front of the number, e.g. 'Version'. Empty shows just the number.")]
		public string Prefix = "Version";

		[Tooltip("Show the version number (Application.version) at all.")]
		public bool ShowVersionNumber = true;

		[Header("Stage tags (togglable and combinable)")]
		[Tooltip("Checked boxes appear after the number. Development is also appended automatically on developer builds / in the editor when Auto Development is on.")]
		public VersionStage Stages;

		[Tooltip("Append 'Development' automatically when Debug.isDebugBuild (true in the editor and in development builds).")]
		public bool AutoDevelopment = true;

		[Header("Extras")]
		[Tooltip("Update name, e.g. 'Fleet Carriers Update'. Empty omits the segment.")]
		public string UpdateName;

		[Tooltip("Show a short build code, FID-style, taken from the build GUID (zeros in the editor before the first real build).")]
		public bool ShowBuildCode;

		[Tooltip("Word in front of the code, e.g. 'FID'. Empty shows the bare code.")]
		public string BuildCodeLabel = "FID";

		[Range(1, 32)]
		[Tooltip("How many characters of the build GUID to show.")]
		public int BuildCodeCharacters = 7;

		[Header("Formatting")]
		[Tooltip("Between segments. Set to a newline for one segment per line.")]
		public string Separator = " - ";

		[UnityEngine.ContextMenu("Refresh")]
		public void Refresh()
		{
			if (Target == null && TargetTMP == null)
			{
				// Legacy first (this project's UI text), TMPro label next.
				Target = GetComponentInChildren<Text>();
			}

			if (Target == null && TargetTMP == null)
			{
				TargetTMP = GetComponentInChildren<TMP_Text>();
			}

			if (Target == null && TargetTMP == null)
			{
				return;
			}

			VersionStage stages = Stages;

			if (AutoDevelopment && Debug.isDebugBuild)
			{
				stages |= VersionStage.Development;
			}

			StringBuilder builder = new StringBuilder(64);

			// First segment: prefix + number, combined because neither alone
			// reads well when the other is present.
			if (ShowVersionNumber || !string.IsNullOrEmpty(Prefix))
			{
				if (!string.IsNullOrEmpty(Prefix))
				{
					builder.Append(Prefix);
				}

				if (ShowVersionNumber)
				{
					if (builder.Length > 0)
					{
						builder.Append(' ');
					}

					builder.Append(string.IsNullOrEmpty(Application.version) ? "0.0.1" : Application.version);
				}

				// Tags hug the number ("0.1.1 Beta"), matching the house
				// StageTag shape (leading space, trailing nothing).
				foreach (VersionStage tag in StageOrder)
				{
					if ((stages & tag) != 0)
					{
						builder.Append(' ').Append(tag);
					}
				}
			}

			AppendSegment(builder, UpdateName);

			if (ShowBuildCode)
			{
				string code = Application.buildGUID;

				if (code.Length > BuildCodeCharacters)
				{
					code = code.Substring(0, Mathf.Min(BuildCodeCharacters, code.Length));
				}

				AppendSegment(builder, string.IsNullOrEmpty(BuildCodeLabel) ? code : BuildCodeLabel + ": " + code);
			}

			// TMPro takes priority when both slots are filled - one writer, never
			// two components fighting over the same label.
			if (TargetTMP != null)
			{
				TargetTMP.text = builder.ToString();
			}
			else
			{
				Target.text = builder.ToString();
			}
		}

		private void AppendSegment(StringBuilder builder, string segment)
		{
			if (string.IsNullOrEmpty(segment))
			{
				return;
			}

			if (builder.Length > 0)
			{
				builder.Append(Separator);
			}

			builder.Append(segment);
		}

		private void Awake()
		{
			Refresh();
		}

		private void OnValidate()
		{
			// Editor-only callback: gives a live preview while authoring. The value
			// written persists in the scene, which is intended - it previews real
			// content - and Awake overwrites it in play mode and builds. Editor
			// builds show Development via Auto Development, plan for it.
			Refresh();
		}

		/// <summary>
		/// Fixed display order for the tags, declared top to bottom; bit order
		/// inside the enum does not decide it.
		/// </summary>
		private static readonly VersionStage[] StageOrder =
		{
			VersionStage.Development,
			VersionStage.Alpha,
			VersionStage.Beta,
			VersionStage.PreRelease,
			VersionStage.Stable,
		};
	}
}
