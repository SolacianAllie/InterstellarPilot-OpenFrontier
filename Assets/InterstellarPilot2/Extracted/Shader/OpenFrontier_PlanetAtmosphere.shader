Shader "OpenFrontier/Planet Atmosphere" {
	Properties {
		_AtmoColor ("Atmosphere Color", Vector) = (0.35,0.78,1,1)
		_SunRimColor ("Sun Rim Color", Color) = (1,0.62,0.30,1)
		_Size ("Size", Float) = 0.75
		_Falloff ("Falloff", Float) = 2
		_Transparency ("Transparency", Float) = 3
		_NightGlow ("Night Glow", Range(0, 0.3)) = 0.03
		[Header(Volume)]
		_ScaleHeight ("Scale Height", Range(0.002, 0.1)) = 0.006
		_Density ("Density", Range(0.3, 12)) = 3
		[Header(SunBleed)]
		_AtmoGlow ("Atmosphere Glow", Range(0, 1.5)) = 0.4
		_SunBleed ("Sun Bleed", Range(0, 1)) = 1
		_SunBleedPower ("Sun Bleed Power", Range(0.5, 6)) = 2
		_SunRimBoost ("Sun Rim Boost", Range(0, 2)) = 1
		[Header(EclipseHalo)]
		_EclipseFalloff ("Eclipse Falloff", Range(0.5, 8)) = 2
		_EclipseGlow ("Eclipse Glow", Range(0, 2)) = 0.5
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
			float _Size;
			float _Falloff;
			float _Transparency;
			float _NightGlow;
			float _ScaleHeight;
			float _Density;
			float _SunBleed;
			float _SunBleedPower;
			float _SunRimBoost;
			float _AtmoGlow;
			float _EclipseFalloff;
			float _EclipseGlow;
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

				// Hue is NOT driven by density. A warm push keyed to how much air
				// the ray crossed reddened the DENSE CORE of the band, which is
				// the atmosphere's own colour in every reference -- and because
				// blue lerped toward orange passes through grey, it desaturated
				// that core to beige. Warmth belongs to the sun's geometry
				// alone, which is what the next block keys off.
				fixed3 airColor = _AtmoColor.rgb;

				// day/night: the shell is concentric with the planet, so the
				// shell normal IS the planet's normal at this limb point.
				// The test is correct even though we are shading a back face.
				float ndl = dot(n, l);
				float day = smoothstep(-0.2, 0.3, ndl);

				// How far the sun's colour has reached the air here. There are
				// TWO different things and they used to be conflated into
				// one warm term, which is why the glow ringed the whole
				// planet instead of appearing where it should.
				//
				// (a) The sunrise arc. The sun is LOW at the terminator, and
				//     that is where its light has crossed the most air
				//     sideways before reaching us. ndl varies along the limb,
				//     so this concentrates in a short arc around the sun's
				//     bearing -- which is what the reference shows.
				//
				//     ndl and NOT abs(ndl): only the sun-ABOVE-the-horizon
				//     side reddens. The night side is already killed by `day`,
				//     and the absolute value would redden the anti-solar limb
				//     too.
				//
				//     And only when the sun is low FOR US. With the sun
				//     directly behind the camera the terminator coincides
				//     with the visible limb, so every limb point reads as
				//     "sun at the horizon" and the entire rim would go warm
				//     on a fully lit day side.
				float lowSun = pow(saturate(1.0 - ndl), _SunBleedPower);
				float lowForUs = saturate(0.55 - dot(l, v) * 1.5);
				float arc = lowSun * day * lowForUs * _SunBleed;

				// (b) The eclipse halo. Eclipsed means the sun is directly
				//     BEHIND the planet from where we are, so every limb
				//     point is looking through the planet at the sun and the
				//     whole silhouette lights up. This is the only case where
				//     an all-round ring is correct, and it is a global on/off:
				//     the camera is far enough away that dot(l, v) barely
				//     changes around the visible limb.
				float eclipsed = saturate(-dot(l, v) * _EclipseFalloff) * _EclipseGlow;

				// The general air along the lit limb, kept deliberately
				// modest so the warm arc reads as the sun rather than as the
				// atmosphere's own colour. At 1.0 this rings the whole lit
				// limb evenly, which is the "glow all the way round" look.
				float airGlow = day * _AtmoGlow + _NightGlow;

				// Two colours, and the sun REPLACES rather than adds. Additive
				// warm on top of full-strength air washes the two together
				// into cream, which is why they used to collapse into one
				// colour instead of reading as a bleed.
				fixed3 tint = lerp(airColor, _SunRimColor.rgb, saturate(arc))
				            * density * airGlow * lightCol;

				// A small additive core on top, for the hot line right where
				// the sun is coming up.
				tint += _SunRimColor.rgb
				      * (density * saturate(arc) * _SunRimBoost * lightCol);

				// The eclipse halo, added on its own and deliberately NOT
				// gated by `day`. This is the exception, and it is the whole
				// reason it is separate: when the sun is behind the planet it
				// is still shining on the ring of air around it, so the halo
				// traces the entire silhouette even where the surface below
				// is in darkness. Gating it by day would hide it exactly when
				// it is the only thing you can see.
				tint += _SunRimColor.rgb * (density * eclipsed * lightCol);

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