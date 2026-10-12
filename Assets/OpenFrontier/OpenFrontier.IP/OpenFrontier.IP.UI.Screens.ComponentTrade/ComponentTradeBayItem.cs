using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.ComponentTrade
{
	public class ComponentTradeBayItem : ScrollListItem<ComponentBay>
	{
		/// <summary>
		/// Turns the group rules off everywhere, without undoing anything.
		/// </summary>
		/// <remarks>
		/// The other way out is to revert the commit - this exists so the look
		/// can be judged first and thrown away with one edit if it is wrong.
		///
		/// A static rather than a serialized field on purpose. A new serialized
		/// field is not in the prefab, so its inspector value would silently
		/// not persist - you would move it, see the change, reload, and it
		/// would be back where it started, which is a worse way to spend an
		/// afternoon than editing two numbers here.
		/// </remarks>
		public static bool GroupDividersEnabled = true;

		// A 55px row, measured off the prefab: name text spans -3..-28,
		// installed text -30..-55, and the icon owns the left 45px. So the
		// only clear horizontal band is the top 3px, and only clear of the
		// icon in the text column. Hence a rule 48px in from the left, which
		// is also where the text starts - so it lines up with the names
		// instead of cutting across the pictures.
		private const float DividerHeight = 2f;
		private const float DividerTopInset = 1f;
		private const float DividerSideInset = 48f;

		private static readonly Color DividerColor = new Color(0.45f, 0.8f, 0.85f, 0.55f);
		private static readonly Color GroupFirstNameColor = new Color(0.62f, 0.92f, 0.95f, 1f);

		public Image FiringArcImage;

		public Image IconImage;

		public Color NothingInstalledColor = Color.gray;

		public Text NameText;

		public Text InstalledText;

		private Color defaultNameTextColor = Color.white;

		private Image groupDivider;

		protected override void awake()
		{
			base.awake();
			defaultNameTextColor = NameText.color;
		}

		public override void Refresh()
		{
			base.Refresh();
			NameText.text = Item.GetFriendlyName();
			InstalledText.text = ((Item.InstalledComponent != null) ? Item.InstalledComponent.ComponentClass.GetFriendlyName() : "[None]");
			NameText.color = GetNameColor();
			IconImage.sprite = EngineASX.Instance.EngineResources.GetBayComponentOrAmmoSprite(Item);
			RefreshFiringArcSprite();
			RefreshGroupDivider();
		}

		/// <summary>
		/// An empty bay is grey, and stays grey even where it starts a group.
		/// </summary>
		/// <remarks>
		/// Nothing installed is the more useful signal of the two - "this
		/// hardpoint is empty" is what a player is looking for - so the group
		/// accent only ever layers on top of an occupied bay. Every row that
		/// does not start a group gets exactly the colour it got before this
		/// existed.
		/// </remarks>
		private Color GetNameColor()
		{
			if (Item.InstalledComponent == null)
			{
				return NothingInstalledColor;
			}

			return StartsGroup() ? GroupFirstNameColor : defaultNameTextColor;
		}

		private bool StartsGroup()
		{
			ComponentTradeBayList list = ParentList as ComponentTradeBayList;
			return (list != null && list.StartsGroup(Item));
		}

		private void RefreshGroupDivider()
		{
			if (!GroupDividersEnabled || !StartsGroup())
			{
				if (groupDivider != null)
				{
					groupDivider.enabled = false;
				}

				return;
			}

			if (groupDivider == null)
			{
				groupDivider = CreateGroupDivider();
			}

			if (groupDivider != null)
			{
				groupDivider.enabled = true;
			}
		}

		/// <summary>
		/// Builds the rule in code rather than putting it in the prefab.
		/// </summary>
		/// <remarks>
		/// Because a prefab is hand-edited here, and a hand-edited prefab that
		/// drops a field does not break the build - the reference just reads as
		/// null and whatever was wired to it stops happening while looking
		/// perfectly fine. That has happened on this project. Building the one
		/// object at runtime means there is no reference to drop, and the
		/// rollback stays "revert a commit" instead of "unwind a prefab by
		/// hand".
		///
		/// No sprite on purpose. Image.OnPopulateMesh hands a null sprite
		/// straight to Graphic, which fills the rect with the colour - checked
		/// in the uGUI source rather than assumed, because a line that renders
		/// nothing is invisible and not obviously broken.
		///
		/// Anchored to the row's top edge, which is safe: the row's height comes
		/// from the LayoutElement on the prefab root (MinHeight and
		/// PreferredHeight both 55), not from its contents, so adding a child
		/// cannot change how tall the row is.
		/// </remarks>
		private Image CreateGroupDivider()
		{
			GameObject dividerObject = new GameObject("GroupDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			dividerObject.layer = gameObject.layer;
			dividerObject.transform.SetParent(transform, worldPositionStays: false);

			RectTransform rect = dividerObject.GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(0f, 1f);
			rect.anchorMax = new Vector2(1f, 1f);
			rect.pivot = new Vector2(0.5f, 1f);
			rect.offsetMin = new Vector2(DividerSideInset, -(DividerTopInset + DividerHeight));
			rect.offsetMax = new Vector2(-DividerSideInset, -DividerTopInset);

			Image image = dividerObject.GetComponent<Image>();
			image.color = DividerColor;

			// The rule is decoration sitting inside a row the player clicks.
			// Left raycastable it would eat taps near the top of every group
			// leader, which is the one row in each group most likely to be the
			// one they were reaching for.
			image.raycastTarget = false;

			return image;
		}

		private void RefreshFiringArcSprite()
		{
			bool flag = Item.ShouldShowFiringArcSprite(showIf360degrees: true);
			FiringArcImage.enabled = flag;
			if (flag)
			{
				FiringArcImage.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f - Item.transform.localRotation.eulerAngles.y));
				FiringArcImage.sprite = Item.GetFiringArcSprite();
			}
		}
	}
}