using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CozyMods.UI
{
    internal sealed class ModMainMenuEntry
    {
        private const string HostName = "CozyMods Main Menu Entries";
        private readonly string objectName;
        private readonly Func<string> title;
        private readonly Action clicked;
        private Scene menuScene;
        private PCDButton[] exits = Array.Empty<PCDButton>();
        private Button entry;
        private TMP_Text caption;

        public ModMainMenuEntry(string objectName, Func<string> title, Action clicked)
        {
            this.objectName = objectName;
            this.title = title;
            this.clicked = clicked;
        }

        public bool Refresh(bool hidden = false)
        {
            var scene = SceneManager.GetActiveScene();
            bool visible = scene.name == "MainMenu" && MainPageVisible(scene);
            bool show = visible && !hidden;
            if (show && entry == null) CreateEntry(scene);
            if (entry != null)
            {
                if (entry.gameObject.activeSelf != show) entry.gameObject.SetActive(show);
                if (show) caption.text = title();
            }
            return visible;
        }

        public void Dispose()
        {
            if (entry == null) return;
            entry.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(entry.gameObject);
            entry = null;
            caption = null;
        }

        private bool MainPageVisible(Scene scene)
        {
            if (scene != menuScene || exits.Length == 0)
            {
                menuScene = scene;
                exits = UnityEngine.Object.FindObjectsOfType<PCDButton>(true).Where(button =>
                    button.gameObject.scene == scene && button.onClick != null &&
                    Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Any(index =>
                        button.onClick.GetPersistentMethodName(index) == nameof(MainMenuMgr.ExitGame) &&
                        button.onClick.GetPersistentTarget(index) is MainMenuMgr)).ToArray();
            }
            return exits.Any(button => button != null && button.isActiveAndEnabled);
        }

        private void CreateEntry(Scene scene)
        {
            var native = PCDGameSettingMgr.TryGetInstance(true);
            var back = native != null ? native.transform.Find("MenuBody/CloseBtn") : null;
            if (back == null) return;
            var style = back.GetComponent<Button>();
            var paper = (Image)style.targetGraphic;
            var text = back.GetComponentInChildren<TMP_Text>(true);

            var layout = GetLayout(scene);
            var item = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            item.layer = layout.gameObject.layer;
            item.transform.SetParent(layout, false);
            var image = item.GetComponent<Image>();
            image.sprite = paper.sprite;
            image.type = paper.type;
            image.pixelsPerUnitMultiplier = paper.pixelsPerUnitMultiplier;
            image.color = paper.color;
            entry = item.GetComponent<Button>();
            entry.targetGraphic = image;
            entry.colors = style.colors;
            entry.onClick.AddListener(() => clicked());

            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.layer = item.layer;
            var rect = (RectTransform)label.transform;
            rect.SetParent(item.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8, 0);
            rect.offsetMax = new Vector2(-8, 0);
            caption = label.GetComponent<TMP_Text>();
            caption.font = text.font;
            caption.fontSharedMaterial = text.fontSharedMaterial;
            caption.fontStyle = text.fontStyle;
            caption.color = text.color;
            caption.fontSize = 17;
            caption.alignment = TextAlignmentOptions.Center;
            caption.enableWordWrapping = false;
            caption.overflowMode = TextOverflowModes.Ellipsis;
            caption.raycastTarget = false;
        }

        private static Transform GetLayout(Scene scene)
        {
            // Both mods compile this file. Coordinate through scene objects and ordinary Unity components.
            var existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == HostName);
            if (existing != null) return existing.transform.Find("Buttons");

            var host = new GameObject(HostName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            host.layer = LayerMask.NameToLayer("UI");
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1152, 720);
            scaler.matchWidthOrHeight = 1;

            var buttons = new GameObject("Buttons", typeof(RectTransform), typeof(GridLayoutGroup));
            buttons.layer = host.layer;
            var rect = (RectTransform)buttons.transform;
            rect.SetParent(host.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            // Leave room for the upper-right launcher used by earlier Retro Filters releases.
            rect.anchoredPosition = new Vector2(-28, -74);
            rect.sizeDelta = new Vector2(310, 34);
            var grid = buttons.GetComponent<GridLayoutGroup>();
            grid.startCorner = GridLayoutGroup.Corner.UpperRight;
            grid.childAlignment = TextAnchor.UpperRight;
            grid.cellSize = new Vector2(150, 34);
            grid.spacing = new Vector2(10, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            return rect;
        }
    }
}
