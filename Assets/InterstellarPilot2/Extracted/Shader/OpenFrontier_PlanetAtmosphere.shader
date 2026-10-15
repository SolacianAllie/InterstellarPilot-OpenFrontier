Shader "OpenFrontier/Planet Atmosphere" {
	Properties {
		_AtmoColor ("Atmosphere Color", Vector) = (0.35,0.78,1,1)
		_SunRimColor ("Sun Rim Color", Color) = (1,0.62,0.30,1)
		_SunRimDeep ("Sun Rim Deep Color", Color) = (1,0.30,0.12,1)
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
		_SunBleedPower ("Sun Bleed Power", Range(1, 16)) = 2
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
			fixed4 _SunRimDeep;
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
				// the VIEW ray crossed reddened the DENSE CORE of the band,
				// which is the atmosphere's own colour in every reference --
				// and because blue lerped toward orange passes through grey, it
				// desaturated that core to beige.
				//
				// airColor is therefore not a constant either: it is resolved
				// further down, once the sun's geometry is known. The air's own
				// glow is scattered sunlight, and that sunlight crossed the
				// sun's air mass before it had anything to scatter -- about one
				// air mass at the sub-solar limb, tens at the terminator limb.
				// Leaving it at a flat _AtmoColor is what put a blue rim around
				// the night side, in the band where the sun is low and the arc
				// has already faded: maximum air mass, minimum arc, and an air
				// colour still claiming to be clear cyan. That is the blue the
				// night-side render showed, and it is the one part of the
				// atmosphere that cannot be right -- no amount of light through
				// that much air comes out blue.

				// day/night: the shell is concentric with the planet, so the
				// shell normal IS the planet's normal at this limb point.
				// The test is correct even though we are shading a back face.
				float ndl = dot(n, l);
				// TWO windows, because "lit" means opposite things for the two
				// terms that used to share one.
				//
				// The air's own glow must NOT wrap. smoothstep(-0.2, 0.3) left
				// it alive 11.5 degrees past the terminator, so the cyan shell
				// ringed the planet into the night side. That is the
				// atmosphere wrapping round the planet, which is wrong here
				// even though the clouds above it legitimately do it
				// (_CloudWrap on the surface shader) -- the clouds scatter
				// forward and sit on top of the surface, whereas what glows
				// out here is light that came from the far side of the sphere
				// and has the planet itself in the way. Its window therefore
				// stops at the terminator: generous toward the sun, zero at and
				// beyond ndl = 0.
				float dayAir = smoothstep(0.0, 0.35, ndl);


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
				float3 nPerp = n - axis * dot(n, axis);

				// |sunPerp| is the sine of the angle between the sun and the view
				// axis, and it is what the bearing divides by. It collapses toward
				// zero precisely when the sun lies along the view axis. 0.02 is
				// about 1 degree off-axis, where the azimuth stops being a
				// direction and becomes a rounding error.
				float sunLen = length(sunPerp);
				float bearing = (sunLen > 0.02)
					? saturate(dot(nPerp, sunPerp) / (length(nPerp) * sunLen))
					: 0.0;

				// BRIGHTNESS follows the sun's screen bearing -- 1 where this limb
				// point faces the sun most directly.
				//
				// This has been wrong twice and the sequence is worth recording.
				// It was first keyed on ndl, the sun's ALTITUDE at this point on
				// the sphere, which is the physically tidy answer and the wrong one
				// visually: ndl is an angle on the sphere, so where it peaks on
				// SCREEN moves as you orbit, and it wrapped most of the limb at
				// every camera angle.
				//
				// It was then replaced by the complementary component of the same
				// decomposition, on the argument that the arc belongs at the
				// terminator-limb crossings. That was half right. The crossings
				// are where the air mass is greatest, but three night-side
				// references -- sun cresting the limb, a crescent at upper left, a
				// bright ring around a dark planet -- are all brightest at the
				// SUN'S BEARING, which is a quarter of the way round from the
				// crossings. Those two places are 90 degrees apart on the limb and
				// driving brightness from the crossings put the highlight exactly
				// where the references show it dimmest.
				//
				// At the silhouette ndl = bearing * sunLen, so bearing and ndl are
				// the same axis rescaled -- which is why this is a rescale and not
				// a reversal: ndl peaks with the sun's bearing on screen too. What
				// was wrong was the saturate(1 - ndl) taper, which dimmed the peak
				// to 0.23 of its value at sunLen 0.77.
				//
				// The taper is gone entirely. dayArc already keeps the arc off the
				// unlit limb, and with it gone the rim spans about 90 degrees
				// either side of the bearing at _SunBleedPower 2, which is where
				// all three references have it fading out.
				//
				// HUE follows air mass, which is a DIFFERENT function again and was
				// missing entirely -- one colour cannot express a sunset. Real air
				// mass goes as 1/cos(zenith) = 1/ndl, so normalised to the bearing
				// it is 1/bearing - 1. That shape matters: most of the reddening
				// happens within the first 40 degrees off the bearing and then
				// saturates, which is why the references read as a short bright
				// orange crest bleeding into a long deep red tail rather than a
				// slow even gradient.
				float airMass = saturate(rcp(max(0.05, bearing)) - 1.0);

				// Whether the arc shows at all.
				//
				// Honest framing: this is NO LONGER a scattering-angle gate. Both
				// day-side and night-side references show the warm limb where
				// dot(l, v) is positive, and a forward-scattering gate suppressed
				// every frame that mattered.
				//
				// What it still has to do is kill one specific artifact: with the
				// sun behind the viewer the terminator coincides with the whole
				// silhouette, and the limb would ring warm all the way round. That
				// is the rejected "glow all the way round" look, and it is what
				// this term blocks.
				float sunBehind = saturate((_ForwardBias - dot(l, v))
				                            * rcp(max(1e-3, _ForwardRange)));

				// No day window on this one. It was tried and it is wrong: the sunset
				// glow is on the night side of the terminator by definition, so a
				// gate that reaches zero below ndl = -0.2 deletes the entire
				// night-side rim -- which is the case most of the references are.
				// The gate that matters is sunBehind above. bearing already keeps
				// the arc off the unlit limb, since it falls to zero there anyway.
				float arc = pow(bearing, _SunBleedPower)
				          * sunBehind * _SunBleed;

				// The general air along the lit limb, kept deliberately
				// modest so the warm arc reads as the sun rather than as the
				// atmosphere's own colour. At 1.0 this rings the whole lit
				// limb evenly, which is the "glow all the way round" look.
				float airGlow = dayAir * _AtmoGlow + _NightGlow;

				// Two colours, and the sun REPLACES rather than adds. Additive
				// warm on top of full-strength air washes the two together
				// into cream, which is why they used to collapse into one
				// colour instead of reading as a bleed.
				// TWO sunset colours, lerped by air mass. Looking straight down the
				// sun's rays the path is short and the light barely changes; at the
				// horizon it has crossed dozens of air masses and arrives deep red.
				// A single colour cannot span that -- and lerping one colour from
				// the air's cyan made the far end of the rim go grey-blue, because
				// cyan has a high green and blue to fall back to. The references
				// show it staying warm as it dims.
				fixed3 sunset = lerp(_SunRimColor.rgb, _SunRimDeep.rgb, airMass);

				// The air's own colour, reddened by the SAME air mass. This is the
				// term that decides what the rim looks like where the arc has faded,
				// and previously it was a flat _AtmoColor -- so the night-side limb
				// came out blue exactly where the sun was lowest.
				fixed3 airColor = lerp(_AtmoColor.rgb, sunset, airMass);

				fixed3 tint = lerp(airColor, sunset, saturate(arc))
				            * density * airGlow * lightCol;

				// A small additive core on top, for the hot line right where
				// the sun is coming up.
				tint += sunset
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