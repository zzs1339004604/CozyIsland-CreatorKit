using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CozyMods.Appearance
{
    internal sealed class Wardrobe : MonoBehaviour
    {
        private const string WindowName = "CozyMods.Appearance.Wardrobe";
        private const string ShortcutPreference = WindowName + ".Shortcut";
        private static readonly Key[] Shortcuts = { Key.F10, Key.F6, Key.F7, Key.F8, Key.F9 };
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
        private Rect window;
        private Vector2 scroll;

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
            shortcut = Mathf.Clamp(PlayerPrefs.GetInt(ShortcutPreference, 0), 0, Shortcuts.Length - 1);
        }

        private void OnEnable() => SceneManager.activeSceneChanged += OnSceneChanged;

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            Close();
        }

        private void OnSceneChanged(Scene previous, Scene next) => Close();

        private void Update()
        {
            if (items == null || Keyboard.current == null) return;
            var keyboard = Keyboard.current;
            if (keyboard[Shortcuts[shortcut]].wasPressedThisFrame)
            {
                if (open) Close();
                else Open();
            }
            else if (open && ui == null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        private void Open()
        {
            ui = ClientUIMgr.TryGetInstance(true);
            if (ui == null) TakeFocus();
            else ui.OpenClientUI(WindowName, TakeFocus, ReleaseFocus);
        }

        private void TakeFocus()
        {
            open = true;
            notice = null;
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

        private void OnGUI()
        {
            if (!open) return;
            float width = Mathf.Min(720, Screen.width - 28);
            float height = Mathf.Min(520, Screen.height - 28);
            window = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Window(GetInstanceID(), window, Draw, L("外观", "Appearance"));
        }

        private void Draw(int id)
        {
            string activeKey = CurrentKey();
            var available = items();
            GUILayout.BeginArea(new Rect(15, 24, window.width - 30, window.height - 38));
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(!IsClothing, L("身体", "Body"), GUI.skin.button) && IsClothing) ChangeSlot(0);
            if (GUILayout.Toggle(IsClothing, L("服装", "Clothing"), GUI.skin.button) && !IsClothing) ChangeSlot(1);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(L("关闭", "Close"), GUILayout.Width(65))) Close();
            GUILayout.EndHorizontal();

            if (IsClothing)
            {
                GUILayout.BeginHorizontal();
                for (int index = 1; index < Slots.Length; index++)
                {
                    if (GUILayout.Toggle(index == slot, SlotName(index), GUI.skin.button) && index != slot)
                        ChangeSlot(index);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(!showRoom, L("我的外观", "My appearances"), GUI.skin.button) && showRoom) ChangeView(false);
            if (GUILayout.Toggle(showRoom, L("房间外观", "Room appearances"), GUI.skin.button) && !showRoom) ChangeView(true);
            GUILayout.FlexibleSpace();
            GUILayout.Label(online() ? L("房间同步已连接", "Room sync connected") : L("本机外观", "Local appearances"));
            GUILayout.EndHorizontal();

            // Tabs may have changed the current slot during this GUI event.
            activeKey = CurrentKey();
            var shown = new List<WardrobeItem>();
            WardrobeItem active = null;
            foreach (var item in available)
            {
                if (item.Slot != Slots[slot]) continue;
                if (item.Key == activeKey) active = item;
                bool local = item.Installed || item.Saved;
                if (showRoom ? !local : local) shown.Add(item);
            }

            GUILayout.BeginHorizontal();
            string current = active?.Name ?? (activeKey.Length == 0
                ? L("原版外观", "Original appearance")
                : L("等待外观资源", "Waiting for appearance"));
            GUILayout.Label(L("当前穿戴 · ", "Wearing · ") + current);
            if (activeKey.Length != 0 && GUILayout.Button(L("恢复原版", "Restore original")))
            {
                select(Slots[slot], "");
                notice = null;
                activeKey = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            DrawList(shown, activeKey);
            DrawDetails(shown, activeKey);
            GUILayout.EndHorizontal();

            if (notice != null) GUILayout.Label(notice);
            GUILayout.BeginHorizontal();
            bool allowed = GUILayout.Toggle(syncAllowed, L("自动接收房间外观", "Receive room appearances automatically"));
            if (allowed != syncAllowed)
            {
                syncAllowed = allowed;
                allowAutoSync(allowed);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(L("快捷键 · ", "Shortcut · ") + Shortcuts[shortcut]))
            {
                shortcut = (shortcut + 1) % Shortcuts.Length;
                PlayerPrefs.SetInt(ShortcutPreference, shortcut);
                PlayerPrefs.Save();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawList(List<WardrobeItem> shown, string activeKey)
        {
            GUILayout.BeginVertical(GUILayout.Width(Mathf.Min(270, window.width * 0.44f)));
            scroll = GUILayout.BeginScrollView(scroll);
            if (shown.Count == 0)
                GUILayout.Label(showRoom ? L("暂无房间外观", "No room appearances yet") : L("此部位暂无外观", "No appearances in this slot"));
            foreach (var item in shown)
            {
                string label = item.Key == activeKey ? L("穿戴中 · ", "Wearing · ") + item.Name : item.Name;
                if (GUILayout.Toggle(item.Key == focusedKey, label, GUI.skin.button))
                    focusedKey = item.Key;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawDetails(List<WardrobeItem> shown, string activeKey)
        {
            WardrobeItem item = shown.Find(candidate => candidate.Key == focusedKey);
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (item == null)
            {
                GUILayout.Label(L("选择一项外观查看详情。", "Choose an appearance to see its details."));
            }
            else
            {
                GUILayout.Label(item.Name);
                if (!string.IsNullOrEmpty(item.Author)) GUILayout.Label(L("作者 · ", "Author · ") + item.Author);
                if (!string.IsNullOrEmpty(item.Source)) GUILayout.Label(item.Source);
                GUILayout.Label(SlotName(slot));
                if (!item.Ready)
                    GUILayout.Label(L("等待下载", "Waiting for download"));
                else if (!item.CanEquip)
                    GUILayout.Label(L("作者仅允许在房间展示", "The author permits room viewing only"));
                else if (item.Key != activeKey)
                {
                    bool local = item.Installed || item.Saved;
                    if (GUILayout.Button(local ? L("穿上", "Wear") : L("保存并穿上", "Save and wear")))
                        Wear(item, local);
                }
            }
            GUILayout.EndVertical();
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
            scroll = Vector2.zero;
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
