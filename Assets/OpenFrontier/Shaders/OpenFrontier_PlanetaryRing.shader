Shader "OpenFrontier/PlanetaryRing"
{
	// A flat annulus of ring dust with rocks scattered through it, blended on
	// one mesh. Built to the conventions of SpaceUnity/PlanetStandard: URP
	// UniversalForward, tangent-space UnpackNormal with the TBN from the
	// mesh's own tangents, a separate sampler state per texture, and
	// SurfaceData -> UniversalFragmentPBR for lighting.
	//
	// MOBILE FIRST. What is left out is deliberate:
	//
	//   * NO SHADOW RECEIVING. No _MAIN_LIGHT_SHADOWS multi_compile. A ring
	//     is lit by one distant sun and nothing casts a shadow across it that
	//     reads. Receiving them would triple the variant count for an effect
	//     nobody can see.
	//   * NO ADDITIONAL LIGHTS. No _ADDITIONAL_LIGHTS. The ring is a solar
	//     system object seen against space, where the main light is the only
	//     thing lighting it. Adding the multi_compile back is one line if a
	//     scene ever needs it.
	//   * NO SOFT PARTICLES. Per AGENTS.md, soft-particle fades in this
	//     project are Near=1/Far=0 and must NOT be written against
	//     _CameraDepthTexture: sceneZ reads ~0 behind empty space and any
	//     sceneZ - eyeZ fade kills alpha outright. A flat annulus has no
	//     near-camera intersection to fade against anyway.
	//   * ONE SAMPLE PER TEXTURE, reused for every channel that needs it.
	//     The dust albedo supplies both its RGB and its coverage from one
	//     fetch, and the rock texture supplies its RGB, its coverage and its
	//     blend mask from one fetch. Sampling a texture twice "because it is
	//     easier to read" doubles its bandwidth cost, and a ring can cover a
	//     large part of the screen.
	//
	// THE TWO TEXTURE SETS
	//
	//   DUST  - _MainTex albedo (RGB, A = coverage), _Normals, _SpecMap.
	//           _SpecMap follows the planet shader's convention: R is the
	//           detail/gloss mask, G and B unused. Dust is a dielectric, so
	//           its metallic is a constant 0 and the mask only moves
	//           smoothness.
	//
	//   ROCKS - _RockAlbedo (RGB, A = coverage), _RockNormals, _RockMask
	//           in the standard Unity packing: METALLIC in R, SMOOTHNESS in
	//           A. The rocks are the metallic half of the pair, which is
	//           precisely why they need a texture set of their own instead
	//           of a tint of the dust.
	//
	// The blend between the two is _BlendMaskSource.

	Properties
	{
		// ---- dust ----
		[MainTexture] _MainTex ("Dust Albedo (RGB, A = coverage)", 2D) = "black" {}
		[Normal] _Normals ("Dust Normals (tangent space)", 2D) = "bump" {}
		_SpecMap ("Dust Spec Mask (R = detail/gloss)", 2D) = "black" {}
		[HDR] _DustColor ("Dust Tint", Color) = (1, 1, 1, 1)
		_DustSmoothness ("Dust Smoothness", Range(0, 1)) = 0.1
		_DustDetailSmoothness ("Dust Detail Smoothness", Range(0, 1)) = 0.2
		_DustNormalStrength ("Dust Normal Strength", Range(0, 2)) = 1

		// ---- rocks / asteroid belt ----
		_RockAlbedo ("Rock Albedo (RGB, A = coverage)", 2D) = "black" {}
		[Normal] _RockNormals ("Rock Normals (tangent space)", 2D) = "bump" {}
		_RockMask ("Rock Mask  R = metallic, A = smoothness", 2D) = "black" {}
		[HDR] _RockColor ("Rock Tint", Color) = (1, 1, 1, 1)
		_RockNormalStrength ("Rock Normal Strength", Range(0, 2)) = 1
		_RockMetallicScale ("Rock Metallic Scale", Range(0, 1)) = 1
		_RockSmoothnessScale ("Rock Smoothness Scale", Range(0, 1)) = 1

		// ---- what turns dust into rock ----
		// A material property rather than baked into the file, so the mask
		// can be changed without a recompile -- and because which source is
		// right depends on how the art ends up being authored.
		[Enum(Rock Alpha, 0, Mask Texture, 1, Vertex Alpha, 2, All Rock, 3)]
		_BlendMaskSource ("Blend Mask Source", Float) = 0
		_BlendMask ("Blend Mask (R)", 2D) = "white" {}
		[Enum(Lerp, 0, Smoothstep, 1)] _BlendShape ("Blend Shape", Float) = 0
		_BlendFloor ("Blend Floor", Range(0, 1)) = 0
		_BlendRange ("Blend Range", Range(0.001, 1)) = 0.35

		// ---- overall ----
		_AlphaScale ("Alpha Scale", Range(0, 1)) = 1
		// x = inner edge start, y = inner fade width,
		// z = outer edge end,   w = outer fade width
		_Edges ("Edges (x=inner start, y=inner fade, z=outer end, w=outer fade)", Vector) = (0, 1, 10000, 1)
		[Enum(Off, 0, Back, 1, Front, 2)] _Cull ("Culling", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
			"Queue" = "Transparent"
			"DisableBatching" = "True"
		}

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			// PREMULTIPLIED, so Blend is One / OneMinusSrcAlpha and the
			// fragment premultiplies before returning. With the ordinary
			// SrcAlpha / OneMinusSrcAlpha and no premultiply, a half
			// transparent pixel contributes its full lit colour at half
			// strength, which darkens toward the background instead of fading
			// into it -- a black fringe around every soft edge in the ring.
			//
			// ZWrite off by default: a flat annulus that wrote depth would
			// punch an opaque disc over the planet and everything behind it.
			// Culling is off by default because a ring is seen from both
			// faces, and a back-facing annulus lit by its own geometric
			// normal renders black.
			Blend One OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Cull [_Cull]

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			// One sampler state per texture, matching the planet shader.
			// Sharing a single sampler across six textures means the last one
			// set silently sets wrap and filter for all the others.
			TEXTURE2D(_MainTex);     SAMPLER(sampler_MainTex);
			TEXTURE2D(_Normals);     SAMPLER(sampler_Normals);
			TEXTURE2D(_SpecMap);     SAMPLER(sampler_SpecMap);
			TEXTURE2D(_RockAlbedo);  SAMPLER(sampler_RockAlbedo);
			TEXTURE2D(_RockNormals); SAMPLER(sampler_RockNormals);
			TEXTURE2D(_RockMask);    SAMPLER(sampler_RockMask);
			TEXTURE2D(_BlendMask);   SAMPLER(sampler_BlendMask);

			// Every material property has to be here, not just the ones the
			// fragment happens to read, or the SRP Batcher falls back to
			// per-draw setup.
			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float4 _Normals_ST;
				float4 _SpecMap_ST;
				float4 _RockAlbedo_ST;
				float4 _RockNormals_ST;
				float4 _RockMask_ST;
				float4 _BlendMask_ST;
				float4 _DustColor;
				float4 _RockColor;
				float4 _Edges;
				float _DustSmoothness;
				float _DustDetailSmoothness;
				float _DustNormalStrength;
				float _RockNormalStrength;
				float _RockMetallicScale;
				float _RockSmoothnessScale;
				float _BlendMaskSource;
				float _BlendShape;
				float _BlendFloor;
				float _BlendRange;
				float _AlphaScale;
				float _Cull;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float2 uv : TEXCOORD0;
				half4 color : COLOR;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				half3 normalWS : TEXCOORD2;
				half4 tangentWS : TEXCOORD3;
				half4 color : COLOR;
				FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				VertexPositionInputs posInputs =
					GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normInputs =
					GetVertexNormalInputs(input.normalOS, input.tangentOS);

				output.positionCS = posInputs.positionCS;
				output.positionWS = posInputs.positionWS;
				output.normalWS = normInputs.normalWS;
				output.tangentWS = half4(normInputs.tangentWS, input.tangentOS.w);
				output.color = input.color;
				output.facing = IS_FRONT_VFACE(input.positionCS, 1.0, -1.0);

				// The RAW uv, not TRANSFORM_TEX'd: each texture carries its own
				// _ST so the dust and the rocks can tile independently while
				// still agreeing on where they sit on the band. A ring is one
				// surface, and if the two sets used different transformed UVs
				// the rocks would slide off the dust.
				output.uv = input.uv;
				return output;
			}

			// Fade the band out at its inner and outer edges without needing a
			// texture for it. Cheap, and it stops a square-edged disc reading
			// as a square-edged disc.
			half EdgeFade(float3 positionWS)
			{
				half d = length(positionWS);
				half inner = saturate((d - _Edges.x) * rcp(max(1e-4, _Edges.y)));
				half outer = 1.0 - saturate((d - _Edges.z) * rcp(max(1e-4, _Edges.w)));
				return saturate(inner) * saturate(outer);
			}

			half4 frag(Varyings input) : SV_Target
			{
				// One fetch each. Anything that needs a second channel below
				// reuses these rather than sampling again.
				float2 uvDust = TRANSFORM_TEX(input.uv, _MainTex);
				float2 uvRock = TRANSFORM_TEX(input.uv, _RockAlbedo);

				half4 dustTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvDust);
				half4 rockTex = SAMPLE_TEXTURE2D(_RockAlbedo, sampler_RockAlbedo, uvRock);
				half4 dustSpec = SAMPLE_TEXTURE2D(_SpecMap, sampler_SpecMap,
					TRANSFORM_TEX(input.uv, _SpecMap));
				half4 rockMask = SAMPLE_TEXTURE2D(_RockMask, sampler_RockMask,
					TRANSFORM_TEX(input.uv, _RockMask));
				half3 dustN = UnpackNormal(SAMPLE_TEXTURE2D(_Normals,
					sampler_Normals, TRANSFORM_TEX(input.uv, _Normals)).xyz);
				half3 rockN = UnpackNormal(SAMPLE_TEXTURE2D(_RockNormals,
					sampler_RockNormals, TRANSFORM_TEX(input.uv, _RockNormals)).xyz);

				// ---- how much rock is there here ----
				// Driven by a uniform, so the branch is uniform-coherent and
				// costs a compare rather than a divergent path. Source 0 reuses
				// the rock fetch above, so the default costs nothing extra.
				half rock;
				if (_BlendMaskSource < 0.5)
				{
					rock = rockTex.a;
				}
				else if (_BlendMaskSource < 1.5)
				{
					rock = SAMPLE_TEXTURE2D(_BlendMask, sampler_BlendMask,
						TRANSFORM_TEX(input.uv, _BlendMask)).r;
				}
				else if (_BlendMaskSource < 2.5)
				{
					rock = input.color.a;
				}
				else
				{
					rock = 1.0;
				}

				// Thresholded, because two materials that are genuinely
				// different should meet at a decided edge rather than sitting
				// in a permanent smear.
				rock = saturate((rock - _BlendFloor) * rcp(max(1e-4, _BlendRange)));
				if (_BlendShape > 0.5)
				{
					rock = rock * rock * (3.0 - 2.0 * rock);
				}

				// ---- normals ----
				// Blended in TANGENT space, before the TBN is built, then
				// transformed once. Blending after the transform would need two
				// TBNs and does not commute.
				half3 normalTS = normalize(lerp(
					dustN * _DustNormalStrength, rockN * _RockNormalStrength, rock));

				// Flip the geometric normal on back faces. Without this the
				// underside of a ring is lit from behind and comes out black,
				// which is the classic single-sided-an-annulus bug.
				half3 normalWS = normalize(input.normalWS) * input.facing;
				half3 tangentWS = normalize(input.tangentWS.xyz);
				half3 bitangentWS = cross(normalWS, tangentWS) * input.tangentWS.w;
				normalWS = normalize(TransformTangentToWorld(normalTS,
					half3x3(tangentWS, bitangentWS, normalWS)));

				// ---- surface ----
				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = lerp(dustTex.rgb * _DustColor.rgb,
					rockTex.rgb * _RockColor.rgb, rock);
				// Dust is a dielectric: constant 0 metallic, no map needed.
				// Rocks are the metallic half of the pair, which is the whole
				// reason they are a separate texture set.
				surfaceData.metallic =
					saturate(rockMask.r * _RockMetallicScale) * rock;
				surfaceData.smoothness = lerp(
					saturate(_DustSmoothness + dustSpec.r * _DustDetailSmoothness),
					saturate(rockMask.a * _RockSmoothnessScale),
					rock);
				surfaceData.normalTS = normalTS;
				surfaceData.occlusion = 1.0;
				surfaceData.emission = half3(0, 0, 0);

				InputData inputData = (InputData)0;
				inputData.positionWS = input.positionWS;
				inputData.normalWS = normalWS;
				inputData.viewDirectionWS =
					GetWorldSpaceNormalizeViewDir(input.positionWS);
				inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				inputData.vertexLighting = half3(0, 0, 0);
				inputData.normalizedScreenSpaceUV =
					GetNormalizedScreenSpaceUV(input.positionCS);
				inputData.shadowMask = half4(1, 1, 1, 1);

				Light mainLight = GetMainLight(inputData.shadowCoord);

				half4 color = UniversalFragmentPBR(inputData, surfaceData);
				color.rgb = MixFog(color.rgb, inputData.fogCoord);

				// ---- coverage ----
				// Each set carries its own coverage, and the mask decides which
				// one is in charge. Lerping both in unconditionally would
				// multiply the rock alpha in over dust the rock is not even
				// covering, thinning the band wherever the two disagree.
				half alpha = lerp(dustTex.a, rockTex.a, rock);
				color.a = saturate(saturate(alpha * _AlphaScale) * EdgeFade(input.positionWS));

				// Premultiply, to match Blend One / OneMinusSrcAlpha above.
				color.rgb *= color.a;
				return color;
			}
			ENDHLSL
		}
	}

	// Opaque platforms without URP still get something, and it is not nothing:
	// the ring draws unlit with its own alpha rather than vanishing.
	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

		Pass
		{
			Blend One OneMinusSrcAlpha
			ZWrite Off
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			sampler2D _RockAlbedo;
			float4 _MainTex_ST;
			float4 _RockAlbedo_ST;
			float4 _DustColor;
			float4 _RockColor;
			float _AlphaScale;
			float _BlendMaskSource;
			float _Edges;

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				half4 dust = tex2D(_MainTex, i.uv);
				half4 rock = tex2D(_RockAlbedo, i.uv);
				half mask = (_BlendMaskSource < 0.5) ? rock.a : 1.0;
				half3 col = lerp(dust.rgb * _DustColor.rgb, rock.rgb * _RockColor.rgb, mask);
				half a = saturate(lerp(dust.a, rock.a, mask) * _AlphaScale);
				return half4(col * a, a);
			}
			ENDCG
		}
	}

	Fallback Off
}