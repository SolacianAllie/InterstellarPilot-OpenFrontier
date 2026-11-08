// Open Frontier - navigation lights.
//
// One additive, fully unlit material for the NavLight prefab's quad, drawn in
// ONE depth-honest pass. There is no lighting model and none is wanted: a nav
// light is a pure emissive chip.
//
// MINIMAL BY INSTRUCTION (2026-11-07): the user stripped the system to its
// five knobs - size, brightness, flashing, colour, texture - and dropped
// everything else the shader used to decide by itself: distance grow, the
// z-pivot relief, the ray fade, the far fade, the round shaping and the
// two-pass occlusion/spill dance. What remains is exactly four jobs, all
// in the GPU so no ship ever runs an Update for a decoration:
//
//  1. BILLBOARD. The quad's own vertices are ignored except for their corner
//     offsets. The shader rebuilds each quad as a PURE billboard - plane hung
//     perpendicular to the anchor->camera sight line, vertical pinned to world
//     up - so a NavLight prefab can sit at any rotation and still faces you
//     from wherever it sits on screen. The plane is planted AT the chip's
//     transform origin: a flare is exactly as large as its Size and exactly
//     where its chip is, at every distance.
//  2. FLASH. Pattern comes per instance (see NavLight.cs): Solid, a single
//     pulse or the tight double pair. Timing is _Time.y on the GPU; the CPU
//     never touches it. frac() bounds the phase argument so a session running
//     for hours cannot drift the pattern.
//  3. COLOUR + TEXTURE. One texture sample, rgb*a, multiplied by the per
//     instance colour (alpha is the brightness slot).
//  4. OCCLUSION - THE PLAIN TRUTH. ZTest LEqual against the hull, and the
//     billboard plane sits AT the anchor: at oblique angles the hull cuts the
//     flare where the plane recedes behind the surface. That cut is what a
//     real lamp on that surface does. Nothing flares past an occluder by
//     design, and nothing decides on the author's behalf.
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
		[NoScaleOffset][MainTex] _FlareTex("Flare Texture", 2D) = "white" {}
		// Nothing else. Grow, push, fades, shaping and the halo pass were
		// removed 2026-11-07 at the user's direction - any new property
		// returned to this block should answer "is this one of the five
		// knobs (size, brightness, flash, colour, texture)?" first.
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

			#include "OpenFrontierNavLights.hlsl"
			ENDHLSL
		}
	}
	Fallback Off
}
