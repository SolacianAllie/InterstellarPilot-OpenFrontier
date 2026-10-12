using System.Text;
using OpenFrontier.IP.Engine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OpenFrontier.EditorTools
{
	public static class MigrationDiagnostics
	{
		[MenuItem("OpenFrontier/Dump DeepSpace Renderers")]
		public static void DumpDeepSpace()
		{
			var sb = new StringBuilder();
			int deepSpaceLayer = 30;
			var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			int shown = 0;
			foreach (var r in renderers)
			{
				if (r.gameObject.layer != deepSpaceLayer) continue;
				shown++;
				if (shown > 12) continue;
				var mats = r.sharedMaterials;
				string matDesc = mats == null || mats.Length == 0
					? "NO MATERIALS"
					: string.Join(", ", System.Linq.Enumerable.Select(mats, m => m == null ? "NULL-MAT" : $"{m.name} [shader: {(m.shader == null ? "NULL-SHADER" : m.shader.name)}, tex: {(m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap").name : "none")}]"));
				sb.AppendLine($"{r.GetType().Name} '{r.name}' active={r.gameObject.activeInHierarchy} enabled={r.enabled} pos={r.transform.position} scale={r.transform.lossyScale} mats=[{matDesc}]");
			}
			sb.AppendLine($"total DeepSpace renderers: {shown}");
			var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			foreach (var c in cams)
			{
				var data = c.GetUniversalAdditionalCameraData();
				sb.AppendLine($"camera '{c.name}' depth={c.depth} type={data.renderType} stackCount={(data.renderType == CameraRenderType.Base ? data.cameraStack.Count : 0)}");
			}
			Debug.Log(sb.ToString());
		}

		[MenuItem("OpenFrontier/Dump Star Particle State")]
		public static void DumpStarParticles()
		{
			var sb = new StringBuilder();
			var systems = Object.FindObjectsByType<OpenFrontier.IP.Engine.StarParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			sb.AppendLine($"StarParticleSystem components found: {systems.Length}");
			foreach (var s in systems)
			{
				sb.AppendLine($"--- '{s.name}' (go active={s.gameObject.activeInHierarchy}, comp enabled={s.enabled})");
				var ps = s.ParticleSystem;
				sb.AppendLine($"    ParticleSystem field: {(ps == null ? "NULL (fake or real)" : "OK " + ps.name)}");
				if (ps != null)
				{
					var rend = ps.GetComponent<ParticleSystemRenderer>();
					sb.AppendLine($"    renderer: {(rend == null ? "MISSING" : $"enabled={rend.enabled} material={(rend.sharedMaterial == null ? "NONE/MISSING" : rend.sharedMaterial.name + " shader=" + rend.sharedMaterial.shader.name)}")}");
					sb.AppendLine($"    ps go active={ps.gameObject.activeInHierarchy}, isPlaying={ps.isPlaying}, particleCount={ps.particleCount}");
				}
			}
			Debug.Log(sb.ToString());
		}

		[MenuItem("OpenFrontier/Dump Formation Chain State")]
		public static void Dump()
		{
			var sb = new StringBuilder();
			sb.AppendLine($"Application.isPlaying: {Application.isPlaying}");
			var gc = GameController.Instance;
			sb.AppendLine($"GameController.Instance: {Describe(gc)}");
			if (gc != null)
			{
				var gs = gc.GameSettings;
				sb.AppendLine($"GameSettings: {Describe(gs)}");
				if (gs != null)
				{
					var fs = gs.FormationSettings;
					sb.AppendLine($"FormationSettings: {Describe(fs)}");
					if (fs != null)
					{
						sb.AppendLine($"FormationSettings.GetInstanceID-safe name: {fs.name}, gameObject: {Describe(fs.gameObject)}");
						var list = fs.FormationStyles;
						sb.AppendLine($"FormationStyles list: {(list == null ? "LIST IS NULL" : $"count={list.Count}")}");
						if (list != null)
						{
							for (int i = 0; i < list.Count; i++)
							{
								var e = list[i];
								sb.AppendLine($"  [{i}] fakeNull={(e == null)} refNull={object.ReferenceEquals(e, null)} {Describe(e)}");
								if (e != null)
								{
									sb.AppendLine($"       name={e.name} uniqueId={e.UniqueId} gameObject={Describe(e.gameObject)} childCount={(e.transform == null ? "TRANSFORM FAKENULL" : e.transform.childCount.ToString())}");
								}
							}
							sb.AppendLine($"DefaultFormationStyle: {Describe(fs.DefaultFormationStyle)}");
						}
					}
				}
			}
			Debug.Log(sb.ToString());
		}

		private static string Describe(Object o)
		{
			if (o == null)
				return object.ReferenceEquals(o, null) ? "REAL-NULL" : "FAKE-NULL(destroyed/missing)";
			return $"OK[{o.GetType().Name} '{o.name}' persistent={EditorUtility.IsPersistent(o)}]";
		}
	}
}
