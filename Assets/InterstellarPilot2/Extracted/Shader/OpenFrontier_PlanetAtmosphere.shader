Shader "OpenFrontier/Planet Atmosphere" {
	Properties {
		_AtmoColor ("Atmosphere Color", Vector) = (0.35,0.78,1,1)
		_SunRimColor ("Sun Rim Color", Color) = (1,0.55,0.3,1)
		_SunRimPower ("Sun Rim Power", Range(1, 16)) = 2
		_Size ("Size", Float) = 0.75
		_Falloff ("Falloff", Float) = 2
		_Transparency ("Transparency", Float) = 3
		_NightGlow ("Night Glow", Range(0, 0.3)) = 0.03
		_TerminatorFalloff ("Terminator Falloff", Range(1, 16)) = 4
		[Header(Volume)]
		_ScaleHeight ("Scale Height", Range(0.004, 0.1)) = 0.012
		_Density ("Density", Range(0.3, 12)) = 3
		_DepthTint ("Depth Tint", Range(0, 1)) = 0.15
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
			float _ScaleHeight;
			float _Density;
			float _DepthTint;
			// URP publishes these every frame, for every camera. For a
			// directional light _MainLightPosition.xyz is the direction TOWARDS
			// the light -- the exact vector GetMainLight().direction hands to
			// PlanetStandard. Using it means the shell's terminator and the
			// surface's terminator are the same line by construction, instead
			// of two systems each guessing where the sun is.
			//
			// This replaces _SunDirectionWorld, set by SunBillboard.LateUpdate.
			// That bails out early when WorldCamera.Resolve() or cachedLight
			// is null, leaving the global unset -- and an unset global is not
			// the scene's real light. That is why the rim used to wrap all the
			// way round the planet while the surface was correctly in shadow.
			// Defaults to (0,0,1) when no main light is visible, so normalising
			// is always safe.
			float4 _MainLightPosition;
			// _MainLightColor is BLACK when there is no main light, so it has
			// to be guarded before being used as a tint -- see below.
			float4 _MainLightColor;

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
				// Measure the view vector from the EXPANDED position. The shell
				// is rasterised at `pos`, so reading it off input.vertex aims
				// the whole band at a spot _Size*0.1 inside the surface it is
				// actually shading, which skews the chord below.
				float3 worldPos = mul(unity_ObjectToWorld, float4(pos, 1)).xyz;
				output.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
				return output;
			}

			fixed4 frag(v2f input) : SV_Target
			{
				float3 n = normalize(input.normalDir);
				float3 v = normalize(input.viewDir);
				// Direction TOWARDS the sun -- the same one the surface is lit by.
				float3 l = normalize(_MainLightPosition.xyz);

				// Take the sun's colour as a HUE only, never as a brightness.
				// URP sets _MainLightColor to black when there is no main light,
				// and letting that through would black out the atmosphere;
				// normalising keeps a red star tinting its air red without
				// changing how bright the rim is.
				float3 lightCol = _MainLightColor.rgb;
				float lum = max(lightCol.r, max(lightCol.g, lightCol.b));
				lightCol = (lum > 1e-4) ? lightCol / lum : float3(1.0, 1.0, 1.0);

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
				// call it b/Rs below, where b is the ray's closest approach
				// to the planet's centre.
				float chord = abs(dot(n, v));
				float expand = 1.0 + _Size * 0.1;
				float sinT = sqrt(saturate(1.0 - chord * chord));       // b / shellRadius
				float bOverP = sinT * expand;                           // b / planetRadius
				float z0 = max(0.0, bOverP - 1.0);                      // perigee altitude, planet radii

				// Slant column through an exponential atmosphere.
				//
				// A real atmosphere has density rho = rho0*exp(-h/H), and what
				// you see is the integral along the view ray, not a ramp. For
				// a ray whose closest approach is b, that integral works out to
				//     tau ~ sqrt(b) * exp(-(b - Rp) / H)
				// (Bessel K1, simplified for b >> H). So brightness grows only
				// as sqrt(b) but decays EXPONENTIALLY with altitude at the
				// scale height H -- and H is the whole knob for how thick this
				// reads. Everything is in planet radii, so H is scale-free and
				// works identically on a moon and a gas giant.
				//
				// This is why the previous version looked like a decal: a
				// normalised chord is a straight ramp with no atmosphere in it.
				float tau = sqrt(bOverP) * exp(-z0 * rcp(max(1e-4, _ScaleHeight)));

				// Window. Without it, any scale height wider than the shell
				// ends in a hard circular cut, because the shell's own
				// silhouette is the only thing bounding it. This forces the
				// glow to die inside the mesh, so _ScaleHeight can be picked
				// freely. _Falloff shapes how sharply it does so.
				tau *= pow(saturate(1.0 - z0 * rcp(max(1e-4, expand - 1.0))), _Falloff);

				// Saturate rather than add tau linearly. Thick air approaches
				// the tint instead of running past it, which is what stops the
				// limb blowing out to white and is most of the "solid" feel.
				float density = 1.0 - exp(-tau * _Density);

				// Hue follows path length, but only slightly. A strong warm push here
				// fights the look: the dense core of the band is CYAN in every
				// reference, and warmth belongs to the sunrise arc alone,
				// which is driven by the sun's geometry further down. Push
				// this too far and the core desaturates to beige, because
				// blue lerped toward orange passes through grey.
				fixed3 air = lerp(_AtmoColor.rgb, _SunRimColor.rgb,
					saturate(density * _DepthTint));

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
				// dusk and nowhere else. Multiply by the density so the rim
				// still needs air to scatter through, and by the terminator
				// so it stays a band rather than washing the whole limb.
				float forwardScatter = pow(saturate(dot(v, -l)), _SunRimPower);
				float rim = density * terminator * forwardScatter * _SunRimColor.a;

				// two colours, as authored: the air itself, and the sun
				// burning through it
				fixed3 tint = air * density * (day + _NightGlow) * lightCol
				            + _SunRimColor.rgb * rim * lightCol;
				// _Transparency is the master gain. The 0.4 scale is not arbitrary:
				// it puts the core of the band at roughly (0.5, 0.85, 1.0)
				// for a _AtmoColor near the default, which is where the
				// reference limb actually sits. At 0.1 the whole shell
				// rendered as dark navy.
				return fixed4(tint * (_Transparency * 0.4), 1);
			}
			ENDHLSL
		}
	}
}