using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldGeneration.Models
{
	public class SectorBlueprint
	{
		public int Id;

		public Vector3 Position;

		public float GateDistanceMultiplier = 1f;

		public List<SectorConnection> Connections = new List<SectorConnection>();

		public SectorType SectorType;

		public AsteroidType AsteroidType;

		public string Name;

		public float SecurityLevel = 0.5f;

		public bool HasPlanets => (SectorType & SectorType.Planet) != 0;

		public bool HasAsteroids => (SectorType & SectorType.Asteroid) != 0;

		public bool HasGasClouds => (SectorType & SectorType.Nebula) != 0;

		public bool ConnectionExistsAtDirection(SectorBlueprint targetNode, float tolerance)
		{
			Vector3 targetDirection = Vector3.Normalize(targetNode.Position - Position);
			return ConnectionExistsAtDirection(targetDirection, tolerance);
		}

		public bool ConnectionExistsAtDirection(Vector3 targetDirection, float toleranceRadians)
		{
			for (int i = 0; i < Connections.Count; i++)
			{
				Vector3 direction = Connections[i].GetDirection(this);
				if (Mathf.Abs(Vector3.Angle(targetDirection, direction) * (MathF.PI / 180f)) < toleranceRadians / 2f)
				{
					return true;
				}
			}
			return false;
		}
	}
}
