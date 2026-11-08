using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP
{
	[Serializable]
	public class TurretGroup
	{
		public List<ActiveTurret> Turrets = new List<ActiveTurret>();
	}
}
