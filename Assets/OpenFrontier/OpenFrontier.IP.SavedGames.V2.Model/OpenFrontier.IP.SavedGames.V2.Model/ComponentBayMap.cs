using System.Collections.Generic;

namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public static class ComponentBayMap
	{
		public static Dictionary<ModelUnitClass, List<ModelComponentBay>> UnitClassComponentBays { get; }

		static ComponentBayMap()
		{
			UnitClassComponentBays = new Dictionary<ModelUnitClass, List<ModelComponentBay>>();
			AddBay(ModelUnitClass.Ship_Ares, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_Ares, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure"
			});
			AddBay(ModelUnitClass.Ship_Ares, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_Ares, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Ares, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_Ares, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_Ares, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Ares, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_Ares, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_Ares, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield"
			});
			AddBay(ModelUnitClass.Ship_Ares, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Ares, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_AresA, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_AresA, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure"
			});
			AddBay(ModelUnitClass.Ship_AresA, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_AresA, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_AresA, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_AresA, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_AresA, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_AresA, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_AresA, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_AresA, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield"
			});
			AddBay(ModelUnitClass.Ship_AresA, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_AresA, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_AresX, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_AresX, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_AresX, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure"
			});
			AddBay(ModelUnitClass.Ship_AresX, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_AresX, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_AresX, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_AresX, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_AresX, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_AresX, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_AresX, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_AresX, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield"
			});
			AddBay(ModelUnitClass.Ship_AresX, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_AresX, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_Creon, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_Creon, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_Creon, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Creon, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Creon, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Lower Turret"
			});
			AddBay(ModelUnitClass.Ship_Creon, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_Creon, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_Creon, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Upper Turret"
			});
			AddBay(ModelUnitClass.Ship_Creon, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Shield,
				Name = "Shield"
			});
			AddBay(ModelUnitClass.Ship_Creon, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Creon, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module "
			});
			AddBay(ModelUnitClass.Ship_CreonML, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_CreonML, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_CreonML, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine"
			});
			AddBay(ModelUnitClass.Ship_CreonML, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_CreonML, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_CreonML, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_CreonML, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_CreonML, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Lower Turret"
			});
			AddBay(ModelUnitClass.Ship_CreonML, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Upper Turret"
			});
			AddBay(ModelUnitClass.Ship_CreonML, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Shield,
				Name = "Shield"
			});
			AddBay(ModelUnitClass.Ship_CreonML, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_CreonA, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_CreonA, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_CreonA, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_CreonA, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_CreonA, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Lower Turret"
			});
			AddBay(ModelUnitClass.Ship_CreonA, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_CreonA, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_CreonA, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Upper Turret"
			});
			AddBay(ModelUnitClass.Ship_CreonA, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Shield,
				Name = "Shield"
			});
			AddBay(ModelUnitClass.Ship_CreonA, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_CreonA, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module "
			});
			AddBay(ModelUnitClass.Ship_Drake, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_Drake, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_Drake, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_Drake, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Drake, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Main Turret"
			});
			AddBay(ModelUnitClass.Ship_Drake, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Drake, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_Drake, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_Drake, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_Drake, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_Drake, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Drake, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_DrakeA, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_DrakeA, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_DrakeA, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_DrakeA, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Left Turret"
			});
			AddBay(ModelUnitClass.Ship_DrakeA, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Main Turret"
			});
			AddBay(ModelUnitClass.Ship_DrakeA, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_DrakeA, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Right Turret"
			});
			AddBay(ModelUnitClass.Ship_DrakeA, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_DrakeA, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_DrakeA, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_DrakeA, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_DrakeA, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_Flyer, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_Flyer, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure"
			});
			AddBay(ModelUnitClass.Ship_Flyer, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Engine,
				Name = "Engine"
			});
			AddBay(ModelUnitClass.Ship_Flyer, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Flyer, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_Flyer, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Mine,
				Name = "Mine"
			});
			AddBay(ModelUnitClass.Ship_Flyer, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_Flyer, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_Flyer, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Flyer, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield"
			});
			AddBay(ModelUnitClass.Ship_Flyer, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Flyer, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_FlyerA, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_FlyerA, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure"
			});
			AddBay(ModelUnitClass.Ship_FlyerA, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Engine,
				Name = "Engine"
			});
			AddBay(ModelUnitClass.Ship_FlyerA, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Left Turret"
			});
			AddBay(ModelUnitClass.Ship_FlyerA, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_FlyerA, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Mine,
				Name = "Mine"
			});
			AddBay(ModelUnitClass.Ship_FlyerA, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_FlyerA, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_FlyerA, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Right Turret"
			});
			AddBay(ModelUnitClass.Ship_FlyerA, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield"
			});
			AddBay(ModelUnitClass.Ship_FlyerA, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_FlyerA, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_FlyerX, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_FlyerX, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine"
			});
			AddBay(ModelUnitClass.Ship_FlyerX, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Turret,
				Name = "Left Turret"
			});
			AddBay(ModelUnitClass.Ship_FlyerX, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure"
			});
			AddBay(ModelUnitClass.Ship_FlyerX, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Right Turret"
			});
			AddBay(ModelUnitClass.Ship_FlyerX, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Bay"
			});
			AddBay(ModelUnitClass.Ship_FlyerX, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_FlyerX, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_FlyerX, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_FlyerX, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_FlyerX, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_FlyerX, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_Hauler, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_Hauler, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_Hauler, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Turret,
				Name = "Front Turret"
			});
			AddBay(ModelUnitClass.Ship_Hauler, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_Hauler, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_Hauler, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_Hauler, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_Hauler, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Bay"
			});
			AddBay(ModelUnitClass.Ship_Hauler, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_Hauler, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_Hauler, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Hauler, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics Bay"
			});
			AddBay(ModelUnitClass.Ship_Hauler, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_HaulerP, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_HaulerP, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerP, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Turret,
				Name = "Front Turret"
			});
			AddBay(ModelUnitClass.Ship_HaulerP, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerP, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_HaulerP, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_HaulerP, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_HaulerP, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerP, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_HaulerP, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerP, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_HaulerP, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerP, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_HaulerA, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_HaulerA, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerA, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Turret,
				Name = "Front Turret"
			});
			AddBay(ModelUnitClass.Ship_HaulerA, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerA, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_HaulerA, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_HaulerA, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_HaulerA, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerA, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_HaulerA, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerA, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_HaulerA, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerA, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_HaulerH, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_HaulerH, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerH, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Turret,
				Name = "Front Turret"
			});
			AddBay(ModelUnitClass.Ship_HaulerH, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerH, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_HaulerH, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_HaulerH, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerH, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_HaulerH, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerH, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_HaulerH, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerH, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_HaulerM, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_HaulerM, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerM, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerM, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Mining,
				Name = "Front Mining Turret"
			});
			AddBay(ModelUnitClass.Ship_HaulerM, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Mining,
				Name = "Mid Mining Turret"
			});
			AddBay(ModelUnitClass.Ship_HaulerM, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_HaulerM, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerM, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_HaulerM, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_HaulerM, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_HaulerM, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_HaulerM, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics Bay"
			});
			AddBay(ModelUnitClass.Ship_Hornet, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_Hornet, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_Hornet, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_Hornet, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Hornet, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Mine,
				Name = "Mine"
			});
			AddBay(ModelUnitClass.Ship_Hornet, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Hornet, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_Hornet, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_Hornet, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_Hornet, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Hornet, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_HornetA, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_HornetA, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_HornetA, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_HornetA, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_HornetA, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Left Turret"
			});
			AddBay(ModelUnitClass.Ship_HornetA, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Mine,
				Name = "Mine"
			});
			AddBay(ModelUnitClass.Ship_HornetA, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Right Turret"
			});
			AddBay(ModelUnitClass.Ship_HornetA, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_HornetA, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_HornetA, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_HornetA, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_HornetX, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_HornetX, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_HornetX, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_HornetX, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_HornetX, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Left Turret"
			});
			AddBay(ModelUnitClass.Ship_HornetX, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Mine,
				Name = "Mine"
			});
			AddBay(ModelUnitClass.Ship_HornetX, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Right Turret"
			});
			AddBay(ModelUnitClass.Ship_HornetX, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_HornetX, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_HornetX, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_HornetX, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Magnus, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_Magnus, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_Magnus, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Magnus, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_Magnus, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Main Turret"
			});
			AddBay(ModelUnitClass.Ship_Magnus, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Magnus, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Turret"
			});
			AddBay(ModelUnitClass.Ship_Magnus, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_Magnus, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Magnus, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_Magnus, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Magnus, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_Magnus, new ModelComponentBay
			{
				Id = 13,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Magnus, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_MagnusA, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_MagnusA, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_MagnusA, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusA, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusA, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Main Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusA, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusA, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusA, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusA, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Left Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusA, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_MagnusA, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Right Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusA, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_MagnusA, new ModelComponentBay
			{
				Id = 13,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_MagnusA, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_MagnusEQ, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_MagnusEQ, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_MagnusEQ, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusEQ, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusEQ, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusEQ, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusEQ, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusEQ, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Left Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusEQ, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_MagnusEQ, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Right Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusEQ, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_MagnusEQ, new ModelComponentBay
			{
				Id = 13,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_MagnusEQ, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_MagnusX, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_MagnusX, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_MagnusX, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusX, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Main Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusX, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusX, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusX, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusX, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Left Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusX, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_MagnusX, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Right Turret"
			});
			AddBay(ModelUnitClass.Ship_MagnusX, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_MagnusX, new ModelComponentBay
			{
				Id = 13,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_MagnusX, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_MagnusX, new ModelComponentBay
			{
				Id = 200,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_Orion, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_Orion, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure"
			});
			AddBay(ModelUnitClass.Ship_Orion, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_Orion, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Orion, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_Orion, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Turret"
			});
			AddBay(ModelUnitClass.Ship_Orion, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Orion, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Orion, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_Orion, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield"
			});
			AddBay(ModelUnitClass.Ship_Orion, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Orion, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_Orion, new ModelComponentBay
			{
				Id = 200,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_Orion, new ModelComponentBay
			{
				Id = 110,
				Type = ModelComponentBayType.Turret,
				Name = "Top Right Turret"
			});
			AddBay(ModelUnitClass.Ship_OrionA, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_OrionA, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure"
			});
			AddBay(ModelUnitClass.Ship_OrionA, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_OrionA, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_OrionA, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_OrionA, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Turret"
			});
			AddBay(ModelUnitClass.Ship_OrionA, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_OrionA, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Left Turret"
			});
			AddBay(ModelUnitClass.Ship_OrionA, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_OrionA, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield"
			});
			AddBay(ModelUnitClass.Ship_OrionA, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_OrionA, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_OrionA, new ModelComponentBay
			{
				Id = 200,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_OrionA, new ModelComponentBay
			{
				Id = 110,
				Type = ModelComponentBayType.Turret,
				Name = "Top Right Turret"
			});
			AddBay(ModelUnitClass.Ship_OrionX, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_OrionX, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure"
			});
			AddBay(ModelUnitClass.Ship_OrionX, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_OrionX, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_OrionX, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_OrionX, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Turret"
			});
			AddBay(ModelUnitClass.Ship_OrionX, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_OrionX, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Left Turret"
			});
			AddBay(ModelUnitClass.Ship_OrionX, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_OrionX, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield"
			});
			AddBay(ModelUnitClass.Ship_OrionX, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_OrionX, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_OrionX, new ModelComponentBay
			{
				Id = 200,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_OrionX, new ModelComponentBay
			{
				Id = 110,
				Type = ModelComponentBayType.Turret,
				Name = "Top Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Overlord, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_Overlord, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_Overlord, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_Overlord, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Overlord, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Overlord, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_Overlord, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Overlord, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Lower Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_Overlord, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_Overlord, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_Overlord, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Overlord, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_Overlord, new ModelComponentBay
			{
				Id = 13,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Overlord, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_OverlordA, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_OverlordA, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_OverlordA, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_OverlordA, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_OverlordA, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Left Turret"
			});
			AddBay(ModelUnitClass.Ship_OverlordA, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_OverlordA, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_OverlordA, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Lower Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_OverlordA, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_OverlordA, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_OverlordA, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Right Turret"
			});
			AddBay(ModelUnitClass.Ship_OverlordA, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_OverlordA, new ModelComponentBay
			{
				Id = 13,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_OverlordA, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_OverlordX, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_OverlordX, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_OverlordX, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_OverlordX, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_OverlordX, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_OverlordX, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_OverlordX, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_OverlordX, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Lower Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_OverlordX, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_OverlordX, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_OverlordX, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_OverlordX, new ModelComponentBay
			{
				Id = 13,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_OverlordX, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_OverlordX, new ModelComponentBay
			{
				Id = 200,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_Pioneer, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_Pioneer, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_Pioneer, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_Pioneer, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Pioneer, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Mine,
				Name = "Mine"
			});
			AddBay(ModelUnitClass.Ship_Pioneer, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_Pioneer, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Pioneer, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_Pioneer, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_Pioneer, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_Pioneer, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Pioneer, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_Pioneer, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_PioneerP, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_PioneerP, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_PioneerP, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_PioneerP, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Left Turret"
			});
			AddBay(ModelUnitClass.Ship_PioneerP, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Mine,
				Name = "Mine"
			});
			AddBay(ModelUnitClass.Ship_PioneerP, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_PioneerP, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Right Turret"
			});
			AddBay(ModelUnitClass.Ship_PioneerP, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_PioneerP, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_PioneerP, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_PioneerP, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_PioneerP, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_PioneerP, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_PioneerA, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_PioneerA, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_PioneerA, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_PioneerA, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Left Turret"
			});
			AddBay(ModelUnitClass.Ship_PioneerA, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Mine,
				Name = "Mine"
			});
			AddBay(ModelUnitClass.Ship_PioneerA, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_PioneerA, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Right Turret"
			});
			AddBay(ModelUnitClass.Ship_PioneerA, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_PioneerA, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_PioneerA, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_PioneerA, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_PioneerA, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_PioneerA, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_Ranger, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_Ranger, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_Ranger, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_Ranger, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Main Turret"
			});
			AddBay(ModelUnitClass.Ship_Ranger, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_Ranger, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_Ranger, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Ranger, new ModelComponentBay
			{
				Id = 16,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Bay"
			});
			AddBay(ModelUnitClass.Ship_Ranger, new ModelComponentBay
			{
				Id = 20,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_RangerA, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_RangerA, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_RangerA, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_RangerA, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Main Turret"
			});
			AddBay(ModelUnitClass.Ship_RangerA, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_RangerA, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_RangerA, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_RangerA, new ModelComponentBay
			{
				Id = 16,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Bay"
			});
			AddBay(ModelUnitClass.Ship_RangerA, new ModelComponentBay
			{
				Id = 20,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_RangerH, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_RangerH, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_RangerH, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_RangerH, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_RangerH, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_RangerH, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_RangerH, new ModelComponentBay
			{
				Id = 16,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Bay"
			});
			AddBay(ModelUnitClass.Ship_RangerH, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Main Turret"
			});
			AddBay(ModelUnitClass.Ship_RangerM, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_RangerM, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_RangerM, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_RangerM, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Mining,
				Name = "Main Mining Turret"
			});
			AddBay(ModelUnitClass.Ship_RangerM, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_RangerM, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_RangerM, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_RangerM, new ModelComponentBay
			{
				Id = 16,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Bay"
			});
			AddBay(ModelUnitClass.Ship_RangerM, new ModelComponentBay
			{
				Id = 20,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_Raptor, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_Raptor, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_Raptor, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_Raptor, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_Raptor, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_Raptor, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Raptor, new ModelComponentBay
			{
				Id = 16,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Bay"
			});
			AddBay(ModelUnitClass.Ship_Raptor, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_Raptor, new ModelComponentBay
			{
				Id = 150,
				Type = ModelComponentBayType.Turret,
				Name = "Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Raptor, new ModelComponentBay
			{
				Id = 120,
				Type = ModelComponentBayType.Turret,
				Name = "Right Turret"
			});
			AddBay(ModelUnitClass.Ship_RaptorA, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_RaptorA, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_RaptorA, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_RaptorA, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_RaptorA, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_RaptorA, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_RaptorA, new ModelComponentBay
			{
				Id = 16,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Bay"
			});
			AddBay(ModelUnitClass.Ship_RaptorA, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_RaptorA, new ModelComponentBay
			{
				Id = 150,
				Type = ModelComponentBayType.Turret,
				Name = "Left Turret"
			});
			AddBay(ModelUnitClass.Ship_RaptorA, new ModelComponentBay
			{
				Id = 120,
				Type = ModelComponentBayType.Turret,
				Name = "Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Shuttle, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_Shuttle, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_Shuttle, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_Shuttle, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Shuttle, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Shuttle, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_Shuttle, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_Shuttle, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Shuttle, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_ShuttleA, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_ShuttleA, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_ShuttleA, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_ShuttleA, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_ShuttleA, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Left Turret"
			});
			AddBay(ModelUnitClass.Ship_ShuttleA, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Right Turret"
			});
			AddBay(ModelUnitClass.Ship_ShuttleA, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_ShuttleA, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_ShuttleA, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Thunder, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_Thunder, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure"
			});
			AddBay(ModelUnitClass.Ship_Thunder, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_Thunder, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Thunder, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Thunder, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Turret"
			});
			AddBay(ModelUnitClass.Ship_Thunder, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_Thunder, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_Thunder, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield"
			});
			AddBay(ModelUnitClass.Ship_Thunder, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Thunder, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_Thunder, new ModelComponentBay
			{
				Id = 200,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_Thunder, new ModelComponentBay
			{
				Id = 300,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Left Turret"
			});
			AddBay(ModelUnitClass.Ship_Thunder, new ModelComponentBay
			{
				Id = 400,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Right Turret"
			});
			AddBay(ModelUnitClass.Ship_ThunderA, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_ThunderA, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure"
			});
			AddBay(ModelUnitClass.Ship_ThunderA, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_ThunderA, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_ThunderA, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_ThunderA, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Turret"
			});
			AddBay(ModelUnitClass.Ship_ThunderA, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_ThunderA, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_ThunderA, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield"
			});
			AddBay(ModelUnitClass.Ship_ThunderA, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_ThunderA, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_ThunderA, new ModelComponentBay
			{
				Id = 200,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_ThunderA, new ModelComponentBay
			{
				Id = 300,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Left Turret"
			});
			AddBay(ModelUnitClass.Ship_ThunderA, new ModelComponentBay
			{
				Id = 400,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Right Turret"
			});
			AddBay(ModelUnitClass.Ship_ThunderX, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_ThunderX, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure"
			});
			AddBay(ModelUnitClass.Ship_ThunderX, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_ThunderX, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Left Turret"
			});
			AddBay(ModelUnitClass.Ship_ThunderX, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.Turret,
				Name = "Front Right Turret"
			});
			AddBay(ModelUnitClass.Ship_ThunderX, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Turret"
			});
			AddBay(ModelUnitClass.Ship_ThunderX, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_ThunderX, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_ThunderX, new ModelComponentBay
			{
				Id = 10,
				Type = ModelComponentBayType.Shield,
				Name = "Shield"
			});
			AddBay(ModelUnitClass.Ship_ThunderX, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_ThunderX, new ModelComponentBay
			{
				Id = 100,
				Type = ModelComponentBayType.Passenger,
				Name = "Passenger Module"
			});
			AddBay(ModelUnitClass.Ship_ThunderX, new ModelComponentBay
			{
				Id = 200,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_ThunderX, new ModelComponentBay
			{
				Id = 300,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Left Turret"
			});
			AddBay(ModelUnitClass.Ship_ThunderX, new ModelComponentBay
			{
				Id = 400,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Right Turret"
			});
			AddBay(ModelUnitClass.Ship_Venture, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_Venture, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_Venture, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_Venture, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Turret"
			});
			AddBay(ModelUnitClass.Ship_Venture, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_Venture, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_Venture, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_Venture, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_Venture, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_Venture, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_Venture, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_Venture, new ModelComponentBay
			{
				Id = 30,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Bay"
			});
			AddBay(ModelUnitClass.Ship_VentureA, new ModelComponentBay
			{
				Id = 1,
				Type = ModelComponentBayType.Capacitor,
				Name = "Capacitor"
			});
			AddBay(ModelUnitClass.Ship_VentureA, new ModelComponentBay
			{
				Id = 2,
				Type = ModelComponentBayType.Engine,
				Name = "Engine Bay"
			});
			AddBay(ModelUnitClass.Ship_VentureA, new ModelComponentBay
			{
				Id = 3,
				Type = ModelComponentBayType.Countermeasure,
				Name = "Countermeasure Bay"
			});
			AddBay(ModelUnitClass.Ship_VentureA, new ModelComponentBay
			{
				Id = 4,
				Type = ModelComponentBayType.Turret,
				Name = "Front Turret"
			});
			AddBay(ModelUnitClass.Ship_VentureA, new ModelComponentBay
			{
				Id = 5,
				Type = ModelComponentBayType.Turret,
				Name = "Mid Turret"
			});
			AddBay(ModelUnitClass.Ship_VentureA, new ModelComponentBay
			{
				Id = 6,
				Type = ModelComponentBayType.Turret,
				Name = "Top Turret"
			});
			AddBay(ModelUnitClass.Ship_VentureA, new ModelComponentBay
			{
				Id = 7,
				Type = ModelComponentBayType.PowerGenerator,
				Name = "Power Generator"
			});
			AddBay(ModelUnitClass.Ship_VentureA, new ModelComponentBay
			{
				Id = 8,
				Type = ModelComponentBayType.Turret,
				Name = "Rear Turret"
			});
			AddBay(ModelUnitClass.Ship_VentureA, new ModelComponentBay
			{
				Id = 9,
				Type = ModelComponentBayType.Shield,
				Name = "Shield Bay"
			});
			AddBay(ModelUnitClass.Ship_VentureA, new ModelComponentBay
			{
				Id = 11,
				Type = ModelComponentBayType.Tractor,
				Name = "Tractor Beam"
			});
			AddBay(ModelUnitClass.Ship_VentureA, new ModelComponentBay
			{
				Id = 12,
				Type = ModelComponentBayType.Electronics,
				Name = "Electronics"
			});
			AddBay(ModelUnitClass.Ship_VentureA, new ModelComponentBay
			{
				Id = 30,
				Type = ModelComponentBayType.Mine,
				Name = "Mine Bay"
			});
		}

		public static IEnumerable<ModelComponentBay> GetComponentBays(ModelUnitClass unitClass)
		{
			List<ModelComponentBay> value = null;
			if (!UnitClassComponentBays.TryGetValue(unitClass, out value))
			{
				value = new List<ModelComponentBay>();
			}
			return value;
		}

		private static void AddBay(ModelUnitClass unitClass, ModelComponentBay componentBay)
		{
			List<ModelComponentBay> value = null;
			if (!UnitClassComponentBays.TryGetValue(unitClass, out value))
			{
				value = new List<ModelComponentBay>();
				UnitClassComponentBays[unitClass] = value;
			}
			value.Add(componentBay);
		}
	}
}
