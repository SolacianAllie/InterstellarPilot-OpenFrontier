Shader "SpaceUnity/PlanetStandard" {
	Properties {
		_MainTex ("_MainTex", 2D) = "black" {}
		_Normals ("_Normals", 2D) = "black" {}
		_Lights ("_Lights", 2D) = "black" {}
		_SpecMap ("_SpecMap", 2D) = "black" {}
		_LightScale ("_LightScale", Float) = 1
		_Emission ("_Emission", 2D) = "black" {}
		_EmissionScale ("_EmissionScale", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
		LOD 200

		Pass {
			Name "ForwardLit"
			Tags { "LightMode"="UniversalForward" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _SHADOWS_SOFT

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
			TEXTURE2D(_Normals);    SAMPLER(sampler_Normals);
			TEXTURE2D(_Lights);     SAMPLER(sampler_Lights);
			TEXTURE2D(_SpecMap);    SAMPLER(sampler_SpecMap);
			TEXTURE2D(_Emission);   SAMPLER(sampler_Emission);

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float _LightScale;
				float _EmissionScale;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float2 uv : TEXCOORD0;
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
				float4 tangentWS : TEXCOORD3;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
				output.positionCS = posInputs.positionCS;
				output.positionWS = posInputs.positionWS;
				output.normalWS = normInputs.normalWS;
				output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				half4 albedoTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
				half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_Normals, sampler_Normals, input.uv));
				half3 lightsTex = SAMPLE_TEXTURE2D(_Lights, sampler_Lights, input.uv).rgb;
				half3 emissionTex = SAMPLE_TEXTURE2D(_Emission, sampler_Emission, input.uv).rgb;

				float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
				float3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
				float3 normalWS = normalize(TransformTangentToWorld(normalTS,
					half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS)));

				InputData inputData = (InputData)0;
				inputData.positionWS = input.positionWS;
				inputData.normalWS = normalWS;
				inputData.viewDirectionWS = viewDirWS;
				inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

				Light mainLight = GetMainLight(inputData.shadowCoord);
				float ndl = dot(normalWS, mainLight.direction);

				// Two independent emitters, and they are deliberately NOT the same:
//
//   _Lights   - city lights. Only visible on the night side, so they
//               read as settlements lit by their own power rather than
//               as part of the lit surface.
//   _Emission - anything glowing on its own terms (lava, magma, glowing
//               crystal, running lights). Emits all the way round,
//               because it does not come from the sun. Keep this one
//               masked by hand if a feature should only show on the day
//               side - baking a small negative into the albedo is not a
//               substitute, and doing the mask here keeps it free.
//
// Both are plain maps rather than HDR keywords: at planet scale the
// glow has to survive being minified into a few pixels, and a keyword
// swap cannot be authored per feature.
float nightMask = saturate(-ndl * 2.0);
half3 emission = lightsTex * _LightScale * nightMask
			   + emissionTex * _EmissionScale;

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = albedoTex.rgb;
				surfaceData.metallic = 0.0;

				// spec map: green channel = land specularity, red channel =
				// ocean specularity. Both surfaces are reflective, so land
				// drives specular strength and ocean drives smoothness.
				//
				// A constant fallback is kept so a black / missing spec map
				// still renders rather than going completely matte.
				half4 specTex = SAMPLE_TEXTURE2D(_SpecMap, sampler_SpecMap, input.uv);
				surfaceData.specular = specTex.g * 0.6f + 0.1f;
				surfaceData.smoothness = specTex.r * 0.6f + 0.15f;
				surfaceData.normalTS = normalTS;
				surfaceData.emission = emission;
				surfaceData.occlusion = 1.0;
				surfaceData.alpha = 1.0;
				surfaceData.clearCoatMask = 0.0;
				surfaceData.clearCoatSmoothness = 0.0;

				half4 color = UniversalFragmentPBR(inputData, surfaceData);
				color.rgb = MixFog(color.rgb, inputData.fogCoord);
				return color;
			}
			ENDHLSL
		}
	}
	FallBack "Universal Render Pipeline/Lit"
}
