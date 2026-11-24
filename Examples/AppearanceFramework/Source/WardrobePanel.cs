using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CozyMods.Appearance
{
    internal sealed partial class Wardrobe
    {
        private sealed class Panel
        {
            private readonly Wardrobe owner;
            private readonly TMP_Text nativeText;
            private readonly Image paper, trim, buttonPaper, accent;
            private readonly ColorBlock buttonColors;
            private readonly ActionButton body, clothing, local, room, close, restore, wear, shortcutButton;
            private readonly List<ActionButton> rows = new List<ActionButton>();
            private readonly List<WardrobeItem> shown = new List<WardrobeItem>();
            private readonly ActionButton[] clothingSlots = new ActionButton[Slots.Length - 1];
            private readonly RectTransform slotTabs, listContent;
            private readonly TMP_Text current, connection, empty, itemName, itemDetails, message, autoReceiveLabel;
            private readonly Toggle autoReceive;
            private WardrobeItem focused;
            private bool canWear;
            private GameObject previousSelection;

            public RectTransform Root { get; }

            public Panel(Wardrobe owner, PCDGameSettingMgr settings)
            {
                this.owner = owner;
                var menu = settings.transform.Find("MenuBody");
                var back = menu.Find("CloseBtn").GetComponent<Button>();
                nativeText = back.GetComponentInChildren<TMP_Text>(true);
                paper = menu.Find("Background/BackgroundBody").GetComponent<Image>();
                trim = menu.Find("TitleContainer/Background-Yellow").GetComponent<Image>();
                buttonPaper = back.transform.Find("Background").GetComponent<Image>();
                accent = menu.Find("TabMenu/Tab-Common-Selected/Background").GetComponent<Image>();
                buttonColors = back.colors;

                Root = NewRect("Cozy Appearance Overlay", owner.transform);
                Root.gameObject.SetActive(false);
                var canvas = Root.gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 121;
                var scaler = Root.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1152, 720);
                scaler.matchWidthOrHeight = .5f;
                Root.gameObject.AddComponent<GraphicRaycaster>();

                var backdrop = NewRect("Modal backdrop", Root);
                backdrop.anchorMin = Vector2.zero;
                backdrop.anchorMax = Vector2.one;
                backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
                backdrop.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .15f);

                var panel = NewRect("Cozy Appearance Panel", Root);
                panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
                panel.sizeDelta = new Vector2(744, 514);
                var edge = Surface(At("Rim", panel, 0, 0, 744, 514), paper, trim.color);
                edge.pixelsPerUnitMultiplier *= 2;
                var center = Surface(At("Paper", panel, 4, 4, 736, 506), paper);
                center.pixelsPerUnitMultiplier = edge.pixelsPerUnitMultiplier;

                Surface(At("Title background", panel, 250, 16, 244, 42), trim);
                Text(panel, L("联机外观", "Appearances"), 250, 16, 244, 42, 25).alignment = TextAlignmentOptions.Center;

                body = Button(panel, "Appearance Body", L("身体", "Body"), 24, 69, 96, 35, () => owner.ChangeSlot(0));
                clothing = Button(panel, "Appearance Clothing", L("服装", "Clothing"), 128, 69, 96, 35,
                    () => { if (!owner.IsClothing) owner.ChangeSlot(1); });
                local = Button(panel, "Appearance My", L("我的外观", "My appearances"), 390, 69, 157, 35,
                    () => owner.ChangeView(false));
                room = Button(panel, "Appearance Room", L("房间外观", "Room appearances"), 555, 69, 165, 35,
                    () => owner.ChangeView(true));

                slotTabs = At("Clothing slots", panel, 24, 112, 430, 31);
                for (int i = 0; i < clothingSlots.Length; i++)
                {
                    int index = i + 1;
                    clothingSlots[i] = Button(slotTabs, "Appearance " + Slots[index], SlotName(index), i * 84, 0, 78, 28,
                        () => owner.ChangeSlot(index));
                }
                connection = Text(panel, "", 489, 109, 231, 32, 15);
                connection.alignment = TextAlignmentOptions.MidlineRight;

                Surface(At("Current appearance background", panel, 24, 149, 696, 42), trim);
                current = Text(panel, "", 37, 149, 511, 42, 18);
                restore = Button(panel, "Appearance Restore", L("恢复原版", "Restore original"), 552, 154, 160, 31,
                    () => { owner.select(Slots[owner.slot], ""); owner.notice = null; });

                var list = At("Appearance list", panel, 24, 203, 280, 222);
                Surface(list, paper, trim.color);
                var scroll = list.gameObject.AddComponent<ScrollRect>();
                var viewport = At("List viewport", list, 6, 6, 268, 210);
                var scrollHit = viewport.gameObject.AddComponent<Image>();
                scrollHit.color = Color.clear;
                viewport.gameObject.AddComponent<RectMask2D>();
                listContent = At("Rows", viewport, 0, 0, 268, 0);
                var layout = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 5;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
                listContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scroll.viewport = viewport;
                scroll.content = listContent;
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 24;
                empty = Text(list, "", 15, 12, 250, 32, 17);

                var details = At("Appearance details", panel, 316, 203, 404, 222);
                Surface(details, paper, trim.color);
                Surface(At("Content paper", details, 3, 3, 398, 216), paper);
                itemName = Text(details, "", 15, 16, 374, 48, 22);
                itemName.enableWordWrapping = true;
                itemDetails = Text(details, "", 15, 66, 374, 98, 17);
                itemDetails.enableWordWrapping = true;
                itemDetails.alignment = TextAlignmentOptions.TopLeft;
                wear = Button(details, "Appearance Wear", "", 218, 175, 170, 34, WearFocused);
                Style(wear, true);

                message = Text(panel, "", 24, 428, 696, 22, 15);
                var checkbox = At("Receive checkbox slot", panel, 24, 460, 24, 26);
                autoReceive = Object.Instantiate(settings.fullScreenToggle, checkbox, false);
                autoReceive.name = "Appearance Auto Receive";
                var toggleRect = (RectTransform)autoReceive.transform;
                toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(.5f, .5f);
                toggleRect.pivot = new Vector2(.5f, .5f);
                toggleRect.anchoredPosition = Vector2.zero;
                toggleRect.sizeDelta = new Vector2(25, 25);
                toggleRect.localScale *= 720f / 1080f;
                autoReceive.group = null;
                autoReceive.onValueChanged = new Toggle.ToggleEvent();
                autoReceive.onValueChanged.AddListener(owner.ToggleAutoReceive);
                autoReceive.gameObject.SetActive(true);
                autoReceiveLabel = Text(panel, L("自动接收房间外观", "Receive room appearances automatically"),
                    57, 454, 320, 38, 16);
                shortcutButton = Button(panel, "Appearance Shortcut", "", 389, 454, 166, 38, owner.CycleShortcut);
                close = Button(panel, "Appearance Close", L("关闭", "Close"), 565, 454, 155, 38, owner.Close);
                Style(close, true);
            }

            public void Show()
            {
                var events = EventSystem.current;
                previousSelection = events == null ? null : events.currentSelectedGameObject;
                Root.gameObject.SetActive(true);
                Refresh();
                if (events != null) events.SetSelectedGameObject(close.Control.gameObject);
            }

            public void Hide()
            {
                Root.gameObject.SetActive(false);
                var events = EventSystem.current;
                if (events != null) events.SetSelectedGameObject(previousSelection);
                previousSelection = null;
            }

            public void ResetScroll() => listContent.anchoredPosition = Vector2.zero;

            public void Refresh()
            {
                string activeKey = owner.CurrentKey();
                WardrobeItem active = null;
                focused = null;
                shown.Clear();
                foreach (var item in owner.items())
                {
                    if (item.Slot != Slots[owner.slot]) continue;
                    if (item.Key == activeKey) active = item;
                    bool installed = item.Installed || item.Saved;
                    if (installed == owner.showRoom) continue;
                    shown.Add(item);
                    if (item.Key == owner.focusedKey) focused = item;
                }

                Style(body, !owner.IsClothing);
                Style(clothing, owner.IsClothing);
                Style(local, !owner.showRoom);
                Style(room, owner.showRoom);
                slotTabs.gameObject.SetActive(owner.IsClothing);
                for (int i = 0; i < clothingSlots.Length; i++) Style(clothingSlots[i], owner.slot == i + 1);
                connection.text = owner.online() ? L("房间同步已连接", "Room sync connected") : L("本机外观", "Local appearances");
                current.text = L("当前穿戴 · ", "Wearing · ") + (active?.Name ?? (activeKey.Length == 0
                    ? L("原版外观", "Original appearance")
                    : L("等待外观资源", "Waiting for appearance")));
                restore.Control.gameObject.SetActive(activeKey.Length != 0);

                BindRows(activeKey);
                BindDetails(activeKey);
                message.text = owner.notice ?? "";
                autoReceive.SetIsOnWithoutNotify(owner.syncAllowed);
                autoReceiveLabel.text = L("自动接收房间外观", "Receive room appearances automatically");
                shortcutButton.Label.text = L("快捷键 · ", "Shortcut · ") + Shortcuts[owner.shortcut];
            }

            private void BindRows(string activeKey)
            {
                while (rows.Count < shown.Count)
                {
                    int index = rows.Count;
                    var row = Button(listContent, "Appearance Row " + index, "", 0, 0, 268, 38,
                        () => owner.focusedKey = shown[index].Key);
                    row.Label.alignment = TextAlignmentOptions.MidlineLeft;
                    row.Control.gameObject.AddComponent<LayoutElement>().preferredHeight = 38;
                    rows.Add(row);
                }
                for (int i = 0; i < rows.Count; i++)
                {
                    bool visible = i < shown.Count;
                    rows[i].Control.gameObject.SetActive(visible);
                    if (!visible) continue;
                    var item = shown[i];
                    rows[i].Label.text = (item.Key == activeKey ? L("穿戴中 · ", "Wearing · ") : "") + item.Name;
                    Style(rows[i], item.Key == owner.focusedKey);
                }
                empty.text = owner.showRoom ? L("暂无房间外观", "No room appearances yet")
                    : L("此部位暂无外观", "No appearances in this slot");
                empty.gameObject.SetActive(shown.Count == 0);
            }

            private void BindDetails(string activeKey)
            {
                canWear = focused != null && focused.Ready && focused.CanEquip && focused.Key != activeKey;
                wear.Control.gameObject.SetActive(canWear);
                if (focused == null)
                {
                    itemName.text = "";
                    itemDetails.text = L("选择一项外观查看详情。", "Choose an appearance to see its details.");
                    return;
                }

                itemName.text = focused.Name;
                string author = string.IsNullOrEmpty(focused.Author) ? "" : L("作者 · ", "Author · ") + focused.Author + "\n";
                string source = string.IsNullOrEmpty(focused.Source) ? "" : focused.Source + "\n";
                string text = author + source + SlotName(owner.slot);
                if (!focused.Ready) text += "\n" + L("等待下载", "Waiting for download");
                else if (!focused.CanEquip) text += "\n" + L("作者仅允许在房间展示", "The author permits room viewing only");
                itemDetails.text = text;
                wear.Label.text = focused.Installed || focused.Saved ? L("穿上", "Wear") : L("保存并穿上", "Save and wear");
            }

            private void WearFocused()
            {
                if (canWear) owner.Wear(focused, focused.Installed || focused.Saved);
            }

            private readonly struct ActionButton
            {
                public readonly Button Control;
                public readonly TMP_Text Label;

                public ActionButton(Button control, TMP_Text label)
                {
                    Control = control;
                    Label = label;
                }
            }

            private ActionButton Button(Transform parent, string name, string value, float x, float y,
                float width, float height, UnityAction onClick)
            {
                var rect = At(name, parent, x, y, width, height);
                var image = Surface(rect, buttonPaper);
                image.raycastTarget = true;
                var control = rect.gameObject.AddComponent<Button>();
                control.targetGraphic = image;
                control.colors = buttonColors;
                control.onClick.AddListener(() =>
                {
                    onClick();
                    if (owner.open) Refresh();
                });
                var label = Text(rect, value, 10, 0, width - 20, height, 17);
                label.alignment = TextAlignmentOptions.Center;
                return new ActionButton(control, label);
            }

            private void Style(ActionButton button, bool selected)
            {
                var style = selected ? accent : buttonPaper;
                button.Control.image.sprite = style.sprite;
                button.Control.image.color = style.color;
                button.Control.image.pixelsPerUnitMultiplier = style.pixelsPerUnitMultiplier;
                button.Label.color = selected ? Color.white : nativeText.color;
            }

            private TMP_Text Text(Transform parent, string value, float x, float y, float width, float height, float size)
            {
                var rect = At("Label", parent, x, y, width, height);
                var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
                label.font = nativeText.font;
                label.fontSharedMaterial = nativeText.fontSharedMaterial;
                label.fontStyle = nativeText.fontStyle;
                label.color = nativeText.color;
                label.fontSize = size;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.enableWordWrapping = false;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.richText = false;
                label.raycastTarget = false;
                label.text = value;
                return label;
            }

            private static Image Surface(RectTransform rect, Image style, Color? color = null)
            {
                var image = rect.gameObject.AddComponent<Image>();
                image.sprite = style.sprite;
                image.type = style.type;
                image.color = color ?? style.color;
                image.pixelsPerUnitMultiplier = style.pixelsPerUnitMultiplier;
                image.raycastTarget = false;
                return image;
            }

            private static RectTransform At(string name, Transform parent, float x, float y, float width, float height)
            {
                var rect = NewRect(name, parent);
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(x, -y);
                rect.sizeDelta = new Vector2(width, height);
                return rect;
            }

            private static RectTransform NewRect(string name, Transform parent)
            {
                var item = new GameObject(name, typeof(RectTransform));
                item.layer = LayerMask.NameToLayer("UI");
                var rect = (RectTransform)item.transform;
                rect.SetParent(parent, false);
                return rect;
            }
        }
    }
}
