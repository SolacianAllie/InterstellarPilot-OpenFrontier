using UnityEngine;
using UnityEngine.Pool;

namespace OpenFrontier.Engine
{
	/// <summary>
	/// One pool per prefab: rents active instances, returns them inactive
	/// under the pool's parent. Wraps UnityEngine.Pool.ObjectPool —
	/// Unity-native first, no hand-rolled pooling. Prewarm keeps the first
	/// burst from instantiating mid-action (a mobile GC hitch reads as a
	/// random frame drop, not as the allocation it is); maxSize caps
	/// runaway growth. Consumers (projectiles, particles, audio sources)
	/// arrive with later milestones — the class exists so the first one
	/// lands through it, not around it. EngineCore holds the pool set when
	/// consumers exist; M1 ships the primitive.
	/// </summary>
	public sealed class PrefabPool<T> where T : Component
	{
		private readonly ObjectPool<T> pool;

		/// <param name="prefab">The template every instance clones.</param>
		/// <param name="prewarm">Instances created up front (the legacy
		/// engine prewarmed 3 per pool).</param>
		/// <param name="maxSize">Returned instances beyond this are
		/// destroyed instead of pooled — the runaway cap.</param>
		/// <param name="parent">Where parked instances live (keep the
		/// hierarchy flat and searchable by one parent).</param>
		public PrefabPool(T prefab, int prewarm = 3, int maxSize = 256, Transform parent = null)
		{
			pool = new ObjectPool<T>(
				createFunc: () => Object.Instantiate(prefab, parent),
				actionOnGet: instance => instance.gameObject.SetActive(true),
				actionOnRelease: instance => instance.gameObject.SetActive(false),
				actionOnDestroy: Object.Destroy,
				maxSize: maxSize);

			for (int i = 0; i < prewarm; i++)
			{
				pool.Release(pool.Get());
			}
		}

		/// <summary>Rent an instance — active, freshly reset by the
		/// caller's own OnEnable/first-use code.</summary>
		public T Get() => pool.Get();

		/// <summary>Return an instance to the pool (deactivated). Double
		/// releases are dev-guarded by ObjectPool's collection check.</summary>
		public void Return(T instance) => pool.Release(instance);

		/// <summary>Destroy every pooled instance (scene teardown).</summary>
		public void Clear() => pool.Clear();
	}
}
