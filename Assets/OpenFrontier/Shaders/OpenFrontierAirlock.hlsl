// Open Frontier - airlock forcefield program, OpenFrontier_Airlock.shader's
// single pass. See the shader's header for the effect contract: four knobs
// (colour, intensity, contact boost, contact thickness), everything else is
// a constant decided here, and the depth-based contact glow is double-guarded
// against the house doctrine's dead `_CameraDepthTexture` reads.

#ifndef OPENFRONTIER_AIRLOCK_SHARED
#define OPENFRONTIER_AIRLOCK_SHARED

// The program is self-sufficient: URP core macros, the space transforms,
// _Time and (_CameraDepthTexture + SampleSceneDepth) arrive HERE. Core.hlsl
// and DeclareDepthTexture.hlsl both carry include guards; order is safe.
// (Core.hlsl shipped once un-included in the NavLights program and the
// shader went pink on "unrecognized identifier 'TEXTURE2D'" - keep it.)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// All material state, in one batcher-required block. The SRP batcher needs
// every material property inside UnityPerMaterial - there are no others.
CBUFFER_START(UnityPerMaterial)
	half4 _Color;
	half _Intensity;
	half _ContactBoost;
	float _ContactThickness;
CBUFFER_END

struct AirlockAttributes
{
	float3 positionOS : POSITION;
	float3 normalOS   : NORMAL;
};

struct AirlockVaryings
{
	float4 positionCS : SV_POSITION;
	// xy = object-space metres (the shimmer's pattern space), z = eye depth
	// in metres (positive, -view-space-z). float, not half: depth ramps
	// band at 12-bit-mantissa magnitude across a big ship.
	float4 osEye      : TEXCOORD0;
	half3 normalWS    : TEXCOORD1;
	float3 positionWS : TEXCOORD2;
	// Perspective-correct screen position (w preserved) for the depth read.
	float4 ndc        : TEXCOORD3;
};

AirlockVaryings AirlockVert(AirlockAttributes input)
{
	AirlockVaryings output;

	VertexPositionInputs vpi = GetVertexPositionInputs(input.positionOS.xyz);

	output.positionCS = vpi.positionCS;
	output.osEye = float4(input.positionOS.xy, -vpi.positionVS.z, 0.0);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	output.positionWS = vpi.positionWS;
	output.ndc = vpi.positionNDC;
	return output;
}

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
		// texture at all - the cheapest draw the family offers.
		return 0.0;
	}

	float sceneEye = LinearEyeDepth(SampleSceneDepth(ndc.xy / ndc.w), _ZBufferParams);
	float diff = sceneEye - eyeDepth;
	return saturate(1.0 - diff / max(contactThickness, 1e-3)) * step(1e-4, diff);
}

half4 AirlockFrag(AirlockVaryings input) : SV_Target
{
	half3 glow = _Color.rgb * _Intensity;

	// --- Shimmer: one travelling wave, frac-bounded so an hours-long
	// --- session cannot drift the phase (NavLights phase doctrine).
	// The wave runs diagonally across the field in OBJECT space metres, so
	// it belongs to the door, not to the camera. 0.55 Hz drift, ~2.9 m setup.
	float diag = dot(input.osEye.xy, float2(0.55, 0.85));
	float phase = frac(diag * 0.35 + _Time.y * 0.55) * 6.2831853;
	half shimmer = 1.0 - 0.18 * (0.5 + 0.5 * sin(phase));

	// --- Sweep: a soft band wanders across the field every ~4.8 s at fixed,
	// --- low strength - life for the free-face areas, no knob survived the
	// --- five-knob discussion into this shader uninvited.
	float bandPhase = frac(input.osEye.y * 0.23 - _Time.y * 0.21);
	half band = smoothstep(0.0, 0.12, bandPhase) * (1.0 - smoothstep(0.22, 0.34, bandPhase));

	// --- Rim: grazing view angles brighten, on BOTH faces (abs() folds the
	// --- backface normal), reading as the boundary of a held-in field.
	half3 viewDir = _WorldSpaceCameraPos.xyz - input.positionWS;
	half nv = abs(dot(normalize(input.normalWS), viewDir / max(length(viewDir), 1e-4)));
	half rim = pow(saturate(1.0 - nv), 3.0);

	half3 rgb = glow * shimmer;
	rgb += glow * (band * 0.35);
	rgb += glow * (rim * 0.5);
	rgb += glow * (AirlockContact(input.ndc, input.osEye.z, _ContactBoost, _ContactThickness) * _ContactBoost);

	return half4(rgb, 1.0);
}

#endif // OPENFRONTIER_AIRLOCK_SHARED
