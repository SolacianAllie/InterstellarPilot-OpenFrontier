using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class StarParticleSystem : CustomParticles
	{
		public bool AutoScale;

		public Vector3 BaseOffset = new Vector3(0f, 0f, 13f);

		public float CameraMaxParticleSpeed = 10f;

		private float cameraSpeed;

		private Vector3 camVelocity = Vector3.zero;

		public float EmitterShapeRadius = 20f;

		public float MaxParticleLifetime = 5f;

		public float maxParticleSize = 0.005f;

		public float MinParticleLifeTime = 1f;

		public float MovementOffsetMultiplier = 0.5f;

		public float ParticleLifeTimeMultiplier = 0.5f;

		private ParticleSystemRenderer particleSystemRenderer;

		public float SizeLerpValue = 1f;

		public float FadeParticlesSpeedThreshold = 8f;

		private MaterialPropertyBlock particlesMaterialPropertyBlock;

		public Color FadedOutColor;

		public Color FadedInColor;

		public float MoveTowardsCameraVelocityRate = 200f;

		protected override void start()
		{
			base.start();
			particleSystemRenderer = (ParticleSystemRenderer)ParticleSystem.GetComponent<Renderer>();
			particleSystemRenderer.maxParticleSize = maxParticleSize;
			particlesMaterialPropertyBlock = new MaterialPropertyBlock();
			if (AutoScale)
			{
				particleSystemRenderer.maxParticleSize = 0f;
			}
			particleSystemRenderer.enabled = false;
		}

		private float GetParticleStartLifeTime()
		{
			if (cameraSpeed == 0f)
			{
				return MaxParticleLifetime;
			}
			float num = EmitterShapeRadius / cameraSpeed * ParticleLifeTimeMultiplier;
			if (num > MaxParticleLifetime)
			{
				return MaxParticleLifetime;
			}
			if (num < MinParticleLifeTime)
			{
				return MinParticleLifeTime;
			}
			return num;
		}

		private float GetMaxParticleSize()
		{
			if (AutoScale)
			{
				return maxParticleSize;
			}
			float b = Mathf.Clamp(cameraSpeed / CameraMaxParticleSpeed, 0f, 1f) * maxParticleSize;
			return Mathf.Lerp(particleSystemRenderer.maxParticleSize, b, SizeLerpValue * Time.deltaTime);
		}

		protected override void update()
		{
			base.update();
			SetParticlesAlpha();
			camVelocity = Vector3.MoveTowards(camVelocity, GameController.Instance.MainCameraVelocity, Time.deltaTime * MoveTowardsCameraVelocityRate);
			cameraSpeed = camVelocity.magnitude;
			ParticleSystem.MainModule main = ParticleSystem.main;
			main.startLifetime = GetParticleStartLifeTime();
			particleSystemRenderer.maxParticleSize = GetMaxParticleSize();
			bool flag = cameraSpeed > 0f;
			if (flag)
			{
				Vector3 vector = GameController.Instance.MainCamera.transform.worldToLocalMatrix * camVelocity;
				ParticleSystem.transform.localPosition = BaseOffset + vector * MovementOffsetMultiplier;
				SetParticlesAlpha();
			}
			particleSystemRenderer.enabled = flag;
		}

		private void SetParticlesAlpha()
		{
			// URP port: legacy "_Color" is unused by URP Particles/Unlit; drive _BaseColor instead
			Color color = particlesMaterialPropertyBlock.GetColor("_BaseColor");
			color = Color.Lerp(FadedOutColor, FadedInColor, Mathf.Clamp01(cameraSpeed / FadeParticlesSpeedThreshold));
			particlesMaterialPropertyBlock.SetColor("_BaseColor", color);
			particleSystemRenderer.SetPropertyBlock(particlesMaterialPropertyBlock);
		}
	}
}
