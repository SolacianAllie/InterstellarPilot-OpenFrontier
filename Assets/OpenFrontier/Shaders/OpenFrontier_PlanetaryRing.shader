Shader "OpenFrontier/PlanetaryRing"
{
	// A ring of dust, seen from both faces.
	//
	// STRIPPED BACK ON PURPOSE
	//
	// This is the floor: one texture, one light, one shadow, one transmit
	// term. The Henyey-Greenstein lobe, the rim, the wrapped diffuse, the
	// grazing-angle alpha falloff, the normal map, the spec map, the band
	// edge fade, the second UV channel and the constant ambient are all gone.
	// They were not cheap and they were not all pulling their weight, and a
	// look cannot be tuned while six other effects are arguing with it.
	//
	// The one that came back is transmission, because it is not a cosmetic
	// effect but the actual lighting of a translucent sheet. See the note on
	// _TransmitStrength.
	//
	// "Double sided" is the whole geometry story: Cull Off, and the normal
	// flipped on back faces. A ring is viewed from above and from below, and
	// without the flip the underside is lit from behind and comes out black.
	//
	// WHICH WAY IS THE SUN
	//
	// SunBillboard.cs sets a global called _SunDirectionWorld, and it looks
	// like the obvious thing to read. It is not, and the project has already
	// been bitten: that write is only reached when both WorldCamera.Resolve()
	// and cachedLight succeed, so on every other frame the global keeps
	// whatever it last held, which is not this scene's light. The planet
	// atmosphere shader stopped reading it for exactly that reason -- its rim
	// wrapped all the way round the planet while the surface was correctly in
	// shadow. See OpenFrontier_PlanetAtmosphere.shader.
	//
	// The sanctioned reference is _MainLightPosition.xyz, which URP publishes
	// every frame as the direction TOWARD the light. GetMainLight() reads it
	// and returns it as .direction, so reading it through there is the same
	// vector the planet's surface is lit by, and the ring's terminator and the
	// planet's terminator are the same line by construction rather than two
	// systems each guessing where the sun is.
	//
	// MOBILE FIRST: three texture samples became one, and the tangent
	// attribute and its interpolator are gone, since nothing needs a tangent
	// frame without a normal map.

	Properties
	{
		[MainTexture] _DustAlbedo ("Dust Albedo (RGB, A = density)", 2D) = "black" {}
		[HDR] _DustColor ("Dust Tint", Color) = (1, 1, 1, 1)
		_DustSmoothness ("Dust Smoothness", Range(0, 1)) = 0.08

		// ---- light through the dust ----
		// There is no ambient term. A constant ambient is the usual way to keep
		// the unlit side off pure black, and it is a lie: it is the same on
		// every surface regardless of what is lighting it, so it cannot darken
		// where the planet's shadow falls and it does not move when the sun
		// does.
		//
		// This is what replaces it. Light entering the sunlit face of a thin
		// sheet scatters through the dust and leaves the far face, so the side
		// you are looking at glows when the sun is on the other side of it.
		// The glow therefore tracks the sun for free -- it swings round as the
		// light does, and it is genuinely absent where the planet is in the way.
		_TransmitStrength ("Light Through Dust", Range(0, 4)) = 0.8
		// How tightly the glow is confined to looking straight through. Low is
		// a broad wash either side of the sun; high is a bright core only when
		// the sun is nearly behind the dust from here.
		_TransmitPower ("Transmit Tightness", Range(0.5, 16)) = 4
		// The tint the light picks up on the way through. Dust is not clear, so
		// it does not pass light through unchanged.
		[HDR] _TransmitColor ("Transmitted Colour", Color) = (1, 0.93, 0.82, 1)
		// How much the dust's own density attenuates it. Thick dust blocks its
		// own glow, which is why the sparse regions shine through and the dense
		// ones do not. Reads the texture's alpha, which is already sampled.
		_TransmitDensity ("Density Attenuation", Range(0, 1)) = 1

		// ---- edge-on brightening ----
		// How much brighter the ring is when the sun sits IN its plane.
		// 1 is off; 4 is a strong lift.
		//
		// A flat sheet lit edge-on has no surface facing the sun at all, so
		// N.L collapses to zero and the ring goes black -- which is wrong,
		// because it is a field of dust rather than a solid plate. What
		// actually happens is that light entering edge-on travels a long way
		// through the sheet before leaving, so it meets far more particles and
		// scatters more. The surface term shrinks as the light path through
		// the volume grows. This is why Saturn's rings stay bright as the sun
		// approaches their plane, and it is the single most recognisable thing
		// about how rings look.
		//
		// Self-shadowing across the sheet is what stops it diverging as the
		// angle goes fully edge-on -- the far side of the belt shades the near
		// side. This does not model that, so the boost is bounded rather than
		// a 1/|N.L| that would blow up at the limit.
		_EdgeOnBrightness ("Edge-On Brightness", Range(1, 8)) = 3

		// ---- planet shadow ----
		// A shadow map cannot reach this ring: the project's shadow distance
		// is 3000 units over 4 cascades, and a ring that reads as a ring is
		// legible far beyond that. Cascades also spend their resolution near
		// the camera, which is the one region a ring does not need.
		//
		// So the occlusion is computed. For a directional light the planet's
		// shadow is an infinite cylinder of the planet's radius along the
		// light direction, so there is no quadratic to solve: a point is
		// occluded exactly when it is BEHIND the planet and within the
		// planet's radius of the light axis. Ten ALU, no fetch, exact at any
		// range.
		//
		// OBJECT units, and the planet's centre is the object's origin --
		// which holds by construction, because the mesh is an annulus centred
		// on it. That is why this needs no world position property: the offset
		// from the centre is positionOS. 0 disables it entirely.
		//
		// ON THIS MESH THE VALID RANGE IS NARROW, and being outside it fails
		// in a way that reads as a lighting bug rather than as a bad number.
		// The band is centred on the planet and 2 * _PlanetRadius wide. The
		// ring's own radii are 40.556 inner and 101.530 outer, so:
		//
		//   above ~20   a narrow strip, the classic look
		//   20 to 30    the strip eats the inner rings, plausible
		//   above ~30   the band is wider than the 60.97-unit width of the
		//               annulus, so the whole anti-sun half goes dark at once
		//               and the only visible edge left is the terminator plane
		//               -- one straight line through the planet's centre,
		//               which looks exactly like the shadow is inverted
		//
		// A ring's inner radius has to sit outside its planet, so the planet
		// must be under 40.556. If the shadow looks like half the ring rather
		// than a band across it, this number is too big -- it is not the sign.
		_PlanetRadius ("Planet Radius (object units, 0 = off)", Float) = 0
		_ShadowStrength ("Planet Shadow Strength", Range(0, 1)) = 1
		// The ring's own radii, in the same object units as _PlanetRadius.
		// Read for the penumbra below; it needs the ring's WIDTH, not the
		// planet's size, to express a softness that means the same thing on a
		// tight ring and an enormous one. Defaults match this mesh
		// (40.556 inner, 101.530 outer) so it looks right before it is set.
		_RingRadii ("Ring Radii (inner, outer)", Vector) = (40.556, 101.530)
		// Penumbra as a FRACTION OF THE RING'S WIDTH, not as a tangent.
		//
		// The physically correct number is tan of the star's angular radius --
		// about 0.005 for a Sun -- and that was what this used to be. It is
		// correct and it is unusable here: 0.005 across a 60-unit ring is
		// half a unit, which is sub-pixel, so a wide ring gets a razor-hard
		// strip across it that reads as a modelling error rather than a
		// shadow, while a narrow ring looks fine. One tangent cannot serve
		// systems whose rings differ by an order of magnitude.
		//
		// As a fraction of the ring it scales with the thing it has to look
		// right on. 0.15 means the shadow's soft edge is a sixth of the ring
		// wide, which reads as a shadow at any scale.
		_ShadowSoftness ("Shadow Softness (fraction of ring width)", Range(0, 0.5)) = 0.15

		[Enum(Off, 0, Back, 1, Front, 2)] _Cull ("Culling", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
			"Queue" = "Transparent"
			"DisableBatching" = "True"
		}

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			// PREMULTIPLIED: One / OneMinusSrcAlpha, and the fragment
			// premultiplies to match. Under the ordinary SrcAlpha form a
			// half-transparent pixel contributes its full lit colour at half
			// strength and darkens toward the background instead of fading
			// into it, which is a black fringe on every soft edge.
			//
			// Cull off: this is the double-sided half of "double sided ring".
			Blend One OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Cull [_Cull]

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURE2D(_DustAlbedo);   SAMPLER(sampler_DustAlbedo);

			// Every material property belongs here, textures included by way
			// of their _ST, or the SRP Batcher drops to per-draw setup.
			CBUFFER_START(UnityPerMaterial)
				float4 _DustAlbedo_ST;
				float4 _DustColor;
				float4 _TransmitColor;
				float _TransmitStrength;
				float _TransmitPower;
				float _TransmitDensity;
				float _EdgeOnBrightness;
				float _DustSmoothness;
				float _PlanetRadius;
				float _ShadowStrength;
				float4 _RingRadii;
				float _ShadowSoftness;
				float _Cull;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float2 uv1 : TEXCOORD0;
				// No TANGENT. There is no normal map, so nothing needs a
				// tangent frame, and fetching one costs an attribute and an
				// interpolator for nothing. GetVertexNormalInputs has a
				// single-argument overload that fills in a dummy frame, which
				// is the supported way to ask for the normal alone -- passing
				// a zero float3 instead would read tangentOS.w off a float3,
				// which is undefined.
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv1 : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float3 positionOS : TEXCOORD2;
				half3 normalWS : TEXCOORD3;
			};

			// SV_IsFrontFace is INPUT only. Declaring it in Varyings, which
			// vert() writes, is an illegal output semantic on Vulkan. It goes
			// in the fragment input, with the varyings nested as
			// `interpolated`, which is how URP does it in SpeedTree8Passes.hlsl.
			struct FragmentInput
			{
				Varyings interpolated;
				FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				VertexPositionInputs posInputs =
					GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

				output.positionCS = posInputs.positionCS;
				output.positionWS = posInputs.positionWS;
				output.positionOS = input.positionOS.xyz;
				output.normalWS = normInputs.normalWS;
				output.uv1 = input.uv1;
				return output;
			}

			// Whether the planet hides this point from the star.
			//
			// The occluded set is axial < 0, where axial = positionOS . lightDir
			// and lightDir points TOWARD the star. A point in front of the
			// planet has a positive axial and can never be occluded, because the
			// ray to the star runs from positionOS toward lightDir and so only
			// ever moves away from the sphere. The sign is worth stating: with
			// a star in +X and a radius-20 planet, (-50, 0, 0) is behind and on
			// axis and is shadowed, (-50, 30, 0) is 30 off axis and is lit, and
			// (50, 0, 0) is in front and is lit. Guarding on `>=` is what keeps
			// it that way, and the opposite guard shadows the sunlit face
			// without looking obviously wrong.
			half PlanetShadow(float3 positionOS, half3 lightDirOS, half lightLen)
			{
				half axial = dot(positionOS, lightDirOS) / lightLen;
				if (axial >= 0.0)
				{
					return 1.0;
				}
				// Perpendicular distance from the light axis. Taken as a
				// difference of squares rather than length(P - L*axial) so it
				// stays non-negative under rounding.
				half perp = sqrt(max(
					dot(positionOS, positionOS) - axial * axial, 0.0));
				// Penumbra measured against the ring rather than against the
				// star, so the same setting reads the same on every ring in the
				// game. The old form also grew with distance behind the occluder
				// (correct -- a penumbra widens as it travels) but at ring
				// scales that growth is half a unit across the whole belt, which
				// is why it looked like it did nothing.
				half ringWidth = max(1e-4, _RingRadii.y - _RingRadii.x);
				half soft = ringWidth * _ShadowSoftness;
				//
				// NO 1.0- prefix. This was inverted for several commits and
				// the inversion is invisible to inspection, because both ends
				// of a smoothstep still reach 0 and 1 -- only WHICH end is
				// where differs. Negated, the term returns 1 (lit) at perp=0,
				// which is precisely where the planet blocks the star most
				// completely, and puts the dark wedge out at perp > Rp where
				// it flanks the axis instead of sitting under the planet. The
				// result reads as a huge band of light with a dark stripe
				// through it, which looks like a sign error in axial rather
				// than what it is. What finally caught it was printing this
				// one function's return value on its own -- worth remembering
				// that an inverted test here is invisible to every other kind
				// of check, because both ends of a smoothstep still reach 0
				// and 1 and only which end is where differs.
				return smoothstep(_PlanetRadius, _PlanetRadius + soft, perp);
			}

			half4 frag(FragmentInput input) : SV_Target
			{
				Varyings v = input.interpolated;

				half4 dustTex = SAMPLE_TEXTURE2D(_DustAlbedo, sampler_DustAlbedo,
					TRANSFORM_TEX(v.uv1, _DustAlbedo));

				// The zero-argument overload on purpose: it returns direction
				// and colour without touching the shadow map. The map cannot
				// reach this far anyway, so sampling it here would be a fetch
				// spent on a guaranteed 1.0.
				Light mainLight = GetMainLight();

				// Hoisted above the lighting so the shadow and the transmission
				// share one fetch and one guard rather than repeating both.
				// TransformWorldToObjectDir uses a plain normalize(), so a zero
				// light direction would give NaN, and
				// NaN multiplied into a black unlit result is still NaN. URP
				// documents _MainLightPosition as defaulting to (0,0,1) when no
				// main light is visible, so it should never be zero -- but
				// "should never" is not a reason to ship a NaN path.
				half lightLen = length(mainLight.direction);
				half3 lightDirOS = (lightLen > 1e-5)
					? TransformWorldToObjectDir(mainLight.direction, true)
					: half3(0, 0, 1);

				// Flip the geometric normal on back faces. This is the other
				// half of "double sided": Cull Off draws the far face, and
				// without this it is lit from behind and renders black.
				half3 geoNormalWS = normalize(v.normalWS)
					* IS_FRONT_VFACE(input.facing, 1.0, -1.0);

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = dustTex.rgb * _DustColor.rgb;
				surfaceData.metallic = 0.0;
				surfaceData.smoothness = _DustSmoothness;
				surfaceData.occlusion = 1.0;

				InputData inputData = (InputData)0;
				inputData.positionWS = v.positionWS;
				inputData.normalWS = geoNormalWS;
				inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(v.positionWS);
				inputData.shadowCoord = TransformWorldToShadowCoord(v.positionWS);
				// Zeroed deliberately. UniversalFragmentPBR folds bakedGI in as
				// the GI term, so it arrives already summed with the direct
				// light in color.rgb -- and the planet shadow multiplies
				// color.rgb. Anything left in there is multiplied by the shadow
				// along with the sun, which is how an earlier version turned the
				// shadowed half of the ring into a hole rather than a shadow.
				//
				// There is nothing to put here now in any case: the unlit side is
				// lit by light passing through the dust from the far face, which
				// is added after the shadow and is a real dependency on where
				// the sun is, rather than a constant.
				inputData.bakedGI = half3(0, 0, 0);
				inputData.vertexLighting = half3(0, 0, 0);
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(v.positionCS);
				inputData.shadowMask = half4(1, 1, 1, 1);

				half4 color = UniversalFragmentPBR(inputData, surfaceData);

				// ---- planet shadow ----
				// Applied to the lit result rather than through inputData.
				// shadowMask only feeds BakedShadow() for lightmaps, and with
				// no lightmaps it is ignored entirely -- a hook that silently
				// does nothing. shadowCoord cannot be used either, since the
				// map does not reach here.
				half shadow = 1.0;
				if (_PlanetRadius > 0.0 && lightLen > 1e-5)
				{
					shadow = lerp(1.0,
						PlanetShadow(v.positionOS, lightDirOS, lightLen),
						_ShadowStrength);
				}
				color.rgb *= shadow;

				// ---- light that came through the dust ----
				// dot(viewDir, -lightDir) is the angle between the direction
				// the light is travelling and the direction to the camera, so
				// it peaks exactly when the sun is on the far side of the sheet
				// from the viewer: looking INTO the sun through the dust. That
				// is the whole effect -- the unlit face glows because the dust
				// is in the way, not because a constant was added to it.
				//
				// Multiplied by the same shadow as the direct term, and that is
				// not tidiness: if the planet is between this patch of ring and
				// the star then no light enters the dust here either, so there
				// is nothing to scatter through. A glow that survived the
				// planet's shadow would be visibly wrong.
				half3 viewDir = inputData.viewDirectionWS;
				half transmit = pow(saturate(dot(viewDir, -mainLight.direction)),
					_TransmitPower);
				// Dense dust blocks its own glow. A lerp rather than exp(): the
				// falloff is not measured to justify a transcendental on a
				// mobile target, and this reads the same.
				half transmitAmount = transmit * _TransmitStrength
					* lerp(1.0, 1.0 - saturate(dustTex.a), _TransmitDensity);

				color.rgb += mainLight.color * _TransmitColor.rgb
					* transmitAmount * shadow;

				// ---- edge-on scattering ----
				// saturate(ndl), not abs(ndl): this is the sunward face's term.
				// The far face is already handled by transmission above, and
				// letting both fire would count the same light twice.
				half ndl = dot(geoNormalWS, mainLight.direction);
				// Exactly 0 at normal incidence, where the direct term already
				// does the work, and exactly _EdgeOnBrightness-1 edge-on. A plain
				// linear ramp rather than the 1/|N.L| the geometry suggests,
				// which diverges at the limit -- real self-shadowing across the
				// belt is what stops that in reality, and this bounds it
				// instead. One multiply, no division, both ends exact.
				half edgeOn = (1.0 - saturate(ndl)) * (_EdgeOnBrightness - 1.0);
				// Tinted by the dust albedo, because this is light scattering
				// off the same particles the direct term reflects off, and
				// scaled by density so the sparse regions do not brighten as
				// much as the dense ones. The same alpha already sampled for
				// transmission, so this costs nothing.
				//
				// Note the alpha runs 0 to about 0.37 on this project's dust
				// texture, which caps how bright this can get; if the ring is
				// still dim edge-on, _EdgeOnBrightness has to make up for it.
				color.rgb += mainLight.color * _DustColor.rgb * dustTex.rgb
					* edgeOn * saturate(dustTex.a) * shadow;

				// Coverage.
				color.a = saturate(dustTex.a);

				// Premultiply, to match Blend One / OneMinusSrcAlpha above.
				color.rgb *= color.a;
				return color;
			}
			ENDHLSL
		}
	}

	// No fallback SubShader. The project is URP-only -- the graphics settings
	// bind a UniversalRenderPipelineAsset -- so a built-in CGPROGRAM pass here
	// is unreachable code that Unity still compiles.
	Fallback Off
}
