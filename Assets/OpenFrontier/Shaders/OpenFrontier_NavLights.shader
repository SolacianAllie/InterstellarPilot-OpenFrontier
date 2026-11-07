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
//     same chip with ZTest ALWAYS at _HaloIntensity of its brightness and
//     _HaloSize times its size: the bite stops being a hole and reads as the
//     glow leaking over the edge, like bloom. The hull cut stays honest
//     because the CORE is still what tests depth; the spill never grows past
//     the same _MaxGrowWorldSize cap, so far chips do not double their fill.
//     Full silhouettes do show a faint ghost spill through the ship - that is
//     the price of the leak, and it is what bloom on the quality tiers was
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
		[Toggle(_ROUND)] _Round("Round Shaping", Float) = 1

		// Growth + fade are material-wide (every nav light shares the world
		// rules), not per instance. Tune here once.
		_GrowReferenceDistance("Grow Starts At (m)", Float) = 250
		_MaxGrowWorldSize("Max Grown Size (world units)", Float) = 40
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

		// ---- SPILL: untested glow that leaks over hull edges ---------------
		// Drawn second, always. Nothing here writes depth or tests it: its
		// only honesties are its quietness and the same far fade the core
		// rides.
		Pass
		{
			Name "Spill"
			Tags { "LightMode" = "UniversalForward" }

			Blend One One
			ColorMask RGB
			ZWrite Off
			ZTest Always
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
