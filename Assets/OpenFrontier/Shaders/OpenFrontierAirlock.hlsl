// Open Frontier - airlock forcefield program, OpenFrontier_Airlock.shader's
// single pass. See the shader's header for the effect contract: the complex
// tier (contact seam, rim, shimmer, sweep, near fade) and the fallback tier
// (one texture fetch, tint, near fade) selected by the _AIRLOCK_FALLBACK
// keyword, plus the depth-doctrine guards on the contact read.

#ifndef OPENFRONTIER_AIRLOCK_SHARED
#define OPENFRONTIER_AIRLOCK_SHARED

// The program is self-sufficient: URP core macros, the space transforms,
// _Time and (_CameraDepthTexture + SampleSceneDepth) arrive HERE. Core.hlsl
// and DeclareDepthTexture.hlsl both carry include guards; order is safe.
// (Core.hlsl shipped once un-included in the NavLights program and the
// shader went pink on "unrecognized identifier 'TEXTURE2D'" - keep it.)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// The fallback tier's single texture. [NoScaleOffset] in the shader keeps
// the _ST constant out of the block entirely, so the UV rides raw.
TEXTURE2D(_SimpleTex);
SAMPLER(sampler_SimpleTex);

// All material state, in one batcher-required block. The SRP batcher needs
// every material property inside UnityPerMaterial - there are no others
// (the fallback toggle float is a real Material property; it lives here too,
// though the branch runs on the compiled keyword, not its value).
CBUFFER_START(UnityPerMaterial)
	half4 _Color;
	half _Intensity;
	half _ContactBoost;
	float _ContactThickness;
	half _ShimmerDepth;
	half _ShimmerScale;
	half _ShimmerSpeed;
	float4 _FieldSize;
	half _SealGlow;
	half _NearFade;
	half _AirlockFallback;
CBUFFER_END

struct AirlockAttributes
{
	float3 positionOS : POSITION;
	float3 normalOS   : NORMAL;
	float2 uv         : TEXCOORD0;
};

struct AirlockVaryings
{
	// The fallback tier is the only texture consumer: no variant pays
	// interpolation it never reads.
#if defined(_AIRLOCK_FALLBACK)
	float2 uv      : TEXCOORD0;
#endif
	float4 positionCS : SV_POSITION;
	// xy = object-space metres (the shimmer's pattern space), z = eye depth
	// in metres (positive, -view-space-z). float, not half: depth ramps and
	// the near fade band at 12-bit-mantissa magnitude across a big ship.
	float4 osEye      : TEXCOORD1;
	half3 normalWS    : TEXCOORD2;
	float3 positionWS : TEXCOORD3;
#if !defined(_AIRLOCK_FALLBACK)
	// Perspective-correct screen position (w preserved) for the depth read;
	// the fallback never samples depth, so the slot stops existing there.
	float4 ndc        : TEXCOORD4;
#endif
};

AirlockVaryings AirlockVert(AirlockAttributes input)
{
	AirlockVaryings output;

	VertexPositionInputs vpi = GetVertexPositionInputs(input.positionOS.xyz);

	output.positionCS = vpi.positionCS;
	output.osEye = float4(input.positionOS.xy, -vpi.positionVS.z, 0.0);
#if defined(_AIRLOCK_FALLBACK)
	output.uv = input.uv;
#endif
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	output.positionWS = vpi.positionWS;
#if !defined(_AIRLOCK_FALLBACK)
	output.ndc = vpi.positionNDC;
#endif
	return output;
}

#if !defined(_AIRLOCK_FALLBACK)

// Contact: how deep the nearest recorded surface sits BEHIND this fragment,
// ramped to zero across `_ContactThickness` metres. The sign guard is the
// house doctrine in one multiply: only a POSITIVE, small diff (surface just
// behind the field) lights the seam. The dead open-space read parks the diff
// NEGATIVE on this OpenGL stack, and a live-but-far scene depth parks it
// LARGE POSITIVE - both saturate cleanly to "no contact".
half AirlockContact(float4 ndc, float eyeDepth, half contactBoost, float contactThickness)
{
	if (contactBoost <= 0.0)
	{
		// Uniform branch: a material with boost 0 never touches the depth
		// texture at all - the cheapest complex-tier draw offered.
		return 0.0;
	}

	float sceneEye = LinearEyeDepth(SampleSceneDepth(ndc.xy / ndc.w), _ZBufferParams);
	float diff = sceneEye - eyeDepth;
	return saturate(1.0 - diff / max(contactThickness, 1e-3)) * step(1e-4, diff);
}

// The boil's hash and lattice. All three axes (x, y, time) wrap at a strict
// 8-cell period with a floor-based modulus (sign-safe), corner indices join
// seamlessly across the boundary, and the hash arguments can never exceed
// single digits at any session length - no sin() precision cliff at 2 a.m.
// on day three of a session (NavLights phase doctrine).
half AirlockHash(float3 cell)
{
	cell -= 8.0 * floor(cell * 0.125);
	return frac(sin(dot(cell, float3(12.9898, 78.233, 37.719))) * 43758.5453);
}

// 3D value noise: (x, y) from the field geometry, z is TIME, sliced
// through. Slicing - not translating - the third axis is what makes the
// pattern evolve with no marching direction: features press out, dissolve
// and reform from every side.
half AirlockBoil(float3 p)
{
	p -= 8.0 * floor(p * 0.125);
	float3 i = floor(p);
	float3 f = p - i;
	float3 u = f * f * (3.0 - 2.0 * f);
	half c000 = AirlockHash(i);
	half c100 = AirlockHash(i + float3(1.0, 0.0, 0.0));
	half c010 = AirlockHash(i + float3(0.0, 1.0, 0.0));
	half c110 = AirlockHash(i + float3(1.0, 1.0, 0.0));
	half c001 = AirlockHash(i + float3(0.0, 0.0, 1.0));
	half c101 = AirlockHash(i + float3(1.0, 0.0, 1.0));
	half c011 = AirlockHash(i + float3(0.0, 1.0, 1.0));
	half c111 = AirlockHash(i + float3(1.0, 1.0, 1.0));
	half x00 = lerp(c000, c100, u.x);
	half x10 = lerp(c010, c110, u.x);
	half x01 = lerp(c001, c101, u.x);
	half x11 = lerp(c011, c111, u.x);
	return lerp(lerp(x00, x10, u.y), lerp(x01, x11, u.y), u.z);
}

#endif // !_AIRLOCK_FALLBACK


half4 AirlockFrag(AirlockVaryings input) : SV_Target
{
	half3 rgb;

#if defined(_AIRLOCK_FALLBACK)
	// FALLBACK TIER: one fetch, rgb*a - black-background and
	// transparent-background art both behave - tinted, faded. No depth
	// slot, no branch, no procedurals. The look lives in the art.
	half4 tex = SAMPLE_TEXTURE2D(_SimpleTex, sampler_SimpleTex, input.uv);
	rgb = _Color.rgb * (_Intensity * tex.rgb * tex.a);
#else
	// COMPLEX TIER ------------------------------------------------------
	half3 glow = _Color.rgb * _Intensity;

	// Boil - the field's metabolism. A three-dimensional value noise whose
	// third axis is TIME, sliced through: the pattern morphs in place with
	// no marching direction anywhere. abs() mirrors x and y per object
	// axis, so a symmetric door churns symmetric: no centre seam. The
	// lattice period-wraps at 8 cells on every axis, so the loop is true
	// and the hash arguments stay bounded at any session length.
	float3 boil = float3(abs(input.osEye.xy) * _ShimmerScale, _Time.y * _ShimmerSpeed);
	half churn = 0.65 * AirlockBoil(boil) + 0.35 * AirlockBoil(boil * 2.6 + 17.31);

	// The seal - where the field meets the hull it works hardest. The L-in
	// finity distance (field-local metres over the mesh's half extents)
	// reads border proximity without any baked data: the churn dose deepens
	// toward the frame and a bright ramp presses outward from it, while the
	// middle keeps only a gentle dose - the passage reads calm, passable.
	// _FieldSize is authored per door; the 0.1 clamps keep an unset
	// property from dividing anything away.
	float2 over = abs(input.osEye.xy) / max(_FieldSize.xy, float2(0.1, 0.1));
	half border = saturate(max(over.x, over.y));
	half shimmer = 1.0 - _ShimmerDepth * churn * (0.4 + 0.6 * border);
	half seal = pow(border, 2.0);

	// - Rim: grazing view angles brighten, on BOTH faces (abs() folds the
	// - backface normal), reading as the boundary of a held-in field.
	half3 viewDir = _WorldSpaceCameraPos.xyz - input.positionWS;
	half nv = abs(dot(normalize(input.normalWS), viewDir / max(length(viewDir), 1e-4)));
	half rim = pow(saturate(1.0 - nv), 3.0);

	rgb = glow * shimmer;
	rgb += glow * (rim * 0.5);
	rgb += glow * (seal * _SealGlow);
	rgb += glow * (AirlockContact(input.ndc, input.osEye.z, _ContactBoost, _ContactThickness) * _ContactBoost);
#endif

	// NEAR FADE - shared by both tiers: the field dissolves as the camera
	// comes to clip through the plane (eyeDepth ~0 at the cut), so flying
	// the opening never fills the view with glow and no rim flashes at the
	// moment of clipping. _NearFade = 0 disables (the 1e-4 clamp keeps
	// smoothstep well-defined); metres to full visibility.
	rgb *= smoothstep(0.0, max(_NearFade, 1e-4), input.osEye.z);

	return half4(rgb, 1.0);
}

#endif // OPENFRONTIER_AIRLOCK_SHARED
