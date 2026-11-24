using System;
using System.Collections.Generic;
using CozyMods.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CozyMods.Appearance
{
    internal sealed partial class Wardrobe : MonoBehaviour
    {
        private const string WindowName = "CozyMods.Appearance.Wardrobe";
        private const string ShortcutPreference = WindowName + ".Shortcut";
        private static readonly Key[] Shortcuts = { Key.F10, Key.F6, Key.F7, Key.F8 };
        private static readonly string[] Slots = { "Body", "Hat", "Cloth", "Shoe", "Glove", "Knee" };

        private Func<IReadOnlyList<WardrobeItem>> items;
        private Func<string> selection;
        private Action<string, string> select;
        private Action<string> save;
        private Func<bool> online;
        private Action<bool> allowAutoSync;
        private ClientUIMgr ui;
        private InputDisableManager inputLocks;
        private EasyCursorToken cursorToken;
        private bool previousCursorVisibility;
        private CursorLockMode previousCursorMode;
        private bool open;
        private bool showRoom;
        private bool syncAllowed = true;
        private int slot;
        private int shortcut;
        private string focusedKey;
        private string notice;
        private ModMainMenuEntry entry;
        private Panel panel;
        private float nextRefresh;

        public void Initialize(Func<IReadOnlyList<WardrobeItem>> items, Func<string> selection,
            Action<string, string> select, Action<string> save, Func<bool> online,
            Action<bool> allowAutoSync, bool initialAllowed = true)
        {
            this.items = items;
            this.selection = selection;
            this.select = select;
            this.save = save;
            this.online = online;
            this.allowAutoSync = allowAutoSync;
            syncAllowed = initialAllowed;
            shortcut = PlayerPrefs.GetInt(ShortcutPreference, 0);
            if (shortcut < 0 || shortcut >= Shortcuts.Length) shortcut = 0;
            entry = new ModMainMenuEntry("Appearance Framework Entry", () => L("联机外观", "Appearances"), Open);
        }

        private void OnEnable() => SceneManager.activeSceneChanged += OnSceneChanged;

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            Close();
            entry?.Dispose();
            DestroyPanel();
        }

        private void OnSceneChanged(Scene previous, Scene next)
        {
            Close();
            DestroyPanel();
        }

        private void DestroyPanel()
        {
            if (panel == null) return;
            Destroy(panel.Root.gameObject);
            panel = null;
        }

        private void Update()
        {
            if (entry == null) return;
            entry.Refresh(open);

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[Shortcuts[shortcut]].wasPressedThisFrame)
            {
                if (open) Close();
                else Open();
            }
            else if (open && ui == null && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
            }

            if (open && Time.unscaledTime >= nextRefresh)
            {
                panel.Refresh();
                nextRefresh = Time.unscaledTime + .25f;
            }
        }

        private void Open()
        {
            if (open) return;
            var settings = PCDGameSettingMgr.Instance;
            if (settings == null || settings.masterVolumeSlider == null || settings.fullScreenToggle == null) return;
            panel ??= new Panel(this, settings);

            ui = ClientUIMgr.TryGetInstance(true);
            if (ui == null) TakeFocus();
            else if (!ui.OpenClientUI(WindowName, TakeFocus, ReleaseFocus)) ui = null;
        }

        private void TakeFocus()
        {
            open = true;
            notice = null;
            panel.Show();
            entry.Refresh(true);
            previousCursorVisibility = Cursor.visible;
            previousCursorMode = Cursor.lockState;
            if (EasyCursorMgr.TryGetInstance(true) != null)
                cursorToken = EasyCursorMgr.SetCursorTemporarily(true, CursorLockMode.None);
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            inputLocks = InputDisableManager.TryGetInstance(true);
            if (inputLocks == null) return;
            inputLocks.DisableGroup(InputGroup.AllGameplay | InputGroup.Backpack | InputGroup.Chat, this);
            var playerInput = PCDPlayerInput.TryGetInstance(true);
            var inputHandler = PCDPlayerInputHandler.TryGetInstance(true);
            if (playerInput != null && inputHandler != null)
            {
                inputHandler.SetMoveAxis(playerInput.playerName, Vector2.zero);
                inputHandler.SetMoveAxis2(playerInput.playerName, Vector2.zero);
            }
        }

        private void Close()
        {
            if (!open) return;
            if (ui != null && ui.curActiveUIName == WindowName)
                ui.CloseCurrentClientUI(WindowName);
            else
                ReleaseFocus();
        }

        private void ReleaseFocus()
        {
            open = false;
            panel.Hide();
            if (inputLocks != null)
                inputLocks.EnableGroup(InputGroup.AllGameplay | InputGroup.Backpack | InputGroup.Chat, this);
            inputLocks = null;
            if (cursorToken != null)
            {
                EasyCursorMgr.ReleaseCursorToken(cursorToken);
                cursorToken = null;
            }
            else
            {
                Cursor.lockState = previousCursorMode;
                Cursor.visible = previousCursorVisibility;
            }
            ui = null;
        }

        private void ToggleAutoReceive(bool allowed)
        {
            syncAllowed = allowed;
            allowAutoSync(allowed);
        }

        private void CycleShortcut()
        {
            shortcut = (shortcut + 1) % Shortcuts.Length;
            PlayerPrefs.SetInt(ShortcutPreference, shortcut);
            PlayerPrefs.Save();
        }

        private void Wear(WardrobeItem item, bool local)
        {
            notice = null;
            if (!local)
            {
                save(item.Key);
                var available = items();
                bool saved = false;
                foreach (var candidate in available)
                {
                    if (candidate.Key != item.Key) continue;
                    saved = candidate.Saved || candidate.Installed;
                    break;
                }
                if (!saved)
                {
                    notice = L("保存未完成，请稍后再试。", "Could not save this appearance. Try again.");
                    return;
                }

                ChangeView(false);
                focusedKey = item.Key;
            }

            select(item.Slot, item.Key);
        }

        private string CurrentKey()
        {
            string[] selected = selection().Split('|');
            return slot < selected.Length ? selected[slot] : "";
        }

        private bool IsClothing => slot != 0;

        private void ChangeSlot(int index)
        {
            slot = index;
            ResetList();
        }

        private void ChangeView(bool room)
        {
            showRoom = room;
            ResetList();
        }

        private void ResetList()
        {
            focusedKey = null;
            notice = null;
            panel.ResetScroll();
        }

        private static string SlotName(int index)
        {
            switch (index)
            {
                case 0: return L("身体", "Body");
                case 1: return L("帽子", "Hat");
                case 2: return L("衣服", "Clothes");
                case 3: return L("鞋", "Shoes");
                case 4: return L("手套", "Gloves");
                default: return L("护膝", "Knee");
            }
        }

        private static string L(string chinese, string english)
        {
            var settings = PCDGameSettingMgr.Instance;
            if (settings != null)
                return settings.language == "简体中文" || settings.language == "繁體中文" ? chinese : english;

            switch (Application.systemLanguage)
            {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return chinese;
                default:
                    return english;
            }
        }
    }
}
