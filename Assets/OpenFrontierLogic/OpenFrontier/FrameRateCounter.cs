namespace OpenFrontier
{
	/// <summary>
	/// Rolling-average framerate over the last second of real frames.
	/// </summary>
	/// <remarks>
	/// Lives in this assembly rather than in either consumer so the dev overlay
	/// and the player-facing dock header cannot drift apart and disagree on what
	/// the framerate is. OpenFrontier.IP already references OpenFrontier, so
	/// DockUIHeader can reach it without an asmdef change; DevHud lives here.
	///
	/// Three approaches were tried against this before settling:
	///
	///   raw per-frame rate  alternates by a frame or two for reasons that do
	///                       not matter, and is unreadable
	///   fixed-length bucket sampled and reset (what DevHud did, at 0.5s)
	///                       jumps - it reports one window's average, then
	///                       snaps to the next
	///   rolling mean        this. Stable, and still live.
	///
	/// Samples are read newest-first and the window is cut at one second of
	/// accumulated time rather than a fixed frame count, so it covers the same
	/// span at 30, 60 and 144fps. A 64-frame window would cover 1.07s at 60fps
	/// but 2.1s at 30fps, so the slower the display the longer the "average".
	///
	/// The sample ring is allocated once up front; Sample() allocates nothing,
	/// which matters because the dock header calls it every frame.
	/// </remarks>
	public class FrameRateCounter
	{
		private const int SampleCount = 128;

		private const float WindowSeconds = 1f;

		private readonly float[] samples = new float[SampleCount];

		private int head;

		private int count;

		/// <summary>Frames per second over the trailing window.</summary>
		public float FramesPerSecond { get; private set; }

		/// <summary>
		/// Adds one frame. Pass Time.unscaledDeltaTime - a scaled reading
		/// reports 0fps behind any screen that sets timeScale to 0, which is
		/// exactly when something is holding the framerate up and you most
		/// want to see the number.
		/// </summary>
		public void Sample(float unscaledDeltaTime)
		{
			if (unscaledDeltaTime <= 0f)
			{
				return;
			}

			samples[head] = unscaledDeltaTime;
			head = (head + 1) % SampleCount;
			if (count < SampleCount)
			{
				count++;
			}

			float windowTime = 0f;
			int windowFrames = 0;
			int index = head;
			while (windowFrames < count)
			{
				index = (index - 1 + SampleCount) % SampleCount;
				float sample = samples[index];
				// Always keep at least one frame, so a single very long frame
				// still produces a reading rather than an empty window.
				if (windowTime + sample > WindowSeconds && windowFrames > 0)
				{
					break;
				}
				windowTime += sample;
				windowFrames++;
			}

			if (windowTime > 0f)
			{
				FramesPerSecond = windowFrames / windowTime;
			}
		}

		/// <summary>Drops the window, so the next reading starts fresh.</summary>
		public void Reset()
		{
			head = 0;
			count = 0;
			FramesPerSecond = 0f;
		}
	}
}