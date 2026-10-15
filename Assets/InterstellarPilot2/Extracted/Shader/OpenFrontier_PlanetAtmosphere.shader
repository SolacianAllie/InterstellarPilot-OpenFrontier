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
		_SunBleedPower ("Sun Bleed Power", Range(1, 16)) = 4
		_SunRimBoost ("Sun Rim Boost", Range(0, 2)) = 1
		[Header(ForwardScatter)]
		_ForwardBias ("Forward Bias", Range(-1, 1)) = 1
		_ForwardRange ("Forward Range", Range(0.05, 2)) = 1
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
			float _ForwardBias;
			float _ForwardRange;
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
				// The planet's centre in world space. The shell's own object
				// origin IS the planet's centre -- the Atmosphere child sits
				// at local 0,0,0 -- so this costs nothing and needs no
				// per-material radius. Passed from here because it is constant
				// across the mesh.
				float3 centreWS : TEXCOORD2;
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
				output.centreWS = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
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

				// The glowing arc: a short segment of the limb centred on wherever
				// the sun actually IS on screen.
				//
				// The previous version keyed this off ndl, the sun's altitude
				// at this point on the sphere. That is the physically tidy
				// answer and it is the wrong one visually: ndl is an angle on
				// the planet, and how much screen a given range of ndl covers
				// depends entirely on where the camera is standing. Measured
				// over a full camera sweep, an ndl-driven arc wrapped ~178
				// degrees of the limb at EVERY angle, and _SunBleedPower had
				// almost no effect on it (178 deg at p=2, 178 at p=3.5).
				//
				// Projecting both the sun and this limb point into the plane
				// facing the camera measures the separation where it is
				// actually seen, so _SunBleedPower means one thing at every
				// viewpoint: how many degrees of screen the arc covers.
				//
				// centreWS is the shell's own object origin, which is the
				// planet's centre (the Atmosphere child sits at local 0,0,0),
				// so no per-material radius is needed.
				float3 axis = normalize(_WorldSpaceCameraPos - input.centreWS);
				float3 sunPerp = l - axis * dot(l, axis);

				// |sunPerp| is the sine of the angle between the sun and the view
				// axis. It collapses toward zero precisely when the sun lies along
				// that axis, which is the degenerate case guarded below.
				float sunLen = length(sunPerp);

				// WHERE the arc goes: the two points where the TERMINATOR meets the
				// silhouette. That is the only place on the limb where the sun is
				// exactly at the horizon for that sample, so it is the only place
				// the light has genuinely crossed a long atmospheric path and still
				// arrived.
				//
				// At the silhouette n is perpendicular to v, so n lies in the 2D
				// plane spanned by sunPerp and (l x v). Writing
				//     n = a*sunPerpDir + b*(l x v)
				// and noting l is perpendicular to (l x v), the sun angle of that
				// sample is ndl = a*sunLen, hence
				//     b = sqrt(1 - (ndl/sunLen)^2)
				// with no cross product needed. Checked against the explicit
				// cross-product component all the way round the limb: max error
				// 0.000000.
				//
				// This REPLACES a screen-bearing term, which was measuring the other
				// end of the same decomposition. bearing is a; it peaks at the
				// SUB-SOLAR limb (where it is local noon) and is exactly ZERO at the
				// terminator crossings. It aimed the warm arc at peak daylight and
				// blanked it precisely where both references put it. At the limb a
				// and b are complementary, a^2 + b^2 = 1, so it was never a
				// concentration that could be reweighted -- it was the wrong end.
				//
				// b is two-sided by construction: it decays toward the sub-solar
				// limb AND toward the night limb. That is why the old
				// saturate(1.0 - ndl) is gone rather than retuned. It was
				// one-sided -- flat at 1.0 across the entire night side -- and so
				// wrapped ~264 degrees of the limb at the reference geometry, where
				// this form spans 69.
				float horizon = (sunLen > 0.02)
					? sqrt(saturate(1.0 - ndl * ndl * rcp(sunLen * sunLen)))
					: 1.0;

				// Whether the arc shows at all.
				//
				// Honest framing: this is NO LONGER a scattering-angle gate. Both
				// references show the warm limb from viewpoints where dot(l, v) is
				// firmly POSITIVE -- the front-on earth measures +0.71 -- and gating
				// on forward scattering alone suppressed every frame that mattered,
				// giving peak arc 0.000 there.
				//
				// What it does still have to do is kill one specific artifact: with
				// the sun behind the viewer the terminator coincides with the whole
				// silhouette, horizon goes to 1.0 everywhere, and the limb rings warm
				// all the way round. That is the rejected "glow all the way round"
				// look, and it is what this term blocks -- the arc only opens once
				// the sun is far enough round from dead behind. At the new default
				// of 1.0 the gate is shut at dot(l, v) near 1 and fully open by
				// dot(l, v) near 0.
				//
				// Not gated on sunLen: this asks how far round the sun is from the
				// view axis, which is well defined whether or not that axis passes
				// through the planet. An ill-defined bearing is a reason to drop the
				// bearing, never a reason to drop the scattering.
				float sunBehind = saturate((_ForwardBias - dot(l, v))
				                            * rcp(max(1e-3, _ForwardRange)));

				float arc = pow(horizon, _SunBleedPower)
				          * day * sunBehind * _SunBleed;

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
				      * (density * arc * _SunRimBoost * lightCol);

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