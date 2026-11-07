// Open Frontier - navigation lights.
//
// One additive, fully unlit material for the NavLight prefab's quad, drawn in
// TWO passes that share one program (OpenFrontierNavLights.hlsl). There is no
// lighting model and none is wanted: a nav light is a pure emissive chip. The
// jobs, all in the GPU so no ship ever runs an Update for a decoration:
//
//  1. BILLBOARD (both passes). The quad's own vertices are ignored except
//     for their corner offsets. The shader rebuilds each quad as a PURE
//     billboard - plane hung perpendicular to the anchor->camera sight line,
//     vertical pinned to world up - so a NavLight prefab can sit at any
//     rotation and still faces you from wherever it sits on screen.
//  2. DISTANCE GROW. A light chip has a real size (a wingtip lamp), but the
//     whole point of nav lights is reading a ship at kilometres. Past a
//     reference distance the core grows linearly with camera distance, capped
//     at a max world size, so the angular size never falls below a readable
//     pinprick - done in the vertex stage, no screen-space pass.
//  3. BLINK. Pattern comes per instance (see NavLight.cs): Solid, a single
//     pulse or the tight double pair. Timing is _Time.y on the GPU; the CPU
//     never touches it. frac() bounds the phase argument so a session running
//     for hours cannot drift the pattern.
//  4. OCCLUSION + FADE. The CORE pass keeps ZTest LEqual against the hull: a
//     light on the far side of a ship is hidden by the ship itself. The far
//     fade ends before the game camera's 8500 far plane so a light never pops.
//  5. SPILL (the "over-render, but not entirely"). Because the anchor hangs
//     ON the hull, depth bites jagged wedges out of lamps mounted beside
//     hull steps, whatever the vertex push does. The SECOND pass draws the
//     same chip with ZTest GREATER at _HaloIntensity of its brightness and
//     _HaloSize times its size: GREATER passes only where something NEARER
//     the camera sits in front of the chip - the occluded half, the bitten
//     wedge, the far side - so the bite stops being a hole and reads as
//     glow leaking INSIDE the silhouette, like bloom. The spill can never
//     paint over empty space (buffer reads far there) nor over anything in
//     front of the chip, which is what the user asked for as "not
//     entirely"; the CORE still owns everything the camera can see of the
//     chip. Full silhouettes ghost faintly inside the occluder's outline -
//     the price of the leak, and what bloom on the quality tiers was
//     already painting.
//
// Per-instance state (colour, pattern, period, phase, size) rides the GPU
// instancing buffer via MaterialPropertyBlock - that is what the SRP batcher
// cannot carry, so the batching story for these draw calls is GPU instancing,
// not SRP batching. The material must have "Enable GPU Instancing" ticked
// (the NavLightPrefabBuilder does it) and all lights share one quad mesh +
// one material, which is the batch that instancing folds into ONE draw call
// PER PASS - two per material once the spill is counted.
//
// This deliberately does NOT read _CameraDepthTexture. AGENTS.md: on this
// OpenGL stack sceneZ reads ~0 behind open space and every depth-compare we
// have written against it has died. The hardware Z test is the whole story
// here.

Shader "OpenFrontier/Nav Lights"
{
	Properties
	{
		[NoScaleOffset][MainTex] _FlareTex("Flare Texture (none = soft round dot)", 2D) = "white" {}
		// The white default is a boring square, so the pass shapes its own
		// round falloff on top of whatever texture it gets. A custom texture
		// that is already a halo gets softly corner-dimmed by this; turn the
		// toggle off for full-bleed art.
		[Toggle(_ROUND_ON)] _Round("Round Shaping", Float) = 1

		// Growth + fade are material-wide (every nav light shares the world
		// rules), not per instance. Tune here once.
		_GrowReferenceDistance("Grow Starts At (m)", Float) = 250
		// 6 world units = several pixels of star at the fade end and about
		// 4% of a large ship's length: readable without becoming a flare
		// bigger than the fin it hangs on. Every extra unit here is also
		// extra distance the billboard plane can hang BEHIND hull geometry
		// at oblique angles, where the per-pixel depth test bites chunks
		// out of the flare (the tail beacon that lost half its star inside
		// the fin silhouette, 2026-11-08). If you want the drama back,
		// push this - but the bite returns with it.
		_MaxGrowWorldSize("Max Grown Size (world units)", Float) = 6
		// Unity's particle system can shift billboards toward the camera out
		// of what should be their world position - this is ours, the z
		// relief in the shared program ("pivot toward camera"). Scale it:
		// 1 = as built, 0 = the billboard plane sits exactly at the chip's
		// anchor (true position - and every hull step then z-fights the
		// plane honestly, bites and all), 2 = push twice as far forward.
		// DEPTH-ONLY displacement: the flare's centre always projects at
		// the chip's origin on screen whatever this says - a flare that
		// visually leaves the lamp is NOT this knob's doing.
		_PushScale("Pivot Toward Camera", Float) = 1
		// The game camera's far plane is 8500 (see ActiveUnit.SetDrawObjectsActive
		// - it is the real bound of anything drawn). End the fade before it.
		_FarFadeStart("Far Fade Start (m)", Float) = 7200
		_FarFadeEnd("Far Fade End (m)", Float) = 8300

		// The spill pass (job 5): quiet glow that leaks over hull edges.
		_HaloIntensity("Spill Brightness (0 = no spill)", Range(0, 1)) = 0.35
		_HaloSize("Spill Size Multiplier", Float) = 1.5

		// Per-instance values (colour / pattern / period / phase / size) are
		// not Properties - they arrive through the instancing buffer from
		// NavLight's MaterialPropertyBlock.
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent+12"
			"RenderType" = "Transparent"
			"RenderPipeline" = "UniversalPipeline"
			"IgnoreProjector" = "True"
		}
		// Above the standard transparent band (3000) so gameplay particles
		// and lasers rate-sort in front of the chips.

		// ---- CORE: depth-honest, what reads as the light -------------------
		Pass
		{
			Name "Core"
			Tags { "LightMode" = "UniversalForward" }

			Blend One One
			// ColorMask RGB: additive chips must not write destination alpha
			// (the frame's alpha is not a shared budget this pass should burn).
			ColorMask RGB
			ZWrite Off
			ZTest LEqual
			Cull Off

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex NavLightVert
			#pragma fragment NavLightFrag
			#pragma multi_compile_instancing
			#pragma multi_compile _ _ROUND_ON

			#define NAV_HALO 0
			#include "OpenFrontierNavLights.hlsl"
			ENDHLSL
		}

		// ---- SPILL: glow that leaks inside the occluder --------------------
		// Drawn second. ZTest GREATER is the "not entirely" contract: the
		// spill exists ONLY where something nearer the camera occludes the
		// chip. Over open space (depth reads far) and over anything in front
		// of the chip it fails, so rays never cross the silhouette or hull
		// parts that are nearer; the leak lives strictly inside the shape
		// that culls it. Its honesties are its quietness and the same far
		// fade the core rides.
		Pass
		{
			Name "Spill"
			Tags { "LightMode" = "UniversalForward" }

			Blend One One
			ColorMask RGB
			ZWrite Off
			ZTest Greater
			Cull Off

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex NavLightVert
			#pragma fragment NavLightFrag
			#pragma multi_compile_instancing
			#pragma multi_compile _ _ROUND_ON

			#define NAV_HALO 1
			#include "OpenFrontierNavLights.hlsl"
			ENDHLSL
		}
	}
	Fallback Off
}
