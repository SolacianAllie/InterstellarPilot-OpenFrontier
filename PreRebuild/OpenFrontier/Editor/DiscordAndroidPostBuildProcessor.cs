#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace OpenFrontier.IP.Discord.Editor
{
	/// <summary>
	/// Adds the Discord Social SDK's authentication activity to the
	/// generated Android manifest, with the discord-{applicationId}
	/// redirect scheme the OAuth flow needs to return to the game.
	/// Without it, Android authorization silently fails.
	/// </summary>
	public class DiscordAndroidPostBuildProcessor : IPostGenerateGradleAndroidProject
	{
		private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";

		public int callbackOrder => 0;

		public void OnPostGenerateGradleAndroidProject(string path)
		{
			var config = LoadConfig();
			if (config == null || config.ApplicationId == 0UL)
			{
				Debug.LogWarning("[Discord] Android build: no DiscordPresenceConfig with an Application ID found - the OAuth redirect activity was not added to the manifest.");
				return;
			}
			string manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
			if (!File.Exists(manifestPath))
			{
				return;
			}
			var manifest = new XmlDocument();
			manifest.Load(manifestPath);
			var namespaceManager = new XmlNamespaceManager(manifest.NameTable);
			namespaceManager.AddNamespace("android", AndroidNamespace);
			var application = manifest.SelectSingleNode("/manifest/application");
			if (application == null)
			{
				return;
			}

			// Replace any stale activity from a previous build
			// so the scheme always matches the current app ID.
			var existingActivity = application.SelectSingleNode(
				"activity[@android:name='com.discord.socialsdk.AuthenticationActivity']",
				namespaceManager);
			if (existingActivity != null)
			{
				application.RemoveChild(existingActivity);
			}

			var activity = manifest.CreateElement("activity");
			activity.SetAttribute("name", AndroidNamespace, "com.discord.socialsdk.AuthenticationActivity");
			activity.SetAttribute("exported", AndroidNamespace, "true");
			var intentFilter = manifest.CreateElement("intent-filter");
			var action = manifest.CreateElement("action");
			action.SetAttribute("name", AndroidNamespace, "android.intent.action.VIEW");
			intentFilter.AppendChild(action);
			var defaultCategory = manifest.CreateElement("category");
			defaultCategory.SetAttribute("name", AndroidNamespace, "android.intent.category.DEFAULT");
			intentFilter.AppendChild(defaultCategory);
			var browsableCategory = manifest.CreateElement("category");
			browsableCategory.SetAttribute("name", AndroidNamespace, "android.intent.category.BROWSABLE");
			intentFilter.AppendChild(browsableCategory);
			var data = manifest.CreateElement("data");
			data.SetAttribute("scheme", AndroidNamespace, "discord-" + config.ApplicationId);
			intentFilter.AppendChild(data);
			activity.AppendChild(intentFilter);
			application.AppendChild(activity);
			manifest.Save(manifestPath);
		}

		private static DiscordPresenceConfig LoadConfig()
		{
			var assets = AssetDatabase.FindAssets("t:DiscordPresenceConfig");
			if (assets.Length == 0)
			{
				return null;
			}
			string guid = assets[0];
			string path = AssetDatabase.GUIDToAssetPath(guid);
			return AssetDatabase.LoadAssetAtPath<DiscordPresenceConfig>(path);
		}
	}
}
#endif
