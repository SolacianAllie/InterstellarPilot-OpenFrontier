using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OpenFrontier.Editor
{
	/// <summary>
	/// Turns the GPU Resident Drawer on and off from the menu.
	/// </summary>
	/// <remarks>
	/// Run from the menu: <b>Rendering / GPU Resident Drawer / ...</b>.
	///
	/// WHY A MENU ITEM AND NOT JUST A COMMITTED VALUE
	///
	/// This is a test. Putting the flag in the URP asset and leaving it there
	/// makes "was it on or off" a git archaeology question the first time
	/// something looks wrong, and the Frame Debugger comparison is only
	/// meaningful if the two runs differ in exactly this one setting.
	///
	/// It also avoids hand-editing the URP asset at all, which is the asset
	/// AGENTS.md warns about: ShadowQualitySync writes
	/// mainLightShadowmapResolution at runtime, and the editor re-serialised
	/// the asset mid-session and kept the value. Anything that rewrites this
	/// file can do the same, so it is done through the SerializedObject
	/// rather than as text.
	///
	/// THE MODES ARE TWO, NOT THREE
	///
	/// GPUResidentDrawerMode in this URP version is Disabled and
	/// InstancedDrawing only. An earlier note here claimed a third
	/// "Compatibility Mode" - there is not one, and guessing at the numbers
	/// would have written a byte that means nothing.
	///
	/// InstancedDrawer's own documentation says the BatchRendererGroup is
	/// used "whenever possible", so anything that cannot live in the BRG path
	/// falls back rather than disappearing. That is worth remembering when
	/// reading a result: a shader that does not support it is not a bug, it
	/// is the fallback working.
	///
	/// WHAT TO COMPARE, AND ON WHAT
	///
	/// Frame Debugger draw-call count and CPU frame time, with everything
	/// else identical. On a phone build more than on the desktop, because
	/// tile-based GPUs gain most from taking per-renderer bookkeeping off
	/// the CPU and also have the least headroom to lose it.
	///
	/// Watch for per-renderer behaviour changing under it: camera-driven
	/// effects, light probes, and anything tinted per renderer - this project
	/// sets team colours per ship in ActiveUnit.ApplyTeamColours, which is
	/// exactly the kind of thing that can look different.
	/// </remarks>
	public static class GpuResidentDrawerToggle
	{
		private const string MenuRoot = "Rendering/GPU Resident Drawer";
		private const string UrpAssetPath =
			"Assets/InterstellarPilot2/Settings/OpenFrontier-URP.asset";

		[MenuItem(MenuRoot + "/Enable", false, 20)]
		public static void Enable()
		{
			Set(GPUResidentDrawerMode.InstancedDrawing);
		}

		[MenuItem(MenuRoot + "/Disable", false, 21)]
		public static void Disable()
		{
			Set(GPUResidentDrawerMode.Disabled);
		}

		[MenuItem(MenuRoot + "/Log Current Mode", false, 22)]
		public static void LogCurrent()
		{
			UniversalRenderPipelineAsset asset = LoadAsset();

			if (asset == null)
			{
				return;
			}

			Debug.Log("GPU Resident Drawer: " + CurrentMode()
				+ " on " + UrpAssetPath
				+ " (SRP Batcher is "
				+ (asset.useSRPBatcher ? "on" : "off") + ").", asset);
		}

		private static void Set(GPUResidentDrawerMode mode)
		{
			UniversalRenderPipelineAsset asset = LoadAsset();

			if (asset == null)
			{
				return;
			}

			SerializedObject serialized = new SerializedObject(asset);
			SerializedProperty property =
				serialized.FindProperty("m_GPUResidentDrawerMode");

			if (property == null)
			{
				Debug.LogError("GPU Resident Drawer: m_GPUResidentDrawerMode"
					+ " not found on " + UrpAssetPath + ". This URP version may"
					+ " name it differently.", asset);

				return;
			}

			property.intValue = (int)mode;
			serialized.ApplyModifiedProperties();
			EditorUtility.SetDirty(asset);
			AssetDatabase.SaveAssets();

			Debug.Log("GPU Resident Drawer set to " + mode
				+ (mode == GPUResidentDrawerMode.InstancedDrawing
					? ". Applies on the next play mode entry or editor restart."
					: "."), asset);
		}

		private static GPUResidentDrawerMode CurrentMode()
		{
			UniversalRenderPipelineAsset asset = LoadAsset();

			if (asset == null)
			{
				return GPUResidentDrawerMode.Disabled;
			}

			SerializedObject serialized = new SerializedObject(asset);
			SerializedProperty property =
				serialized.FindProperty("m_GPUResidentDrawerMode");

			return property == null
				? GPUResidentDrawerMode.Disabled
				: (GPUResidentDrawerMode)property.intValue;
		}

		private static UniversalRenderPipelineAsset LoadAsset()
		{
			UniversalRenderPipelineAsset asset =
				AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);

			if (asset == null)
			{
				Debug.LogError("GPU Resident Drawer: no URP asset at "
					+ UrpAssetPath + ". If it has moved, update the path in "
					+ "GpuResidentDrawerToggle.");
			}

			return asset;
		}
	}
}