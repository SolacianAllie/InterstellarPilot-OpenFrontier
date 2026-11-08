// Open Frontier - airlock forcefield.
//
// A transparent glowing mesh. Where part of the mesh approaches or crosses
// other geometry (hull, trim, a body floating through the opening), that part
// glows hotter than the free-face parts - the forcefield look. The field
// fades out as the camera approaches to clip through it (_NearFade).
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
//      via the rim + shimmer furniture below.
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
// FALLBACK TIER. Some devices will neither read the depth texture honestly
// nor want the contact pass at all, so the shader carries a second shape:
// toggle the AIRLOCK FALLBACK keyword (per material - or at runtime with
// EnableKeyword for device tiers) and the pass drops to its simplest form -
// ONE unlit texture fetch (the Simple Field Texture, rgb*a, tinted by the
// colour and intensity, exactly the nav-light flare's sample discipline),
// plus the near fade. No depth read, no branch, no procedurals. Both
// variants always compile and ship (multi_compile, not a build-strippable
// feature), so a script can pick the tier per device with confidence.
//
// The complex tier's furniture needs no UVs, no depth read and no textures:
// a view-angle rim so the silhouette glows like a held-in boundary, a
// frac-bounded travelling shimmer (NavLights doctrine: hours-long sessions
// cannot drift the phase) and a slow sweep band. The fallback tier is the
// only consumer of UVs - an unwritten UV slot costs it alone.
//
// NEAR FADE. The whole output (seam included) scales by
// smoothstep(0, _NearFade, eyeDepth): as the camera comes to clip through
// the plane the field dissolves instead of filling the view with glow, and
// there is no rim flash at the moment of the cut. _NearFade = 0 disables
// (the clamped 1e-4 edge keeps smoothstep well-defined).
//
// MOBILE BUDGET: one pass, `multi_compile_local` across exactly two
// variants, no keywords beyond the toggle (no stack pollution - local
// keyword space), one texture sample in the fallback tier only, two float
// depth scalars in the complex tier. The URP asset already pays for
// `_CameraDepthTexture` (m_RequireDepthTexture: 1); the renderer copies the
// depth After Opaques so the transparent band reads this-frame depth. The
// fallback tier is the cheapest shape in the family: fetch, tint, fade.
//
// KNOBS - colour, brightness, contact response, the animation's four
// speeds/strengths, the near fade and the fallback's texture. The rim
// power/intensity and the shimmer's geometric frequencies are constants in
// the program half; ask and any of them becomes a property.

Shader "OpenFrontier/Airlock"
{
	Properties
	{
		[Header(Energy)]
		[HDR]_Color("Glow Color", Color) = (0.25, 0.7, 1.0, 1.0)
		_Intensity("Glow Intensity", Float) = 1.5

		[Header(Contact)]
		_ContactBoost("Contact Boost", Float) = 3.0
		_ContactThickness("Contact Thickness", Float) = 0.35

		[Header(Animation)]
		_ShimmerDepth("Shimmer Depth", Range(0.0, 1.0)) = 0.18
		_ShimmerSpeed("Shimmer Speed", Float) = 0.55
		_SweepStrength("Sweep Strength", Float) = 0.35
		_SweepSpeed("Sweep Speed", Float) = 0.21

		[Header(Fading)]
		_NearFade("Near Fade", Float) = 0.6

		[Header(Fallback)]
		[Toggle(_AIRLOCK_FALLBACK)]_AirlockFallback("Airlock Fallback", Float) = 0
		[NoScaleOffset][MainTexture]_SimpleTex("Simple Field Texture", 2D) = "white" {}
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
			// Both variants always ship; scripts may select per device tier.
			// Local keyword space - no global stack pollution.
			#pragma multi_compile_local _ _AIRLOCK_FALLBACK

			#include "OpenFrontierAirlock.hlsl"
			ENDHLSL
		}
	}
	Fallback Off
}
