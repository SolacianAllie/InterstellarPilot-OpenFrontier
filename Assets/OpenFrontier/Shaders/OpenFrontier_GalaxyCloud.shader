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
// Twinkle runs on the _OpenFrontierGalaxyTime script global, NOT _Time: the
// galaxy stage boots at timeScale 0 and _Time freezes exactly there (house
// rule; the _OpenFrontierHoloTime pattern). GalaxyInstanceCloud pushes the
// global every frame from Time.unscaledTime.
//
// Sprite variants (fx.w, flat across the quad):
//   0..31       - a cell of the ED star sheet (GalaxyStarSprites.png, 8x4
//                 grid of the real star sprites Frontier's sky billboards
//                 draw; the alpha channel is the sprite shape)
//   241         - haze: the soft-glow cell of the ED lens-flare sheet
//   242         - brightest stars: the ED radial flare
//   243         - brightest stars: the ED diffraction-spike star
//   244         - rare: the ED spiral star
//
// No LightMode tag: renders as SRPDefaultUnlit under URP. Queue discipline:
// tag says Transparent; the MATERIAL carries the 2990 offset (backdrop,
// just under the 2999 nebulae and the 3000 gameplay particles).
Shader "OpenFrontier/Galaxy/Cloud" {
	Properties {
		[HDR] _CloudTint ("Cloud Tint", Color) = (1,1,1,1)
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
				clipPos.xy += v.vertex.xy * d.posSize.w * scale
					* float2(UNITY_MATRIX_P._m00, UNITY_MATRIX_P._m11);
				o.pos = clipPos;
				o.uv = v.uv * 2.0 - 1.0;
				// per-star brightness oscillation; haze carries amplitude 0
				float twinkle = 1.0 + d.fx.y * sin(d.fx.z * _OpenFrontierGalaxyTime + d.fx.x);
				o.color = d.color * _CloudTint * twinkle;
				o.variant = d.fx.w;
				return o;
			}

			fixed4 frag(v2f_cloud i) : SV_Target
			{
				float a;
				if (i.variant > 240.5)
				{
					// high-res ED lens-flare cells (GalaxyBrightSprites.png,
					// 4 across): 241 haze -> soft glow (3), 242 flare (0),
					// 243 spike (1), 244 spiral (2)
					float cell = i.variant > 241.5 ? (i.variant - 242.0) : 3.0;
					cell = clamp(cell, 0.0, 3.0);
					float2 uv = (float2(cell, 0.0) + saturate(i.uv * 0.5 + 0.5))
						* float2(0.25, 1.0);
					a = tex2D(_BrightTex, uv).a;
				}
				else                        // ED star sheet: 8x4 atlas cell
				{
					float2 cell = float2(fmod(i.variant, 8.0), floor(i.variant / 8.0));
					float2 uv = (cell + saturate(i.uv * 0.5 + 0.5)) * float2(0.125, 0.25);
					a = tex2D(_SpriteTex, uv).a;
				}
				a *= i.color.a;
				return fixed4(i.color.rgb, a);
			}
			ENDHLSL
		}
	}
}
