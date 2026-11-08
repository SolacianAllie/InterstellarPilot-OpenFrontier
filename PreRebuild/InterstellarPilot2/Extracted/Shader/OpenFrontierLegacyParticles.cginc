// Open Frontier: shared core for the legacy BiRP particle shader family.
// Mirrors the classic "Particles/*" builtins: vertex color x _TintColor
// x _MainTex, _InvFade soft particles, Cull Off, ZWrite Off, transparent
// queue. No fog (project standard) and no LightMode tag: renders as
// SRPDefaultUnlit under URP.
#ifndef OPENFRONTIER_LEGACY_PARTICLES_CGINC
#define OPENFRONTIER_LEGACY_PARTICLES_CGINC

#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_ST;
fixed4 _TintColor;
float _InvFade;
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

struct appdata_legacy_particle
{
	float4 vertex : POSITION;
	fixed4 color : COLOR;
	float2 uv : TEXCOORD0;
};

struct v2f_legacy_particle
{
	float4 pos : SV_POSITION;
	fixed4 color : COLOR;
	float2 uv : TEXCOORD0;
	float4 screenPos : TEXCOORD1;
	float eyeDepth : TEXCOORD2;
};

v2f_legacy_particle vert_legacy_particle(appdata_legacy_particle input)
{
	v2f_legacy_particle output;
	output.pos = UnityObjectToClipPos(input.vertex);
	output.uv = TRANSFORM_TEX(input.uv, _MainTex);
	output.color = input.color * _TintColor;
	output.screenPos = ComputeScreenPos(output.pos);
	output.eyeDepth = -UnityObjectToViewPos(input.vertex.xyz).z;
	return output;
}

// The legacy soft-particle fade is DISABLED on this project: on the
// OpenGL stack the depth texture reads 0 behind empty space (see
// AGENTS.md - the same reason soft particles are standardized
// Near=1/Far=0), which made sceneZ - partZ negative and killed alpha
// entirely - additive particles (lasers!) against open space rendered
// invisible. The _InvFade property stays for material compatibility.
fixed4 apply_soft_fade(v2f_legacy_particle input, fixed4 col)
{
	return col;
}

#endif
