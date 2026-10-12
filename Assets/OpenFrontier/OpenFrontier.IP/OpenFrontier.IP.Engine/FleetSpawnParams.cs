using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class FleetSpawnParams
	{
		public Faction Faction;

		[FormerlySerializedAs("GroupPrefab")]
		public Fleet FleetPrefab;

		public Unit HomeBase;

		[FormerlySerializedAs("HomeScene")]
		public Sector HomeSector;

		public string ShipDesignation;

		public List<FleetSpawnShipParams> Ships = new List<FleetSpawnShipParams>();

		public Unit TargetDock;

		[FormerlySerializedAs("TargetPosition")]
		public Vector3 TargetSectorPosition = Vector3.zero;

		[FormerlySerializedAs("TargetScene")]
		public Sector TargetSector;
	}
}
