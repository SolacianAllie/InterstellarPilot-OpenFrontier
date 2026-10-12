namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// What a slot is for. The identity a component class must match exactly to
	/// fit.
	/// </summary>
	/// <remarks>
	/// APPEND ONLY. Never insert a value above the new-types section, and never
	/// reorder anything.
	///
	/// This enum is serialized as an integer in every ComponentBayType prefab -
	/// all fourteen of them carry an explicit BayType - so the number a prefab
	/// stores is the number it will mean forever. Inserting a value in the
	/// middle does not compile differently and does not warn: it silently
	/// re-points every prefab above the insertion. EngineBayType stores 3, so
	/// anything added at position 3 becomes "Engine" as far as that asset is
	/// concerned, and the bay type a part must match changes underneath it.
	///
	/// That has already happened once on this project in a different enum:
	/// MaxClass was renamed without FormerlySerializedAs and every value in
	/// 1,237 bays would have reset to the field initializer. Same failure, same
	/// silence.
	///
	/// Values are written out rather than left implicit for that reason - so
	/// reordering a line becomes a visible edit rather than a silent shift.
	///
	/// Each value also names an icon: GetIconSpriteName returns
	/// "{BayType}Bay", so a new type wants a sprite of that name in
	/// Resources/Textures/UI/ComponentBayTypeIcons. Missing one is not fatal -
	/// GetComponentBayTypeSpriteOrDefault falls back to a default sprite - so
	/// the type can be created before its artwork exists.
	/// </remarks>
	public enum BayType
	{
		Capacitor = 0,
		Countermeasure = 1,
		Electronics = 2,
		Engine = 3,
		Hyperdrive = 4,
		Mine = 5,
		Turret = 6,
		PowerGenerator = 7,
		Shield = 8,
		TractorBeam = 9,
		PassengerModule = 10,
		Mining = 11,
		DamageControl = 12,

		// Nothing above this line may ever move.
		//
		// The new component system's core internals. Distinct from the old ones
		// rather than renamed versions of them, so the two systems stay
		// separable and the old UIs can still find both: a CoreInternal bay of
		// the new Reactor type refuses a legacy PowerGenerator class, and a
		// legacy one refuses a new Reactor class, because Category is compared
		// exactly and both sides would have to agree.
		Bulkhead = 13,
		Reactor = 14,
		ThrusterMount = 15,
		FrameShiftDrive = 16,
		EnvironmentControl = 17,
		PowerCoupling = 18,
		SensorSuite = 19,
		FuelStore = 20
	}
}