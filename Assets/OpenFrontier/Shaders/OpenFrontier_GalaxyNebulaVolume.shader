// Open Frontier: true 3D nebula placables. A box mesh is only the volume's
// bounding proxy; the fragment ray-marches the real ED nebula volume
// (ED's 64^3/128^3/256^3 RGBA32 Texture3D). RGB is the emission colour, A is
// optical density.
//
// One box serves every camera: perspective rays start at the eye, orthographic
// rays run along the camera forward - the standard ortho map view and the
// perspective sky view both see the same volume. The ray is intersected
// analytically with the OCCUPIED part of the cube (_BoundsMin/_BoundsMax,
// derived from the alpha at upload), so a camera inside the nebula still works
// and the empty padding around the cloud costs nothing.
//
// All shaping lives in the bake (tools/galaxy/nebula_volume_bake.py): ED's own
// volume data for every prefab, including its named Barnard's Loop and
// Pleiades volumes, which bake raw (no guard, no normalisation). The cards
// only supply colour LUTs for the five generic brights and flat fallbacks.
// The shader carries no shaping of its own.
//
// Cost: _Steps samples per covered pixel. GalaxyNebulaVolume seeds the native
// 32/64/128 (two texels per step at every rung) and GalaxyNebulaField lowers
// it by on-screen size - a phone's 64^3 volume tops out at 24 steps and a
// distant smudge at 6. Two early-outs keep the loop from paying for work that
// cannot be seen: a ray that misses the occupied box is discarded before the
// march, and the loop stops once transmittance drops below 0.4%.
//
// Blend is premultiplied so dense cores dim the stars behind them; queue 2999
// keeps it under the galaxy cloud (2990) and over the gameplay particles
// (3000). No LightMode tag: renders as SRPDefaultUnlit under URP.
Shader "OpenFrontier/Galaxy/NebulaVolume" {
	Properties {
		[HDR] _Tint ("Tint", Color) = (1,1,1,1)
		_Brightness ("Emission", Float) = 3
		_Density ("Density", Float) = 4
		_Absorption ("Absorption", Float) = 1
		_Steps ("Raymarch Steps", Float) = 32
		_BoundsMin ("Occupied Min", Vector) = (-0.5,-0.5,-0.5,0)
		_BoundsMax ("Occupied Max", Vector) = (0.5,0.5,0.5,0)
		_Volume ("Volume", 3D) = "" {}
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Blend One OneMinusSrcAlpha
		Cull Front
		ZWrite Off

		Pass {
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler3D _Volume;
			float4 _Tint;
			float _Brightness;
			float _Density;
			float _Absorption;
			float _Steps;
			float4 _BoundsMin;
			float4 _BoundsMax;

			struct appdata { float4 vertex : POSITION; };
			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
			};

			v2f vert(appdata v)
			{
				v2f o;
				float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.worldPos = world;
				o.pos = mul(UNITY_MATRIX_VP, float4(world, 1.0));
				return o;
			}

			float ign12(float2 p)
			{
				// interleaved gradient noise (Jimenez 14): blue-noise-like
				// dither, one instruction, no texture. Static per pixel - no
				// TAA needed, and the grain does not crawl while the camera
				// moves.
				return frac(52.9829189 * frac(dot(p, float2(0.06711056, 0.00583715))));
			}

			float4 frag(v2f i) : SV_Target
			{
				bool ortho = abs(UNITY_MATRIX_P._m33 - 1.0) < 0.5;
				// camera space is OpenGL-style (-Z forward), so the inverse
				// view's third column points backwards; negate for the ray
				float3 dir = ortho
					? normalize(-unity_CameraToWorld._m02_m12_m22)
					: normalize(i.worldPos - _WorldSpaceCameraPos);
				// start ortho rays on the camera plane (not behind the box):
				// an inside-the-nebula map view must not add gas behind the eye
				float3 origin = ortho
					? i.worldPos - dir * dot(i.worldPos - _WorldSpaceCameraPos, dir)
					: _WorldSpaceCameraPos;

				float3 ro = mul(unity_WorldToObject, float4(origin, 1.0)).xyz;
				float3 rd = normalize(mul((float3x3)unity_WorldToObject, dir));
				rd = sign(rd + 1e-9) * max(abs(rd), 1e-6);

				// occupied box, not the full cube: ED volumes carry a lot of
				// empty padding, and a miss here discards before the march
				float3 t0 = (_BoundsMin.xyz - ro) / rd;
				float3 t1 = (_BoundsMax.xyz - ro) / rd;
				float3 tmin = min(t0, t1);
				float3 tmax = max(t0, t1);
				float tNear = max(max(tmin.x, tmin.y), max(tmin.z, 0.0));
				float tFar = min(min(tmax.x, tmax.y), tmax.z);
				if (tFar <= tNear) discard;

				int steps = (int)clamp(_Steps, 4.0, 128.0);
				float stepLen = (tFar - tNear) / (float)steps;
				float jitter = ign12(i.pos.xy) * stepLen;
				float3 p = ro + rd * (tNear + jitter);

				// half precision on mobile: the volume is 8-bit RGBA and this
				// is a single-pass transparent accumulation, not a light buffer.
				// Emission stays float - it is the colour the eye reads.
				half transmittance = 1.0;
				float3 emission = 0.0;
				[loop]
				for (int s = 0; s < 128; s++)
				{
					// dense cores reach 0.4% transmittance long before the
					// march ends; everything past that is invisible
					if (s >= steps || transmittance < 0.004) break;

					float3 uvw = saturate(p + 0.5);
					half4 voxel = tex3Dlod(_Volume, float4(uvw, 0.0));
					half density = voxel.a * (half)_Density;
					emission += transmittance * voxel.rgb * _Tint.rgb * _Brightness
						* density * stepLen;
					transmittance *= (half)exp(-density * _Absorption * stepLen);
					p += rd * stepLen;
				}
				return float4(emission, 1.0 - transmittance);
			}
			ENDHLSL
		}
	}
}
