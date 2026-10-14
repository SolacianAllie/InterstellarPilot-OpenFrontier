using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OpenFrontier.Editor
{
	/// <summary>
	/// Turns GPU instancing on for the materials that can actually use it, and
	/// turns it off again.
	/// </summary>
	/// <remarks>
	/// Run from the menu: <b>Rendering / Enable GPU Instancing</b>, and its
	/// partner <b>Disable GPU Instancing</b> for reverting.
	///
	/// WHY A SCRIPT AND NOT A BULK EDIT OF THE .mat FILES
	///
	/// AGENTS.md is explicit that materials whose shader has a known URP
	/// ShaderID get their YAML stripped by the URP material postprocessor, and
	/// that they must be recreated through the Material API. A find-and-
	/// replace across .mat text would be exactly the edit that postprocessor
	/// eats, and it would eat it silently - the material still works, the
	/// setting is just gone again after an import. Every material here is
	/// written through the API, so Unity writes the change itself.
	///
	/// WHICH MATERIALS, AND WHY NOT ALL OF THEM
	///
	/// Of the 500 materials in the project, 349 use
	/// OpenFrontier_ParticlesUnlitNoFog, which has no
	/// multi_compile_instancing and cannot be instanced at all. Particle
	/// systems batch their own quads already; instancing a particle material
	/// does nothing but dirty the asset and force a reimport. Roughly 71 use
	/// URP's Lit shader, which does support it - and those are the ship and
	/// station hulls, which is where repeated meshes actually appear.
	///
	/// So the shader is checked rather than assumed. A package shader is
	/// taken on trust from a known list, because its source is not ours to
	/// read. A custom .shader is read and searched for the pragma, which is
	/// the only reliable answer and costs one file read per shader - a few
	/// dozen, not five hundred materials.
	///
	/// WHAT IT COSTS
	///
	/// Nothing visual. URP Lit is unchanged by the flag.
	///
	/// Memory: an instance buffer of per-instance matrices, roughly 64 bytes
	/// each. A thousand instances is about 64 KB, against a working set this
	/// project already struggles with - see the mipmap streaming note in
	/// AGENTS.md - so it is noise, but it is not literally free.
	///
	/// CPU: that buffer is uploaded per batch per frame. That is the trade
	/// instancing makes - a little bandwidth to remove a draw call - and it
	/// is a good trade on the tile-based GPUs in phones, which is why this is
	/// worth doing at all in a mobile-first project.
	///
	/// The one thing that cannot be answered from the source tree is whether
	/// SRP Batcher and GPU Instancing coexist cleanly on this URP version for
	/// these materials. SRP Batcher cuts state changes; instancing cuts draw
	/// calls; they are different jobs, and Unity prefers the SRB path where it
	/// applies. Whether a given material lands on one or the other is
	/// something to read off the Frame Debugger before and after, not
	/// something to assert from here. Use Disable to compare.
	/// </remarks>
	public static class GpuInstancingSetup
	{
		private const string MenuEnable = "Rendering/Enable GPU Instancing (all materials)";
		private const string MenuDisable = "Rendering/Disable GPU Instancing (all materials)";

		/// <summary>Shaders we trust without reading, because they are not ours.</summary>
		private static readonly HashSet<string> TrustedPackageShaders = new HashSet<string>
		{
			"Universal Render Pipeline/Lit",
			"Universal Render Pipeline/Simple Lit",
			"Universal Render Pipeline/Complex Lit",
			"Universal Render Pipeline/Unlit",
			"Standard",
			"Standard (Specular setup)"
		};

		[MenuItem(MenuEnable)]
		public static void Enable()
		{
			Apply(true);
		}

		[MenuItem(MenuDisable)]
		public static void Disable()
		{
			Apply(false);
		}

		private static void Apply(bool enable)
		{
			// One read per shader, not one per material: 500 materials use a
			// few dozen shaders.
			Dictionary<Shader, bool> support = new Dictionary<Shader, bool>();
			Dictionary<string, int> byShader = new Dictionary<string, int>();
			Dictionary<string, int> skippedByShader = new Dictionary<string, int>();

			List<Material> materials = new List<Material>(1024);

			string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });

			for (int i = 0; i < guids.Length; i++)
			{
				Material material = AssetDatabase.LoadAssetAtPath<Material>(
					AssetDatabase.GUIDToAssetPath(guids[i]));

				if (material != null)
				{
					materials.Add(material);
				}
			}

			int changed = 0;
			int alreadyCorrect = 0;

			AssetDatabase.StartAssetEditing();

			try
			{
				for (int i = 0; i < materials.Count; i++)
				{
					Material material = materials[i];

					if (material == null)
					{
						continue;
					}

					Shader shader = material.shader;

					if (shader == null)
					{
						continue;
					}

					bool canInstance;

					if (!support.TryGetValue(shader, out canInstance))
					{
						canInstance = ShaderCanInstance(shader);
						support[shader] = canInstance;
					}

					if (!canInstance)
					{
						Count(skippedByShader, shader.name);

						continue;
					}

					Count(byShader, shader.name);

					if (material.enableInstancing == enable)
					{
						alreadyCorrect++;

						continue;
					}

					material.enableInstancing = enable;
					EditorUtility.SetDirty(material);
					changed++;
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
			}

			AssetDatabase.SaveAssets();

			int eligible = 0;

			foreach (KeyValuePair<string, int> pair in byShader)
			{
				eligible += pair.Value;
			}

			int skipped = 0;

			foreach (KeyValuePair<string, int> pair in skippedByShader)
			{
				skipped += pair.Value;
			}

			Debug.Log(string.Format(
				"GPU instancing {0}: {1} material(s) changed, {2} already correct. "
				+ "Eligible {3}, skipped {4} because their shader has no "
				+ "instancing variant - particle materials mostly, which batch "
				+ "their own quads and gain nothing here.",
				enable ? "enabled" : "disabled", changed, alreadyCorrect,
				eligible, skipped), null);

			foreach (KeyValuePair<string, int> pair in byShader)
			{
				Debug.Log("  eligible: " + pair.Value + "  " + pair.Key);
			}

			foreach (KeyValuePair<string, int> pair in skippedByShader)
			{
				Debug.Log("  skipped:  " + pair.Value + "  " + pair.Key);
			}
		}

		private static void Count(Dictionary<string, int> counts, string key)
		{
			int existing;

			counts.TryGetValue(key, out existing);
			counts[key] = existing + 1;
		}

		/// <summary>
		/// Whether a shader has an instancing variant.
		/// </summary>
		/// <remarks>
		/// A package shader is trusted from a known list, because its source
		/// is inside a package and not ours to read. A custom .shader is read
		/// and searched for the pragma, which is the only answer that is
		/// actually correct rather than usually right.
		/// </remarks>
		private static bool ShaderCanInstance(Shader shader)
		{
			if (TrustedPackageShaders.Contains(shader.name))
			{
				return true;
			}

			string path = AssetDatabase.GetAssetPath(shader);

			if (string.IsNullOrEmpty(path) || !path.EndsWith(".shader", System.StringComparison.OrdinalIgnoreCase))
			{
				// A built-in or otherwise unreadable shader. Not worth guessing
				// at; the count is reported so nothing goes quiet.
				return false;
			}

			try
			{
				return File.ReadAllText(path)
					.Contains("multi_compile_instancing");
			}
			catch (IOException)
			{
				return false;
			}
		}
	}
}