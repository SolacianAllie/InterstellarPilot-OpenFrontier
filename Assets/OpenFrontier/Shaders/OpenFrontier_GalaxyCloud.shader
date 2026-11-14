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
//   0 = star   - soft halo + hot core
//   1 = haze   - broad, very soft, steady (the band's glow, stacked)
//   2 = bright - core + a subtle 4-point diffraction cross
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
				float d2 = dot(i.uv, i.uv);
				float rim = saturate(1.0 - d2);
				float a;
				if (i.variant < 0.5)        // star: halo + hot core
				{
					a = rim * rim * 0.35 + exp(-d2 * 5.0) * 0.65;
				}
				else if (i.variant < 1.5)   // haze: broad, soft, stacked low
				{
					a = pow(rim, 1.6) * 0.85;
				}
				else                        // bright star: core + diffraction cross
				{
					float spikeH = exp(-abs(i.uv.x) * 5.0) * exp(-i.uv.y * i.uv.y * 25.0);
					float spikeV = exp(-abs(i.uv.y) * 5.0) * exp(-i.uv.x * i.uv.x * 25.0);
					a = rim * rim * 0.25 + exp(-d2 * 6.0) * 0.75 + (spikeH + spikeV) * 0.45;
				}
				a *= i.color.a;
				return fixed4(i.color.rgb, a);
			}
			ENDHLSL
		}
	}
}
