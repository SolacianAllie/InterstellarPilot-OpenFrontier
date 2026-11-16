// Open Frontier: ED's SkyboxLocalStar, procedural variant, driving a REAL 3D
// star catalog in the explorable miniature.
//
// ED's shipped local-star family has textured variants, and those pixel
// shaders do declare and sample Texture2D resources. This shader keeps the
// arithmetic core/halo/spike/ring shape instead: the miniature's stars are
// actual 3D positions, so a flat star sheet would be the wrong model. The
// important part is the vertex stage:
//
//   - Stars are drawn AT their catalog position (no infinity forcing):
//     they parallax as you fly and can be approached. From far away the
//     distance-modulus term pushes most of them below the visibility
//     floor and the field collapses into the band - the same reason ED's
//     sky shows a resolved local volume over an unresolved galaxy.
//   - Apparent magnitude = absolute magnitude + 5*log10(dist /
//     _TenParsecDistance), the real distance modulus with the miniature's
//     10-pc distance as a tuning knob. Galactus-free, physics-shaped.
//   - Sky half only: layer 31. ED draws one catalog twice: textured sheet
//     sprites in the map (StarMapStar) and procedural stars in the sky. This
//     is the sky half; GalaxyMapStars carries the map half on layer 30. The
//     game cameras' culling masks are exact complements, so a star is never
//     drawn twice, regardless of projection or zoom.
//
// Size is in PIXELS against ED's recovered curve: magnitude thresholds
// (capped/dimmest), radius and alpha exponents, super-bright branch
// (pow 3), the 10..40 px clamps implied by the {10, 40} immediates, and
// the visibility floor. Static by design: ED's sky stars do not twinkle -
// the billboard cloud owns the shimmer (_OpenFrontierGalaxyTime).
//
// Queue offset lives on the MATERIAL (3000); the tag stays a defined name.
Shader "OpenFrontier/Galaxy/SkyStar" {
	Properties {
		[HDR] _StarTint ("Star Tint", Color) = (1,1,1,1)
		_StarBrightness ("Star Brightness", Float) = 1
		_StarSizeScale ("Star Size Scale", Float) = 1
		_TenParsecDistance ("Ten Parsec Distance (units)", Float) = 1000
		_DimmestMag ("Dimmest Apparent Magnitude", Float) = 6.5
		_CappedMag ("Capped Apparent Magnitude", Float) = -1.5
		_SuperBrightMag ("Super-Bright Threshold", Float) = 1
		_SizeMin ("Dimmest Size (px)", Float) = 6
		_SizeMax ("Brightest Size (px)", Float) = 30
		_StarRadPow ("Radius Exponent", Float) = 2
		_StarAlphaPow ("Alpha Exponent", Float) = 1.4
		_HaloStart ("Halo Start", Range(0,1)) = 0.55
		_HaloStrength ("Halo Strength", Float) = 0.55
		_SpikeStart ("Spike Width", Range(0,1)) = 0.15
		_SpikeStrength ("Spike Strength", Float) = 0.15
		_RingStart ("Ring Start", Range(0,1)) = 0.85
		_RingStrength ("Ring Strength", Float) = 0.85
		_MinVisAlpha ("Visibility Floor", Range(0,0.5)) = 0.02
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		Blend One One
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

			struct StarInstance
			{
				float4 posMag;   // xyz = galaxy-local position, w = ABSOLUTE magnitude
				float4 color;    // rgb = tint, a unused
				float4 params;   // x = size jitter, y = seed, z/w spare
			};

			StructuredBuffer<StarInstance> _InstanceData;
			float4x4 _GalaxyToWorld;
			float4 _StarTint;
			float _StarBrightness;
			float _StarSizeScale;
			float _TenParsecDistance;
			float _DimmestMag;
			float _CappedMag;
			float _SuperBrightMag;
			float _SizeMin;
			float _SizeMax;
			float _StarRadPow;
			float _StarAlphaPow;
			float _HaloStart;
			float _HaloStrength;
			float _SpikeStart;
			float _SpikeStrength;
			float _RingStart;
			float _RingStrength;
			float _MinVisAlpha;

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
				StarInstance d = _InstanceData[id];

				// Sky presentation is a LAYER decision, not a camera-distance
				// one: the in-sky Galaxy Camera includes GalaxySkyStars
				// (layer 31), the MAP cameras mask it out.
				float3 worldWS = mul(_GalaxyToWorld, float4(d.posMag.xyz, 1.0)).xyz;
				float4 clip = mul(UNITY_MATRIX_VP, float4(worldWS, 1.0));

				// distance modulus at the miniature's scale: a star at
				// _TenParsecDistance gains nothing from range, farther stars
				// dim by 5 mag per decade (ED's own apparent-magnitude model)
				float dist = distance(worldWS, GetCameraPositionWS());
				float appMag = d.posMag.w + 5.0 * log10(max(dist, 1.0) / max(_TenParsecDistance, 1.0));

				// t = 1 at the bright cap, 0 at the dimmest visible star.
				// The previous form divided a negative by a negative in the
				// opposite direction, so it made faint stars large and bright
				// stars vanish - the field read as uniform dim dots.
				float t = saturate((_DimmestMag - appMag) / max(_DimmestMag - _CappedMag, 1e-4));
				float sizePx = lerp(_SizeMin, _SizeMax, pow(t, _StarRadPow));
				sizePx = max(sizePx, 1.5) * _StarSizeScale * d.params.x;

				float alpha = pow(t, _StarAlphaPow);
				if (appMag < _SuperBrightMag)
				{
					// ED's super-bright branch (pSuperBrightPow = 3)
					alpha *= 3.0;
					sizePx = min(sizePx * 1.25, 40.0 * _StarSizeScale);
				}
				alpha = max(alpha, _MinVisAlpha) * _StarBrightness;

				// Pixel-sized sprite at the REAL position: convert the
				// desired diameter to NDC through this camera's w.
				float scale = clip.w > 0.0 ? 1.0 : 0.0;
				clip.xy += v.positionOS.xy * sizePx * 2.0 / _ScreenParams.xy * clip.w * scale;
				o.positionCS = clip;
				o.uv = v.uv * 2.0 - 1.0;
				o.color = d.color.rgb * _StarTint.rgb;
				o.alpha = alpha;
				return o;
			}

			float4 frag(Varyings i) : SV_Target
			{
				float r = length(i.uv);

				float core = pow(saturate(1.0 - r), _StarRadPow);
				// A soft halo under the core; the old radial ramp started at
				// _HaloStart and rose toward the rim, which drew every star as
				// a bubble with a dark gap in the middle.
				float halo = pow(saturate(1.0 - r), 2.0) * _HaloStrength;
				float sx = 1.0 - smoothstep(0.0, max(_SpikeStart, 1e-3), abs(i.uv.x));
				float sy = 1.0 - smoothstep(0.0, max(_SpikeStart, 1e-3), abs(i.uv.y));
				float spike = max(sx, sy) * _SpikeStrength * saturate(1.0 - r);
				float ring = smoothstep(_RingStart, 1.0, r) * _RingStrength;
				ring *= 1.0 - smoothstep(0.94, 1.0, r);

				// The quad only reaches r=1 at its edge midpoints. A fade
				// that starts at 1.0 and ends outside the quad clips every
				// sprite into a rounded square; fade inside the inscribed
				// circle instead so the star is actually round.
				float edge = 1.0 - smoothstep(0.90, 1.0, r);
				float3 col = i.color * (core + halo + spike + ring) * i.alpha * edge;
				return float4(col, 1.0);
			}
			ENDHLSL
		}
	}
}
