// Open Frontier: a standalone nebula placable. ONE quad, additive,
// camera-facing, world-pinned. No script and no per-frame CPU: the vertex
// stage reads the object's transform directly, so the SAME quad billboards
// correctly in BOTH galaxy cameras - the ortho map and the perspective
// in-sky view - because the offset is pushed through each camera's own
// projection matrix (the same trick the galaxy cloud uses).
//
// Authoring (scene view):
//   position  - where the nebula sits in the galaxy
//   scale X/Y - diameter in world units (max of the two is used)
//   rotation Z- rolls the card in the billboard
//   material  - _MainTex = one of the Nebula*.png cards, _Color.a = opacity
//
// Queue discipline: tag says Transparent; the MATERIAL carries 2999 (the
// house nebula/backdrop tier - above the galaxy cloud's 2990, below
// gameplay particles at 3000). Additive, so order between nebulae and stars
// is irrelevant; opaque geometry still occludes via ZTest LEqual.
Shader "OpenFrontier/Galaxy/Nebula" {
	Properties {
		_MainTex ("Nebula Card", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,0.45)
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		Blend SrcAlpha One
		Cull Off
		ZWrite Off
		ZTest LEqual

		Pass {
			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;

			struct appdata_nebula
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f_nebula
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f_nebula vert(appdata_nebula v)
			{
				v2f_nebula o;
				float4 centerCS = mul(UNITY_MATRIX_VP,
					mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0)));
				// transform scale = diameter (larger axis wins), Z rotation = roll
				float size = max(length(unity_ObjectToWorld._m00_m10_m20),
					length(unity_ObjectToWorld._m01_m11_m21));
				float roll = atan2(unity_ObjectToWorld._m10, unity_ObjectToWorld._m00);
				float sr = sin(roll);
				float cr = cos(roll);
				float2 corner = v.vertex.xy;
				corner = float2(corner.x * cr - corner.y * sr,
					corner.x * sr + corner.y * cr);
				// push through THIS camera's projection: perspective-correct in
				// the sky, constant world size under the ortho map. A quad whose
				// centre is behind the camera collapses instead of flipping.
				float visible = centerCS.w > 0.0 ? 1.0 : 0.0;
				centerCS.xy += corner * size * visible
					* float2(UNITY_MATRIX_P._m00, UNITY_MATRIX_P._m11);
				o.pos = centerCS;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag(v2f_nebula i) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, i.uv) * _Color;
				return fixed4(c.rgb, c.a);
			}
			ENDHLSL
		}
	}
}
