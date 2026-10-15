Shader "OpenFrontier/Planet Atmosphere" {
	// ------------------------------------------------------------------------
	// A single-scattering limb atmosphere.
	//
	// What is physics here, and is not, is worth stating up front because the
	// two were tangled together in the version this replaces.
	//
	// FROM FIRST PRINCIPLES:
	//
	//  * The column the view ray crosses. Density falls off exponentially with
	//    altitude, so the slant integral is a Bessel K1, not a ramp.
	//  * The air mass along the SUN's ray to each point. The plane-parallel
	//    secant 1/cos(zeta) diverges as the sun reaches the local horizon. It
	//    does not diverge in reality: the atmosphere is curved and finite, and
	//    the longest possible path is the grazing ray. In an exponential
	//    atmosphere that path has optical depth sqrt(2*pi*R/H) at ground level
	//    -- 69 for Earth at H = 8.5 km, R = 6371 km -- falling as exp(-h/H) as
	//    the grazing altitude rises. _ScaleHeight alone sets it, so it is
	//    scale-free and identical on a moon and a gas giant.
	//  * Extinction. Rayleigh optical depth goes as 1/lambda^4, so the
	//    cross-section at 630/550/465 nm is 0.581 / 1.000 / 1.957 relative to
	//    green. Blue is scattered 3.4x more than red: that is the blue sky,
	//    and it is also why a sunset is red, because the transmitted sunlight
	//    keeps exactly what the scatterer took out. _Rayleigh is the vertical
	//    optical depth at 550 nm for one air mass (Earth 0.098).
	//  * Aerosols. Mie extinction is essentially wavelength-independent, so it
	//    dims without shifting hue, and its phase function is strongly
	//    forward-peaked (Henyey-Greenstein, _MieG).
	//
	// NOT physics, kept because the references need it, and switchable:
	//
	//  * The bearing lobe, _SunBleedPower + _SunBleedTighten. Air-mass
	//    extinction does NOT narrow the rim as the sun goes behind -- measured,
	//    the visible span only moves from 87 to 72 degrees over the whole range,
	//    because the crest-to-terminator contrast falls just 28x to 10.6x. A
	//    real eclipse seen from space is a dim red RING, not a narrow crescent.
	//    The converging crescent is a compositional choice, so it lives here
	//    under its own name, out of the way of the physics. Set
	//    _SunBleedTighten to 0 and the rim keeps its natural width at every sun
	//    angle, which is the honest picture.
	// ------------------------------------------------------------------------
	Properties {
		// Scattering. SIGMA_R is (550/lambda)^4 at 630/550/465 nm, hardcoded
		// in the fragment stage because it is a constant of the atmosphere and
		// not something a material should be able to change.
		[Header(Scattering)]
		// Vertical optical depth at 550 nm for one air mass. Earth is 0.098.
		// This one number sets the whole sunset ramp: at 1 it is barely warm,
		// at 10 it is orange, at 30 it is deep red, and past 60 it is gone.
		_Rayleigh ("Rayleigh Depth", Range(0.005, 0.4)) = 0.1
		// Grey (wavelength-independent) aerosol extinction per air mass.
		_Aerosol ("Aerosol Depth", Range(0, 0.2)) = 0.02
		// Aerosol asymmetry. 0 is isotropic, 0.76+ is a strong forward halo.
		_MieG ("Aerosol Asymmetry", Range(0, 0.95)) = 0.76
		// How fast single scattering's blue selection washes out to neutral as
		// the path thickens. Multiple scattering saturates, which is why a deep
		// sunset is red rather than invisible-blue.
		// 0.10, not 0.25. This is how much single scattering's blue
		// selection survives before multiple scattering washes it out, and
		// 0.25 was flattening the sun-side limb to near-white. Measured at
		// the crest, side-on: B/R 1.36 at 0.25, 1.44 at 0.15, 1.48 at 0.10.
		// Safe to lower because the terminator saturates either way -- the
		// flatten term is saturate(airMass * _ScatterFlat) and airMass is
		// 11.5 there, so anything above 1/11.5 = 0.087 is identical. The
		// terminator holds R/B 16.27 down to 0.10 and only starts lifting
		// at 0.06 (R/B 13.42).
		_ScatterFlat ("Scatter Flattening", Range(0, 2)) = 0.1
		_AtmoGlow ("Scattering Coefficient", Range(0, 4)) = 0.95
		// Hue only, applied to the sun's TRANSMITTED beam and not to the
		// Rayleigh selection below, so a red star reddens its air without
		// turning the day limb red. Default white leaves the physics alone.
		// Kept under its old name because Atmosphere-Brown-Medium.mat
		// serialises a value for it (1, 0.331, 0.298); renaming would orphan
		// that and the material would silently lose its hue.
		_SunRimColor ("Sun Beam Hue", Color) = (1,1,1,1)
		// How quickly that hue takes effect along the sun's path. Small
		// values confine it to the deep end of the ramp so it cannot
		// touch the Rayleigh blue at the crest.
		// 1.0 or more applies it everywhere, which is what used to turn
		// the sun-side limb orange.
		_SunRimReach ("Sun Beam Reach", Range(0.01, 2)) = 0.12

		[Header(Volume)]
		// Scale height in PLANET RADII, so it is scale-free. Also sets the
		// grazing air mass, via sqrt(2*pi/_ScaleHeight).
		_ScaleHeight ("Scale Height", Range(0.002, 0.1)) = 0.006
		_Density ("Density", Range(0.3, 12)) = 3
		_Falloff ("Falloff", Float) = 2
		_Size ("Size", Float) = 0.75

		[Header(BearingLobe)]
		// NOT physics. See the header comment. Exponent of the lobe centred on
		// the sun's screen bearing, at which the limb is brightest.
		// Fraction of the scattered light pulled onto the sun's bearing.
		// 0 is pure physics, 1 is all of it on the bearing.
		//
		// This one dial is the only thing trading the twilight band against
		// the converging crescent, and they are the two ends of a single
		// limb profile 90 degrees apart -- the crescent sits on the sun's
		// bearing, the terminator is always at +/-90 from it -- so the trade
		// is inherent rather than a tuning accident. Measured:
		//
		//   _SunBleed   span ends @10% of crest, sun off axis     band @85
		//                    90    45    31    25    20    16    12   45deg  16deg
		//       0.0      87   86   85   84   83   82   80    19.9%   5.3%
		//       0.6      86   84   82   81   80   78   75     8.6%   2.6%
		//       0.8      83   80   78   76   73   70   67     4.9%   1.8%
		//       1.0      62   52   36   30   27   25   24     1.1%   0.9%
		//
		// The span only starts moving above about 0.7, and the terminator
		// band is what pays for it. 0.75 is the compromise shipped here;
		// 0.0 is the honest picture and keeps the band at its brightest.
		_SunBleed ("Sun Bleed", Range(0, 1)) = 0.75
		_SunBleedPower ("Sun Bleed Power", Range(0, 16)) = 3
		// Added to the exponent as the sun goes behind, which is what makes the
		// rim's two ends converge on the sun. 0 disables it entirely and leaves
		// the width to the extinction, which does not converge.
		_SunBleedTighten ("Sun Bleed Tighten", Range(0, 64)) = 28
		// Power applied to how far the sun is behind, so the tightening
		// concentrates near alignment instead of starting at the limb.
		_SunBleedShape ("Sun Bleed Shape", Range(1, 12)) = 8
		// A small additive core, for the hot line right where the sun is
		// coming up.
		_SunRimBoost ("Sun Rim Boost", Range(0, 2)) = 1
		// How far past the terminator the sun's light still reaches. Softens
		// the day/night edge of the shell, which would otherwise be a hard
		// circle where cosZ crosses zero.
		_TwilightReach ("Twilight Reach", Range(0.01, 0.9)) = 0.45

		[Header(Airglow)]
		// Chemical emission in the upper atmosphere. It is not a sunset and
		// nothing about it has crossed a terminator, so it takes the air's own
		// colour. Faded out where the sky is already sunlit, because airglow is
		// not visible against a daylit sky -- and because being cyan, ungated,
		// it cancels the sunset colour rather than adding to it.
		_AtmoColor ("Airglow Color", Vector) = (0.35,0.78,1,1)
		_NightGlow ("Night Glow", Range(0, 0.3)) = 0.03

		[Header(Output)]
		_Transparency ("Transparency", Float) = 3
		// Blocks the forward-scattered rim when the sun is behind the viewer,
		// where the terminator coincides with the whole silhouette and the limb
		// would ring warm all the way round.
		_ForwardBias ("Forward Bias", Range(-1, 2)) = 1
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
			float _SunRimReach;
			float _Rayleigh;
			float _Aerosol;
			float _MieG;
			float _ScatterFlat;
			float _AtmoGlow;
			float _ScaleHeight;
			float _Density;
			float _Falloff;
			float _Size;
			float _SunBleed;
			float _SunBleedPower;
			float _SunBleedTighten;
			float _SunBleedShape;
			float _SunRimBoost;
			float _TwilightReach;
			float _NightGlow;
			float _Transparency;
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
				float lc = max(lightCol.r, max(lightCol.g, lightCol.b));
				lightCol = (lc > 1e-4) ? lightCol / lc : float3(1.0, 1.0, 1.0);

				// ----------------------------------------------------------------
				// 1. HOW MUCH AIR THE VIEW RAY CROSSED
				//
				// Cull Front draws the shell's BACK faces, so the outward normal
				// points AWAY from the camera and dot(n, v) is negative at every
				// on-screen pixel. abs() of it is the cosine of the angle
				// between the shell normal and the view ray, which is exactly
				// the chord the ray cuts through the shell as a fraction of the
				// shell radius: 1 through the centre, 0 at the limb. Call it
				// b/Rs below, where b is the ray's closest approach to the
				// planet's centre.
				//
				// A real atmosphere has density rho = rho0*exp(-h/H), and what
				// you see is the integral along the view ray, not a ramp. For a
				// ray whose closest approach is b, that integral works out to
				//     tau ~ sqrt(b) * exp(-(b - Rp) / H)
				// (Bessel K1, simplified for b >> H). So brightness grows only as
				// sqrt(b) but decays EXPONENTIALLY with altitude at the scale
				// height H -- and H is the whole knob for how thick this reads.
				// Everything is in planet radii, so H is scale-free and works
				// identically on a moon and a gas giant.
				//
				// This is why the previous version looked like a decal: a
				// normalised chord is a straight ramp with no atmosphere in it.
				// ----------------------------------------------------------------
				float chord = abs(dot(n, v));
				float expand = 1.0 + _Size * 0.1;
				float sinT = sqrt(saturate(1.0 - chord * chord));   // b / shellRadius
				float bOverP = sinT * expand;                        // b / planetRadius
				float z0 = max(0.0, bOverP - 1.0);                   // perigee altitude
				float invH = rcp(max(1e-4, _ScaleHeight));

				float tau = sqrt(bOverP) * exp(-z0 * invH);

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

				// ----------------------------------------------------------------
				// 2. THE SUN'S AIR MASS AT THIS POINT  -- the whole model
				//
				// cosZ is the cosine of the sun's ZENITH angle at this fragment:
				// it is the sun's altitude above the local horizon. At the
				// silhouette n is perpendicular to the view axis, so
				//     cosZ = cos(theta) * sin(alpha)
				// with theta the angle along the limb from the sun's screen
				// bearing and alpha the sun's angle off the view axis. That is
				// why the air mass, and so the rim's shape and colour, depend on
				// the geometry without anything being told about it.
				//
				// The secant 1/cosZ diverges at the horizon, which is wrong. The
				// longest real path is the grazing ray, whose optical depth in
				// an exponential atmosphere is sqrt(2*pi*R/H)*exp(-h/H) -- 69
				// for Earth at ground level. So the secant is CLAMPED to it:
				//
				//     airMass = min(1/cosZ, grazing)
				//
				// which is continuous (they meet where the secant equals the
				// grazing value) and bounded. Clamping is not a fudge: it is the
				// difference between "the sun's ray just reaches the horizon" and
				// "the sun's ray goes round the planet to reach it".
				// ----------------------------------------------------------------
				float cosZ = dot(n, l);
				float grazing = sqrt(2.0 * UNITY_PI * rcp(max(1e-4, _ScaleHeight)))
				              * exp(-z0 * invH);
				float secant = rcp(max(cosZ, 1e-3));
				float airMass = min(secant, grazing);

				// Soft day/night edge. cosZ crossing zero is where the sun sets,
				// and on its own that is a hard circle on the shell. The reach
				// is in cosine units: 0.45 puts the edge about 27 degrees past
				// the terminator. This is the twilight wedge, and it needs no
				// window of its own -- the air mass above is already maximal and
				// maximally reddened there, so the band arrives on its own and
				// vanishes just past the terminator, which is where it should.
				float dayLit = smoothstep(-_TwilightReach, _TwilightReach * 0.25, cosZ);

				// ----------------------------------------------------------------
				// 3. EXTINCTION OF THE SUN'S RAY  -- the entire sunset ramp
				//
				// Rayleigh optical depth scales as 1/lambda^4, so the scattering
				// cross-section at 630 / 550 / 465 nm, relative to green, is
				// (550/lambda)^4 = 0.581 / 1.000 / 1.957.
				//
				// One number then spans the whole ramp, with no colour ramps to
				// tune and nothing to keep in sync:
				//
				//     airMass   transmit              reads as
				//          1    0.92 0.89 0.81        pale warm white
				//          2    0.86 0.79 0.65        warm white
				//         10    0.56 0.37 0.14        orange
				//         30    0.08 0.02 0.001       deep red
				//
				// Aerosol extinction is very nearly wavelength-independent, so
				// it is a flat add: it dims without shifting hue, which is what
				// keeps a hazy atmosphere from going blood red as fast.
				// ----------------------------------------------------------------
				const float3 SIGMA_R = float3(0.581, 1.000, 1.957);
				float3 sigma = SIGMA_R * _Rayleigh + _Aerosol;
				float3 transmit = exp(-sigma * airMass);
				// Per-material hue on the beam, normalised so it cannot change
				// how bright the rim is.
				//
				// WEIGHTED BY THE AIR MASS, and this is the fix for the limb
				// coming out orange on the sun's side. Applied unconditionally
				// it reaches all the way back to the crest, where the path is
				// short and the light is Rayleigh blue -- and blue is exactly
				// what it cancels. Measured at the crest, side-on:
				//
				//     _SunRimColor white       B/R 1.36   pale blue
				//     _SunRimColor (1,.33,.30) B/R 0.40   ORANGE
				//
				// and Atmosphere-Brown-Medium.mat is the only material in the
				// set that overrides it. A 'rim colour' was always meant for
				// the deep end of the ramp, so weight it by the path: at the
				// crest (air mass 1) the reach leaves 12% of the tint, and by
				// the terminator (air mass 11.5) it is saturated. Crest back
				// to B/R 1.35 on that material, and the terminator deepens
				// rather than lifting: R/B 288 against 86 without it.
				float3 beamHue = _SunRimColor.rgb
				              / max(1e-4, max(_SunRimColor.r,
				                        max(_SunRimColor.g, _SunRimColor.b)));
				float beamAmt = saturate(airMass * _SunRimReach);
				// Explicit float3, not a 1.0.xxx swizzle on a scalar literal -- that
				// compiles on desktop and is not worth finding out about on a
				// mobile target.
				transmit *= lerp(float3(1.0, 1.0, 1.0), beamHue, beamAmt);

				// ----------------------------------------------------------------
				// 4. WHICH WAY THE LIGHT SCATTERED
				//
				// The scattering angle is between the light arriving from the
				// sun (-l) and the light leaving for the camera (-v), so
				// cos(psi) = dot(l, v).
				//
				// Rayleigh is 3/(16*pi) * (1 + cos^2), which integrates to
				// 1/(4*pi) -- so normalised to isotropic it is 3/4 of that
				// expression, i.e. 1 + cos^2 after folding the 3/4 into the
				// overall gain. It dips at 90 degrees and is symmetric front and
				// back.
				//
				// Aerosols use Henyey-Greenstein, (1-g^2) / (1+g^2-2g*cos)^1.5,
				// which is already normalised. At g = 0.76 it is ~30x forward
				// of sideways -- the bright halo that sits on the sun's side of
				// the limb.
				//
				// Weighted by how much of the total scattering each one is
				// responsible for, so a clear atmosphere gets Rayleigh and a
				// hazy one gets the halo.
				// ----------------------------------------------------------------
				float cosPsi = dot(l, v);
				float phaseR = 1.0 + cosPsi * cosPsi;
				float g2 = _MieG * _MieG;
				float hg = max(1e-4, 1.0 + g2 - 2.0 * _MieG * cosPsi);
				float phaseM = (1.0 - g2) / (hg * sqrt(hg));
				float sR = (0.581 + 1.000 + 1.957) * _Rayleigh;
				float wR = sR / max(1e-6, sR + 3.0 * _Aerosol);
				float phase = wR * phaseR + (1.0 - wR) * phaseM;

				// Blocks the rim when the sun is behind the viewer. Physically
				// that geometry IS the brightest forward scattering there is,
				// but the terminator then coincides with the whole silhouette
				// and the limb rings warm all the way round, which is the look
				// that was explicitly rejected. One dial, defaulting to only
				// catching the extreme case.
				float sunBehind = saturate((_ForwardBias - dot(l, v))
				                            * rcp(max(1e-3, _ForwardRange)));

				// ----------------------------------------------------------------
				// 5. WHAT COLOUR THE AIR SCATTERS IT AS
				//
				// transmit is the colour of the sunlight arriving. What we see is
				// that sunlight after scattering, and a SINGLE scattering event
				// is spectrally selective in the same 1/lambda^4 way -- which is
				// why the day limb is Rayleigh blue and not white. It is written
				// as a lerp from the cross-section ratio to neutral, driven by
				// the path depth, because multiple scattering saturates the
				// selection: a thin path is blue, a deep one is the transmitted
				// colour with no selection left to apply.
				//
				// Normalised twice, and the second is the one that matters: to
				// UNIT LUMINANCE, not to the blue channel.
				//
				// Normalising to blue alone inflates red by 1/0.297 = 3.4x, and
				// because blue carries the smallest luma weight of the three
				// (0.072 against 0.213 for red) the flattening then ADDS
				// brightness as the path lengthens. Measured, the crest went
				// from 1.93 at 90 degrees off axis to 2.66 at 20 -- the rim
				// brightening as the sun went behind, the exact opposite of
				// what extinction does. Dividing by the luma of the blue-heavy
				// end keeps both ends of the lerp at unit luminance, so the
				// only thing that dims the rim is the air.
				// ----------------------------------------------------------------
				const float3 RAY_TINT = SIGMA_R / SIGMA_R.b;
				const float RAY_LUMA = 0.2126 * RAY_TINT.r
				                   + 0.7152 * RAY_TINT.g
				                   + 0.0722 * RAY_TINT.b;
				float flat = saturate(airMass * _ScatterFlat);
				float3 scatterTint = lerp(RAY_TINT / RAY_LUMA, 1.0, flat);

				// ----------------------------------------------------------------
				// 6. THE BEARING LOBE  -- compositional, not physics
				//
				// Screen-space angle from the sun's bearing along the limb. Both
				// the sun and this limb point are projected into the plane facing
				// the camera, so this measures the separation where it is
				// actually seen and means the same thing at every viewpoint.
				//
				// See the header. Air-mass extinction does not narrow the rim
				// and this is what does; _SunBleedTighten 0 turns it off and
				// leaves the width to the physics.
				// ----------------------------------------------------------------
				float3 axis = normalize(_WorldSpaceCameraPos - input.centreWS);
				float3 sunPerp = l - axis * dot(l, axis);
				float3 nPerp = n - axis * dot(n, axis);
				// |sunPerp| is the sine of the sun's angle off the view axis. It
				// collapses toward zero when the sun lies along the view axis,
				// where an azimuth stops being a direction and becomes a
				// rounding error. 0.02 is about 1 degree.
				float sunLen = length(sunPerp);
				float lobe = (sunLen > 0.02)
				           ? pow(saturate(dot(nPerp, sunPerp)
				                            / max(1e-4, length(nPerp) * sunLen)),
				                 _SunBleedPower
				                 + pow(saturate(-dot(l, v)), _SunBleedShape)
				                   * _SunBleedTighten)
				           : 0.0;

				// The lobe REDISTRIBUTES the scattered light; it does not
				// attenuate it. Multiplying the whole profile by cos^P deleted
				// the twilight band outright -- cos^3 is 6e-5 at 85 degrees off
				// the bearing -- which sent the terminator to black instead of
				// deep red. Same mistake the wedge commit fixed on the air
				// term, reintroduced one commit later on the core.
				//
				// _SunBleed is how much of the light is pulled onto the bearing;
				// the remainder stays wherever the path length put it, which
				// is the terminator. 0 is pure physics: the band sits where
				// the extinction alone puts it, about 9.4x under the crest, and
				// the rim keeps the same width at every sun angle. 1 puts all
				// of it on the bearing -- the converging crescent, and the band
				// with it.
				float shape = saturate((1.0 - _SunBleed) + _SunBleed * lobe);
				// ----------------------------------------------------------------
				float3 tint = transmit
				            * scatterTint
				            * phase
				            * (density * _AtmoGlow * shape
				               * dayLit * sunBehind * lightCol);

				// A small additive core on top, for the hot line right where
				// the sun is coming up. Same transmitted colour, so it reddens
				// with everything else rather than staying white and turning
				// the rim grey where the two overlap.
				tint += transmit
				      * (density * _SunRimBoost * shape * dayLit * sunBehind
				         * lightCol);

				// Airglow. Faded out where the sky is already sunlit, because
				// airglow is not visible against a daylit sky -- and because
				// being cyan it cancels the sunset colour rather than adding to
				// it. Measured, ungated it was 42% of the crest and drove the
				// whole rim grey.
				float skyLit = saturate(0.5 - cosZ * 8.0);
				tint += _AtmoColor.rgb
				      * (density * _NightGlow * (1.0 - skyLit) * lightCol);

				// _Transparency is the master gain. The 0.4 scale is not
				// arbitrary: it puts the core of the band at roughly
				// (0.5, 0.85, 1.0) for an _AtmoColor near the default, which is
				// where the reference limb actually sits. At 0.1 the whole
				// shell rendered as dark navy.
				return fixed4(tint * (_Transparency * 0.4), 1);
			}
			ENDHLSL
		}
	}
}