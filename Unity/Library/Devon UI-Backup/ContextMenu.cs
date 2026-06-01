using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace DevionGames.UIWidgets
{
	public class ContextMenu : UIWidget
	{
		[Header ("Reference")]
		[SerializeField]
		protected MenuItem m_MenuItemPrefab= null;
		protected List<MenuItem> itemCache = new List<MenuItem> ();
		protected Canvas m_Canvas;

		public override void Show ()
		{
			base.Show ();
		}

		protected override void Update ()
		{
			base.Update();
			if (m_CanvasGroup.alpha > 0f && (Input.GetMouseButtonDown (0) || Input.GetMouseButtonDown (1) || Input.GetMouseButtonDown (2))) {

				var pointer = new PointerEventData (EventSystem.current);
				pointer.position = Input.mousePosition;
				var raycastResults = new List<RaycastResult> ();
				EventSystem.current.RaycastAll (pointer, raycastResults);

				for (int i = 0; i < raycastResults.Count; i++) {
					MenuItem item = raycastResults [i].gameObject.GetComponent<MenuItem> ();
					if (item != null) {
						if (item.transform.IsChildOf (m_RectTransform)) {
							item.OnPointerClick (pointer);
						}
						return;
					}
				}

				Close ();
			}
		}

		public virtual void Clear ()
		{
			for (int i = 0; i < itemCache.Count; i++) {
				itemCache [i].gameObject.SetActive (false);
			}

			ContextMenu subMenu = WidgetUtility.Find<ContextMenu> ("ContextMenuSub");
			if (subMenu != null && subMenu != this) {
				subMenu.Close ();
			}
		}

		public virtual MenuItem AddMenuItem (string text, UnityAction used)
		{
			MenuItem item = itemCache.Find (x => !x.gameObject.activeSelf);

			if (item == null) {
				Debug.Log(text);
				item = Instantiate (m_MenuItemPrefab) as MenuItem;
				itemCache.Add (item);
			}
			Text itemText = item.GetComponentInChildren<Text> ();

			if (itemText != null) {
				itemText.text = text;
			}
			item.onTrigger.RemoveAllListeners ();
			item.interactable = used != null;
			item.gameObject.SetActive (true);
			item.transform.SetParent (m_RectTransform, false);
			SetArrowVisible (item, false);
			if (used != null) {
				item.onTrigger.AddListener (delegate() {
					Close ();
					used.Invoke ();
				});
			}
			return item;
		}

		public virtual MenuItem AddSubMenuItem (string text, List<(string, UnityAction)> subMenuItems)
		{
			MenuItem item = AddMenuItem (text, delegate() {});
			item.onTrigger.RemoveAllListeners ();
			item.interactable = subMenuItems != null && subMenuItems.Count > 0;
			SetArrowVisible (item, item.interactable);
			if (item.interactable) {
				item.onTrigger.AddListener (delegate() {
					ContextMenu subMenu = WidgetUtility.Find<ContextMenu> ("ContextMenuSub");
					if (subMenu == null || subMenu == this) {
						return;
					}

					subMenu.Clear ();
					for (int i = 0; i < subMenuItems.Count; i++) {
						string itemText = subMenuItems [i].Item1;
						UnityAction itemAction = subMenuItems [i].Item2;
						UnityAction wrappedAction = null;
						if (itemAction != null) {
							wrappedAction = delegate() {
								Close ();
								itemAction.Invoke ();
							};
						}
						subMenu.AddMenuItem (itemText, wrappedAction);
					}

					subMenu.ShowNextTo (item);
				});
			}
			return item;
		}

		protected virtual void SetArrowVisible (MenuItem item, bool visible)
		{
			if (item == null) {
				return;
			}

			Transform arrow = item.transform.Find ("Arrow");
			if (arrow != null) {
				arrow.gameObject.SetActive (visible);
			}
		}

		public virtual void ShowAt (Vector3 position)
		{
			m_RectTransform.position = position;
			base.Show ();
		}

		public virtual void ShowNextTo (MenuItem item)
		{
			RectTransform itemTransform = item.GetComponent<RectTransform> ();
			if (itemTransform == null) {
				ShowAt (item.transform.position);
				return;
			}

			gameObject.SetActive (true);
			Canvas.ForceUpdateCanvases ();

			Vector3[] itemCorners = new Vector3[4];
			itemTransform.GetWorldCorners (itemCorners);
			Vector3 itemBottomRight = itemCorners [3];

			Vector3 subMenuBottomLeftOffset = new Vector3 (
				-m_RectTransform.pivot.x * m_RectTransform.rect.width,
				-m_RectTransform.pivot.y * m_RectTransform.rect.height,
				0f
			);
			if (m_RectTransform.parent != null) {
				subMenuBottomLeftOffset = m_RectTransform.parent.TransformVector (subMenuBottomLeftOffset);
			}

			m_RectTransform.position = itemBottomRight - subMenuBottomLeftOffset;
			m_IsShowing = false;
			m_CanvasGroup.alpha = 0f;
			m_RectTransform.localScale = Vector3.zero;
			base.Show ();
		}

		public virtual void ShowAtScreenPosition (Vector2 screenPosition)
		{
			SetPositionFromScreenPoint (screenPosition);
			base.Show ();
		}

		protected virtual void SetPositionFromScreenPoint (Vector2 screenPosition)
		{
			if (this.m_Canvas == null) {
				this.m_Canvas = GetComponentInParent<Canvas> ();
			}

			if (this.m_Canvas == null) {
				m_RectTransform.position = screenPosition;
				return;
			}

			Vector2 localPosition;
			RectTransform canvasTransform = this.m_Canvas.transform as RectTransform;
			Camera canvasCamera = this.m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : this.m_Canvas.worldCamera;
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle (canvasTransform, screenPosition, canvasCamera, out localPosition)) {
				m_RectTransform.position = canvasTransform.TransformPoint (localPosition);
			} else {
				m_RectTransform.position = screenPosition;
			}
		}
	}
}
