using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CozyMods
{
    [Serializable]
    internal sealed class RetroSettings
    {
        public bool dither = true;
        public float ditherAmount = 0.57f;
        public int grain = 2;
        public float chroma = 0.52f;
        public int ditherSteps = 8;
        public bool lowResolution = true;
        public int renderLines = 539;
        public bool posterize = true;
        public int bands = 8;
        public bool outline = true;
        public float outlineAmount = 0.75f;
    }

    internal sealed class RetroControls : MonoBehaviour
    {
        private const string Prefix = "cozy.example.colordither.";
        private const string SettingsKey = Prefix + "retro";
        private const float PanelWidth = 440;
        private const float RowWidth = 396;
        private const float ControlScale = 720f / 1080f;

        private readonly List<Action> refresh = new List<Action>();
        private RetroSettings settings;
        private Material material;
        private bool open;
        private Scene cachedMenu;
        private PCDButton[] menuExits = Array.Empty<PCDButton>();
        private Button entry;
        private Button done;
        private RectTransform panel;
        private Slider sliderStyle;
        private Toggle toggleStyle;
        private TMP_Text captionStyle;
        private Image rowEdge;
        private Image buttonPaper;
        private Image buttonAccent;
        private ColorBlock buttonColors;
        private GameObject previousSelection;

        public bool HasEffects => (settings.dither && settings.ditherAmount > 0) ||
            settings.lowResolution || settings.posterize || UsesDepth;
        public bool UsesDepth => settings.outline && settings.outlineAmount > 0;

        public void Initialize(Material target)
        {
            material = target;
            if (PlayerPrefs.HasKey(SettingsKey))
            {
                settings = JsonUtility.FromJson<RetroSettings>(PlayerPrefs.GetString(SettingsKey));
            }
            else
            {
                settings = new RetroSettings();
                // Read preferences saved by the original Color Dither mod.
                settings.ditherAmount = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "strength", settings.ditherAmount));
                settings.grain = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "size", settings.grain), 1, 4);
                settings.chroma = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "chroma", settings.chroma));
                settings.ditherSteps = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "steps", settings.ditherSteps), 8, 64);
            }
            Apply();
        }

        private void Update()
        {
            bool menuVisible = IsMainMenuVisible();
            bool shortcut = Input.GetKeyDown(KeyCode.F9);
            if ((menuVisible || shortcut) && panel == null) CreateUi();
            if (panel == null) return;
            if (shortcut) SetOpen(!open);
            if (open && Input.GetKeyDown(KeyCode.Escape)) SetOpen(false);
            bool visible = menuVisible && !open;
            if (entry.gameObject.activeSelf != visible) entry.gameObject.SetActive(visible);
        }

        private bool IsMainMenuVisible()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "MainMenu") return false;
            if (scene != cachedMenu)
            {
                cachedMenu = scene;
                menuExits = FindObjectsOfType<PCDButton>(true).Where(button => button.gameObject.scene == scene &&
                    Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                        .Any(i => button.onClick.GetPersistentMethodName(i) == nameof(MainMenuMgr.ExitGame))).ToArray();
            }
            return menuExits.Any(button => button != null && button.isActiveAndEnabled);
        }

        private void CreateUi()
        {
            var native = PCDGameSettingMgr.Instance;
            if (native == null || native.masterVolumeSlider == null || native.fullScreenToggle == null) return;
            sliderStyle = native.masterVolumeSlider;
            toggleStyle = native.fullScreenToggle;
            var settingsMenu = native.transform.Find("MenuBody");
            var volumeRow = sliderStyle.transform.parent;
            captionStyle = volumeRow.Find("ItemName").GetComponent<TMP_Text>();
            rowEdge = volumeRow.Find("BackgroundOuter").GetComponent<Image>();
            var backButton = settingsMenu.Find("CloseBtn").GetComponent<Button>();
            buttonPaper = backButton.transform.Find("Background").GetComponent<Image>();
            buttonAccent = settingsMenu.Find("TabMenu/Tab-Common-Selected/Background").GetComponent<Image>();
            buttonColors = backButton.colors;

            var root = NewRect("Retro Filters Overlay", transform);
            root.gameObject.SetActive(false);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1152, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1;
            root.gameObject.AddComponent<GraphicRaycaster>();

            panel = NewRect("Retro Filters Panel", root);
            panel.anchorMin = panel.anchorMax = new Vector2(1, 1);
            panel.pivot = new Vector2(1, 1);
            panel.anchoredPosition = new Vector2(-22, -22);
            panel.sizeDelta = new Vector2(PanelWidth, 586);

            var settingsBackground = settingsMenu.Find("Background");
            var title = settingsMenu.Find("TitleContainer");
            var paper = settingsBackground.Find("BackgroundBody").GetComponent<Image>();
            var trim = title.Find("Background-Yellow").GetComponent<Image>();
            // Use matching corner sizes so the settings paper has an even rim.
            Surface(panel, settingsBackground.Find("SoftOutlineShadow").GetComponent<Image>(),
                PanelWidth / 2, 295, PanelWidth + 6, 592);
            var rim = Surface(panel, paper, PanelWidth / 2, 293, PanelWidth, 586);
            rim.color = trim.color;
            rim.pixelsPerUnitMultiplier *= 2;
            var body = Surface(panel, paper, PanelWidth / 2, 293, PanelWidth - 8, 578);
            body.pixelsPerUnitMultiplier = rim.pixelsPerUnitMultiplier;
            body.raycastTarget = true;

            Surface(panel, trim, PanelWidth / 2, 34, 164, 40);
            var heading = Label(panel, "复古画面", 152, 34, 136, 24);
            heading.alignment = TextAlignmentOptions.Center;
            Section("彩色抖动", 80, () => settings.dither, value => settings.dither = value);
            Range("抖动强度", 116, 0, 100, () => Mathf.RoundToInt(settings.ditherAmount * 100),
                value => settings.ditherAmount = value / 100f, () => settings.dither, "%");
            Range("颗粒大小", 149, 1, 4, () => settings.grain, value => settings.grain = value, () => settings.dither, " px");
            Range("彩点", 182, 0, 100, () => Mathf.RoundToInt(settings.chroma * 100),
                value => settings.chroma = value / 100f, () => settings.dither, "%");
            Range("抖动色阶", 215, 8, 64, () => settings.ditherSteps, value => settings.ditherSteps = value, () => settings.dither);
            MakeButton(panel, "细颗粒", 228, 255, 110, () => PresetDither(0.75f, 1, 20, 0.4f));
            MakeButton(panel, "明显彩点", 356, 255, 115, () => PresetDither(0.95f, 2, 16, 0.95f));

            Section("低分辨率", 304, () => settings.lowResolution, value => settings.lowResolution = value);
            Range("画面像素高度", 339, 144, 720, () => settings.renderLines,
                value => settings.renderLines = value, () => settings.lowResolution);

            Section("色阶分层", 385, () => settings.posterize, value => settings.posterize = value);
            Range("明暗档数", 420, 3, 16, () => settings.bands, value => settings.bands = value, () => settings.posterize);

            Section("轮廓描边", 466, () => settings.outline, value => settings.outline = value);
            Range("描边强度", 501, 0, 100, () => Mathf.RoundToInt(settings.outlineAmount * 100),
                value => settings.outlineAmount = value / 100f, () => settings.outline, "%");

            MakeButton(panel, "全部关闭", 83, 550, 116, DisableAll);
            MakeButton(panel, "恢复默认", 220, 550, 116, ResetDefaults);
            done = MakeButton(panel, "完成", 357, 550, 116, () => SetOpen(false), primary: true);
            entry = MakeButton(root, "复古画面", -103, 46, 150, () => SetOpen(true));
            entry.name = "Retro Filters Entry";
            ((RectTransform)entry.transform).anchorMin = ((RectTransform)entry.transform).anchorMax = new Vector2(1, 1);

            panel.gameObject.SetActive(false);
            root.gameObject.SetActive(true);
            Apply();
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)item.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Place(RectTransform item, float x, float y, float width, float height)
        {
            item.anchorMin = item.anchorMax = new Vector2(0, 1);
            item.pivot = new Vector2(0.5f, 0.5f);
            item.anchoredPosition = new Vector2(x, -y);
            item.sizeDelta = new Vector2(width, height);
        }

        private static Image Surface(RectTransform parent, Image style, float x, float y, float width, float height)
        {
            var rect = NewRect(style.name, parent);
            Place(rect, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = style.sprite;
            image.type = style.type;
            image.pixelsPerUnitMultiplier = style.pixelsPerUnitMultiplier;
            image.color = style.color;
            image.raycastTarget = false;
            return image;
        }

        private TMP_Text Label(RectTransform parent, string text, float left, float y, float width, int size)
        {
            var item = NewRect(text, parent);
            Place(item, left + width / 2, y, width, 36);
            var label = item.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = captionStyle.font;
            label.fontSharedMaterial = captionStyle.fontSharedMaterial;
            label.fontStyle = captionStyle.fontStyle;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = captionStyle.color;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        private Button MakeButton(RectTransform parent, string text, float x, float y, float width,
            UnityAction click, bool primary = false)
        {
            var image = Surface(parent, primary ? buttonAccent : buttonPaper, x, y, width, 34);
            image.name = text;
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = buttonColors;
            button.onClick.AddListener(click);
            var label = Label(image.rectTransform, text, 0, 17, width, 17);
            label.alignment = TextAlignmentOptions.Center;
            if (primary) label.color = Color.white;
            return button;
        }

        private void Section(string title, float y, Func<bool> get, Action<bool> set)
        {
            Surface(panel, rowEdge, PanelWidth / 2, y, RowWidth, 34);
            Label(panel, title, 36, y, 300, 19);
            var toggle = Instantiate(toggleStyle, panel, false);
            toggle.name = title;
            Place((RectTransform)toggle.transform, 398, y, 25, 25);
            toggle.transform.localScale *= ControlScale;
            toggle.group = null;
            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.onValueChanged.AddListener(value => { set(value); Apply(); });
            toggle.gameObject.SetActive(true);
            refresh.Add(() => toggle.SetIsOnWithoutNotify(get()));
        }

        private void Range(string name, float y, int min, int max, Func<int> get, Action<int> set,
            Func<bool> active, string suffix = "")
        {
            var caption = Label(panel, name, 36, y, 139, 17);
            var valueLabel = Label(panel, "", 368, y, 42, 16);
            valueLabel.alignment = TextAlignmentOptions.MidlineRight;
            var slider = Instantiate(sliderStyle, panel, false);
            slider.name = name;
            Place((RectTransform)slider.transform, 272, y, 190 / ControlScale, 5);
            slider.transform.localScale *= ControlScale;
            slider.onValueChanged = new Slider.SliderEvent();
            slider.wholeNumbers = true;
            slider.minValue = min;
            slider.maxValue = max;
            slider.onValueChanged.AddListener(value => { set(Mathf.RoundToInt(value)); Apply(); });
            slider.gameObject.SetActive(true);
            refresh.Add(() =>
            {
                slider.SetValueWithoutNotify(get());
                slider.interactable = active();
                caption.alpha = valueLabel.alpha = slider.interactable ? 1 : 0.45f;
                valueLabel.text = get() + suffix;
            });
        }

        private void SetOpen(bool value)
        {
            open = value;
            var events = EventSystem.current;
            if (open && events != null) previousSelection = events.currentSelectedGameObject;
            panel.gameObject.SetActive(open);
            entry.gameObject.SetActive(!open && IsMainMenuVisible());
            if (events != null && open) events.SetSelectedGameObject(done.gameObject);
            if (!open)
            {
                if (events != null && previousSelection != null) events.SetSelectedGameObject(previousSelection);
                Save();
            }
        }

        private void ResetDefaults()
        {
            settings = new RetroSettings();
            Apply();
        }

        private void DisableAll()
        {
            settings.dither = settings.lowResolution = settings.posterize = settings.outline = false;
            Apply();
        }

        private void PresetDither(float strength, int pixels, int steps, float color)
        {
            settings.ditherAmount = strength;
            settings.grain = pixels;
            settings.ditherSteps = steps;
            settings.chroma = color;
            Apply();
        }

        private void Apply()
        {
            material.SetFloat("_Strength", settings.dither ? settings.ditherAmount : 0);
            material.SetFloat("_GrainSize", settings.grain);
            material.SetFloat("_Chroma", settings.chroma);
            material.SetFloat("_ColorSteps", settings.ditherSteps);
            material.SetFloat("_RenderLines", settings.lowResolution ? settings.renderLines : 0);
            material.SetFloat("_PosterizeBands", settings.posterize ? settings.bands : 0);
            material.SetFloat("_Outline", settings.outline ? settings.outlineAmount : 0);
            foreach (var update in refresh) update();
        }

        public void Save()
        {
            if (settings == null) return;
            PlayerPrefs.SetString(SettingsKey, JsonUtility.ToJson(settings));
            PlayerPrefs.Save();
        }
    }
}
