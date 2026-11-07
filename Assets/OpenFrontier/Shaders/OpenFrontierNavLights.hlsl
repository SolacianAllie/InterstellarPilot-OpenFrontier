// Open Frontier - nav light program, OpenFrontier_NavLights.shader's
// single pass.
//
// MINIMAL BY INSTRUCTION (2026-11-07): the user stripped the system to
// its five knobs - size, brightness, flashing, colour, texture. Everything
// the previous revisions knew how to decide on the user's behalf is gone:
//
//   - NO distance grow. A chip is its authored Size at any range; it
//     becomes small on screen with the ship around it, the way a real
//     lamp does. (Bloom, on the tiers that run it, keeps a big enough
//     chip's inner pixel visible at extreme range.)
//   - NO knob-carried z push. How far the flare hangs off the hull is
//     DERIVED, not authored: half the flare's own Size along the chip's
//     +Z mount axis (see job 4), so a bigger lamp rests further off its
//     mount and neither number is a dial. Nothing flares past an
//     occluder by design, and nothing decides on the author's behalf.
//   - NO ray fade, NO far fade, NO halo pass, NO round shaping. The
//     texture is the texture, end to end, always.
//
// The shader therefore does exactly four things: billboard the quad at
// the chip's rest position, scale it by the per-instance size, flash it
// by the per-instance pattern, and multiply the texture by the
// per-instance colour. Everything stateful about the owning unit lives
// in NavLight.cs and NavLightTicker.cs.

#ifndef OPENFRONTIER_NAV_LIGHTS_SHARED
#define OPENFRONTIER_NAV_LIGHTS_SHARED

// The program is self-sufficient: URP core macros (TEXTURE2D, the space
// transforms, the instancing buffer, _Time) arrive HERE. Core.hlsl has
// include guards; the pass does not re-include it. (This file shipped
// once without it and the shader went pink: "unrecognized identifier
// 'TEXTURE2D'" - keep the include.)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_FlareTex);
SAMPLER(sampler_FlareTex);

// Per instance. Written by NavLight.ApplyInstanceData once at spawn (and on
// inspector tweaks), never per frame.
// _Color: rgb = light colour (HDR allowed), a = intensity (brightness).
// _Flash: x = period (s), y = pattern (0 solid, 1 single, 2 double),
//         z = phase in [0,1).
// _Size:  chip size in world units.
UNITY_INSTANCING_BUFFER_START(Props)
	UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
	UNITY_DEFINE_INSTANCED_PROP(float4, _Flash)
	UNITY_DEFINE_INSTANCED_PROP(float, _Size)
UNITY_INSTANCING_BUFFER_END(Props)

struct NavLightAttributes
{
	float4 positionOS : POSITION;
	float2 uv : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct NavLightVaryings
{
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
	half4 color : TEXCOORD1;
};

// Sharp aviation-style spike: hard on, hard off, most of the cycle dark.
half NavLightFlashSpike(half t)
{
	half rise = smoothstep(0.0, 0.02, t);
	half fall = 1.0 - smoothstep(0.08, 0.10, t);
	return saturate(rise * fall);
}

half NavLightFlash(half t, half pattern)
{
	// 0 solid, 1 single pulse at cycle start,
	// 2 a tight pair: the second pulse a fifth of a cycle behind
	// the first, then DARK - two flashes, then a pause until the
	// cycle restarts.
	if (pattern < 0.5)
	{
		return 1.0;
	}
	half first = NavLightFlashSpike(t);
	if (pattern < 1.5)
	{
		return first;
	}
	// The pair: first spans [0, 0.10], second [0.20, 0.30]. The
	// blinks sit a tenth of a cycle apart (~120 ms at the default
	// 1.2 s period, so they read as two, not one flicker) and the
	// remaining ~70% of the cycle is the pause. Phase-encoded, so
	// the shape holds at any FlashPeriod.
	return max(first, NavLightFlashSpike(frac(t + 0.80)));
}

NavLightVaryings NavLightVert(NavLightAttributes input)
{
	UNITY_SETUP_INSTANCE_ID(input);

	// anchor = the transform origin; the quad's own geometry
	// contributes only its corner sign (.xy in [-0.5, 0.5]).
	float3 anchorWS = TransformObjectToWorld(float3(0, 0, 0));

	// The chip's +Z is the MOUNT AXIS (job 4 below): the author lays
	// the chip flat on the hull with +Z pointing out of the surface,
	// and this is the axis the flare hangs its rest along. Normalized
	// - a scaled chip must not scale the lift.
	float3 mountWS = TransformObjectToWorldDir(float3(0, 0, 1));

	float3 camDir = anchorWS - _WorldSpaceCameraPos.xyz;
	float dist = length(camDir);

	// Billboard basis out of the SIGHT LINE, pure: the plane hangs
	// perpendicular to anchor-to-camera (a spherical billboard).
	// The screen-parallel version built from UNITY_MATRIX_I_V
	// rows kept looking wrong at angles - a flare that belongs to
	// the SCREEN rotates with camera roll and reads as not
	// belonging to the lamp's hull at oblique view. A chip whose
	// sight line is straight up/down the world Y would make
	// cross(worldUp, sight) degenerate, so the reference axis steps
	// aside to X there.
	float3 sight = camDir / max(dist, 1e-4);
	float3 refAxis = abs(sight.y) > 0.999 ? float3(1, 0, 0) : float3(0, 1, 0);
	float3 right = normalize(cross(refAxis, sight));
	float3 up = cross(sight, right);

	float4 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
	float4 flash = UNITY_ACCESS_INSTANCED_PROP(Props, _Flash);
	float size = UNITY_ACCESS_INSTANCED_PROP(Props, _Size);

	half brightness = NavLightFlash(frac(_Time.y / max(flash.x, 1e-3) + frac(flash.z)),
		flash.y);

	// The plane hangs at the REST (2026-11-07, the user's "fade out
	// into the surface" request): half its own Size off the mount
	// along +Z, so the flare's rim touches the hull and the hull's
	// depth slice can only land on the texture's already-faded rim -
	// at every viewing angle the bright core is beyond the
	// surface's reach. The in-plane offsets still carry the corner
	// signs against Size, unchanged.
	float3 posWS = anchorWS
		+ mountWS * (size * 0.5)
		+ right * (input.positionOS.x * size)
		+ up * (input.positionOS.y * size);

	NavLightVaryings output;
	output.positionCS = TransformWorldToHClip(posWS);
	output.uv = input.uv;
	output.color = half4(tint.rgb * tint.a * brightness, 1.0);
	return output;
}

half4 NavLightFrag(NavLightVaryings input) : SV_Target
{
	// The texture is the texture: sampled whole, scaled whole.
	half4 flare = SAMPLE_TEXTURE2D(_FlareTex, sampler_FlareTex, input.uv);

	// RGB carries the art's colour, alpha the mask; both multiply
	// so a plain white texture or a black-background sprite both
	// behave.
	half3 glow = flare.rgb * flare.a;

	return half4(glow * input.color.rgb, 1.0);
}

#endif // OPENFRONTIER_NAV_LIGHTS_SHARED
