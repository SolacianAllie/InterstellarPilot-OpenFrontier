Shader "OpenFrontier/PlanetGasGiant"
{
	// A gas giant: banded albedo with differential band drift, and the same
	// atmosphere the rocky planet shader has.
	//
	// WHY A SEPARATE SHADER RATHER THAN PlanetStandard WITH A FLAG
	//
	// PlanetStandard spends most of its per-pixel budget on things a gas giant
	// does not have: a spec map split into land and water masks, city lights
	// with a growth threshold, a land-detail normal, an emission map, and a
	// separate cloud deck with its own normals, specular and projected
	// shadows. A gas giant has none of those. A flag would leave every one of
	// them sampled, branched around and paid for.
	//
	// THE BANDS DRIFT, AND THE DRIFT TABLE IS THE "NORMAL" MAP
	//
	// The *_normal.png that ships with the gas giants is not a normal map.
	// Measured on the four of them against the terrain set:
	//
	//     texture                            red between-band   within
	//     gas_giant_01_normal                      56.5          7.2
	//     gas_giant_06_normal                      73.2          2.4
	//     dwarf_gas_giant_01_normal                59.0          4.2
	//     cloudless_gas_giant_01_normal            22.4          2.1
	//     alpine_01_normal                          0.5          5.6
	//     barren_01_normal                          1.1          1.6
	//
	// Red's between-band variance is 3x to 30x its within-band variance, so
	// it holds ONE number per latitude band, constant across it. The terrain
	// maps have the ratio inverted, below 1, which is what a genuine
	// normal map looks like. Green is flat in all of them (0.7 to 1.7).
	//
	// And that is the tell: a surface banded by latitude has its height
	// gradient in LATITUDE, which in a tangent-space normal map is the green
	// channel. Green carries nothing here. Red carries everything, in the axis
	// a banded planet should have none in.
	//
	// Remapped to -1..1 the per-band values straddle zero -- -0.81 to +0.40 on
	// gas_giant_01 -- so some bands run one way and some the other. That is
	// differential rotation, and it is the whole effect: each band slides
	// along its own latitude at its own rate.
	//
	// The blue channel is 0 in EVERY planet normal map here, terrain included,
	// because Unity's UnpackNormal takes the UnpackNormalMapRGorAG path, which
	// rebuilds Z from red and green and ignores blue entirely. So the zero
	// blue is the project's convention, not something special about gas
	// giants, and it is not where the animation data lives. Alpha is 255 with
	// about 1% of texels slightly under, scattered across every row: noise.
	//
	// WHY THE PLANET SPARKLES, AND WHAT _BandDriftLOD COSTS TO FIX IT
	//
	// The albedo sample's point moves much faster across the screen than the
	// surface itself does, because the drift steps between adjacent bands: the
	// mean step is 0.25 of a texture width, across a band that is about 7
	// pixels tall on a 700px planet. The GPU picks a mip from the screen-space
	// derivative, so where the sample point outruns the surface, the mip is far
	// too sharp and the albedo's fine striations alias into speckle.
	//
	// That ratio is not adjustable. It is the drift's own spatial gradient, and
	// it is the same whether the drift is fast or slow, strong or weak --
	// scaling the whole field scales visible motion and aliasing together. Only
	// FILTERING moves it, and there are exactly two places to filter, and they
	// cost opposite things. Measured on this table, planet at 700px:
	//
	//   lod   field    rate vs surface rate   contrast left
	//     0-2 512x256            ~205x             97-100%
	//     3   64x32               155x              91%
	//     4   32x16               109x              84%
	//     5   16x8                 13.6x             24%
	//     6   8x4                   4.4x             12%
	//
	//  - Blur the DRIFT (this dial): cheap, and the planet stays sharp, but the
	//    47 bands collapse to a 4-8 step ramp. Most of the differential rotation
	//    goes with them.
	//  - Blur the ALBEDO instead: keeps every band, but the rate is ~200x the
	//    surface rate, so the mip it needs is roughly 128 texels across the
	//    whole planet. The bands dissolve into a smear.
	//
	// WHERE THE FILTERING GOES, AND WHY IT IS NOT HERE
	//
	// Mipping this table is the wrong place to filter, and a long stretch of
	// these notes argued otherwise. It does reduce the aliasing, and it
	// introduces something worse: the mip is a GRID, and reconstructing a grid
	// with bilinear interpolation makes the offset continuous but not its
	// DERIVATIVE. The derivative jumps at every knot, the knot spacing is the
	// whole planet divided by the grid, and each cell between two knots is a
	// differently-sheared patch of texture. On a 2048-wide table at LOD 6 the
	// grid is 32x16, so the cells are about 22 screen pixels and plainly
	// visible as rectangles with hard vertical and horizontal edges.
	//
	// That is not a tuning problem, it is what bilinear reconstruction of a
	// coarse field does. Only lowering the resolution can hide it, and lowering
	// the resolution is the thing that is destroying the effect.
	//
	// So this dial now defaults to 0 and the filtering moved to
	// _AlbedoMaxTexelsPerPixel, where it belongs:
	//
	//   - the DRIFT field is sampled at full resolution, so it is smooth and
	//     there are no cells and no derivative jumps;
	//   - the ALBEDO is blurred only where the drift's true footprint demands
	//     it, and the ceiling bounds that so a band edge cannot reach a mip
	//     that degenerates.
	//
	// Band edges come out soft and the rest of the planet stays sharp, which is
	// the correct division: seam softening on the texture that has the seam,
	// not on the texture that defines it.
	//
	// _BandDriftSpeed = 0 is still a legitimate end state, not a failure.
	//
	// Note the consequence for the earlier mip reasoning in this shader: letting
	// the sampler take its gradient from the drifting UV is not a fix, it is the
	// OTHER half of this trade. At a band step that derivative is half a texture
	// width in one pixel, so the mip jumps to the top of the chain and the
	// planet goes bright and blurry there instead of sparkling. Taking the
	// gradient from the undrifted UV keeps the mip sane and leaves the aliasing
	// visible, which is the right way round: it is visible rather than wrong.
	//
	// ATMOSPHERE
	//
	// The aerial-perspective and twilight block is carried across from
	// PlanetStandard essentially intact, and the property NAMES are identical
	// on purpose: the atmosphere shell (OpenFrontier/PlanetAtmosphere) reads
	// _AtmoColor and _SunRimColor from the same material, so the inside and
	// outside halves of the atmosphere cannot disagree about where the sunset
	// is. Renaming anything here would split that in half again.
	//
	// There is no cloud deck. On a gas giant the bands ARE the cloud; adding a
	// separate deck above them would double the atmosphere and put the storms
	// in the wrong place.

	Properties
	{
		_MainTex ("Banded Albedo", 2D) = "black" {}
		// Named _Normals because the existing materials bind that slot, and
		// because renaming it would break every gas giant material on disk.
		// It is a drift table, not a normal map.
		_Normals ("Band Drift Table (R = signed drift per band)", 2D) = "black" {}
		_BandDriftSpeed ("Band Drift Speed", Range(-0.02, 0.02)) = 0.003

		// How much blur the drifting albedo is allowed, as a footprint in
		// texels per pixel. 1 is the surface rate, i.e. no extra blur at all.
		// See the long note above: this is the dial that makes the drift
		// renderable, and it is the one that was missing.
		_AlbedoMaxTexelsPerPixel ("Albedo Blur Ceiling (texels/px)", Range(1, 64)) = 16

		// G is a HEIGHT field for band relief, converted to a tangent-space
		// normal here. The channel spans 0.282..0.690 and a one-texel step in
		// it is 0.0064 on average and 0.125 at most, so the slope this can
		// express is tiny and the dial has to be large to be seen at all: 6 puts
		// the strongest wisp near 35 degrees and the average texel near 2.
		_ReliefScale ("Band Relief (height -> normal)", Range(0, 16)) = 6

		// How wide the height difference is allowed to reach, as a multiple of
		// the pixel footprint. 1 is the textbook minimum; higher low-passes G
		// harder and drops its finest structure. Measured against
		// gas_giant_06_diffuse, which carries striations down to a texel or
		// two, this is the dial between "soft relief" and "sparkle".
		_ReliefFilter ("Relief Filter (x pixel footprint)", Range(0.5, 8)) = 2
		// Mip level the drift table is read at, and the fix for the bands
		// looking smeared with glowing edges.
		//
		// The drift steps between adjacent bands, so two fragments either side
		// of a boundary are sampling the albedo at very different places in the
		// texture -- 0.6 of its width apart after a minute at the old speed. The
		// GPU picks a mip from the screen-space UV derivative, so a quad that
		// straddles a boundary reports a derivative of half a texture width and
		// selects the smallest mip. The table has 47 bands in 256 rows, about
		// 5 rows each, so once the planet is more than a few hundred pixels
		// across, MOST quads straddle a boundary and the whole planet goes
		// blurry. The bright edges are a displaced band landing somewhere else
		// in the albedo.
		//
		// Reading the table at a higher mip blurs the drift field itself, which
		// turns the steps into a gradient, and a gradient has no boundary to
		// straddle. Costs nothing -- same texture, same sampler, a lod bias.
		// The table is 256 rows tall, so lod 3 is 32 rows: well under the 94
		// rows that 47 bands would need to stay distinct.
		_BandDriftLOD ("Band Drift Smoothing", Range(0, 6)) = 0
		_BandSmoothness ("Band Smoothness", Range(0, 1)) = 0.25

		// ---- atmosphere, names fixed by PlanetStandard ----
		[Header(AerialPerspective)]
		_AtmoColor ("Atmosphere Color", Color) = (0.35, 0.78, 1, 1)
		_SunRimColor ("Sun Rim Color", Color) = (1, 0.62, 0.30, 1)
		_SunRimDeep ("Sun Rim Deep Color", Color) = (1, 0.30, 0.12, 1)
		_HazeStrength ("Haze Strength", Range(0, 1)) = 0.6
		_HazeSunStrength ("Haze Sun Strength", Range(0, 2)) = 0.8
		_HazeFalloff ("Haze Falloff", Range(0.5, 8)) = 2.5
		_HazeSunPower ("Haze Sun Power", Range(0.5, 6)) = 1.3
		_HazeSunAngle ("Haze Sun Angle", Range(0, 180)) = 45
		_HazeSunSpread ("Haze Sun Spread", Range(1, 180)) = 35

		[Header(Twilight)]
		_TwilightColor ("Twilight Color", Color) = (1.0, 0.62, 0.30, 1)
		_TwilightStrength ("Twilight Strength", Range(0, 1)) = 0.55
		_TwilightDim ("Twilight Dimming", Range(0, 1)) = 0.3
		_TwilightSharpness ("Twilight Sharpness", Range(0.01, 1)) = 0.13
		_TwilightDepth ("Twilight Night Depth", Range(0.01, 1)) = 0.25
		_TwilightHaze ("Twilight Haze", Range(0, 1)) = 0.6

		// 0 final | 1 albedo | 2 drifting albedo | 3 band drift | 4 the bound
		// table raw | 5 the relief normal from G
		_AlbedoDebug ("DEBUG Surface Bisect", Range(0, 5)) = 0
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
		}

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			ZWrite On
			ZTest LEqual
			Cull Back

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			// Deliberately NO multi_compile_fog and NO MixFog. The bound URP
			// asset carries no fog settings and no shader in this project ever
			// assigns inputData.fogCoord, so PlanetStandard's MixFog reads a
			// zeroed field and does nothing. Copying it would pay for a fog
			// variant on every planet pixel to reproduce dead code.

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
			TEXTURE2D(_Normals);   SAMPLER(sampler_Normals);

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float4 _Normals_ST;

				// Unity fills these in from the bound textures but does NOT emit the
				// declaration for them, so they have to be written out here or every
				// use is an undeclared identifier and the shader goes pink. Neither
				// of the project's other planet shaders uses one, which is why this
				// was missed until a shader finally asked for it. Neither URP nor
				// core declare them either -- those only manage their own framebuffer
				// textures. .xy is the texel step in UV, .zw the dimensions.
				float4 _MainTex_TexelSize;
				float4 _Normals_TexelSize;
				float4 _AtmoColor;
				float4 _SunRimColor;
				float4 _SunRimDeep;
				float4 _TwilightColor;
				float _BandDriftSpeed;
				float _BandDriftLOD;
				float _AlbedoMaxTexelsPerPixel;
				float _ReliefScale;
				float _ReliefFilter;
				float _BandSmoothness;
				float _HazeStrength;
				float _HazeSunStrength;
				float _HazeFalloff;
				float _HazeSunPower;
				float _HazeSunAngle;
				float _HazeSunSpread;
				float _TwilightStrength;
				float _TwilightDim;
				float _TwilightSharpness;
				float _TwilightDepth;
				float _TwilightHaze;
				float _AlbedoDebug;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				half3 normalWS : TEXCOORD2;
				half4 tangentWS : TEXCOORD3;
			};

			// SV_IsFrontFace is INPUT only; declaring it in Varyings, which
			// vert() writes, is an illegal output semantic on Vulkan.
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
				VertexNormalInputs normInputs =
					GetVertexNormalInputs(input.normalOS, input.tangentOS);

				output.positionCS = posInputs.positionCS;
				output.positionWS = posInputs.positionWS;
				output.normalWS = normInputs.normalWS;
				output.tangentWS = half4(normInputs.tangentWS, input.tangentOS.w);
				output.uv = input.uv;
				return output;
			}

			// The signed drift rate of the band this texel is in, remapped to
			// -1..1.
			//
			// Sampled on the ALBEDO's uv, not its own _ST, so the table's bands
			// land exactly on the albedo's bands. Giving the table its own
			// tiling would silently slide one relative to the other and the
			// drift would animate bands that are not the ones you can see.
			//
			// Smoothing is a MIP BIAS on the table, not an extra tap. Blurring
			// the drift field turns the step between bands into a gradient, and
			// a gradient has no boundary for a pixel quad to straddle -- which
			// was what was driving the mip selection to the top of the chain
			// and smearing the planet. An extra tap would not have fixed that:
			// two samples of a step function still step.
			// G IS A HEIGHT FIELD. B is identically zero and A is opaque, so the
			// only other thing in this texture is G, and it is not a packed
			// normal -- R and G differ in kind (R is 96.7% latitude-banded, G is
			// 0.9%, so G varies with longitude) and it sits centred at 0.483 with
			// a spread of 0.029, which is a height, not a tilt. Differentiating it
			// is what makes a normal out of it.
			//
			// Three taps, not four: a central difference wants the neighbours both
			// sides, and the forward difference this uses is wrong by half a texel
			// on a field whose whole dynamic range is 0.4 across 5-row bands. One
			// saved fetch on every pixel of every gas giant is worth that.
			//
			// Differenced in TEXTURE space with explicit texel offsets, never with
			// ddx() of a sampled height. ddx is a screen-space quantity: near the
			// poles a sphere's U compresses to nothing, so a screen-space gradient
			// explodes there and collapses the relief flat exactly where the
			// geometry is most foreshortened. Texel offsets are immune.
			//
			// Sampled at the UNDRIFTED uvT, deliberately. At bandUV the auto mip
			// is chosen from the drifted coordinate, whose derivative is enormous
			// at a band edge, so G would blur to a uniform grey and the relief
			// would vanish at every boundary -- which is where it is most wanted.
			half3 BandRelief(float2 uvT, half3 nWS, half3 tWS, half3 bWS)
			{
				// LOW-PASS BEFORE DIFFERENCING. This is the bug that made the
				// planet sparkle, and it is the standard rule of bump mapping that
				// was not applied: a one-texel difference IS a high-pass. It keeps
				// precisely the finest detail the table has and discards the shape,
				// so a soft field comes out as shading noise.
				//
				// It was invisible on gas_giant_01 because that table is 512 wide
				// and one texel is already several pixels. gas_giant_06 is 2048
				// wide, so the same one-texel difference is four times finer and
				// lands well under a pixel -- which is why the same code is fine
				// on one planet and speckled on another.
				//
				// The step is the pixel footprint measured in THIS table's texels,
				// so it is resolution-independent -- a fixed step would filter a
				// 512-wide table and a 2048-wide one by different amounts. It also
				// means the relief fades as the planet recedes, which is what a
				// bump map should do on its own and never did before.
				float texelsPerPixel = max(length(ddx(uvT)), length(ddy(uvT)))
					/ max(_Normals_TexelSize.x, _Normals_TexelSize.y);
				float step = max(1.0, texelsPerPixel * _ReliefFilter);
				float2 offset = _Normals_TexelSize.xy * step;

				float h  = SAMPLE_TEXTURE2D(_Normals, sampler_Normals, uvT).g;
				float hx = SAMPLE_TEXTURE2D(_Normals, sampler_Normals,
					uvT + float2(offset.x, 0.0)).g;
				float hy = SAMPLE_TEXTURE2D(_Normals, sampler_Normals,
					uvT + float2(0.0, offset.y)).g;
				half3 nTS = normalize(half3(
					-(hx - h) * _ReliefScale,
					-(hy - h) * _ReliefScale,
					1.0));
				return normalize(tWS * nTS.x + bWS * nTS.y + nWS * nTS.z);
			}

			half BandDrift(float2 uvT)
			{
				return SAMPLE_TEXTURE2D_LOD(_Normals, sampler_Normals, uvT,
					_BandDriftLOD).r * 2.0 - 1.0;
			}

			half4 frag(FragmentInput input) : SV_Target
			{
				Varyings v = input.interpolated;

				float2 uvT = TRANSFORM_TEX(v.uv, _MainTex);
				half drift = BandDrift(uvT);

				// The slide is in TEXTURE space, applied after the tiling, so
				// _BandDriftSpeed means the same number however the material is
				// tiled. Doing it before TRANSFORM_TEX would scale the drift by
				// the tiling too and the rate would change with it.
				//
				// LONGITUDE ONLY, and that is load-bearing rather than a
				// simplification. The map (u,v) -> (u + f(v), v) has Jacobian
				// [[1, f'], [0, 1]], whose determinant is 1: the texture is
				// rigidly sheared and nothing can compress or fold.
				//
				// Adding a vertical term g(v) makes the determinant 1 + g', and
				// g' carries the accumulated offset, so it grows with elapsed
				// time without bound. Measured on this drift field at
				// df/dv ~ 9.6 per unit V, the vertical axis came out stretched
				// 1.3x at a minute, 6.8x at five, 18x at fifteen, and goes
				// NEGATIVE -- a fold, which mirrors the texture -- if the shear
				// dial is pushed. That stretching is what pulled the bands into
				// converging diamonds.
				//
				// It bought nothing anyway. Differential rotation IS bands
				// sliding at different rates along latitude, which is exactly
				// what the U term alone does.
				//
				// The accumulated offset is wrapped PER BAND, at a whole texture
				// width, rather than wrapping the clock.
				//
				// Wrapping the clock (fmod on _Time.y * speed) resets every band
				// at the same instant but by DIFFERENT amounts, since their drift
				// differs -- a visible tear, once per period. Wrapping each band's
				// own offset instead makes every one step by exactly 1.0, and
				// under Repeat a jump of one whole texture width lands on the
				// identical texel. No tear, at any speed, for any drift.
				//
				// Wrap the OFFSET, not the phase before it. Folding the phase and
				// then multiplying by drift a second time would step by `drift`
				// widths, not one -- 0.8 for the strongest bands here, which is a
				// tear in all but name. This shader had that bug for one commit.
				float offset = drift * _Time.y * _BandDriftSpeed;
				offset -= floor(offset);
				float2 bandUV = uvT + float2(offset, 0.0);

				// Sampled with gradients taken from the UN-drifted uv.
				//
				// A plain SAMPLE_TEXTURE2D derives its mip from ddx/ddy of the
				// coordinate it is handed. Hand it bandUV and the drift steps go
				// straight into that: either side of a band boundary the offset
				// differs, the quad straddling it reports a derivative of order
				// half a texture width, and the sampler selects the smallest mip
				// there. The albedo is full of high-contrast striations, so a mip
				// too low averages toward the mean and reads BRIGHTER than the
				// sharp sample -- which is exactly the short bright dashes sitting
				// on the band boundaries.
				//
				// The drift is a translation in U. A translation has no derivative,
				// so the honest gradients are the ones the surface itself has,
				// ddx(uvT)/ddy(uvT). Passing those is not a fudge to hide the step:
				// it is the correct footprint, and the step stops distorting the
				// LOD of a sample it does not change the density of.
				//
				// _BandDriftLOD still matters, for the other half of it. GRAD fixes
				// the mip; it cannot make two pixels either side of a boundary agree
				// on what texel to read, because they genuinely read different ones.
				// That residual tear is what the smoothing dial is for.
				float2 ddxUV = ddx(bandUV);
				float2 ddyUV = ddy(bandUV);

				// The TRUE gradient, capped.
				//
				// ddx(uvT) -- the surface's own footprint -- is wrong here, and
				// badly so: the drift moves the sample point a long way faster
				// than the surface does, so the mip is far too sharp and the
				// albedo's fine striations alias into speckle.
				//
				// The true gradient fixes that and creates the opposite fault. The
				// drift is a step function, so at a band edge it reports half a
				// texture width inside one pixel, the mip jumps to the top of the
				// chain, and that latitude reads as a bright blurry stripe. Left
				// alone the two faults cancel by luck: noisy in the middle of the
				// bands, striped at the edges. That is what every previous version
				// of this shader was, and why fixing either one alone made it worse.
				//
				// The cap separates them. Take the real footprint -- so the drift
				// is genuinely filtered, no speckle -- but bound it to what the
				// planet is allowed to look like, so a step cannot reach a mip that
				// degenerates, and blurs instead. Both faults need an unbounded
				// term; one bound removes them together.
				//
				// Both vectors scale by the same factor, so the gradient's shape
				// and the anisotropy between the axes are preserved and only the
				// mip is bounded.
				float footprint = max(length(ddxUV), length(ddyUV))
					* max(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
				if (footprint > _AlbedoMaxTexelsPerPixel)
				{
					float k = _AlbedoMaxTexelsPerPixel / footprint;
					ddxUV *= k;
					ddyUV *= k;
				}
				half4 albedoTex = SAMPLE_TEXTURE2D_GRAD(_MainTex, sampler_MainTex,
					bandUV, ddxUV, ddyUV);

				if (_AlbedoDebug > 0.5 && _AlbedoDebug < 1.5)
					return half4(albedoTex.rgb, 1.0);
				// Stage 2 is the same sample with NO drift, so the two can be
				// compared side by side and the motion attributed to the drift
				// rather than to anything else that changed.
				if (_AlbedoDebug > 1.5 && _AlbedoDebug < 2.5)
					return half4(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvT).rgb, 1.0);
				// Stage 3 paints the drift itself: red = one way, blue = the
				// other, and the bands read as horizontal stripes because that
				// is what it is. If this shows structure other than bands, the
				// table is not the per-band drift it was measured to be.
				//
				// Bounded above, unlike stage 4 below which is the catch-all --
				// an unbounded final stage swallows whatever comes after it.
				if (_AlbedoDebug > 2.5 && _AlbedoDebug < 3.5)
					return half4(drift * 0.5 + 0.5, 1.0 - drift * 0.5 - 0.5, 0.0, 1.0);
				// Stage 4 shows _Normals itself, raw, for every pixel. This is
				// the one that answers "is the right texture even bound",
				// which the drift view cannot: a rocky planet's normal map
				// also has a strong red channel, it just varies in two axes
				// instead of one, and warping the UV by THAT produces exactly
				// the rectangular blocking rather than band motion. Horizontal
				// stripes here means the gas giant table; a two-axis gradient
				// means the wrong map; a flat colour means nothing is bound.
				if (_AlbedoDebug > 3.5 && _AlbedoDebug < 4.5)
					return SAMPLE_TEXTURE2D(_Normals, sampler_Normals, uvT);
				// Stage 5 shows the normal built from G. Flat lavender means the
				// channel is being read but is flat; visible wisps means the relief
				// is real and only _ReliefScale decides how strong it looks.
				if (_AlbedoDebug > 4.5)
					return half4(BandRelief(uvT, half3(0, 0, 1), half3(1, 0, 0), half3(0, 1, 0)) * 0.5 + 0.5, 1.0);

				// Two normals, deliberately.
				//
				// geoNormalWS is the sphere's own and stays geometric. The
				// atmosphere below keys off it regardless of relief, because
				// feeding a perturbed one in is what used to make haze appear on
				// the night side and made the limb blotchy -- the terminator has
				// to be where the GEOMETRY puts it or the sunset lands in the
				// wrong place.
				//
				// shadingNormalWS adds the relief built from G, and only that one
				// goes to the lighting. This is the first normal-mapped lighting in
				// this shader, so it is worth knowing it is here.
				half3 geoNormalWS = normalize(v.normalWS);
				half3 viewDirWS = GetWorldSpaceNormalizeViewDir(v.positionWS);
				half3 tangentWS = normalize(v.tangentWS.xyz);
				half3 bitangentWS = cross(geoNormalWS, tangentWS) * v.tangentWS.w;
				half3 shadingNormalWS = BandRelief(uvT, geoNormalWS, tangentWS, bitangentWS);

				InputData inputData = (InputData)0;
				inputData.positionWS = v.positionWS;
				inputData.normalWS = shadingNormalWS;
				inputData.viewDirectionWS = viewDirWS;
				inputData.shadowCoord = TransformWorldToShadowCoord(v.positionWS);
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(v.positionCS);
				inputData.shadowMask = half4(1, 1, 1, 1);

				Light mainLight = GetMainLight();

				float geoNdL = dot(geoNormalWS, mainLight.direction);
				float cosZ = geoNdL;
				// Airmass through an exponential atmosphere goes as 1/cos of the
				// angle to the normal: 1 looking straight down, unbounded at the
				// limb. The 0.06 floor keeps it finite there.
				float airMass = rcp(max(0.06, cosZ));
				float twilight = saturate((airMass - 1.0) * _TwilightSharpness);
				// How far PAST the terminator the twilight band reaches. Without
				// this the band wraps right around the night side, which is not
				// twilight, it is daylight.
				twilight *= 1.0 - saturate(-cosZ * rcp(max(1e-3, _TwilightDepth)));
				half3 sunTint = lerp(half3(1, 1, 1), _TwilightColor.rgb,
					saturate(twilight * _TwilightStrength))
				              * lerp(1.0, _TwilightDim, twilight);
				half3 sunColour = mainLight.color * sunTint;

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = albedoTex.rgb;
				surfaceData.metallic = 0.0;
				surfaceData.smoothness = _BandSmoothness;
				// UniversalFragmentPBR never reads normalTS -- InitializeBRDFData
				// takes only albedo, metallic, specular, smoothness and alpha --
				// and there is no normal map here anyway.
				surfaceData.normalTS = half3(0, 0, 1);
				surfaceData.occlusion = 1.0;
				surfaceData.alpha = 1.0;
				surfaceData.clearCoatMask = 0.0;
				surfaceData.clearCoatSmoothness = 0.0;

				half4 color = UniversalFragmentPBR(inputData, surfaceData);

				// Twilight rides on the lit term, which is exactly why it is
				// applied here rather than by tinting mainLight: the light
				// URP hands UniversalFragmentPBR is fetched inside it and never
				// passed in, so the only separable place is the result.
				color.rgb *= sunTint;

				// ---- aerial perspective ----
				// The atmosphere shell only ever draws OUTSIDE the silhouette,
				// so the air stops dead at the planet's edge and the atmosphere
				// reads as a sticker. This is the inside half: the view ray
				// crosses a lot of air near the limb and that air scatters
				// light back over the surface. A lerp toward a tint, so the
				// bands stay visible underneath.
				float mu = saturate(dot(geoNormalWS, viewDirWS));  // 1 face-on, 0 at the limb
				float grazing = pow(saturate(1.0 - mu), _HazeFalloff);

				// Judged on the sphere's own terminator, so haze cannot leak
				// onto the night side.
				float hazeLit = smoothstep(0.0, 0.35, geoNdL);
				// ...but the twilight band sits ON the terminator rather than on
				// the daylit side of it, so it gets a window that straddles it.
				float twilightLit = smoothstep(-0.15, 0.10, geoNdL);

				float lowSun = pow(saturate(1.0 - geoNdL), _HazeSunPower);
				// Angle between the sun and the view ray in degrees: 0 is
				// looking straight into the sun, 90 is the sun square across the
				// view, 180 is the night side. One acos per pixel, which is
				// noise beside UniversalFragmentPBR's few hundred instructions.
				float sunAngle = degrees(acos(saturate(dot(mainLight.direction, viewDirWS))));
				float lowForUs = saturate((sunAngle - _HazeSunAngle)
				                         * rcp(max(1.0, _HazeSunSpread)));
				float hazeSun = saturate(lowSun * lowForUs);

				// TWO stages, not one. A single lerp from the air's cyan to the
				// rim's orange passes through grey, and the twilight zone then
				// cannot look orange however far _TwilightHaze is pushed,
				// because it would be lerping toward a colour with no chroma
				// left in it. Routing through the deep sunset colour keeps the
				// chroma up at every step. Squared on the second stage, since
				// the deep end is only reached right at the terminator.
				half3 hazeTint = lerp(lerp(_AtmoColor.rgb, _SunRimColor.rgb, saturate(hazeSun)),
				                      _SunRimDeep.rgb, saturate(hazeSun * hazeSun));
				// The band term as well as the limb term, so the blue visibly
				// bends to red across the terminator instead of only at the
				// silhouette.
				float hazeAmt = saturate(grazing * hazeLit
					* (_HazeStrength + _HazeSunStrength * hazeSun)
					+ twilight * _TwilightHaze * twilightLit);
				color.rgb = lerp(color.rgb, hazeTint, hazeAmt);

				return color;
			}
			ENDHLSL
		}
	}
}
