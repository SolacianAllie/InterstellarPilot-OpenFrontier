// Open Frontier: ED's StarMapStar, the MAP's presentation of the catalog.
//
// ED draws one catalog twice: a textured sprite from the systemmap_stars
// sheet in the galaxy map (StarMapStar, StarInstanceCount 2000/2000/4000)
// and a procedural star in the sky (SkyboxLocalStar). This is the map half:
// the 512x256 Atlas repacked from the BC1 sheet as 8x4 cells of 64 px,
// sampled per star at the cell the C# seeded for it.
//
// Map presentation is layer 30: the MAP camera includes it (orthographic or
// angled perspective), the in-sky Galaxy Camera masks it out. The sky camera
// includes layer 31 and sees GalaxySkyStars instead; the two masks are exact
// complements, so a star is never drawn twice. ED itself only ships the
// top-down map, but the explorable miniature is viewed at an angle too, and
// the map's textured sprites are what carry its dense star field.
//
// Blend is SrcAlpha One, ED's additive sky/map blend: the sprite's baked
// glow does the shaping, the per-star tint and brightness only scale it.
//
// Queue offset lives on the MATERIAL (2991); the tag stays a defined name.
Shader "OpenFrontier/Galaxy/MapStar" {
	Properties {
		_SpriteTex ("Star Sprite Atlas (8x4)", 2D) = "white" {}
		[HDR] _Tint ("Tint", Color) = (1,1,1,1)
		_Brightness ("Brightness", Float) = 2
		_SizeMin ("Dimmest Size (px)", Float) = 8
		_SizeMax ("Brightest Size (px)", Float) = 22
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		Blend SrcAlpha One
		Cull Off
		ZWrite Off
		ZTest LEqual

		Pass {
			HLSLPROGRAM
			#pragma target 4.5
			#pragma exclude_renderers gles
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			#define ATLAS_COLS 8.0
			#define ATLAS_ROWS 4.0

			struct MapStarInstance
			{
				float4 posCell;  // xyz = galaxy-local position, w = atlas cell
				float4 color;    // rgb = tint, a unused
				float4 params;   // x = size jitter, y = alpha jitter, z/w spare
			};

			StructuredBuffer<MapStarInstance> _InstanceData;
			float4x4 _GalaxyToWorld;
			TEXTURE2D(_SpriteTex);
			SAMPLER(sampler_SpriteTex);
			float4 _Tint;
			float _Brightness;
			float _SizeMin;
			float _SizeMax;

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 color : TEXCOORD1;
				float alpha : TEXCOORD2;
			};

			Varyings vert(Attributes v, uint id : SV_InstanceID)
			{
				Varyings o;
				MapStarInstance d = _InstanceData[id];

				// Map presentation is a LAYER decision, not a camera-distance
				// one: the MAP cameras include GalaxyMapStars (layer 30), the
				// in-sky Galaxy Camera masks it out. This keeps the catalog
				// layer alive at every zoom, including close to the disc.
				float3 worldWS = mul(_GalaxyToWorld, float4(d.posCell.xyz, 1.0)).xyz;
				float4 clip = mul(UNITY_MATRIX_VP, float4(worldWS, 1.0));

				float sizePx = lerp(_SizeMin, _SizeMax, saturate(d.params.x));
				float scale = clip.w > 0.0 ? 1.0 : 0.0;
				clip.xy += v.positionOS.xy * sizePx * 2.0 / _ScreenParams.xy * clip.w * scale;

				// atlas cell -> cell-local UV. Row 0 is the top row of the
				// repacked sheet, Unity's v axis points up, hence the flip.
				float cell = d.posCell.w;
				float2 cellXY = float2(fmod(cell, ATLAS_COLS), floor(cell / ATLAS_COLS));
				float2 uv = (cellXY + v.uv) / float2(ATLAS_COLS, ATLAS_ROWS);
				uv.y = 1.0 - uv.y;

				o.positionCS = clip;
				o.uv = uv;
				o.color = d.color.rgb * _Tint.rgb;
				o.alpha = d.params.y * _Brightness;
				return o;
			}

			float4 frag(Varyings i) : SV_Target
			{
				float4 tex = SAMPLE_TEXTURE2D(_SpriteTex, sampler_SpriteTex, i.uv);
				float3 col = tex.rgb * i.color;
				float a = tex.a * i.alpha;
				// SrcAlpha One: dst += col * a
				return float4(col, a);
			}
			ENDHLSL
		}
	}
}
