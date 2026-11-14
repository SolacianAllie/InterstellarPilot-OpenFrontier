Shader "OpenFrontier/GalaxyBillboards"
{
	// Camera-facing billboards for the galaxy's DISCRETE dressing layer --
	// GalaxyStars (thousands of point stars) and GalaxyClouds (a few
	// hundred soft dust puffs) share this shader.
	//
	// WHY THIS EXISTS. The GalaxyDisk shader is smooth fbm, and smooth
	// noise has a detail ceiling: magnified, it is still gradients. The
	// reference look survives close range because the galaxy resolves into
	// discrete elements -- far away they blend into glowing structure,
	// close up they separate into stars and puffs. That perception cue is
	// this file's job, layered on top of the disk, which stays the
	// diffuse base.
	//
	// ONE DRAW CALL PER LAYER. The mesh is a merged static grid of quads:
	// positionOS is the billboard's CENTRE, TEXCOORD0 carries the -1..1
	// corner, TEXCOORD1 carries size / brightness seed / vertical weight,
	// all baked by the C# side. Billboarding happens in the vertex shader
	// from the camera position, so the mesh is built once and never
	// rebuilt per frame.
	//
	// FRUSTUM-CULLING NOTE (baked in the C# bounds, repeated here because
	// it is the classic silent killer): the mesh vertices are billboard
	// CENTRES, but the quads extend well past them. The generated mesh's
	// bounds are padded by the maximum size, or edge-of-screen billboards
	// pop in and out as their centre crosses the frustum while their quad
	// is still visible.
	//
	// COLOUR IS COMPUTED PER-VERTEX, from the same radial/arm functions
	// the disk paints, so the palette knobs live in THIS material and the
	// population (where, how many, how big) lives in the C#. The arm band
	// is LIVE: the same smoothstep the disk's arm term uses dims the
	// inter-arm population, and TEXCOORD1.w whitens the bright tail the
	// C# bakes. Per-vertex colour costs tens of thousands of interpolator
	// writes instead of hundreds of thousands of per-pixel evaluations.
	//
	// SHAPE TEXTURES: either layer (each material binds its own) can
	// swap the procedural falloff for a texture -- rgb tints the vertex
	// palette (white textures preserve it), alpha is the mask, _ST
	// honours tiling/offset so sprite atlases work. The texture owns the
	// edge: fade it to black at its border, or the quad's rim shows.
	// The SpaceUnity star-field and nebula textures already in
	// Resources/SpaceUnity are fair game.
	//
	// PROXIMITY: stars brighten as the camera closes on them -- the
	// depth-lit fly-through field. _ProximityBoost opts a material in;
	// it multiplies into the near fade so the two form one distance
	// response. The world-space quads already grow on approach, so size
	// AND brightness now rise together; a close dim star can out-shine
	// a far bright one.
	//
	// ADDITIVE, like the disk: black adds nothing, draw order is free,
	// and the layers compose over the disk in any order.
	//
	// DETERMINISM: positions and sizes come from an integer splitmix64
	// stream in the C# (same seed, same galaxy, every session). The arm
	// angle below is the same logarithmic spiral the disk shader paints,
	// so density lands exactly on the painted arms.
	//
	// MOBILE FIRST: the per-frame cost is vertex shading only -- the mesh
	// is static, additive, ZWrite-off. Counts are a ONE-SHOT CAPTURE
	// budget: the galaxy renders for seconds while the sky cubemap is
	// grabbed, so star counts into the six figures are affordable for
	// that window; tune them down for anything that persists. The near
	// fade (dissolve when the camera gets close -- the Elite behaviour)
	// also bounds the worst fill-rate case, because the puffs that would
	// cover the most pixels are the ones it fades out.

	Properties
	{
		// ---- palette / placement shading (same functions as the disk) ----
		[IntRange] _Arms ("Arm Count", Range(1, 8)) = 2
		_Winding ("Spiral Winding", Range(0, 14)) = 5
		// Feeds the inter-arm dimming's smoothstep width, exactly as in
		// the disk's arm term.
		_ArmSoftness ("Arm Softness", Range(0.05, 0.9)) = 0.35
		// How dark the inter-arm population goes (1 = no banding). The
		// C# snaps most disk stars ONTO the crests; this knob shapes the
		// uniform remainder so the arms read with contrast.
		_ArmDimFloor ("Inter-Arm Dimming", Range(0, 1)) = 0.55
		// How quickly points dim toward the rim.
		_PresenceFalloff ("Radial Dimming", Range(0.5, 8)) = 3
		// Extra brightness inside the bulge: stars in the core are the
		// dense, warm swarm the reference shows.
		_BulgeRadius ("Bulge Radius", Range(0.02, 0.6)) = 0.3
		_BulgeBoost ("Bulge Brightening", Range(0, 6)) = 2
		[HDR] _InnerColor ("Inner Colour", Color) = (1, 0.78, 0.68, 1)
		[HDR] _OuterColor ("Outer Colour", Color) = (0.7, 0.74, 1, 1)

		// ---- look ----
		_Intensity ("Overall Intensity", Range(0, 8)) = 1
		// Global size multiplier on the per-point baked size. Stars want
		// ~1; clouds are tuned with this rather than rebuilding the mesh.
		_SizeScale ("Size Scale", Range(0.05, 4)) = 1
		// exp(-d*d*k) on the -1..1 corner: high k = tight points (stars,
		// ~18), low k = soft puffs (clouds, ~3.5). Only used while the
		// shape-texture keyword is OFF.
		_EdgeSharpness ("Edge Sharpness", Range(0.5, 30)) = 3.5
		// Optional shape texture: rgb tints, alpha masks. Assign a
		// texture BEFORE enabling the toggle -- enabled with none
		// assigned samples black and the layer vanishes.
		[MainTexture] _MainTex ("Shape Texture", 2D) = "black" {}
		[Toggle(_SHAPE_TEXTURE)] _UseShapeTexture ("Use Shape Texture (else procedural)", Float) = 0
		// Near fade in WORLD units: brighten from 0 at Start to full at
		// End as distance grows. This is what lets the camera sit inside
		// the population without one nearby puff covering the lens -- and
		// during a sky capture it keeps the destination anchor's own
		// neighbourhood from washing out the frame.
		_NearFadeStart ("Near Fade Start (world units)", Float) = 40
		_NearFadeEnd ("Near Fade End (world units)", Float) = 150
		// Proximity brightening: stars near the camera gain brightness
		// -- the depth-lit field of a fly-through, closer stars blazing
		// while the field behind stays dim. Neutral at 0; the stars
		// material opts in (clouds keep the plain near fade).
		_ProximityDistance ("Proximity Range (world units)", Float) = 400
		_ProximityBoost ("Proximity Brightening", Range(0, 8)) = 0
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
			// Explicit layer order for the galaxy stack: the disk paints
			// at Transparent (3000), the stars at +1, the dust shader
			// (GalaxyDust: Scatter/Occlude) at +2 so its RevSub pass
			// darkens the glow drawn before it, and the local shell's
			// material sets +3 so the nearest stars punch through the
			// dust. Deterministic ordering instead of distance-sorted
			// ties between bounds-centred meshes.
			"Queue" = "Transparent+1"
		}

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			Blend One One
			ZWrite Off
			ZTest LEqual
			Cull Off

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			// Local keyword: only the material that opts in pays for the
			// texture variant.
			#pragma shader_feature_local _SHAPE_TEXTURE

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			// Shape texture (see Properties): per material -- stars and
			// clouds each bind their own. The _ST vector lives inside the
			// cbuffer for SRP-batcher compatibility; the sampler cannot.
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			CBUFFER_START(UnityPerMaterial)
				float4 _InnerColor;
				float4 _OuterColor;
				float _Arms;
				float _Winding;
				float _ArmSoftness;
				float _ArmDimFloor;
				float _PresenceFalloff;
				float _BulgeRadius;
				float _BulgeBoost;
				float _Intensity;
				float _SizeScale;
				float _EdgeSharpness;
				float _NearFadeStart;
				float _NearFadeEnd;
				float _ProximityDistance;
				float _ProximityBoost;
				float4 _MainTex_ST;
			CBUFFER_END

			struct Attributes
			{
				// xyz: the billboard's centre, in the galaxy's local frame.
				float4 positionOS : POSITION;
				// -1..1 quad corner.
				float2 corner : TEXCOORD0;
				// x: size in local units (times the transform's scale in
				// the shader). y: brightness 0..1, pre-shaped by the C#
				// distribution (few bright, many dim). z: vertical weight
				// baked against the disk's thickness profile.
				float4 data : TEXCOORD1;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 corner : TEXCOORD0;
				half4 color : TEXCOORD1;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;

				float3 centerOS = input.positionOS.xyz;
				float3 centerWS = TransformObjectToWorld(centerOS);

				// ---- spherical billboard ----
				float3 toCamera = _WorldSpaceCameraPos.xyz - centerWS;
				float dist = length(toCamera);
				float3 viewDir = toCamera / max(dist, 1e-4);

				// Camera-facing frame from the world-up cross product.
				// Degenerate guard is LOAD-BEARING: a camera straight above
				// a puff (the galaxy stage's overhead view!) sends the
				// cross product to zero, and normalize(0) is NaN -- which
				// renders as a black hole exactly at the top of the frame.
				// The fallback keeps those billboards facing along world X.
				float3 right = cross(float3(0, 1, 0), viewDir);
				float rightLen = length(right);
				right = rightLen > 1e-4 ? right / rightLen : float3(1, 0, 0);
				float3 up = cross(viewDir, right);

				// Size: baked local size x the object's world scale (taken
				// from the x axis; the galaxy scales uniformly), x the
				// material's global multiplier.
				float size = input.data.x * _SizeScale
					* length(mul((float3x3)GetObjectToWorldMatrix(), float3(1, 0, 0)));

				float3 cornerWS = centerWS
					+ (right * input.corner.x + up * input.corner.y) * size;
				output.positionCS = TransformWorldToHClip(cornerWS);
				output.corner = input.corner;

				// ---- colour, from the disk's own functions ----
				float r = length(centerOS.xz);
				float theta = atan2(centerOS.z, centerOS.x);
				// round() is load-bearing here exactly as in the disk
				// shader: sin(N*theta + phase) is continuous across the
				// atan2 wrap only for integer N.
				float armCount = max(1.0, round(_Arms));
				float twist = _Winding * log(max(r, 0.03));
				half band = sin(armCount * theta + twist) * 0.5 + 0.5;
				// The SAME smoothstep the disk's arm term uses. The C#
				// snaps most disk stars onto the crests, so the band's job
				// is the inter-arm population: uniform-angle points dim
				// between the arms, reinforcing the painted contrast. The
				// effect fades out inside the bulge -- the core is no
				// place for arm banding.
				half armBand = smoothstep(1.0 - _ArmSoftness, 1.0, band);
				half armDim = lerp(1.0,
					lerp(_ArmDimFloor, 1.0, armBand),
					smoothstep(0.0, _BulgeRadius, r));
				half innerMix = smoothstep(0.65, 0.2, r);
				half3 col = lerp(_OuterColor.rgb, _InnerColor.rgb, innerMix);

				half presence = exp(-r * _PresenceFalloff);
				float coreD2 = dot(centerOS.xz, centerOS.xz);
				half coreBoost = exp(-coreD2
					/ max(_BulgeRadius * _BulgeRadius, 1e-5));

				// Brightness: vertical weight (the disk's profile, or the
				// core's own gentler curve) x the pre-shaped distribution
				// (few bright, many dim) x radial presence x the bulge
				// swarm boost x the inter-arm band.
				half bright = input.data.z
					* (0.3 + 0.7 * input.data.y)
					* presence
					* (1.0 + _BulgeBoost * coreBoost)
					* armDim;

				// The bright tail whitens (data.w, baked by the C#): a
				// dense field reads as temperature variation, not one
				// tint. Clouds bake w = 0 and are untouched.
				col = lerp(col, half3(1.0, 0.97, 0.9),
					saturate(input.data.w) * 0.75);

				// Near fade: 0 at Start, full at End. Also the fill-rate
				// guard -- the closest (largest-on-screen) billboards are
				// the ones it removes.
				half fade = saturate((dist - _NearFadeStart)
					/ max(_NearFadeEnd - _NearFadeStart, 1e-3));

				// Proximity brightening: full at the camera, gone at
				// _ProximityDistance. Multiplied into the same product as
				// the near fade, so the two knobs compose into ONE
				// distance response: rise through the fade band, keep
				// rising as the star closes. Neutral at boost 0.
				half proximity = 1.0 + _ProximityBoost
					* (1.0 - saturate(dist / max(_ProximityDistance, 1e-3)));

				output.color = half4(
					col * bright * fade * _Intensity * proximity, 1.0);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				// Full-quad UV from the -1..1 corner: the mesh bakes
				// corners, not UVs, so texture mode costs nothing extra.
				float2 uv = input.corner * 0.5 + 0.5;
			#if defined(_SHAPE_TEXTURE)
				// Texture mode: the texture owns the shape. rgb tints
				// (white textures preserve the vertex palette), alpha is
				// the mask; the manual _ST transform honours
				// tiling/offset for sprite atlases.
				float2 texUV = uv * _MainTex_ST.xy + _MainTex_ST.zw;
				half4 shape = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, texUV);
				return half4(input.color.rgb * shape.rgb * shape.a, 1.0);
			#else
				// Procedural mode: exp falloff on the -1..1 corner: high
				// sharpness gives a tight round point (stars), low gives
				// a soft puff (clouds). exp(d*d) is zero enough by the
				// diagonal that no quad edge is ever visible.
				float d = length(input.corner);
				half falloff = exp(-d * d * _EdgeSharpness);
				return half4(input.color.rgb * falloff, 1.0);
			#endif
			}
			ENDHLSL
		}
	}

	// No fallback SubShader. The project is URP-only -- the graphics
	// settings bind a UniversalRenderPipelineAsset -- so a built-in
	// CGPROGRAM pass here is unreachable code Unity still compiles.
	Fallback Off
}
