// Open Frontier: ED's StarMapDensityMap, rebuilt.
//
// The smooth "milky" haze that fills the disc between the billboards in the
// MAP view (findings §3.3). ED samples a dedicated managed texture
// (StarMapDensityMap, rebuilt from GalaxyOctree/MassiveStarDatabase) whose
// two immediates are {50.0, 1.2}; the rebuild binds the blueprint's density
// map here, while the band/dust/glow sample the textured background. That is
// ED's split: smooth haze underneath, structured light and dust on top. The
// old version reused the band texture for both, which made the density a
// second copy of the band instead of a haze.
//
// MAP LAYER: ED's density map is a map-pipeline layer (findings §3.3; the
// sky pipeline in §4.3 does not list it), and in the sky the band +
// billboards carry the disc instead. The geometry is a disc quad in the
// galactic plane, so in the ortho map the UV is exact - no projection
// needed. GalaxyBackdrop puts this child on layer 30 (the map presentation
// layer shared with GalaxyMapStars), so the in-sky camera (layer 31) never
// sees the disc at all; an angled perspective map that includes layer 30
// still does.
//
// Additive, no depth write, drawn under the dust and the billboard cloud.
// Queue offset (2972) lives on the MATERIAL, not the shader; the tag stays a
// defined name (Unity 6 rejects bare integers in Queue tags).
Shader "OpenFrontier/Galaxy/Density" {
	Properties {
		_DensityTex ("Density Map", 2D) = "black" {}
		[HDR] _DensityTint ("Density Tint", Color) = (1,1,1,1)
		_DensityGain ("Density Gain", Float) = 1.2
		_DensityGamma ("Density Gamma", Float) = 1
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		Blend One One
		Cull Off
		ZWrite Off
		ZTest LEqual

		Pass {
			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_DensityTex);
			SAMPLER(sampler_DensityTex);

			float4 _DensityTint;
			float _DensityGain;
			float _DensityGamma;

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Varyings vert(Attributes v)
			{
				Varyings o;
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				o.uv = v.uv;
				return o;
			}

			float4 frag(Varyings i) : SV_Target
			{
				float4 bg = SAMPLE_TEXTURE2D(_DensityTex, sampler_DensityTex, i.uv);
				// The density map is the smooth Milky-Way mass, not the
				// textured band. Its arms sit well below white; a low
				// coverage window keeps the outer haze alive instead of
				// requiring near-core values to light up.
				float lum = dot(bg.rgb, float3(0.2125, 0.7154, 0.0721));
				float coverage = smoothstep(0.005, 0.38, lum * _DensityGain);
				float d = pow(coverage, _DensityGamma);
				float r01 = length(i.uv * 2.0 - 1.0);
				float rim = 1.0 - smoothstep(0.94, 1.0, r01);
				float3 col = bg.rgb * _DensityTint.rgb * d * rim;
				return float4(col, 1.0);
			}
			ENDHLSL
		}
	}
}
