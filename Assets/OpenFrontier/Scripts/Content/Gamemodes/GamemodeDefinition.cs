using UnityEngine;

namespace OpenFrontier.Gamemodes
{
	/// <summary>
	/// One playable gamemode, authored as its own asset under
	/// Assets/OpenFrontier/Resources/Gamemodes/. Nothing names a gamemode in
	/// code or in a scene: the NewGame screen, and later the mod loader, all
	/// work through these records (docs/gamemode-modding-design.md).
	///
	/// Id is the stable cross-reference everything else keys on - the launch
	/// seam, saves, later achievements. It is namespaced from birth
	/// ("core.devtest"; mods are "mod.<author>.*") because renaming after
	/// saves exist is the reserved-id-band problem all over again. Ship it
	/// once, never retype it.
	///
	/// Deliberately PRESENTATION-ONLY for now: launch payload (seeding,
	/// starting conditions) lands with the engine's world-boot. That is safe
	/// to defer - ScriptableObjects take added serialized fields without
	/// breaking existing assets, so the record grows without a migration.
	///
	/// An asset is self-contained by design (its preview sprite lives inside
	/// it), which is what will let a mod's definition ship inside an
	/// AssetBundle unchanged.
	/// </summary>
	[CreateAssetMenu(menuName = "OpenFrontier/Gamemode Definition")]
	public class GamemodeDefinition : ScriptableObject
	{
		[Tooltip("Stable unique id, never retyped after first ship: 'core.devtest'. Built-ins are 'core.*'; mods are 'mod.<author>.*'. The registry dedupes on this and saves will key on it.")]
		public string Id;

		[Tooltip("Shown on the selection row and the preview card.")]
		public string Title;

		[Tooltip("Full description, shown on the preview card.")]
		[TextArea]
		public string Description;

		[Tooltip("Preview image for the card. Self-contained on purpose: a mod's definition carries its own sprite, so the asset ships inside its bundle unchanged.")]
		public Sprite PreviewImage;

		[Tooltip("Registry sorts by this, then Title. Built-ins own 0..99; keep the dev gamemode at 999 so it never shoves a real mode down the list.")]
		public int SortOrder;

		[Tooltip("Visible only in the editor and development builds - the registry filters it out, so a release build never even lists it.")]
		public bool DeveloperOnly;
	}
}
