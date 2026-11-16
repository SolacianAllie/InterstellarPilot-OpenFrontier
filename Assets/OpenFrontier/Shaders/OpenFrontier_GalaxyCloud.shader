// Open Frontier: the realtime galaxy band. ONE draw of GPU-instanced,
// camera-facing additive billboards; per-instance data (position, size,
// colour, fx) arrives via _InstanceData, filled by GalaxyInstanceCloud from
// the blueprint's CDF sampling. fx = (twinkle phase, amplitude, speed,
// sprite variant).
//
// Billboard orientation happens in clip space against the per-camera
// projection matrix, so the same indirect draw is correct in every camera
// that renders it - the ortho map view and the perspective in-sky view both
// included. The cloud's object transform arrives as _LocalToWorld (set from
// script every frame), so moving/rotating the cloud never rebuilds anything.
//
// SIZE: ED's map billboards are drawn as small star sprites, not as
// world-sized discs. The default mode is therefore screen pixels: a 2-pixel
// star stays 2 pixels in the ortho map and in an angled perspective view,
// so the same cloud cannot turn into cotton balls when the camera moves
// close. _GalaxyCloudSizeMode = 0 keeps the old world-size path for
// comparison. The pixel path uses clipPos.w so it is perspective-correct
// (the ortho path has w = 1 and falls out of the same expression).
//
// Twinkle runs on the _OpenFrontierGalaxyTime script global, NOT _Time: the
// galaxy stage boots at timeScale 0 and _Time freezes exactly there (house
// rule; the _OpenFrontierHoloTime pattern). GalaxyInstanceCloud pushes the
// global every frame from Time.unscaledTime.
//
// Sprite variants (fx.w, flat across the quad):
//   0..31       - a cell of the ED star sheet (GalaxyStarSprites.png, 8x4
//                 grid of the real star sprites Frontier's sky billboards
//                 draw; the alpha channel is the sprite shape)
//   241         - haze: analytic soft radial glow (the bright-mask atlas is
//                 not a star sprite; its ring/cloud silhouettes made cotton
//                 balls)
//   242         - brightest stars: analytic core + soft halo
//   243         - brightest stars: core + halo + diffraction cross
//   244         - retired alias, treated as 243 for old buffers
//
// _BrightTex stays bound for compatibility but is no longer sampled: the
// high-res sheet is a set of solid masks, not pre-blurred star sprites.
//
// No LightMode tag: renders as SRPDefaultUnlit under URP. Queue discipline:
// tag says Transparent; the MATERIAL carries the 2990 offset (backdrop,
// just under the 2999 nebulae and the 3000 gameplay particles).
Shader "OpenFrontier/Galaxy/Cloud" {
	Properties {
		[HDR] _CloudTint ("Cloud Tint", Color) = (1,1,1,1)
		// Both sheets are bound on GalaxyCloud.mat. Declaring them here keeps
		// the material's texture binding explicit and inspectable instead of
		// relying on HLSL-only samplers.
		_SpriteTex ("Star Sprites", 2D) = "white" {}
		_BrightTex ("Bright Sprites", 2D) = "white" {}
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		Blend SrcAlpha One
		Cull Off
		ZWrite Off

		Pass {
			HLSLPROGRAM
			#pragma target 4.5
			#pragma exclude_renderers gles
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"

			struct InstanceData
			{
				float4 posSize; // xyz = cloud-local position, w = sprite size (world units)
				float4 color;   // linear tint, a = per-point strength
				float4 fx;      // x = phase, y = twinkle amplitude, z = speed, w = variant
			};

			StructuredBuffer<InstanceData> _InstanceData;
			float4x4 _LocalToWorld;
			fixed4 _CloudTint;
			float _OpenFrontierGalaxyTime;
			float _GalaxyCloudBrightness;
			float _GalaxyCloudSize;
			float _GalaxyCloudSizeMode;      // 0 = world units, 1 = screen pixels
			float4 _GalaxyCloudPx;           // starMin, starMax, hazeMin, hazeMax
			float4 _GalaxyCloudWorld;        // starMin, starMax, hazeMin, hazeMax
			sampler2D _SpriteTex;
			sampler2D _BrightTex;

			struct appdata_cloud
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f_cloud
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;              // -1..1 across the sprite
				nointerpolation float variant : TEXCOORD1;
				fixed4 color : COLOR0;
			};

			v2f_cloud vert(appdata_cloud v, uint id : SV_InstanceID)
			{
				v2f_cloud o;
				InstanceData d = _InstanceData[id];
				float3 world = mul(_LocalToWorld, float4(d.posSize.xyz, 1.0)).xyz;
				float4 clipPos = mul(UNITY_MATRIX_VP, float4(world, 1.0));
				// clip-space billboard: push the sprite size through THIS
				// camera's projection - perspective-correct in-sky, constant
				// world size under the ortho map. Sprites whose centre is
				// behind the camera collapse instead of flipping.
				float scale = clipPos.w > 0.0 ? 1.0 : 0.0;
				if (_GalaxyCloudSizeMode > 0.5)
				{
					// Screen-space size. Star and haze have separate ranges,
					// because the buffer carries their world sizes and the
					// two tiers are three orders of magnitude apart. The
					// rare bright cells are stars, not haze: scale them from
					// the star range and give them a readable minimum glow.
					bool haze = d.fx.w > 240.5 && d.fx.w < 241.5;
					bool bright = d.fx.w > 241.5;
					float wMin = haze ? _GalaxyCloudWorld.z : _GalaxyCloudWorld.x;
					float wMax = haze ? _GalaxyCloudWorld.w : _GalaxyCloudWorld.y;
					float pMin = haze ? _GalaxyCloudPx.z : _GalaxyCloudPx.x;
					float pMax = haze ? _GalaxyCloudPx.w : _GalaxyCloudPx.y;
					float t = saturate((d.posSize.w - wMin) / max(wMax - wMin, 1e-4));
					// Bias the star tier toward the floor: most of the 40k
					// field stays 1-1.5 px, while a tail reaches the top of
					// the range. Uniform 1-2.6 px read as one flat dot
					// texture; the catalog layer supplies the bright variance.
					if (!haze) t = pow(t, 1.7);
					float px = lerp(pMin, pMax, t) * _GalaxyCloudSize;
					if (bright) px = max(px * 2.2, 5.0);
					px = max(px, 1.0);
					clipPos.xy += v.vertex.xy * px * 2.0 / _ScreenParams.xy * clipPos.w * scale;
				}
				else
				{
					clipPos.xy += v.vertex.xy * d.posSize.w * _GalaxyCloudSize * scale
						* float2(UNITY_MATRIX_P._m00, UNITY_MATRIX_P._m11);
				}
				o.pos = clipPos;
				o.uv = v.uv * 2.0 - 1.0;
				// per-star brightness oscillation; haze carries amplitude 0
				float twinkle = 1.0 + d.fx.y * sin(d.fx.z * _OpenFrontierGalaxyTime + d.fx.x);
				o.color = d.color * _CloudTint * _GalaxyCloudBrightness * twinkle;
				o.variant = d.fx.w;
				return o;
			}

			fixed4 frag(v2f_cloud i) : SV_Target
			{
				float r = length(i.uv);
				float a;
				if (i.variant > 240.5)
				{
					// Analytic sprites, not the bright-mask atlas. That atlas
					// is a set of solid silhouettes (circle, cloud, spiral,
					// ring); using it directly made core stars render as flat
					// edged discs and spirals. A soft radial core plus a rare
					// diffraction cross is much closer to ED's bright stars,
					// and it costs nothing but a few exponentials on the few
					// hundred sprites that use this path.
					float r2 = dot(i.uv, i.uv);
					float edge = saturate((1.0 - r) * 4.0);
					if (i.variant < 241.5)
					{
						// haze: wide, dim, steady. The band needs this soft
						// weld, not a ring-shaped mask.
						a = exp(-r2 * 3.0) * edge;
					}
					else
					{
						float core = exp(-r2 * 26.0);
						float halo = exp(-r2 * 7.0) * 0.55;
						float spikes = 0.0;
						if (i.variant > 242.5)
						{
							// E/W and N/S diffraction arms, tapered along
							// their length instead of a full-screen star.
							float ax = abs(i.uv.x);
							float ay = abs(i.uv.y);
							spikes = (exp(-ay * 42.0) * exp(-ax * 2.2)
								+ exp(-ax * 42.0) * exp(-ay * 2.2)) * 0.55;
						}
						a = saturate((core + halo + spikes) * edge);
					}
				}
				else                        // ED star sheet: 8x4 atlas cell
				{
					float2 cell = float2(fmod(i.variant, 8.0), floor(i.variant / 8.0));
					float2 uv = (cell + saturate(i.uv * 0.5 + 0.5)) * float2(0.125, 0.25);
					a = tex2D(_SpriteTex, uv).a;
					// the ED sheet bakes two square nebula frames into its 32
					// cells (9 and 13); fade every tile out to a circle so a
					// stacked star field can never read as a field of cards
					a *= saturate((1.0 - dot(i.uv, i.uv)) * 3.0);
				}
				a *= i.color.a;
				return fixed4(i.color.rgb, a);
			}
			ENDHLSL
		}
	}
}
