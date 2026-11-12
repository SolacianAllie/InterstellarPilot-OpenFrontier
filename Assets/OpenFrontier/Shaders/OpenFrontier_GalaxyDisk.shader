Shader "OpenFrontier/GalaxyDisk"
{
	// A galactic disk -- the galaxy stage's first dressing layer and the
	// backdrop every later sky capture photographs.
	//
	// FULLY PROCEDURAL, NO TEXTURES, ON PURPOSE. A sky capture can happen at
	// any range: the stage camera looking down at the plane, a system camera
	// INSIDE the disk, the MAP camera orthographic top-down. Anything
	// texture-based would either tile visibly or need a huge asset to
	// survive the close look. Polar math plus noise stays sharp at every
	// range, and the whole look costs one draw call.
	//
	// THE GEOMETRY IS NOT THIS SHADER'S BUSINESS. The mesh is a stack of
	// flat disks -- a cheap volumetric sheet built by GalaxyDisk.cs. Seen
	// from above it is the classic spiral disk; seen edge-on (a system
	// capture from inside the galaxy, the Milky-Way band view) a single
	// flat quad would collapse to an infinitely thin line, while the stack
	// renders as a band with real thickness. The VERTICAL PROFILE is baked
	// into the vertex COLOR alpha by GalaxyDisk.cs when it builds the mesh,
	// so the falloff shape has exactly one owner and there is no
	// mesh/material thickness pair to drift out of sync. This shader
	// multiplies everything by that alpha and never thinks about thickness
	// again. A mesh WITHOUT vertex colours renders nothing at all, by
	// design: the alpha IS the falloff, and zero alpha is zero disk.
	//
	// ADDITIVE over the black sky (Blend One One). Black adds nothing, so
	// dark means absent, and later additive layers (nebula billboards, star
	// fields) compose with this in any draw order. "Dust" therefore works
	// by MULTIPLYING the finished emission down inside the same fragment --
	// never a dark semi-transparent sheet, which additive cannot draw.
	//
	// UNLIT ON PURPOSE. A galaxy is emissive; the main light's direction
	// has no meaning at galactic scale, and a lighting fetch here would be
	// spent on a term that cannot exist. (The planetary ring is lit dust
	// and needs the sun; this is not.)
	//
	// MOBILE FIRST: no texture samples, no lighting, no shadow fetches. The
	// cost is pure ALU -- two fbm stacks of three octaves each on a small
	// vertex count. A particle galaxy spends thousands of billboards of
	// overdraw to fake exactly this.

	Properties
	{
		_Intensity ("Overall Intensity", Range(0, 8)) = 1

		// ---- radial structure ----
		// Two falloffs on purpose: the diffuse haze dies faster than the
		// arms, so the arms read as structures reaching out into darkness
		// instead of as a pattern painted inside a fading disc.
		_DiskFalloff ("Haze Falloff", Range(1, 12)) = 4.5
		_ArmFalloff ("Arm Falloff", Range(0.5, 10)) = 2.8

		// ---- core ----
		// The bulge -- glow that grows the closer to the centre. Flatten
		// below 1 squashes one axis into the bar many spirals have (the
		// reference core is visibly elongated); 1 = round.
		_CoreRadius ("Core Radius", Range(0.02, 0.6)) = 0.3
		_CoreIntensity ("Core Intensity", Range(0, 12)) = 3.5
		_CoreFlatten ("Core Flatten (1 = round, < 1 = bar)", Range(0.2, 1)) = 0.62
		_CoreRotation ("Core Bar Angle (degrees)", Range(0, 360)) = 0

		// ---- spiral arms ----
		// Logarithmic spiral: phase = N*theta + winding*ln(r). That is the
		// shape real disks have (constant pitch angle), and it keeps arm
		// width from changing character across three orders of scale.
		_Arms ("Arm Count (rounded)", Float) = 2
		_Winding ("Spiral Winding", Range(0, 14)) = 5
		_ArmStrength ("Arm Strength", Range(0, 4)) = 1.6
		_ArmSoftness ("Arm Softness", Range(0.05, 0.9)) = 0.35

		// ---- grain ----
		// Arms are not smooth ribbons; they are made of clumps (star
		// clusters, H-II regions). The reference is pointillistic: the
		// granulation IS the arm texture, and multiplying the arm band by
		// fine noise is what turns a mathematically perfect spiral into a
		// photograph.
		_GrainScale ("Grain Scale", Range(4, 96)) = 34
		_GrainStrength ("Grain Strength", Range(0, 2)) = 0.85

		// ---- dust ----
		// Dark lanes that hug the arms and eat into the glow. Multiplied
		// down, never blended dark (see ADDITIVE above).
		_DustStrength ("Dust Strength", Range(0, 1)) = 0.5
		_DustScale ("Dust Scale", Range(2, 40)) = 9
		_DustThreshold ("Dust Threshold", Range(0, 1)) = 0.52

		// ---- palette ----
		// The reference's colour story: a white-hot heart, warm peach inner
		// arm regions, cool lavender outer arms, dim blue-grey haze between.
		[HDR] _CoreColor ("Core Colour", Color) = (1, 0.9, 0.75, 1)
		[HDR] _InnerArmColor ("Inner Arm Colour", Color) = (1, 0.72, 0.62, 1)
		[HDR] _OuterArmColor ("Outer Arm Colour", Color) = (0.62, 0.66, 0.95, 1)
		[HDR] _DiskColor ("Inter-Arm Haze Colour", Color) = (0.35, 0.42, 0.65, 1)
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
			"Queue" = "Transparent"
		}

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			// ADDITIVE: One/One over the black sky. The source alpha is
			// ignored -- everything that matters is already in rgb.
			Blend One One
			ZWrite Off
			ZTest LEqual
			Cull Off

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			// Every material property belongs here, or the SRP Batcher
			// drops to per-draw setup. No textures, so no _ST entries.
			CBUFFER_START(UnityPerMaterial)
				float4 _CoreColor;
				float4 _InnerArmColor;
				float4 _OuterArmColor;
				float4 _DiskColor;
				float _Intensity;
				float _DiskFalloff;
				float _ArmFalloff;
				float _CoreRadius;
				float _CoreIntensity;
				float _CoreFlatten;
				float _CoreRotation;
				float _Arms;
				float _Winding;
				float _ArmStrength;
				float _ArmSoftness;
				float _GrainScale;
				float _GrainStrength;
				float _DustStrength;
				float _DustScale;
				float _DustThreshold;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				// Alpha carries the vertical profile baked by GalaxyDisk.cs.
				// RGB is unused (baked black).
				float4 color : COLOR;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionOS : TEXCOORD0;
				half4 vertColor : TEXCOORD1;
			};

			// ---- noise ----
			// Hash-based value noise (no sin() in the hash: sin hashes are
			// the classic mobile-precision trap). Three octaves, weights 1/2/4,
			// normalised by their sum so the result spans 0..1. Octave
			// multipliers are deliberately non-integer so their grids do not
			// line up into visible structure.

			half hash12(float2 p)
			{
				float3 p3 = frac(p.xyx * 0.1031);
				p3 += dot(p3, p3.yzx + 33.33);
				return frac((p3.x + p3.y) * p3.z);
			}

			half vnoise(float2 p)
			{
				float2 i = floor(p);
				float2 f = frac(p);
				f = f * f * (3.0 - 2.0 * f);
				half a = hash12(i);
				half b = hash12(i + float2(1, 0));
				half c = hash12(i + float2(0, 1));
				half d = hash12(i + float2(1, 1));
				return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
			}

			half fbm(float2 p)
			{
				return (vnoise(p) * 0.5
					+ vnoise(p * 2.13) * 0.25
					+ vnoise(p * 4.31) * 0.125) / 0.875;
			}

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.positionOS = input.positionOS.xyz;
				output.vertColor = input.color;
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				float3 lp = input.positionOS;

				// Vertical profile, baked per layer into vertex alpha. Each
				// layer is a flat plane at constant weight -- seen edge-on
				// the stack of weights IS the band's brightness structure.
				half layerFall = input.vertColor.a;

				float r = length(lp.xz);
				float theta = atan2(lp.z, lp.x);

				// ---- radial structure ----
				half haze = exp(-r * _DiskFalloff);
				half armReach = exp(-r * _ArmFalloff);

				// ---- core: bar-capable bulge ----
				// Rotate the core's axes by _CoreRotation, then squash the
				// second axis: an exponential that is round in the squashed
				// frame is a bar in the world frame.
				float barAngle = radians(_CoreRotation);
				float snBar, csBar;
				sincos(barAngle, snBar, csBar);
				float2 coreQ = float2(
					lp.x * csBar - lp.z * snBar,
					lp.x * snBar + lp.z * csBar);
				coreQ.y *= _CoreFlatten;
				float coreD2 = dot(coreQ, coreQ);
				half core = exp(-coreD2 / max(_CoreRadius * _CoreRadius, 1e-5));
				// The white-hot heart, radius ~0.063 of the disk. A second
				// term rather than a brighter colour: a tight spike over a
				// broad bulge is not the shape of one exp.
				half heart = exp(-coreD2 / 0.004);

				// ---- spiral arms ----
				// round() is LOAD-BEARING, not cosmetic: sin(N*theta + phase)
				// is continuous across the atan2 wrap only when N is an
				// integer. A 2.5 would jump a half-phase at theta = +-pi and
				// cut the disk with a visible seam line.
				float armCount = max(1.0, round(_Arms));
				float twist = _Winding * log(max(r, 0.03));
				half band = sin(armCount * theta + twist) * 0.5 + 0.5;

				// Grain: the clumps. One fbm stack, sampled once, multiplied
				// into the arm term. The noise is everywhere, but only the
				// arms let it show. The 0.35 floor keeps inter-clump gaps
				// from going fully black inside the arms.
				half grain = fbm(lp.xz * _GrainScale);
				half arm = smoothstep(1.0 - _ArmSoftness, 1.0, band)
					* armReach * _ArmStrength
					* (0.35 + _GrainStrength * grain);

				// ---- dust ----
				// A broader, independent stack: dust patches are larger than
				// grain clumps. Weighted toward the arm band so the lanes
				// hug the arms.
				half dustN = fbm(lp.xz * _DustScale + 43.7);
				half dust = smoothstep(_DustThreshold, 1.0, dustN)
					* lerp(0.35, 1.0, band);

				// ---- assembly ----
				// Haze gets its own quieter grain so the between-arms space
				// is not dead smooth; +17.3 decorrelates it from the arm
				// stack over the same base domain.
				half hazeGrain = 0.55
					+ _GrainStrength * 0.6 * fbm(lp.xz * _GrainScale * 0.5 + 17.3);
				half3 col = _DiskColor.rgb * haze * hazeGrain;

				// Arms: warm toward the centre (the reference's pink inner
				// regions), cool outward.
				half innerMix = smoothstep(0.65, 0.2, r);
				half3 armColor = lerp(_OuterArmColor.rgb, _InnerArmColor.rgb, innerMix);
				col += armColor * arm;

				col += _CoreColor.rgb * core * _CoreIntensity;
				col += heart * _CoreIntensity;

				// Dust eats the finished glow, core included -- that is what
				// cuts the dark rifts into the bulge's rim.
				col *= 1.0 - dust * _DustStrength;

				col *= layerFall * _Intensity;
				return half4(col, 1.0);
			}
			ENDHLSL
		}
	}

	// No fallback SubShader. The project is URP-only -- the graphics
	// settings bind a UniversalRenderPipelineAsset -- so a built-in
	// CGPROGRAM pass here is unreachable code Unity still compiles.
	Fallback Off
}
