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
	half _ShimmerSpeed;
	half _SweepStrength;
	half _SweepSpeed;
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

	// Shimmer: one travelling wave, frac-bounded so an hours-long session
	// cannot drift the phase (NavLights phase doctrine). The wave runs
	// diagonally across the field in OBJECT space metres, so it belongs to
	// the door, not to the camera.
	float diag = dot(input.osEye.xy, float2(0.55, 0.85));
	float phase = frac(diag * 0.35 + _Time.y * _ShimmerSpeed) * 6.2831853;
	half shimmer = 1.0 - _ShimmerDepth * (0.5 + 0.5 * sin(phase));

	// Sweep: a soft band wanders across the field, strength and rate from
	// the material.
	float bandPhase = frac(input.osEye.y * 0.23 - _Time.y * _SweepSpeed);
	half band = smoothstep(0.0, 0.12, bandPhase) * (1.0 - smoothstep(0.22, 0.34, bandPhase));

	// Rim: grazing view angles brighten, on BOTH faces (abs() folds the
	// backface normal), reading as the boundary of a held-in field.
	half3 viewDir = _WorldSpaceCameraPos.xyz - input.positionWS;
	half nv = abs(dot(normalize(input.normalWS), viewDir / max(length(viewDir), 1e-4)));
	half rim = pow(saturate(1.0 - nv), 3.0);

	rgb = glow * shimmer;
	rgb += glow * (band * _SweepStrength);
	rgb += glow * (rim * 0.5);
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
