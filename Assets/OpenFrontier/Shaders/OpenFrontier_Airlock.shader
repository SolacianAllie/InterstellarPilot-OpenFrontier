// Open Frontier - airlock forcefield.
//
// A transparent glowing mesh. Where part of the mesh approaches or crosses
// other geometry (hull, trim, a body floating through the opening), that part
// glows hotter than the free-face parts - the forcefield look.
//
// CONTACT GLOW + THE HOUSE DEPTH DOCTRINE. AGENTS.md: on this OpenGL stack
// `_CameraDepthTexture` reads ~0 behind open space, and every depth-compare
// written against it has died (the legacy-particle shaders lost whole alpha
// fades to it). The classic intersection ramp computes `diff = sceneZ - eyeZ`
// and glows as diff approaches zero; the dead read parks diff at NEGATIVE
// eyeZ, which saturates the ramp to full glow - the field would light up
// everywhere, exactly the failure the doctrine forbids. So the read here is
// DOUBLE-GUARDED:
//
//   1. UNIFORM GATE - `_ContactBoost > 0` decides, per material, whether the
//      depth read happens at all. Set boost to 0 on any platform where the
//      read proves dead and the draw becomes pure ALU; the field still renders
//      via the rim + pulse furniture below.
//   2. SIGN GUARD - the ramp is multiplied by `step(1e-4, diff)`. A negative
//      or zero diff (the dead read, or a depth leak) yields NO contact glow -
//      never glow-everywhere, never invisibility. Where the read IS live
//      (PC tiers, and Android tiles where the URP asset's
//      `m_RequireDepthTexture: 1` binds a real texture over real geometry),
//      the ring fires honestly at the seam.
//
// The degrade contract is therefore: worst case the field loses its seam
// highlight, it NEVER gains a false one. The hardware Z test stays the whole
// truth for occlusion, exactly as on the nav lights.
//
// ALWAYS-LIVE FURNITURE (needs no depth texture): a view-angle rim so the
// field's silhouette glows like a held-in energy boundary, and a frac-bounded
// travelling shimmer (NavLights doctrine: hours-long sessions cannot drift
// the phase) so free-face geometry never reads as dead glass.
//
// MOBILE BUDGET: one pass, no keywords (one compiled variant total), no
// textures, no precision crossover beyond the two float depth scalars, SRP
// batcher via a complete UnityPerMaterial block (no instancing path). The
// URP asset already pays for `_CameraDepthTexture` (m_RequireDepthTexture: 1);
// this shader adds no frame-wide cost of its own. _ContactBoost = 0 skips the
// read on materials that do not want it.
//
// KNOBS - four, in the NavLights minimalism spirit: colour, brightness, how
// much a contact seam brightens, how many metres of "touching" count. The rim
// power/intensity, shimmer depth/speed and sweep rate are constants in the
// program half; ask and any of them becomes a property.

Shader "OpenFrontier/Airlock"
{
	Properties
	{
		[HDR]_Color("Glow Color", Color) = (0.25, 0.7, 1.0, 1.0)
		_Intensity("Glow Intensity", Float) = 1.5
		_ContactBoost("Contact Boost", Float) = 3.0
		_ContactThickness("Contact Thickness", Float) = 0.35
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
			"RenderPipeline" = "UniversalPipeline"
			"IgnoreProjector" = "True"
			"PreviewType" = "Plane"
		}
		// Standard transparent band: the field is hull-mounted and distance
		// sorts honestly against gameplay particles and lasers.

		Pass
		{
			Name "AirlockForcefield"
			Tags { "LightMode" = "UniversalForward" }

			Blend One One
			// ColorMask RGB: additive glow must not write destination alpha
			// (the frame's alpha is not a shared budget this pass should burn).
			ColorMask RGB
			ZWrite Off
			ZTest LEqual
			// Two-sided: the field reads from both sides of the opening.
			Cull Off

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex AirlockVert
			#pragma fragment AirlockFrag

			#include "OpenFrontierAirlock.hlsl"
			ENDHLSL
		}
	}
	Fallback Off
}
