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
		// The rocks get their OWN projection rather than inheriting the mesh's.
		// A ring's UV layout is strongly anisotropic -- a polar one smears
		// every texel radially, a planar one smears it angularly -- and that is
		// right for dust, which is a smooth band, and wrong for a rock texture,
		// which wants near-isotropic texels. Sharing one layout for both is
		// what produces the radial streaks.
		//
		// The generated UV is then still run through _RockAlbedo_ST, so the
		// inspector's tiling and offset keep working on top of it.
		[Enum(Mesh UV, 0, Polar, 1, Angular Only, 2)]
		_RockUVMode ("Rock UV Mode", Float) = 2
		// Which object-space plane the ring lies in. Y-up space games put
		// orbits in XZ, so that is the default; a ring built in another
		// plane streaks along the wrong axis without this.
		[Enum(XZ, 0, XY, 1, YZ, 2)]
		_RockUVPlane ("Rock UV Plane", Float) = 0
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
		// Measured in OBJECT space, not world space -- see EdgeFade.
		_Edges ("Edges (x=inner start, y=inner fade, z=outer end, w=outer fade)", Vector) = (0, 1, 10000, 1)
		// 0 = no edge fade at all. Default OFF deliberately: the fade
		// multiplies alpha, so a fade that is subtly wrong does not dim the
		// ring, it erases it, with nothing in the render to say why.
		_EdgeFadeStrength ("Edge Fade Strength", Range(0, 1)) = 0
		// Staged bisect, matching the planet shader's _AlbedoDebug so the two
		// are read the same way.
		//   0 = final   1 = raw dust albedo   2 = blended albedo
		//   3 = lit colour, alpha forced to 1
		// Stages 1-3 return before the lighting and with alpha 1, so what is on
		// screen is the value under test and nothing else.
		_AlbedoDebug ("Albedo Debug", Range(0, 3)) = 0
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
				float _RockUVMode;
				float _RockUVPlane;
				float _RockSmoothnessScale;
				float _BlendMaskSource;
				float _BlendShape;
				float _BlendFloor;
				float _BlendRange;
				float _AlphaScale;
				float _Cull;
				float _EdgeFadeStrength;
				float _AlbedoDebug;
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
				float3 positionOS : TEXCOORD4;
				half4 color : COLOR;
			};

			// The facing flag goes in the FRAGMENT input, never in the vertex
			// output. SV_IsFrontFace is input-only, so declaring it in
			// Varyings -- which vert() writes -- is an illegal output semantic
			// on Vulkan. URP does it this way in SpeedTree8Passes.hlsl:
			// the varyings are nested as `interpolated`, and `facing` sits
			// alongside them.
			struct FragmentInput
			{
				Varyings interpolated;
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
				output.positionOS = input.positionOS.xyz;
				output.normalWS = normInputs.normalWS;
				output.tangentWS = half4(normInputs.tangentWS, input.tangentOS.w);
				output.color = input.color;

				// The RAW uv, not TRANSFORM_TEX'd: each texture carries its own
				// _ST so the dust and the rocks can tile independently while
				// still agreeing on where they sit on the band. A ring is one
				// surface, and if the two sets used different transformed UVs
				// the rocks would slide off the dust.
				output.uv = input.uv;
				return output;
			}

			// Fade the band out at its inner and outer edges.
			//
			// OBJECT space, not world. A ring's radius is a property of the ring,
			// so it belongs to the object; measuring it in world space ties the
			// fade to wherever the system happens to sit, and this did: with the
			// default outer end at 10000 units, any world further than 10000
			// units from the origin multiplied alpha by ZERO and the ring drew
			// nothing at all -- no error, no pink, just an absence.
			half EdgeFade(float3 positionOS)
			{
				half d = length(positionOS);
				half inner = saturate((d - _Edges.x) * rcp(max(1e-4, _Edges.y)));
				half outer = 1.0 - saturate((d - _Edges.z) * rcp(max(1e-4, _Edges.w)));
				return saturate(inner) * saturate(outer);
			}

			half4 frag(FragmentInput input) : SV_Target
			{
				// One fetch each. Anything that needs a second channel below
				// reuses these rather than sampling again.
				float2 uvDust = TRANSFORM_TEX(input.interpolated.uv, _MainTex);

				// The rock UV, generated from object space rather than read from
				// the mesh. Driven by uniforms, so the branch is uniform-
				// coherent and costs a compare.
				float2 rockP = input.interpolated.positionOS.xz;
				if (_RockUVPlane > 0.5)
				{
					rockP = _RockUVPlane < 1.5
						? input.interpolated.positionOS.xy
						: input.interpolated.positionOS.yz;
				}
				// atan2 gives -pi..pi; fold it to 0..1 around the band.
				float rockAngle = atan2(rockP.y, rockP.x) * 0.15915494 + 0.5;
				float rockRadius = length(rockP);
				float2 rockUV;
				if (_RockUVMode > 1.5)
				{
					// Angular only: one row of the texture repeated around the
					// ring. No radial coordinate at all, so a rock texel is as
					// wide as it is tall and nothing can smear along the radius.
					rockUV = float2(rockAngle, 0.5);
				}
				else if (_RockUVMode > 0.5)
				{
					// Polar: the layout an annulus actually wants, independent
					// of how the mesh happens to be unwrapped.
					rockUV = float2(rockAngle, rockRadius);
				}
				else
				{
					rockUV = input.interpolated.uv;
				}
				float2 uvRock = TRANSFORM_TEX(rockUV, _RockAlbedo);

				half4 dustTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvDust);
				half4 rockTex = SAMPLE_TEXTURE2D(_RockAlbedo, sampler_RockAlbedo, uvRock);
				half4 dustSpec = SAMPLE_TEXTURE2D(_SpecMap, sampler_SpecMap,
					TRANSFORM_TEX(input.interpolated.uv, _SpecMap));
				half4 rockMask = SAMPLE_TEXTURE2D(_RockMask, sampler_RockMask,
					TRANSFORM_TEX(input.interpolated.uv, _RockMask));
				// The WHOLE sample, not .xyz. The live overload in the core package is
				// `real3 UnpackNormal(real4 packedNormal)` -- it takes a float4.
				// Passing a half3 does not convert, and the only half3 overload
				// is the DEPRECATED GBuffer one, which a UniversalForward pass
				// does not resolve to.
				half3 dustN = UnpackNormal(SAMPLE_TEXTURE2D(_Normals,
					sampler_Normals, TRANSFORM_TEX(input.interpolated.uv, _Normals)));
				half3 rockN = UnpackNormal(SAMPLE_TEXTURE2D(_RockNormals,
					sampler_RockNormals, TRANSFORM_TEX(input.interpolated.uv, _RockNormals)));

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
						TRANSFORM_TEX(input.interpolated.uv, _BlendMask)).r;
				}
				else if (_BlendMaskSource < 2.5)
				{
					rock = input.interpolated.color.a;
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

				// Declared before the bisect, which reads it. HLSL resolves an
				// identifier where it is USED, not file-wide, so declaring this
				// further down with the rest of the surface is "undeclared
				// identifier" at the point of use.
				half3 surfaceDebugAlbedo = lerp(dustTex.rgb * _DustColor.rgb,
					rockTex.rgb * _RockColor.rgb, rock);

				// ---- staged bisect ----
				// Returned BEFORE the lighting and with alpha 1, so what is on
				// screen is the value under test and nothing else. Bounded at
				// BOTH ends, which is not decoration: written as `> 0` alone,
				// stages 1, 2 and 3 would all match and every one of them would
				// look identical to stage 1, so the later stages would be
				// unreachable and would report nothing. That is the bug the
				// planet shader's own _AlbedoDebug comment warns about.
				if (_AlbedoDebug > 0.5 && _AlbedoDebug < 1.5)
				{
					// Raw dust albedo. Opaque. Grey here means the texture or
					// its import; flat grey across the whole ring means the UVs
					// are not varying, i.e. the mesh has none.
					return half4(dustTex.rgb, 1.0);
				}
				if (_AlbedoDebug > 1.5 && _AlbedoDebug < 2.5)
				{
					// After the dust/rock mask. Banded means the mask is
					// reading the rock texture; uniform means it is not.
					return half4(surfaceDebugAlbedo, 1.0);
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
				half3 normalWS = normalize(input.interpolated.normalWS)
					* IS_FRONT_VFACE(input.facing, 1.0, -1.0);
				half3 tangentWS = normalize(input.interpolated.tangentWS.xyz);
				half3 bitangentWS = cross(normalWS, tangentWS) * input.interpolated.tangentWS.w;
				normalWS = normalize(TransformTangentToWorld(normalTS,
					half3x3(tangentWS, bitangentWS, normalWS)));

				// ---- surface ----
				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = surfaceDebugAlbedo;
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
				inputData.positionWS = input.interpolated.positionWS;
				inputData.normalWS = normalWS;
				inputData.viewDirectionWS =
					GetWorldSpaceNormalizeViewDir(input.interpolated.positionWS);
				inputData.shadowCoord = TransformWorldToShadowCoord(input.interpolated.positionWS);
				inputData.vertexLighting = half3(0, 0, 0);
				inputData.normalizedScreenSpaceUV =
					GetNormalizedScreenSpaceUV(input.interpolated.positionCS);
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
				color.a = saturate(saturate(alpha * _AlphaScale)
					* lerp(1.0, EdgeFade(input.interpolated.positionOS), _EdgeFadeStrength));

				// Stage 3: everything the shader computed, with alpha forced.
				// Black here with stage 1 showing colour means the LIGHTING is
				// taking it away -- no main light reaching the ring, or the
				// normal map pointing it away from the sun.
				if (_AlbedoDebug > 2.5)
				{
					return half4(color.rgb, 1.0);
				}

				// Premultiply, to match Blend One / OneMinusSrcAlpha above.
				color.rgb *= color.a;
				return color;
			}
			ENDHLSL
		}
	}

	// No fallback SubShader. The project is URP-only -- the graphics settings
	// bind a UniversalRenderPipelineAsset -- so a built-in CGPROGRAM pass here
	// is unreachable code that still gets compiled. Three compile errors came
	// out of this file already; dead HLSL is not worth being unable to check.
	Fallback Off
}