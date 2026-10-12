using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.ComponentTrade
{
	/// <summary>
	/// One row in the bay list, which is either a bay or a heading above a
	/// group of bays.
	/// </summary>
	/// <remarks>
	/// The same prefab does both, chosen by what the list handed it. A heading
	/// gets a short band with just the type name; a bay gets exactly what it
	/// got before this existed. Doing it with one prefab rather than two means
	/// the heading cannot drift out of step with the rows it sits above - same
	/// background, same font, same skin.
	///
	/// Everything the heading path changes is put back explicitly on the bay
	/// path, because rows are pooled and reused. A row that drew a heading and
	/// is then handed a bay has to be fully restored, not merely have the
	/// heading turned off, or a stale grey icon frame follows the list down the
	/// screen.
	/// </remarks>
	public class ComponentTradeBayItem : ScrollListItem<object>
	{
		// A heading is a caption, not a row of content, so it is shorter than
		// the 55px the bay rows use. Both come from the LayoutElement on the
		// prefab root, which the VerticalLayoutGroup on the ItemContainer
		// honours - so changing it here is what resizes the row.
		private const float HeaderHeight = 26f;

		private static readonly Color HeaderTextColor = new Color(0.62f, 0.92f, 0.95f, 1f);
		private static readonly Color HeaderBackgroundTint = new Color(0.55f, 0.72f, 0.85f, 0.16f);

		public Image FiringArcImage;

		public Image IconImage;

		public Color NothingInstalledColor = Color.gray;

		public Text NameText;

		public Text InstalledText;

		private Color defaultNameTextColor = Color.white;

		private Image background;

		private Color defaultBackgroundColor = Color.white;

		private LayoutElement layoutElement;

		private float defaultMinHeight = -1f;

		private float defaultPreferredHeight = -1f;

		private float defaultNameTextY;

		protected override void awake()
		{
			base.awake();
			defaultNameTextColor = NameText.color;
			defaultNameTextY = NameText.rectTransform.anchoredPosition.y;

			// Found rather than wired in the prefab, so this works on a prefab
			// that does not have the field and does not become a null reference
			// that fails by doing nothing. A missing one just means the heading
			// gets no tint, which is survivable; a dropped serialized field is
			// the failure mode this project has already been bitten by.
			background = GetComponent<Image>();
			if (background != null)
			{
				defaultBackgroundColor = background.color;
			}

			layoutElement = GetComponent<LayoutElement>();
			if (layoutElement != null)
			{
				defaultMinHeight = layoutElement.minHeight;
				defaultPreferredHeight = layoutElement.preferredHeight;
			}
		}

		public override void Refresh()
		{
			base.Refresh();

			ComponentBayGroupHeader header = Item as ComponentBayGroupHeader;
			if (header != null)
			{
				RefreshAsHeader(header);
				return;
			}

			ComponentBay bay = Item as ComponentBay;
			if (bay == null)
			{
				// Nothing this row knows how to draw. Left blank rather than
				// throwing, because a row that throws inside a pooled list
				// takes the rest of the list with it.
				RefreshAsBlank();
				return;
			}

			RefreshAsBay(bay);
		}

		private void RefreshAsHeader(ComponentBayGroupHeader header)
		{
			SetRowHeight(HeaderHeight);

			// Not just so it looks unselected - a non-interactable toggle is
			// what stops the pointer reaching OnToggleValueChanged, which is
			// the only route from a click to selecting a row.
			if (Toggle != null)
			{
				Toggle.interactable = false;
			}

			NameText.text = header.Text;
			NameText.color = HeaderTextColor;

			// anchoredPosition rather than the prefab's -3, because the
			// heading is a different height and the value that centres it in
			// one does not centre it in the other.
			NameText.rectTransform.anchoredPosition = new Vector2(NameText.rectTransform.anchoredPosition.x, 0f);

			InstalledText.text = string.Empty;

			if (IconImage != null)
			{
				IconImage.gameObject.SetActive(false);
			}

			FiringArcImage.enabled = false;

			if (background != null)
			{
				background.color = defaultBackgroundColor * HeaderBackgroundTint;
			}
		}

		private void RefreshAsBay(ComponentBay bay)
		{
			SetRowHeight(-1f);

			if (Toggle != null)
			{
				Toggle.interactable = true;
			}

			if (IconImage != null)
			{
				IconImage.gameObject.SetActive(true);
			}

			NameText.text = bay.GetFriendlyName();
			NameText.rectTransform.anchoredPosition = new Vector2(NameText.rectTransform.anchoredPosition.x, defaultNameTextY);

			InstalledText.text = ((bay.InstalledComponent != null) ? bay.InstalledComponent.ComponentClass.GetFriendlyName() : "[None]");
			NameText.color = ((bay.InstalledComponent != null) ? defaultNameTextColor : NothingInstalledColor);

			IconImage.sprite = EngineASX.Instance.EngineResources.GetBayComponentOrAmmoSprite(bay);

			if (background != null)
			{
				background.color = defaultBackgroundColor;
			}

			RefreshFiringArcSprite(bay);
		}

		private void RefreshAsBlank()
		{
			SetRowHeight(-1f);

			if (Toggle != null)
			{
				Toggle.interactable = false;
			}

			if (IconImage != null)
			{
				IconImage.gameObject.SetActive(false);
			}

			NameText.text = string.Empty;
			InstalledText.text = string.Empty;
			FiringArcImage.enabled = false;
		}

		/// <summary>
		/// Pass -1 to go back to whatever the prefab asked for.
		/// </summary>
		private void SetRowHeight(float height)
		{
			if (layoutElement == null)
			{
				return;
			}

			layoutElement.minHeight = (height < 0f) ? defaultMinHeight : height;
			layoutElement.preferredHeight = (height < 0f) ? defaultPreferredHeight : height;
		}

		private void RefreshFiringArcSprite(ComponentBay bay)
		{
			bool flag = bay.ShouldShowFiringArcSprite(showIf360degrees: true);
			FiringArcImage.enabled = flag;
			if (flag)
			{
				FiringArcImage.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f - bay.transform.localRotation.eulerAngles.y));
				FiringArcImage.sprite = bay.GetFiringArcSprite();
			}
		}
	}
}