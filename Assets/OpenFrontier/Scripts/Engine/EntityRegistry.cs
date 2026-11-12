using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.Engine
{
	/// <summary>
	/// One entity registry, keyed by id — the generic machinery behind the
	/// legacy Register/Deregister* shape (per-type facades grow as types
	/// arrive). Rules carried from the legacy engine's registry contract:
	/// add-only with a LOUD duplicate log (a dup id is an authoring or
	/// authority bug, never a silent overwrite), deregister is quiet, and
	/// Reassign is a pure map swap that deliberately advances no counter —
	/// the legacy lesson (a reassign once dragged its counter through the
	/// reserved band) is structurally impossible here because bands are
	/// physical, but the counter discipline is kept anyway.
	/// </summary>
	public sealed class EntityRegistry<T> where T : class
	{
		private readonly Dictionary<long, T> byId = new Dictionary<long, T>();
		private readonly List<T> all = new List<T>();

		public int Count => all.Count;
		public IReadOnlyList<T> All => all;

		/// <summary>Register under an id. False on a duplicate or invalid
		/// id — with a loud log; the registry never silently replaces.</summary>
		public bool Register(long id, T entity)
		{
			if (entity == null)
			{
				Debug.LogError($"[EntityRegistry] Register(null, {id}) — refusing.");
				return false;
			}

			if (!EntityId.IsValid(id))
			{
				Debug.LogError($"[EntityRegistry] Register with invalid id {id} ({entity.GetType().Name}) — refusing.");
				return false;
			}

			if (byId.ContainsKey(id))
			{
				Debug.LogError($"[EntityRegistry] Duplicate id {id} for {EntityId.KindOf(id)} — keeping the existing {byId[id].GetType().Name}, refusing {entity.GetType().Name}.");
				return false;
			}

			byId.Add(id, entity);
			all.Add(entity);
			return true;
		}

		/// <summary>Quiet deregister: true if it was there, false on a miss
		/// (a deregister of something absent is normal in cleanup paths).</summary>
		public bool Deregister(long id)
		{
			if (!byId.TryGetValue(id, out T entity))
			{
				return false;
			}

			byId.Remove(id);
			all.Remove(entity);
			return true;
		}

		public bool Contains(long id) => byId.ContainsKey(id);

		public bool TryGet(long id, out T entity) => byId.TryGetValue(id, out entity);

		/// <summary>Null on a miss — the legacy Get*ById shape.</summary>
		public T GetById(long id)
		{
			byId.TryGetValue(id, out T entity);
			return entity;
		}

		/// <summary>Move an entity from one id to another without
		/// allocating a new one from any counter (the reassign seam).</summary>
		public bool Reassign(long oldId, long newId)
		{
			if (!byId.TryGetValue(oldId, out T entity))
			{
				Debug.LogWarning($"[EntityRegistry] Reassign: {oldId} is not registered.");
				return false;
			}

			if (!EntityId.IsValid(newId) || byId.ContainsKey(newId))
			{
				Debug.LogError($"[EntityRegistry] Reassign: target {newId} is invalid or already taken.");
				return false;
			}

			byId.Remove(oldId);
			byId.Add(newId, entity);
			return true;
		}
	}
}
