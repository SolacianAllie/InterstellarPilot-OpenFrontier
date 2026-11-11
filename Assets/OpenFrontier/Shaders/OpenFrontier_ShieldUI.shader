Shader "OpenFrontier/Shield UI" {
	// The shield UI's arc shader - the hologram family's look, as a UI
	// line. Name SPACES ON PURPOSE (same rule as every OpenFrontier
	// shader): a curator typing "shield" finds it.
	//
	// WHAT IT IS
	//
	// Body copied from the stock TextMeshPro/Sprite machinery (the same
	// shape as OpenFrontier/UI Saturation, verified against this stack's
	// installed UnityUI.cginc), with three changes that make it the
	// shield UI:
	//
	//   - ADDITIVE, the hologram doctrine: Blend One One, light that
	//     ARRIVES over what is behind it, never a blend that reveals.
	//     Because nothing is blended away, every alpha on the path
	//     becomes BRIGHTNESS: the sprite's own shape (ShieldLine is
	//     white art on transparency), the per-Image alpha and the
	//     canvas fade (CanvasRenderer bakes both into the vertex
	//     color's alpha) and a RectMask2D clip all dim the emitted
	//     light. Fading an additive glow means emitting less - that is
	//     the honest behavior for a shield line, and it is why the
	//     alpha math runs on rgb, not on an alpha channel.
	//
	//   - HDR by construction: [HDR] Tint + an Intensity knob on the
	//     hologram family's strength ceiling (0-8, the same range the
	//     hologram's Outline Strength and Normal Edge Strength ride -
	//     equal sliders, equal brightness across the family). Energy
	//     past 1.0 feeds the loading screens' ACES+Bloom volume, which
	//     fattens a hot line exactly like it fattens the hologram's.
	//     TWO usage notes the shader cannot enforce:
	//       * URP renders Screen Space - Overlay canvases AFTER
	//         post-processing - bloom never sees them. Put the shield
	//         ring on a Screen Space - Camera (or World Space) canvas
	//         or the HDR range reads tone-mapped flat.
	//       * The per-Image color is the old game's per-section tint
	//         path (the health ramp, LDR by design - the STATE lives
	//         there, six arcs share one material); the material's
	//         Intensity and HDR Tint are the shared look knobs.
	//
	//   - The energy fold (tint * intensity) runs PER-VERTEX: the
	//     fragment stays one texture tap and one multiply chain.
	//     Mobile-first: target 2.0, one sampler, no branches beyond
	//     the stock keyword ifs.
	//
	// Stencil/mask machinery is the stock set - the card-art Mask
	// recipe keeps working unchanged. _TextureSampleAdd is kept (uGUI's
	// bitmap slot machinery).
	Properties {
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[HDR] _Color ("Tint (HDR)", Color) = (0.25, 0.85, 1.0, 1.0)
		// Hologram-family ceiling: 0-8, same as the ship hologram's
		// line systems. 1 = no overdrive; 2+ starts feeding bloom.
		_Intensity ("HDR Intensity", Range(0, 8)) = 2.0

		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_CullMode ("Cull Mode", Float) = 0
		_ColorMask ("Color Mask", Float) = 15
		_ClipRect ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}

	SubShader {
		Tags {
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Stencil {
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull [_CullMode]
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		// Pure additive - the hologram family's blend (Shape Line, the
		// holo's forward passes). Alpha never writes: light only adds.
		Blend One One
		ColorMask [_ColorMask]

		Pass {
			Name "Default"
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0

				#include "UnityCG.cginc"
				#include "UnityUI.cginc"

				#pragma multi_compile __ UNITY_UI_CLIP_RECT
				#pragma multi_compile __ UNITY_UI_ALPHACLIP

				struct appdata_t {
					float4 vertex   : POSITION;
					float4 color    : COLOR;
					float2 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				// half, not the stock fixed: this shader carries HDR
				// energy (tint * intensity can reach 8x), and lowp
				// fixed clamps to [-2, 2] on some hardware - the
				// overdrive would die in the interpolator.
				struct v2f {
					float4 vertex   : SV_POSITION;
					half4 color     : COLOR;
					float2 texcoord : TEXCOORD0;
					float4 worldPosition : TEXCOORD1;
					UNITY_VERTEX_OUTPUT_STEREO
				};

				sampler2D _MainTex;
				half4 _Color;
				half _Intensity;
				half4 _TextureSampleAdd;
				float4 _ClipRect;
				float4 _MainTex_ST;

				v2f vert(appdata_t v) {
					v2f OUT;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
					OUT.worldPosition = v.vertex;
					OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
					OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
					// The energy fold, per-vertex: the per-Image tint
					// (the old game's per-section health-ramp path)
					// times the material's HDR tint and intensity.
					// Alpha stays FADE ONLY - it dims, never overdrives.
					OUT.color = v.color;
					OUT.color.rgb *= _Color.rgb * _Intensity;
					OUT.color.a = v.color.a;
					return OUT;
				}

				half4 frag(v2f IN) : SV_Target {
					half4 tex = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;

					// Everything that "would have been transparency"
					// on an alpha-blended UI is brightness here: the
					// sprite's shape, the canvas fade, a RectMask2D.
					half brightness = tex.a * IN.color.a;
					#ifdef UNITY_UI_CLIP_RECT
						brightness *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
					#endif
					#ifdef UNITY_UI_ALPHACLIP
						clip (brightness - 0.001);
					#endif

					return half4(tex.rgb * brightness * IN.color.rgb, brightness);
				}
			ENDCG
		}
	}
}
