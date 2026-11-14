Shader "OpenFrontier/GalaxyDust"
{
	// Two-term dust for GalaxyClouds -- the physical model the additive
	// billboards shader cannot express: a dust cloud both SCATTERS the
	// starlight hitting it and EXTINCTS the light behind it.
	//
	// PASS 1 "Occlude" (multiplicative): the silhouette term.
	// result = dst x (1 - alpha): each cloud multiplies the light
	// behind it -- stacked clouds attenuate exponentially, ratios
	// survive, and the core stays visible THROUGH the rift instead of
	// clamping flat black (the RevSub first draft subtracted linearly
	// per layer; with tens of puffs stacked on every core sightline the
	// sum saturated and the whole centre went black). Over empty black
	// it is still a no-op. Extinction is a property of the dust COLUMN,
	// not of the starlight, so this pass deliberately has NO
	// presence/bulge terms: that asymmetry (scatter falls with radius,
	// extinction does not) is what makes outer dust darken instead of
	// glow. Extinction DOES take the arm band (lanes hug the arms), the
	// vertical weight, and the near fade -- the fade is load-bearing on
	// this pass: without it a cloud at the lens blacks out the whole
	// frame.
	//
	// PASS 2 "Scatter" (additive): the glow term, and it draws AFTER
	// the extinction ON PURPOSE -- a cloud's inscattered light is
	// emitted toward the camera and must not be eaten by its own
	// column (see the ORDER note). Brightness follows the starlight
	// around the cloud -- the same radial/bulge/arm functions the
	// billboards shader uses -- so core clouds glow hard (dense
	// starlight to scatter) and rim clouds barely. data.w is not tinted
	// by: clouds bake w = 0 (dust does not whiten) and this shader never
	// reads it.
	//
	// THE RIFT GATE: extinction fades IN with radius, between
	// _RiftInner and _RiftOuter (unit-disk radii). The bulge's dust only
	// SCATTERS: at the core, eaten light is re-emitted at near unity
	// (radiative equilibrium) and the blazing bulge is
	// re-emission-dominated -- but our blend cannot re-emit what it
	// eats, so left ungated, core extinction over-competes and the
	// centre went black (measured). The gate encodes "core dust glows,
	// disk dust silhouettes" in one ramp: inside Inner the Occlude pass
	// is silent, outside Outer it is full strength, and the rifts live
	// exactly where the reference puts them -- from the bulge's edge
	// outward, where there is background worth eating.
	//
	// THE DEPTH GATE: extinction applies only to light from BEHIND the
	// quad, and at galaxy scale the only emissive structure whose
	// brightness matters is the CORE. So the Occlude pass gates per
	// QUAD on CAMERA DISTANCE against the galaxy's origin: dust nearer
	// than the core occludes fully, dust beyond it goes silent (it
	// would otherwise darken the core from behind -- far-side blotches
	// on the bulge in edge-on views). The fade runs from the core's
	// distance OUTWARD over a band of one Rift Inner radius of world
	// size, floored at 5% of the scale so Rift Inner at 0 keeps a thin
	// sane transition instead of a zero-width smoothstep (hard step,
	// NaN at the tie). Dust AT the core's distance still dims: it sits
	// inside the emitting volume, which is the reference's band
	// mottling. A
	// side effect is the classic edge-on look: the dust lane reads on
	// the NEAR half of the disk while the far side's dust goes quiet.
	// First draft compared VIEW-AXIS depth (TransformWorldToView) and
	// silently silenced extinction whenever the core was not ahead of
	// the lens -- the axis projection drops it behind nearer dust.
	// Camera distance is orientation-proof, and a quad that projects
	// over the core lies near the camera-core axis, where radial and
	// axis ordering agree. The honest limitation: this protects the
	// core, not every star -- per-pixel occlusion would need depth
	// prepasses (the hologram's Solid Depth pattern), heavier than the
	// galaxy earns today.
	//
	// THE GLOW FLOOR: a cloud that extincts still inscatters ambient
	// starlight (radiative equilibrium, locally) -- occluding dust
	// reads as faint glowing wisps, never pure black holes. The floor
	// ties to the per-cloud occlusion (the occluders are the ones
	// glowing), the arm band and the vertical weight, and deliberately
	// NOT to radial presence: it is what keeps the rim rifts backlit.
	//
	// URP PASS RULE (measured on the hologram, repeated here because it
	// cost the feature a silent day): the forward draw matches ONE pass
	// per LightMode name. A second pass tagged UniversalForward never
	// issues a draw call -- the material rendered purely additive and
	// the occlusion knob did nothing. So the two passes carry
	// UniversalForward (Occlude) and UniversalForwardOnly (Scatter),
	// and the order between them is LOAD-BEARING now: the tag list
	// order (SRPDefaultUnlit -> UniversalForward ->
	// UniversalForwardOnly) mirrors the declared order, so Occlude
	// draws first and the glow lands on top of all extinction. Any
	// future third pass needs a distinct tag of its own.
	//
	// OCCLUSION CHANNEL: TEXCOORD2.x carries the per-cloud occlusion the
	// C# bakes (GalaxyClouds.OcclusionPower shapes it: mostly wispy, a
	// few dense). A separate stream, not data.w, so a clouds material
	// left on the plain billboards shader keeps its exact old look.
	// _Occlusion is the global strength multiplier -- THE knob against
	// the additive wash along sightlines through the disk.
	//
	// QUEUE CONTRACT (baked per shader, overridable by a material's
	// Custom Render Queue): the disk paints at Transparent (3000), the
	// billboards shader (stars) draws at Transparent+1, this shader at
	// Transparent+2 -- the subtract must draw AFTER what it darkens --
	// and the local shell's material sets +3 so the nearest stars punch
	// through the dust undimmed. Global stars draw before the dust and
	// read as "behind" it: the aggregate look is right (a dark lane
	// shows fewer stars), and per-star front/behind is unresolvable
	// without depth this layer does not keep.
	//
	// ORDER IS LOAD-BEARING between the two passes (measured): with
	// Scatter first, every cloud's glow landed BEFORE the extinction,
	// so a dense occluder multiplied down its OWN just-added glow --
	// a blob at alpha ~0.7 erased 70% of its own light and read as a
	// pure black hole ("the occluding clouds are not visible
	// themselves"). Now Occlude draws first and Scatter second: the
	// extinction eats only what the queue placed behind the clouds
	// (disk, stars), and every cloud's inscattered glow lands on top
	// of ALL extinction. The honest cost: clouds no longer dim each
	// other's GLOW either (dust in front of glowing dust fails to
	// darken it) -- accepted at galaxy scale; per-quad depth is not
	// kept in this layer. Quad order INSIDE a pass still does not
	// matter: both blending ops are linear in the framebuffer (dst +
	// glow, dst x (1 - dark)), no depth sorting needed inside the
	// layer.
	//
	// NO HDR CAVEAT: multiplicative blending cannot push the buffer
	// negative, so the RevSub draft's float-target worry is gone.
	//
	// The disk shader's own painted dust term (_DustStrength) still
	// carves the sheet's rifts beneath -- the billboard lanes add the
	// volumetric structure on top.
	//
	// MOBILE: two passes over the same mesh = two draw calls, vertex
	// shading doubled for the CLOUD layer only (a few thousand quads);
	// the occlude pass also runs its fragments, and clouds are already
	// the fill-rate layer -- Count stays the first knob down.
	//
	// Same property names as GalaxyBillboards: swapping a material over
	// keeps its authored values; new properties (Dust Occlusion) take
	// their defaults.
	//
	// DETERMINISM and the LOCAL-UNITS contract: identical to
	// GalaxyBillboards -- positions and sizes baked by the C#, splitmix64
	// seeds, bounds padded past the billboard centres.

	Properties
	{
		// ---- palette / placement shading (same functions as the disk) ----
		[IntRange] _Arms ("Arm Count", Range(1, 8)) = 2
		_Winding ("Spiral Winding", Range(0, 14)) = 5
		// Feeds the inter-arm dimming's smoothstep width, exactly as in
		// the disk's arm term.
		_ArmSoftness ("Arm Softness", Range(0.05, 0.9)) = 0.35
		// How dark the inter-arm population goes (1 = no banding). On
		// the Occlude pass this same curve also shapes EXTINCTION:
		// lanes hug the arms.
		_ArmDimFloor ("Inter-Arm Dimming", Range(0, 1)) = 0.55
		// How quickly the SCATTER glow dims toward the rim. Does not
		// touch extinction (see the header's asymmetry note).
		_PresenceFalloff ("Radial Dimming", Range(0.5, 8)) = 3
		// Extra scatter brightness inside the bulge: dust there sits in
		// the densest starlight.
		_BulgeRadius ("Bulge Radius", Range(0.02, 0.6)) = 0.3
		_BulgeBoost ("Bulge Brightening", Range(0, 6)) = 2
		[HDR] _InnerColor ("Inner Colour", Color) = (1, 0.78, 0.68, 1)
		[HDR] _OuterColor ("Outer Colour", Color) = (0.7, 0.74, 1, 1)

		// ---- look ----
		_Intensity ("Overall Intensity", Range(0, 8)) = 1
		// Global size multiplier on the per-point baked size.
		_SizeScale ("Size Scale", Range(0.05, 4)) = 1
		// exp(-d*d*k) on the -1..1 corner: low k = soft puffs. Shapes
		// BOTH passes: the glow and the extinction share the cloud's
		// soft edge. Only used while the shape-texture keyword is OFF.
		_EdgeSharpness ("Edge Sharpness", Range(0.5, 30)) = 3.5
		// Per-layer extinction opacity for the Occlude pass: each cloud
		// multiplies the light behind it by (1 - this). THE knob for the
		// rifts -- and it works in SMALL values: N overlapping clouds
		// attenuate exponentially (x (1-a)^N), and the core is where
		// tens of puffs stack on every sightline, so the core wants
		// ~0.05-0.2 per layer or the whole centre goes black. Per-cloud
		// variation (TEXCOORD2 from the C#) rides on top; at 1 the
		// densest cloud erases everything behind it.
		_Occlusion ("Dust Occlusion", Range(0, 1)) = 0.25
		// The RIFT GATE (see the header): extinction fades in between
		// these unit-disk radii. Inside Inner, bulge dust only
		// scatters; outside Outer it is full silhouette. The
		// disk shader's bulge is ~10-20% of the radius, so the defaults
		// start the rifts just past its edge.
		_RiftInner ("Rift Inner Radius", Range(0, 1)) = 0.3
		_RiftOuter ("Rift Outer Radius", Range(0, 1)) = 0.65
		// The GLOW FLOOR (see the header): minimum inscattered glow,
		// tied to the per-cloud occlusion -- occluding dust still
		// glows. Survives radial dimming on purpose.
		_GlowFloor ("Glow Floor", Range(0, 1)) = 0.15
		// Optional shape texture: rgb tints the scatter glow, alpha is
		// the mask for BOTH passes (extinction is achromatic -- the
		// texture's colour belongs to the light, not the dust).
		// Assign a texture BEFORE enabling the toggle -- enabled with
		// none assigned samples black and the layer vanishes.
		[MainTexture] _MainTex ("Shape Texture", 2D) = "black" {}
		[Toggle(_SHAPE_TEXTURE)] _UseShapeTexture ("Use Shape Texture (else procedural)", Float) = 0
		// Near fade in WORLD units, shared by both passes: brighten the
		// scatter from 0 at Start to full at End, and -- load-bearing on
		// the subtract -- keep the Occlude pass from blacking out the
		// frame when the camera sits inside a cloud.
		_NearFadeStart ("Near Fade Start (world units)", Float) = 40
		_NearFadeEnd ("Near Fade End (world units)", Float) = 150
		// Proximity brightening on the SCATTER only: dust near the
		// camera gains glow as the starlight around it concentrates.
		// Neutral at 0.
		_ProximityDistance ("Proximity Range (world units)", Float) = 400
		_ProximityBoost ("Proximity Brightening", Range(0, 8)) = 0
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
			// After the disk (3000) and the stars (+1) so the subtract
			// has glow to eat; the local shell sets +3 to punch through.
			"Queue" = "Transparent+2"
		}

		Pass
		{
			Name "Occlude"
			// UniversalForward, and FIRST: URP's forward draw matches
			// ONE pass per LightMode name -- a second UniversalForward
			// pass never issues a draw call (the same measured lesson
			// the hologram's outline hull paid for). The tag list order
			// (SRPDefaultUnlit -> UniversalForward ->
			// UniversalForwardOnly) mirrors the declared order, so the
			// extinction draws BEFORE the scatter glow: a cloud's own
			// light must not be eaten by its own column (see the
			// header's ORDER note).
			Tags { "LightMode" = "UniversalForward" }

			// Multiplicative extinction -- result = dst x (1 - alpha):
			// the radiative-transfer shape. Each layer multiplies the
			// light behind it, stacked layers attenuate exponentially,
			// and RATIOS survive -- the core stays the brightest thing
			// THROUGH the dust instead of clamping flat black, which is
			// what the earlier RevSub pass did once tens of puffs stacked
			// on every core sightline (dst - c per layer accumulates
			// linearly and saturates). Over empty black it is still a
			// no-op. RGB is discarded (source factor zero); the frag
			// writes the extinction into ALPHA. ZWrite stays off, no
			// depth: this layer orders by queue, not by depth.
			Blend Zero OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Cull Off

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature_local _SHAPE_TEXTURE

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			// Each HLSLPROGRAM compiles alone (structs/vertices are never
			// shared across passes), so the cbuffer, texture and structs
			// repeat here in full.
			CBUFFER_START(UnityPerMaterial)
				float4 _InnerColor;
				float4 _OuterColor;
				float _Arms;
				float _Winding;
				float _ArmSoftness;
				float _ArmDimFloor;
				float _PresenceFalloff;
				float _BulgeRadius;
				float _BulgeBoost;
				float _Intensity;
				float _SizeScale;
				float _EdgeSharpness;
				float _Occlusion;
				float _RiftInner;
				float _RiftOuter;
				float _GlowFloor;
				float _NearFadeStart;
				float _NearFadeEnd;
				float _ProximityDistance;
				float _ProximityBoost;
				float4 _MainTex_ST;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 corner : TEXCOORD0;
				float4 data : TEXCOORD1;
				// x: per-cloud occlusion 0..1, baked by the C#'
				// OcclusionPower (mostly wispy, a few dense). A separate
				// stream so a clouds material left on the plain
				// billboards shader keeps its exact old look.
				float4 occlusion : TEXCOORD2;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 corner : TEXCOORD0;
				// rgb all carry the SAME value: extinction is achromatic.
				half4 color : TEXCOORD1;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;

				float3 centerOS = input.positionOS.xyz;
				float3 centerWS = TransformObjectToWorld(centerOS);

				// ---- spherical billboard, identical to the scatter pass
				// (each HLSLPROGRAM compiles alone) ----
				float3 toCamera = _WorldSpaceCameraPos.xyz - centerWS;
				float dist = length(toCamera);
				float3 viewDir = toCamera / max(dist, 1e-4);

				float3 right = cross(float3(0, 1, 0), viewDir);
				float rightLen = length(right);
				right = rightLen > 1e-4 ? right / rightLen : float3(1, 0, 0);
				float3 up = cross(viewDir, right);

				float size = input.data.x * _SizeScale
					* length(mul((float3x3)GetObjectToWorldMatrix(), float3(1, 0, 0)));

				float3 cornerWS = centerWS
					+ (right * input.corner.x + up * input.corner.y) * size;
				output.positionCS = TransformWorldToHClip(cornerWS);
				output.corner = input.corner;

				// ---- extinction: the same arm band the scatter pass
				// uses (lanes hug the arms), WITHOUT the starlight terms
				// -- the asymmetry that makes outer dust darken instead
				// of glow ----
				float r = length(centerOS.xz);
				float theta = atan2(centerOS.z, centerOS.x);
				float armCount = max(1.0, round(_Arms));
				float twist = _Winding * log(max(r, 0.03));
				half band = sin(armCount * theta + twist) * 0.5 + 0.5;
				half armBand = smoothstep(1.0 - _ArmSoftness, 1.0, band);
				half armDim = lerp(1.0,
					lerp(_ArmDimFloor, 1.0, armBand),
					smoothstep(0.0, _BulgeRadius, r));

				// Near fade on the SUBTRACT is load-bearing: a cloud at
				// the lens would black out the whole frame. Same world
				// units, same material knobs as the scatter pass.
				half fade = saturate((dist - _NearFadeStart)
					/ max(_NearFadeEnd - _NearFadeStart, 1e-3));

				// The RIFT GATE: extinction fades in with radius --
				// bulge dust only scatters (see the header). The max
				// guard keeps smoothstep's edges ordered if the knobs
				// are authored crossing.
				half rift = smoothstep(_RiftInner,
					max(_RiftOuter, _RiftInner + 0.01), r);
				// ---- the DEPTH GATE: far-side dust cannot darken the
				// core ---- extinction applies only to light from BEHIND
				// the quad; at galaxy scale the only emitter whose
				// brightness matters is the core, referenced at the
				// disk's own origin (robust to the galaxy sitting off
				// world origin). FIRST DRAFT compared view-axis depth
				// (TransformWorldToView) -- and silently silenced
				// extinction in any camera pose where the core is not
				// ahead of the lens: the origin's axis projection drops
				// behind nearer dust and the gate ramped to zero
				// (measured as "occlusion has stopped occluding" while
				// flying). CAMERA DISTANCE -- radial, reusing the near
				// fade's `dist` at zero extra cost -- is
				// orientation-proof: a quad projecting over the core is
				// near the camera-core axis, where radial and axis
				// ordering agree. The band is one Rift Inner radius of
				// world size -- the same knob, so the no-occlude region
				// is the same size in radius and depth.
				float galaxyScale = length(
					mul((float3x3)GetObjectToWorldMatrix(), float3(1, 0, 0)));
				float3 galaxyOriginWS = mul(GetObjectToWorldMatrix(),
					float4(0, 0, 0, 1)).xyz;
				float coreDist = length(
					galaxyOriginWS - _WorldSpaceCameraPos.xyz);
				// The band floors at 5% of the scale: with Rift Inner
				// authored to 0 (the reference look wants dust dimming
				// right at the core), a raw zero-width smoothstep
				// degenerates to a hard step and NaNs at the exact tie.
				float depthBand = max(_RiftInner, 0.05) * galaxyScale;
				// The ramp runs from the core's distance OUTWARD, not
				// centred on it: dust AT the core's depth legitimately
				// dims the glow -- it sits inside the emitting volume,
				// which is precisely the reference's band mottling --
				// and centring the ramp was halving exactly those
				// clouds.
				half depthGate = 1.0 - smoothstep(
					coreDist, coreDist + depthBand, dist);
				half dark = input.data.z          // vertical weight
					* saturate(input.occlusion.x) // per-cloud density
					* _Occlusion                  // global strength
					* armDim                      // lanes hug the arms
					* fade
					* rift
					* depthGate;

				// Extinction rides the ALPHA channel (the blend multiplies
				// the framebuffer by 1 - alpha); rgb is discarded.
				output.color = half4(0, 0, 0, dark);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				float2 uv = input.corner * 0.5 + 0.5;
			#if defined(_SHAPE_TEXTURE)
				// Alpha is the mask: the texture's colour belongs to the
				// scatter pass's light, not to the dust.
				float2 texUV = uv * _MainTex_ST.xy + _MainTex_ST.zw;
				half shape = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, texUV).a;
			#else
				float d = length(input.corner);
				half shape = exp(-d * d * _EdgeSharpness);
			#endif
				return half4(0, 0, 0, input.color.a * shape);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Scatter"
			// UniversalForwardOnly, NOT a second UniversalForward: URP's
			// forward draw matches ONE pass per LightMode name (see the
			// Occlude pass's note). Second in the file = second to
			// draw: the glow lands on top of ALL extinction, so an
			// occluding cloud stays visible inside the darkening it
			// applies -- the measured fix for the pure-black-hole
			// blobs.
			Tags { "LightMode" = "UniversalForwardOnly" }

			Blend One One
			ZWrite Off
			ZTest LEqual
			Cull Off

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			// Local keyword: only the material that opts in pays for the
			// texture variant.
			#pragma shader_feature_local _SHAPE_TEXTURE

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			// Shape texture (see Properties): per material. The _ST
			// vector lives inside the cbuffer for SRP-batcher
			// compatibility; the sampler cannot.
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			CBUFFER_START(UnityPerMaterial)
				float4 _InnerColor;
				float4 _OuterColor;
				float _Arms;
				float _Winding;
				float _ArmSoftness;
				float _ArmDimFloor;
				float _PresenceFalloff;
				float _BulgeRadius;
				float _BulgeBoost;
				float _Intensity;
				float _SizeScale;
				float _EdgeSharpness;
				float _Occlusion;
				float _RiftInner;
				float _RiftOuter;
				float _GlowFloor;
				float _NearFadeStart;
				float _NearFadeEnd;
				float _ProximityDistance;
				float _ProximityBoost;
				float4 _MainTex_ST;
			CBUFFER_END

			struct Attributes
			{
				// xyz: the billboard's centre, in the galaxy's local frame.
				float4 positionOS : POSITION;
				// -1..1 quad corner.
				float2 corner : TEXCOORD0;
				// x: size in local units (times the transform's scale in
				// the shader). y: brightness 0..1, pre-shaped by the C#
				// distribution. z: vertical weight baked against the
				// disk's thickness profile. w: 0 for clouds.
				float4 data : TEXCOORD1;
				// x: per-cloud occlusion 0..1, baked by the C#'s
				// OcclusionPower -- the glow floor reads it (the
				// occluders are the ones glowing).
				float4 occlusion : TEXCOORD2;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 corner : TEXCOORD0;
				half4 color : TEXCOORD1;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;

				float3 centerOS = input.positionOS.xyz;
				float3 centerWS = TransformObjectToWorld(centerOS);

				// ---- spherical billboard ----
				float3 toCamera = _WorldSpaceCameraPos.xyz - centerWS;
				float dist = length(toCamera);
				float3 viewDir = toCamera / max(dist, 1e-4);

				// Camera-facing frame from the world-up cross product.
				// Degenerate guard is LOAD-BEARING: a camera straight above
				// a puff (the galaxy stage's overhead view!) sends the
				// cross product to zero, and normalize(0) is NaN -- which
				// renders as a black hole exactly at the top of the frame.
				// The fallback keeps those billboards facing along world X.
				float3 right = cross(float3(0, 1, 0), viewDir);
				float rightLen = length(right);
				right = rightLen > 1e-4 ? right / rightLen : float3(1, 0, 0);
				float3 up = cross(viewDir, right);

				// Size: baked local size x the object's world scale (taken
				// from the x axis; the galaxy scales uniformly), x the
				// material's global multiplier.
				float size = input.data.x * _SizeScale
					* length(mul((float3x3)GetObjectToWorldMatrix(), float3(1, 0, 0)));

				float3 cornerWS = centerWS
					+ (right * input.corner.x + up * input.corner.y) * size;
				output.positionCS = TransformWorldToHClip(cornerWS);
				output.corner = input.corner;

				// ---- colour, from the disk's own functions ----
				float r = length(centerOS.xz);
				float theta = atan2(centerOS.z, centerOS.x);
				// round() is load-bearing here exactly as in the disk
				// shader: sin(N*theta + phase) is continuous across the
				// atan2 wrap only for integer N.
				float armCount = max(1.0, round(_Arms));
				float twist = _Winding * log(max(r, 0.03));
				half band = sin(armCount * theta + twist) * 0.5 + 0.5;
				half armBand = smoothstep(1.0 - _ArmSoftness, 1.0, band);
				half armDim = lerp(1.0,
					lerp(_ArmDimFloor, 1.0, armBand),
					smoothstep(0.0, _BulgeRadius, r));
				half innerMix = smoothstep(0.65, 0.2, r);
				half3 col = lerp(_OuterColor.rgb, _InnerColor.rgb, innerMix);

				half presence = exp(-r * _PresenceFalloff);
				float coreD2 = dot(centerOS.xz, centerOS.xz);
				half coreBoost = exp(-coreD2
					/ max(_BulgeRadius * _BulgeRadius, 1e-5));

				// Scatter brightness: vertical weight x the pre-shaped
				// distribution x radial presence x the bulge swarm boost
				// x the inter-arm band. This is "the starlight around
				// the cloud": it falls with radius, which is what makes
				// core clouds glow and rim clouds go quiet -- plus the
				// glow floor, which keeps occluding rim dust backlit
				// instead of black (see the header's GLOW FLOOR note).
				half bright = input.data.z
					* (0.3 + 0.7 * input.data.y)
					* presence
					* (1.0 + _BulgeBoost * coreBoost)
					* armDim
					+ _GlowFloor * saturate(input.occlusion.x)
						* input.data.z * armDim;

				// No data.w tint: clouds bake w = 0, and occlusion lives
				// on TEXCOORD2 -- the light, not the dust, carries colour.

				// Near fade: 0 at Start, full at End. Also the fill-rate
				// guard -- the closest (largest-on-screen) billboards are
				// the ones it removes.
				half fade = saturate((dist - _NearFadeStart)
					/ max(_NearFadeEnd - _NearFadeStart, 1e-3));

				// Proximity brightening (scatter only): full at the
				// camera, gone at _ProximityDistance. Neutral at boost 0.
				half proximity = 1.0 + _ProximityBoost
					* (1.0 - saturate(dist / max(_ProximityDistance, 1e-3)));

				output.color = half4(
					col * bright * fade * _Intensity * proximity, 1.0);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				// Full-quad UV from the -1..1 corner: the mesh bakes
				// corners, not UVs, so texture mode costs nothing extra.
				float2 uv = input.corner * 0.5 + 0.5;
			#if defined(_SHAPE_TEXTURE)
				// Texture mode: the texture owns the shape. rgb tints
				// (white textures preserve the vertex palette), alpha is
				// the mask; the manual _ST transform honours
				// tiling/offset for sprite atlases.
				float2 texUV = uv * _MainTex_ST.xy + _MainTex_ST.zw;
				half4 shape = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, texUV);
				return half4(input.color.rgb * shape.rgb * shape.a, 1.0);
			#else
				// Procedural mode: exp falloff on the -1..1 corner, soft
				// by default for puffs. exp(d*d) is zero enough by the
				// diagonal that no quad edge is ever visible.
				float d = length(input.corner);
				half falloff = exp(-d * d * _EdgeSharpness);
				return half4(input.color.rgb * falloff, 1.0);
			#endif
			}
			ENDHLSL
		}
	}

	// No fallback SubShader. The project is URP-only -- the graphics
	// settings bind a UniversalRenderPipelineAsset -- so a built-in
	// CGPROGRAM pass here is unreachable code Unity still compiles.
	Fallback Off
}
