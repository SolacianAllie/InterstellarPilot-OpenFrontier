Shader "OpenFrontier/Planet Atmosphere" {
	Properties {
		_AtmoColor ("Atmosphere Color", Vector) = (0.5,0.5,1,1)
		_SunRimColor ("Sun Rim Color", Color) = (1,0.55,0.3,1)
		_SunRimPower ("Sun Rim Power", Range(1, 16)) = 2
		_Size ("Size", Float) = 0.75
		_Falloff ("Falloff", Float) = 2
		_Transparency ("Transparency", Float) = 3
		_NightGlow ("Night Glow", Range(0, 0.3)) = 0.03
		_TerminatorFalloff ("Terminator Falloff", Range(1, 16)) = 4
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		ZWrite Off
		Cull Front
		Lighting Off
		Fog { Mode Off }
		Blend SrcAlpha One

		Pass {
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _AtmoColor;
			fixed4 _SunRimColor;
			float _SunRimPower;
			float _Size;
			float _Falloff;
			float _Transparency;
			float _NightGlow;
			float _TerminatorFalloff;
			// Set per frame by SunBillboard: world direction TOWARDS the sun.
			float4 _SunDirectionWorld;

			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};
			struct v2f {
				float4 pos : SV_POSITION;
				float3 normalDir : TEXCOORD0;
				float3 viewDir : TEXCOORD1;
			};

			v2f vert(appdata input)
			{
				v2f output;
				// expand the shell slightly
				float3 pos = input.vertex.xyz * (1.0 + _Size * 0.1);
				output.pos = UnityObjectToClipPos(float4(pos, 1));
				output.normalDir = UnityObjectToWorldNormal(input.normal);
				float3 worldPos = mul(unity_ObjectToWorld, input.vertex).xyz;
				output.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
				return output;
			}

			fixed4 frag(v2f input) : SV_Target
			{
				float3 n = normalize(input.normalDir);
				float3 v = normalize(input.viewDir);
				float3 l = (dot(_SunDirectionWorld.xyz, _SunDirectionWorld.xyz) < 0.0001) ? float3(0, 1, 0) : normalize(_SunDirectionWorld.xyz);

				// The radial coordinate that actually matters.
				//
				// Cull Front draws the shell's BACK faces, so the outward
				// normal points AWAY from the camera and dot(n, v) is
				// negative at every on-screen pixel. Feeding that straight
				// into saturate() clamped it to 0, which is why this shell
				// rendered pure black and looked like it was missing: it
				// was not faint, it was emitting exactly nothing.
				//
				// abs(dot(n, v)) is the cosine of the angle between the
				// shell normal and the view ray, and that is exactly the
				// chord the ray cuts through the shell as a fraction of
				// the shell radius: 1 through the centre, 0 at the limb.
				// Dividing by the chord at the planet's own silhouette
				// turns it into a profile that is 1 at the planet's edge
				// and 0 at the shell's edge, so the glow hugs the horizon
				// and thins outward the way real air does.
				//
				// The silhouette angle is free: the vertex stage expands the
				// shell by (1 + _Size * 0.1) about the planet's centre, so
				// the planet subtends asin(1 / (1 + _Size * 0.1)) whatever
				// its radius is. No per-material radius to keep in sync with
				// the mesh, and it works at any scale.
				float chord = abs(dot(n, v));
				float expand = 1.0 + _Size * 0.1;
				float silhouette = sqrt(max(0.0, 1.0 - rcp(expand * expand)));
				float profile = pow(saturate(chord / max(1e-4, silhouette)), _Falloff);

				// day/night: the shell is concentric with the planet, so the
				// shell normal IS the planet's normal at this limb point.
				// The test is correct even though we are shading a back face.
				float ndl = dot(n, l);
				float day = smoothstep(-0.2, 0.3, ndl);

				// sunset band hugging the terminator line
				float terminator = pow(saturate(1.0 - abs(ndl)), _TerminatorFalloff);

				// Sunrise/sunset arc. dot(v, -l) is high only when the sun
				// is on the far side of this limb point from the camera --
				// i.e. when the sun is beyond the planet, not behind you --
				// so the hot rim appears on the sunward limb at dawn and
				// dusk and nowhere else. Multiply by the profile so the rim
				// still needs air to scatter through, and by the terminator
				// so it stays a band rather than washing the whole limb.
				float forwardScatter = pow(saturate(dot(v, -l)), _SunRimPower);
				float rim = profile * terminator * forwardScatter * _SunRimColor.a;

				// two colours, as authored: the air itself, and the sun
				// burning through it
				fixed3 tint = _AtmoColor.rgb * profile * (day + _NightGlow)
				            + _SunRimColor.rgb * rim;
				return fixed4(tint * (_Transparency * 0.1), 1);
			}
			ENDHLSL
		}
	}
}