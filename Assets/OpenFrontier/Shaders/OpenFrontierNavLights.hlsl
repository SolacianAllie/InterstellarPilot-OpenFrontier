// Open Frontier - nav light program, OpenFrontier_NavLights.shader's
// single pass.
//
// ARCHITECTURE (the 2026-11-07 redo of the two-pass leak system):
// ONE depth-honest additive pass. The old design drew a depth-tested core
// and then a ZTest GREATER spill that repainted the occluded flare pieces
// over the hull - ray shape included, at a bigger size than the rays it
// was healing - and every tuning round of it ended with the user seeing
// lights installed inside the ship. The redo moves the soft spread to
// where it belongs:
//   - the single pass never paints over anything nearer than the chip's
//     plane (the honest LEqual bite); nothing flare-shaped ever prints
//     across an occluder;
//   - the glare that continues past a bite is POST-PROCESS BLOOM on the
//     quality tiers that run it - a soft blob against the hull edge,
//     which is what the spill pass tried to hand-draw and could not;
//   - the cross texture's RAYS belong at close range only: past the ray
//     fade the fragment blends into the SAME texture's central core,
//     magnified - a soft dot with no ray geometry to outgrow the hull.
//     Every distance-breakage report in the old system traced back to ray
//     geometry, never to the billboard's own anchor maths.
//
// Everything stateful about the owning unit lives in NavLight.cs /
// NavLightTicker.cs; everything here is per-frame GPU maths only.

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

// Material-wide (every chip shares the world rules), not per instance.
float _GrowReferenceDistance;
float _MaxGrowWorldSize;
// The pivot multiplier (see the shader property of the same name): scales
// the whole z relief. 0 puts the billboard plane exactly at the anchor -
// a genuine Unity-Particle pivot-off position; 1 is the tuned default.
// It moves DEPTH only: the flare's screen centre stays pinned to the
// chip's origin on screen whatever the value.
float _PushScale;
float _FarFadeStart;
float _FarFadeEnd;

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

// The ray fade: past this range the cross's rays collapse into the core.
// Bands chosen against the geometry of the old failures: the grow term
// starts dominating at _GrowReferenceDistance (250), and ray geometry
// over hull was what every far-range report described. Dots take over
// well inside that band, so the grown star reads as a soft pinprick
// wherever the hull beside it is already small on screen. Compile-time
// constants, not knobs: the two leak passes before this lost the battle
// with their own tuning surface.
static const float RAY_FADE_START = 150.0;
static const float RAY_FADE_END = 400.0;

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
	half rayFade : TEXCOORD2;
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
	// glow. Where the push loses (deep hull steps, far-range depth
	// quantisation) the bite shows - and the bite is now the HONEST
	// outcome: a cut flare, which bloom on the quality tiers softens.
	float zPush = _PushScale * min(coreSize * 0.75, dist * 0.05);

	float size = coreSize;
	half brightness = NavLightFlash(frac(_Time.y / max(flash.x, 1e-3) + frac(flash.z)),
		flash.y);
	float farFade = 1.0 - smoothstep(_FarFadeStart, _FarFadeEnd, dist);

	half rayFade = smoothstep(RAY_FADE_START, RAY_FADE_END, dist);

	float3 posWS = anchorWS
		+ sight * zPush
		+ right * (input.positionOS.x * size)
		+ up * (input.positionOS.y * size);

	NavLightVaryings output;
	output.positionCS = TransformWorldToHClip(posWS);
	output.uv = input.uv;
	output.color = half4(
		tint.rgb * tint.a * brightness * farFade, 1.0);
	output.rayFade = rayFade;
	return output;
}

half4 NavLightFrag(NavLightVaryings input) : SV_Target
{
	half4 flare = SAMPLE_TEXTURE2D(_FlareTex, sampler_FlareTex, input.uv);

	// The dot: the same texture's core, magnified (the middle 35%
	// remapped to full UV). The cross's rays cannot appear in it, so
	// a chip at range glows like a star, not like a four-armed
	// signpost out-scaling the hull beside it. The lerp also bounds
	// what can ever cross over nearer hull: ray geometry dies with
	// the fade, the core blob is what bloom spreads.
	half4 core = SAMPLE_TEXTURE2D(_FlareTex, sampler_FlareTex,
		input.uv * 0.35 + 0.325);
	flare = lerp(flare, core, input.rayFade);

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
