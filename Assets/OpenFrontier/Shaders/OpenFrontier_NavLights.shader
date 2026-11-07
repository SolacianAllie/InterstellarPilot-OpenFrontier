// Open Frontier - navigation lights.
//
// One additive, fully unlit material for the NavLight prefab's quad, drawn in
// ONE depth-honest pass. There is no lighting model and none is wanted: a nav
// light is a pure emissive chip. The jobs, all in the GPU so no ship ever runs
// an Update for a decoration:
//
//  1. BILLBOARD. The quad's own vertices are ignored except for their corner
//     offsets. The shader rebuilds each quad as a PURE billboard - plane hung
//     perpendicular to the anchor->camera sight line, vertical pinned to world
//     up - so a NavLight prefab can sit at any rotation and still faces you
//     from wherever it sits on screen.
//  2. DISTANCE GROW. A light chip has a real size (a wingtip lamp), but the
//     whole point of nav lights is reading a ship at kilometres. Past a
//     reference distance the core grows linearly with camera distance, capped
//     at a max world size, so the angular size never falls below a readable
//     pinprick - done in the vertex stage, no screen-space pass.
//  3. BLINK. Pattern comes per instance (see NavLight.cs): Solid, a single
//     pulse or the tight double pair. Timing is _Time.y on the GPU; the CPU
//     never touches it. frac() bounds the phase argument so a session running
//     for hours cannot drift the pattern.
//  4. OCCLUSION + FADE. ZTest LEqual against the hull: a light on the far
//     side of a ship is hidden by the ship itself, and a hull step standing
//     in front of a lamp cuts the flare where it stands. The far fade ends
//     before the game camera's 8500 far plane so a light never pops.
//  5. THE RAYS FADE OUT WITH RANGE. The cross texture's ray spikes belong to
//     close range: past a fade band the fragment blends into the same
//     texture's central core, magnified - a soft dot with no ray geometry.
//     Every distance-breakage report of the previous two-pass system traced
//     back to ray geometry, never to the billboard's own anchor maths, so the
//     dot is what gets grown out to the kilometres: a pinprick star, not a
//     four-armed signpost out-scaling the hull beside it.
//
// WHY THERE IS NO SECOND PASS ANYMORE (the 2026-11-07 redo of the leak).
// The system previously drew a depth-tested CORE plus a ZTest GREATER SPILL
// that repainted the occluded flare pieces over the hull - ray shape
// included - as a hand-drawn stand-in for bloom. Tuning could not save it:
// a pass whose job is to re-project a flare across an occluder prints
// flare-shaped pieces inside the silhouette, and the user saw exactly that
// ("they render inside the ship despite not being inside it"). The soft
// spread past a bite is now POST-PROCESS BLOOM on the quality tiers that
// run it - a blob, which is what real glare does - and on tiers without
// bloom the bite stays what it is: an honest cut flare. Full silhouettes of
// far-side chips render nothing at all, which is the correct fact a giant
// lens-flare spill can never illustrate.
//
// Per-instance state (colour, pattern, period, phase, size) rides the GPU
// instancing buffer via MaterialPropertyBlock - that is what the SRP batcher
// cannot carry, so the batching story for these draw calls is GPU instancing,
// not SRP batching. The material must have "Enable GPU Instancing" ticked
// (the NavLightPrefabBuilder does it) and all lights share one quad mesh +
// one material, which is the batch instancing folds into ONE draw call.
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
		// relief ("pivot toward camera"). Scale it:
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

		// ---- THE ONE PASS: depth-honest, what reads as the light ----------
		// Blend additive, depth-tested against everything opaque before it.
		// Nothing occluded is ever re-projected; the spread past a bite
		// belongs to bloom.
		Pass
		{
			Name "NavLights"
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

			#include "OpenFrontierNavLights.hlsl"
			ENDHLSL
		}
	}
	Fallback Off
}
