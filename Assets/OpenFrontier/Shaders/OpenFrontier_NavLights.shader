// Open Frontier - navigation lights.
//
// One additive, fully unlit pass for the NavLight prefab's quad. There is no
// lighting model and none is wanted: a nav light is a pure emissive chip. The
// pass does four jobs, all of them in the GPU so no ship ever runs an Update
// for a decoration:
//
//  1. BILLBOARD. The quad's own vertices are ignored except for their corner
//     offsets. The shader rebuilds the quad facing the camera from the anchor
//     (the transform origin) - so a NavLight prefab can sit at any rotation
//     and still faces you.
//
//  2. DISTANCE GROW. A light chip has a real size (a wingtip lamp), but the
//     whole point of nav lights is reading a ship at kilometres. Past a
//     reference distance the chip grows linearly with camera distance, capped
//     at a max world size, so the angular size never falls below a readable
//     pinprick - the same trick URP's lens flares call attenuation, done in
//     the vertex shader instead of a screen-space pass.
//
//  3. BLINK. Pattern comes per instance (see NavLight.cs): Solid, a single
//     pulse or a double pulse - the aviation kit. Timing is _Time.y on the
//     GPU; the CPU never touches it. frac() bounds the phase argument so a
//     session running for hours cannot drift the pattern.
//
//  4. OCCLUSION + FADE. ZTest LEqual against the hull means a light on the
//     far side of a ship is hidden by the ship itself, for free. The far fade
//     ends before the game camera's 8500 far plane so a light never pops.
//
// Per-instance state (colour, pattern, period, phase, size) rides the GPU
// instancing buffer via MaterialPropertyBlock - that is what the SRP batcher
// cannot carry, so the batching story for these draw calls is GPU instancing,
// not SRP batching. The material must have "Enable GPU Instancing" ticked
// (the NavLightPrefabBuilder does it) and all lights share one quad mesh +
// one material, which is the batch that instancing folds into ONE draw call
// per material.
//
// This deliberately does NOT read _CameraDepthTexture. AGENTS.md: on this
// OpenGL stack sceneZ reads ~0 behind open space and every depth-compare we
// have written against it has died. The hardware Z test is the whole story
// here.

Shader "OpenFrontier/Nav Lights"
{
	Properties
	{
		[NoScaleOffset][MainTex] _FlareTex("Flare Texture (none = soft round dot)", 2D) = "white" {}
		// The white default is a boring square, so the pass shapes its own
		// round falloff on top of whatever texture it gets. A custom texture
		// that is already a halo gets softly corner-dimmed by this; turn the
		// toggle off for full-bleed art.
		[Toggle(_ROUND)] _Round("Round Shaping", Float) = 1

		// Growth + fade are material-wide (every nav light shares the world
		// rules), not per instance. Tune here once.
		_GrowReferenceDistance("Grow Starts At (m)", Float) = 250
		_MaxGrowWorldSize("Max Grown Size (world units)", Float) = 40
		// The game camera's far plane is 8500 (see ActiveUnit.SetDrawObjectsActive
		// - it is the real bound of anything drawn). End the fade before it.
		_FarFadeStart("Far Fade Start (m)", Float) = 7200
		_FarFadeEnd("Far Fade End (m)", Float) = 8300

		// Per-instance values (colour / pattern / period / phase / size) are
		// not Properties - they arrive through the instancing buffer from
		// NavLight's MaterialPropertyBlock.
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent+12"
			"RenderType" = "Transparent"
			"RenderPipeline" = "UniversalPipeline"
			"IgnoreProjector" = "True"
		}
		// Above the standard transparent band (3000) so gameplay particles
		// and lasers rate-sort in front of the chips.
		Pass
		{
			Name "Forward"
			Tags { "LightMode" = "UniversalForward" }

			Blend One One
			// ColorMask RGB: additive chips must not write destination alpha
			// (the frame's alpha is not a shared budget this pass should burn).
			ColorMask RGB
			ZWrite Off
			ZTest LEqual
			Cull Off

			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile _ _ROUND_ON

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_FlareTex);
			SAMPLER(sampler_FlareTex);

			float _GrowReferenceDistance;
			float _MaxGrowWorldSize;
			float _FarFadeStart;
			float _FarFadeEnd;

			// Per instance. Written by NavLight.ApplyInstanceData once at
			// spawn (and on inspector tweaks), never per frame.
			// _Color: rgb = light colour (HDR allowed), a = intensity.
			// _Flash: x = period (s), y = pattern (0 solid, 1 single, 2 double),
			//         z = phase in [0,1).
			// _Size:  base chip size in world units.
			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Flash)
				UNITY_DEFINE_INSTANCED_PROP(float, _Size)
			UNITY_INSTANCING_BUFFER_END(Props)

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				half4 color : TEXCOORD1;
			};

			// Write your own smoothstep pair for a sharp aviation-style spike:
			// hard on, hard off, most of the cycle dark.
			half FlashSpike(half t)
			{
				half rise = smoothstep(0.0, 0.02, t);
				half fall = 1.0 - smoothstep(0.08, 0.10, t);
				return saturate(rise * fall);
			}

			half FlashPattern(half t, half pattern)
			{
				// 0 solid, 1 single pulse at cycle start,
				// 2 a tight pair: the second pulse a fifth of a cycle behind
				// the first, then DARK - two flashes, then a pause until the
				// cycle restarts.
				if (pattern < 0.5)
				{
					return 1.0;
				}
				half first = FlashSpike(t);
				if (pattern < 1.5)
				{
					return first;
				}
				// The pair: first spans [0, 0.10], second [0.20, 0.30]. The
				// blinks sit a tenth of a cycle apart (~120 ms at the default
				// 1.2 s period, so they read as two, not one flicker) and the
				// remaining ~70% of the cycle is the pause. Phase-encoded, so
				// the shape holds at any FlashPeriod.
				return max(first, FlashSpike(frac(t + 0.80)));
			}

			Varyings vert(Attributes input)
			{
				UNITY_SETUP_INSTANCE_ID(input);

				// anchor = the transform origin; the quad's own geometry
				// contributes only its corner sign (.xy in [-0.5, 0.5]).
				float3 anchorWS = TransformObjectToWorld(float3(0, 0, 0));

				float3 camDir = anchorWS - _WorldSpaceCameraPos.xyz;
				float dist = length(camDir);

				// Billboard basis rows out of the inverse view matrix.
				float3 right = normalize(UNITY_MATRIX_I_V._m00_m01_m02);
				float3 up = normalize(UNITY_MATRIX_I_V._m10_m11_m12);

				float4 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
				float4 flash = UNITY_ACCESS_INSTANCED_PROP(Props, _Flash);
				float size = UNITY_ACCESS_INSTANCED_PROP(Props, _Size);

				// Grow: below the reference distance the chip is its real
				// size; beyond, it grows linearly and stops at the cap.
				float grow = min(dist / max(_GrowReferenceDistance, 1.0), _MaxGrowWorldSize);
				size = max(size, grow);

				// Z RELIEF. The anchor hangs ON the hull, and a screen-aligned
				// billboard lies in a plane of CONSTANT depth - so it z-fights
				// the hull pixel for pixel and ZTest LEqual bites jagged
				// wedges out of any lamp mounted on a hull crest or seen at an
				// oblique angle (the "not facing the camera" read: it was a
				// depth cut, not an orientation fault). Displacing ALONG the
				// view ray keeps every corner on the ray it was on - the
				// projection and therefore the flare's screen footprint do
				// not move; only the depth does, which is exactly what the
				// LEqual test needed. The displacement is clamped near the
				// camera, where shrinking the apparent-size denominator by
				// the push would otherwise visibly inflate the flare.
				float zPush = min(size * 0.5, dist * 0.05);

				float3 posWS = anchorWS
					+ (camDir / max(dist, 1e-4)) * zPush
					+ right * (input.positionOS.x * size)
					+ up * (input.positionOS.y * size);

				float period = max(flash.x, 1e-3);
				half pattern = flash.y;
				half phase = frac(flash.z);

				// The phase argument stays bounded, so an always-running
				// session cannot degrade the blink into mush.
				half t = frac(_Time.y / period + phase);
				half brightness = FlashPattern(t, pattern);

				float farFade = 1.0 - smoothstep(_FarFadeStart, _FarFadeEnd, dist);

				Varyings output;
				output.positionCS = TransformWorldToHClip(posWS);
				output.uv = input.uv;
				output.color = half4(tint.rgb * tint.a * brightness * farFade, 1.0);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				half4 flare = SAMPLE_TEXTURE2D(_FlareTex, sampler_FlareTex, input.uv);

				// RGB carries the art's colour, alpha the mask; both multiply
				// so a plain white texture or a black-background sprite both
				// behave.
				half3 glow = flare.rgb * flare.a;

			#if defined(_ROUND_ON)
				half2 centered = abs(input.uv - 0.5) * 2.0;
				half radial = 1.0 - saturate(dot(centered, centered));
				glow *= smoothstep(0.0, 0.35, radial);
			#endif

				return half4(glow * input.color.rgb, 1.0);
			}
			ENDHLSL
		}
	}
	Fallback Off
}
