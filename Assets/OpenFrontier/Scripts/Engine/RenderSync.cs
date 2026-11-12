using System.Collections.Generic;

namespace OpenFrontier.Engine
{
	/// <summary>Something whose transform is driven by sim state. This is
	/// the ONLY legal path from sim numbers to transforms: RenderSync calls
	/// it once per render frame. Sim never reads render state.</summary>
	public interface ISimRenderable
	{
		void RenderSync(in RenderFrame frame);
	}

	/// <summary>Read-only per-frame render data. Blittable — the render
	/// offset each renderable computes is
	/// (float)(simPos − playerSimPos): floats hold only render offsets,
	/// bounded by the scaled system radius (pinned-origin architecture).</summary>
	public readonly struct RenderFrame
	{
		public readonly long TickIndex;
		public readonly double SimTime;
		public readonly double PlayerPosX;
		public readonly double PlayerPosY;
		public readonly double PlayerPosZ;

		public RenderFrame(long tickIndex, double simTime, in SimBody player)
		{
			TickIndex = tickIndex;
			SimTime = simTime;
			PlayerPosX = player.PosX;
			PlayerPosY = player.PosY;
			PlayerPosZ = player.PosZ;
		}
	}

	/// <summary>
	/// Per-render-frame sync — THE ONLY gameplay transform writer. Runs
	/// from EngineCore.Update (render cadence), never in FixedUpdate; the
	/// sim machine must not depend on frame rate and render must not write
	/// sim state. M1 walks an empty list: the stub world simulates nothing
	/// yet, so the deliverable is the seam and the doctrine it enforces.
	/// Renders the LATEST snapshot; interpolation between the two kept
	/// snapshots is an M3 decision.
	/// </summary>
	public sealed class RenderSync
	{
		private readonly SimStateBuffer simState;
		private readonly List<ISimRenderable> renderables = new List<ISimRenderable>(64);

		public RenderSync(SimStateBuffer simState)
		{
			this.simState = simState;
		}

		public void Register(ISimRenderable renderable)
		{
			// Double-register guard: the same double-tick lesson as the
			// scheduler — a double entry double-writes a transform.
			if (renderable == null || renderables.Contains(renderable))
			{
				return;
			}

			renderables.Add(renderable);
		}

		public void Unregister(ISimRenderable renderable)
		{
			renderables.Remove(renderable);
		}

		public void RenderUpdate()
		{
			if (!simState.HasSnapshot || renderables.Count == 0)
			{
				return;
			}

			ref readonly SimSnapshot latest = ref simState.Latest;
			var frame = new RenderFrame(latest.TickIndex, latest.SimTime, in latest.Player);

			int count = renderables.Count;
			for (int i = 0; i < count; i++)
			{
				renderables[i].RenderSync(in frame);
			}
		}
	}
}
