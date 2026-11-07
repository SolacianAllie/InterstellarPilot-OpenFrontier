// Open Frontier - nav light program, shared by BOTH passes of
// OpenFrontier_NavLights.shader.
//
// NAV_HALO must be #defined before the include in each pass:
//   0 = the depth-tested core (full brightness, hull occlusion honest)
//   1 = the always-drawn spill (reduced brightness, slightly larger)
// Exactly one shape story: the two passes cannot drift.
//
// Everything stateful about the owning unit lives in NavLight.cs /
// NavLightTicker.cs; everything here is per-frame GPU maths only.

#ifndef OPENFRONTIER_NAV_LIGHTS_SHARED
#define OPENFRONTIER_NAV_LIGHTS_SHARED

// The whole program is self-sufficient: the passes only include THIS file,
// so the URP core macros (TEXTURE2D, the space transforms, the instancing
// buffer, _Time) arrive HERE. Core.hlsl has include guards; the passes do
// not re-include it. (This file shipped once without it and the shader went
// pink: "unrecognized identifier 'TEXTURE2D'" at the first line that used a
// macro - keep the include.)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_FlareTex);
SAMPLER(sampler_FlareTex);

// Material-wide (every chip shares the world rules), not per instance.
float _GrowReferenceDistance;
float _MaxGrowWorldSize;
float _FarFadeStart;
float _FarFadeEnd;

// The spill pass knobs. _HaloIntensity 0 silences the spill visually (the
// pass still costs its fill; it is decoration budget, not simulation).
float _HaloIntensity;
float _HaloSize;

// Per instance. Written by NavLight.ApplyInstanceData once at spawn (and on
// inspector tweaks), never per frame.
// _Color: rgb = light colour (HDR allowed), a = intensity.
// _Flash: x = period (s), y = pattern (0 solid, 1 single, 2 double),
//         z = phase in [0,1).
// _Size:  base chip size in world units.
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
	// aside to X there; the pop only shows on an asymmetric flare
	// texture dead centre of a perfect pole view - wing lamps
	// never ride that line.
	float3 sight = camDir / max(dist, 1e-4);
	float3 refAxis = abs(sight.y) > 0.999 ? float3(1, 0, 0) : float3(0, 1, 0);
	float3 right = normalize(cross(refAxis, sight));
	float3 up = cross(sight, right);

	float4 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
	float4 flash = UNITY_ACCESS_INSTANCED_PROP(Props, _Flash);
	float baseSize = UNITY_ACCESS_INSTANCED_PROP(Props, _Size);

	// Grow: below the reference distance the chip is its real
	// size; beyond, it grows linearly and stops at the cap.
	float grow = min(dist / max(_GrowReferenceDistance, 1.0), _MaxGrowWorldSize);
	float coreSize = max(baseSize, grow);

	// Z RELIEF. The anchor hangs ON the hull, and the billboard lies
	// in a plane of CONSTANT depth - so it z-fights the hull pixel
	// for pixel and ZTest LEqual bites jagged wedges out of any lamp
	// mounted beside a hull step or seen at an oblique angle (nav
	// lamps mount at wingtips, strips and cowling edges). Displacing
	// ALONG the view ray keeps every corner on the ray it was on -
	// the projection therefore does not move; only the depth does,
	// which is exactly what the LEqual test needed. The bound is a
	// few percent of apparent size at the clamp, invisible on a
	// glow. Steps the push cannot clear belong to the SPILL pass,
	// not a bigger push: a light tucked beside a cowling ring or in
	// a scoop mouth loses to local hull curvature no matter how far
	// it shoves, and shoving farther would start poking through the
	// NEIGHBOURING hull lines it is meant to lose to.
	float zPush = min(coreSize * 0.75, dist * 0.05);

	float size = coreSize;
	half brightness = NavLightFlash(frac(_Time.y / max(flash.x, 1e-3) + frac(flash.z)),
		flash.y);
	float farFade = 1.0 - smoothstep(_FarFadeStart, _FarFadeEnd, dist);
	half intensityMul = 1.0;

#if NAV_HALO
	// The spill pass: slightly larger, much quieter, never depth-cut
	// (ZTest Always in the pass state). It is what turns the hull's
	// bite from a hole into a leak of light over the edge. The grown
	// size is clamped to the same cap as the core so a far chip's
	// spill never doubles its fill - the spill belongs to lamps at
	// readable range, which is also where bites happen.
	size = min(coreSize * max(_HaloSize, 1.0), _MaxGrowWorldSize);
	intensityMul = saturate(_HaloIntensity);
#endif

	float3 posWS = anchorWS
		+ sight * zPush
		+ right * (input.positionOS.x * size)
		+ up * (input.positionOS.y * size);

	NavLightVaryings output;
	output.positionCS = TransformWorldToHClip(posWS);
	output.uv = input.uv;
	output.color = half4(
		tint.rgb * tint.a * brightness * farFade * intensityMul, 1.0);
	return output;
}

half4 NavLightFrag(NavLightVaryings input) : SV_Target
{
	half4 flare = SAMPLE_TEXTURE2D(_FlareTex, sampler_FlareTex, input.uv);

	// RGB carries the art's colour, alpha the mask; both multiply
	// so a plain white texture or a black-background sprite both
	// behave.
	half3 glow = flare.rgb * flare.a;

#if defined(_ROUND_ON)
	half2 centered = abs(input.uv - 0.5) * 2.0;
	half radial = 1.0 - saturate(dot(centered, centered));
	glow *= smoothstep(0.0, 0.35, radial);
#endif

	return half4(glow * input.color.rgb, 1.0);
}

#endif // OPENFRONTIER_NAV_LIGHTS_SHARED
