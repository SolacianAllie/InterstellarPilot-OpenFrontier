using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
// The renderer-list descriptor is nested inside RendererUtils in the
// RenderGraph module; alias it for readable call sites.
using RendererListDesc = UnityEngine.Rendering.RendererUtils.RendererListDesc;

namespace OpenFrontier
{
	/// <summary>
	/// Open Frontier: publishes _CloakBackdropRT, the frame snapshot the
	/// ship cloak shader displaces. Registered onto the URP renderers by
	/// the editor command in Assets/Editor (CloakBackdropFeatureInstaller).
	///
	/// WHY THIS EXISTS (measured, see OpenFrontier_ShipCloak.shader):
	/// stock URP's opaque-texture snapshot is copied after opaques and
	/// the skybox, but this game's visible backdrop -- the nebula and sun
	/// quads -- is transparent-phase world content (queue 2999/3000,
	/// distance-sorted under the ships), so the stock copy never held the
	/// nebulae the player actually sees behind a hull. Bending the stock
	/// copy was bending black space.
	///
	/// HOW IT RUNS: at the same event stock URP copies to
	/// _CameraOpaqueTexture (BeforeRenderingTransparents) this blits the
	/// camera colour into a small RT and re-draws the backdrop quads over
	/// it -- nebulae (layer 0, queue 2999) and the sun (layer 16) -- then
	/// publishes the result as a global texture with
	/// SetGlobalTextureAfterPass, the same contract stock URP uses for
	/// the opaque copy. The redraw uses the frame's own culling results
	/// (no scene walk), and everything bails when no hull wants a
	/// snapshot (CloakBackdropRequests.Any), so a frame without cloaks
	/// pays nothing but the feature's return.
	///
	/// COST SHAPE (mobile-first): one quarter-scale blit plus a handful
	/// of sky quads per frame, only on frames where something is
	/// cloaking; live with the cloak work, not the post stack.
	/// </summary>
	public class CloakBackdropFeature : ScriptableRendererFeature
	{
		/// <summary>Longest edge of the snapshot, in pixels. Bent backdrop is smooth content; quarter scale is enough.</summary>
		[System.Serializable]
		public class Settings
		{
			public int snapshotLongEdge = 256;
		}

		public Settings settings = new Settings();

		private CloakBackdropPass m_Pass;

		public override void Create()
		{
			m_Pass = new CloakBackdropPass();
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			CameraType cameraType = renderingData.cameraData.cameraType;
			// Probes, previews and offscreen depth renders must not overwrite
			// the global snapshot the player camera published.
			if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
			{
				return;
			}
			m_Pass.snapshotLongEdge = settings.snapshotLongEdge;
			renderer.EnqueuePass(m_Pass);
		}

		protected override void Dispose(bool disposing)
		{
		}
	}

	/// <summary>
	/// The recorder: blit camera colour to _CloakBackdropRT (aspect kept,
	/// long edge from the feature settings), overlay the backdrop quads,
	/// publish as a global texture. Recorders are cheap per camera: the
	/// RG resource graph owns the RT's lifetime per frame.
	/// </summary>
	class CloakBackdropPass : ScriptableRenderPass
	{
		private static readonly string kPassName = "OpenFrontier Cloak Backdrop";
		private static readonly int kBackdropId = Shader.PropertyToID("_CloakBackdropRT");
		private static readonly ShaderTagId[] kShaderTags = new ShaderTagId[]
		{
			new ShaderTagId("SRPDefaultUnlit"),
			new ShaderTagId("UniversalForward"),
			new ShaderTagId("UniversalForwardOnly")
		};

		private class PassData
		{
			internal RendererListHandle nebulaList;
			internal RendererListHandle sunList;
		}

		internal int snapshotLongEdge = 256;

		public CloakBackdropPass()
		{
			// profilingSampler's setter is protected internal: only the
			// subclass may set it, so it lives here rather than on the
			// feature's object initializer.
			profilingSampler = new ProfilingSampler("OpenFrontier Cloak Backdrop");
			renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
		}

		// One-print-per-change skip/recording diagnostics (editor only --
		// nothing here informs the player). Enumerating why the pass bails
		// took a Frame Debugger session the first time; these replace that
		// archaeology with a console line.
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		private static void ReportOnChange(ref string last, string detail, string prefix)
		{
			if (!string.Equals(last, detail))
			{
				last = detail;
				Debug.Log($"{prefix}: {detail}");
			}
		}

		private static string s_SkipReason;
		private static string s_RecordCamera;

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			// Nobody cloaks this frame: the snapshot has no reader.
			if (!CloakBackdropRequests.Any)
			{
				ReportOnChange(ref s_SkipReason, "no cloaking hull wants a snapshot", "[CloakBackdrop] skip");
				s_RecordCamera = null;
				return;
			}

			var cameraData = frameData.Get<UniversalCameraData>();
			// The engine hands the render camera over at CreateCamera; other
			// game-type cameras must not stomp the player's snapshot.
			if (CloakBackdropRequests.ProvidedCamera != null && cameraData.camera != CloakBackdropRequests.ProvidedCamera)
			{
				string provided = CloakBackdropRequests.ProvidedCamera != null ? CloakBackdropRequests.ProvidedCamera.name : "<null>";
				ReportOnChange(ref s_SkipReason, $"camera '{cameraData.camera.name}' is not the provided '{provided}'", "[CloakBackdrop] skip");
				s_RecordCamera = null;
				return;
			}

			var resourceData = frameData.Get<UniversalResourceData>();
			if (!resourceData.cameraColor.IsValid())
			{
				ReportOnChange(ref s_SkipReason, $"cameraColor invalid on '{cameraData.camera.name}'", "[CloakBackdrop] skip");
				s_RecordCamera = null;
				return;
			}

			if (!string.Equals(s_RecordCamera, cameraData.camera != null ? cameraData.camera.name : "<null>"))
			{
				s_RecordCamera = cameraData.camera != null ? cameraData.camera.name : "<null>";
				s_SkipReason = null;
				Debug.Log($"[CloakBackdrop] recording on '{s_RecordCamera}'");
			}

			var universalRenderingData = frameData.Get<UniversalRenderingData>();

			TextureDesc backdropDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
			int longEdge = Mathf.Max(64, snapshotLongEdge);
			var targetDescriptor = cameraData.cameraTargetDescriptor;
			float aspect = ((float)targetDescriptor.height) / Mathf.Max(1, targetDescriptor.width);
			backdropDesc.name = "_CloakBackdropRT";
			backdropDesc.width = longEdge;
			backdropDesc.height = Mathf.Max(32, Mathf.RoundToInt(longEdge * aspect));
			backdropDesc.filterMode = FilterMode.Bilinear;
			TextureHandle backdrop = renderGraph.CreateTexture(backdropDesc);

			// Downscale + copy. The source already contains opaques + the
			// skybox (both draw before this event); empty space comes through
			// as the camera's sky colour, not black.
			renderGraph.AddBlitPass(resourceData.cameraColor, backdrop, Vector2.one, Vector2.zero, passName: "Cloak Backdrop Copy");

			// The transparent-phase backdrop quads, redrawn from the same
			// culling snapshot the transparent bucket uses. Nebulae mark
			// themselves with queue 2999 on purpose (338 materials); the sun
			// sits on layer 16. Ship hulls are queue 3000 on neither layer
			// set and cannot leak in.
			RendererListDesc nebulaDesc = new RendererListDesc(kShaderTags, universalRenderingData.cullResults, cameraData.camera);
			nebulaDesc.renderQueueRange = new RenderQueueRange(2999, 2999);
			nebulaDesc.layerMask = -1;
			RendererListHandle nebulaList = renderGraph.CreateRendererList(nebulaDesc);
			RendererListDesc sunDesc = new RendererListDesc(kShaderTags, universalRenderingData.cullResults, cameraData.camera);
			sunDesc.renderQueueRange = new RenderQueueRange(2999, 3000);
			sunDesc.layerMask = 1 << 16;
			RendererListHandle sunList = renderGraph.CreateRendererList(sunDesc);

			using (var builder = renderGraph.AddRasterRenderPass<PassData>(kPassName, out var passData, profilingSampler))
			{
				passData.nebulaList = nebulaList;
				passData.sunList = sunList;
				builder.SetRenderAttachment(backdrop, 0, AccessFlags.ReadWrite);
				builder.UseRendererList(passData.nebulaList);
				builder.UseRendererList(passData.sunList);

				builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
				{
					context.cmd.DrawRendererList(data.nebulaList);
					context.cmd.DrawRendererList(data.sunList);
				});

				// Publish for the cloak shader, the same global-texture
				// contract stock URP uses for _CameraOpaqueTexture.
				builder.SetGlobalTextureAfterPass(backdrop, kBackdropId);
			}
		}
	}
}
