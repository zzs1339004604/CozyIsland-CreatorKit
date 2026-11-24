using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CozyIsland.Modding;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace CozyMods.Appearance
{
    public sealed class AppearanceFrameworkMod : ICozyMod
    {
        private sealed class Preferences
        {
            public string Selection = AppearanceSlots.Empty;
            public bool AutoReceive = true;
        }

        private sealed class Avatar
        {
            public GameObject Skeleton;
            public Placement Placement;
            public string Applied = AppearanceSlots.Empty;
            public string Failed;
            public int Attempts;
            public float RetryAt;
        }

        internal static AppearanceFrameworkMod Active { get; private set; }

        private readonly Dictionary<GameObject, Avatar> avatars = new Dictionary<GameObject, Avatar>();
        private Preferences preferences;
        private string preferencesPath;
        private AppearanceStore store;
        private RoomSync room;
        private float refreshAfter, applyAfter, trimAfter;
        private CozyModContext context;
        private bool quitting;

        public void OnLoad(CozyModContext modContext)
        {
            context = modContext;
            var steam = SteamAppMgr.TryGetInstance();
            var profile = steam != null && steam.SteamInited ? steam.SteamUserID : "local";
            var directory = Path.Combine(Application.persistentDataPath, "CozyAppearance", profile);
            Directory.CreateDirectory(directory);
            preferencesPath = Path.Combine(directory, "preferences.json");
            preferences = ReadPreferences();
            store = new AppearanceStore(directory, context.Log);
            room = new RoomSync(context, store.ReadShareable, store.Accept, preferences.AutoReceive);
            Application.quitting += OnQuitting;
            context.AddCleanup(Dispose);
            var wardrobe = context.Root.AddComponent<Wardrobe>();
            wardrobe.Initialize(Items, () => preferences.Selection, Select, Save, store.Preview, () => room.Connected,
                SetAutoReceive, preferences.AutoReceive);
            context.OnUpdate(Tick);
            Active = this;
        }

        public void OnUnload() { }

        private void OnQuitting()
        {
            quitting = true;
            store?.CancelModelImports();
        }

        private Preferences ReadPreferences()
        {
            if (!File.Exists(preferencesPath)) return new Preferences();
            try
            {
                var value = JsonConvert.DeserializeObject<Preferences>(File.ReadAllText(preferencesPath));
                return AppearanceSlots.Parse(value?.Selection) != null ? value : new Preferences();
            }
            catch (Exception error)
            {
                context.Log("Appearance preferences could not be read: " + error.Message);
                return new Preferences();
            }
        }

        private void Select(string slot, string key)
        {
            var index = Array.IndexOf(AppearanceSlots.All, slot);
            key ??= "";
            if (index < 0 || key != "" && !store.CanSelect(key, slot)) return;
            var parts = AppearanceSlots.Parse(preferences.Selection);
            parts[index] = key;
            preferences.Selection = string.Join("|", parts);
            WritePreferences();
        }

        private void Save(string key) => store.Save(key);

        private void SetAutoReceive(bool value)
        {
            preferences.AutoReceive = value;
            room.SetAutoReceive(value);
            WritePreferences();
        }

        private void WritePreferences()
        {
            try
            {
                var temporary = preferencesPath + ".tmp";
                File.WriteAllText(temporary, JsonConvert.SerializeObject(preferences, Formatting.Indented));
                if (File.Exists(preferencesPath)) File.Replace(temporary, preferencesPath, null);
                else File.Move(temporary, preferencesPath);
            }
            catch (IOException error) { context.Log("Appearance preferences could not be saved: " + error.Message); }
            catch (UnauthorizedAccessException error) { context.Log("Appearance preferences could not be saved: " + error.Message); }
        }

        private IReadOnlyList<WardrobeItem> Items() => store.Items(RoomKeys());

        private IEnumerable<(string Key, string Slot)> RoomKeys()
        {
            foreach (var selection in room.Players.Values)
            {
                var parts = AppearanceSlots.Parse(selection);
                if (parts == null) continue;
                for (var i = 0; i < parts.Length; i++)
                    if (parts[i].Length != 0) yield return (parts[i], AppearanceSlots.All[i]);
            }
        }

        private void Tick(float elapsed)
        {
            if ((refreshAfter -= elapsed) <= 0)
            {
                refreshAfter = 2;
                var service = CozyWorkshopService.Instance;
                if (service != null)
                {
                    store.RefreshInstalled(service.Mods.Where(entry => entry.enabled && string.IsNullOrEmpty(entry.error))
                        .Select(entry => entry.directory));
                    var equipped = AppearanceSlots.Parse(preferences.Selection);
                    for (var i = 0; i < equipped.Length; i++)
                        equipped[i] = store.UpgradeSelection(equipped[i], AppearanceSlots.All[i]);
                    var upgraded = string.Join("|", equipped);
                    if (upgraded != preferences.Selection)
                    {
                        preferences.Selection = upgraded;
                        WritePreferences();
                    }
                }
            }

            var local = PlayerIdentifier.TryGetInstance()?.GetPlayer(needOriginal: true)?.gameObject;
            var selection = AppearanceSlots.Parse(preferences.Selection);
            var visible = new string[AppearanceSlots.All.Length];
            var published = new string[AppearanceSlots.All.Length];

            for (var i = 0; i < selection.Length; i++)
            {
                var slot = AppearanceSlots.All[i];
                var key = selection[i];
                if (!store.CanSelect(key, slot) && local != null)
                    key = store.NativeAppearance(local, slot);
                if (store.CanSelect(key, slot))
                {
                    visible[i] = key;
                    if (store.CanShare(key, slot)) published[i] = key;
                }
            }

            room.Tick(string.Join("|", published));
            trimAfter -= elapsed;
            if ((applyAfter -= elapsed) > 0) return;
            applyAfter = 0.1f;
            if (preferences.AutoReceive)
                foreach (var (key, _) in RoomKeys())
                    if (AppearanceData.TryKey(key, out var digest, out _) && !store.HasPack(digest))
                        room.RequestPack(digest);

            var present = new HashSet<GameObject>();
            var used = new HashSet<string>(StringComparer.Ordinal);
            if (local != null)
            {
                present.Add(local);
                Apply(local, visible, used, true);
            }
            foreach (var identity in NetworkClient.spawned.Values)
            {
                if (identity == null || !identity.TryGetComponent<NetworkPlayerHandler>(out var handler)) continue;
                var actor = handler.playerInstance;
                if (actor == null || !present.Add(actor)) continue;
                room.Players.TryGetValue(handler.playerName ?? "", out var state);
                Apply(actor, AppearanceSlots.Parse(state), used, false);
            }
            foreach (var actor in avatars.Keys.Where(actor => actor == null || !present.Contains(actor)).ToArray())
            {
                avatars[actor].Placement?.Dispose();
                avatars.Remove(actor);
            }
            store.TrimLoaded(used);
            if (trimAfter <= 0)
            {
                trimAfter = 15;
                foreach (var (key, _) in RoomKeys())
                    if (AppearanceData.TryKey(key, out var digest, out _)) used.Add(digest);
                store.TrimCache(used);
            }
        }

        private void Apply(GameObject actor, string[] keys, HashSet<string> used, bool isLocal)
        {
            var skeleton = actor.GetComponent<CozyPlayer>()?.pbSkeleton;
            if (skeleton == null) return;
            if (!avatars.TryGetValue(actor, out var avatar) || avatar.Skeleton != skeleton)
            {
                avatar?.Placement?.Dispose();
                avatars[actor] = avatar = new Avatar { Skeleton = skeleton };
            }

            var ready = new List<LoadedAppearance>();
            var signature = new string[AppearanceSlots.All.Length];
            if (keys != null)
                for (var i = 0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    if (!AppearanceData.TryKey(key, out var digest, out _) ||
                        !isLocal && !store.IsShareable(digest)) continue;
                    var item = store.Load(key, AppearanceSlots.All[i]);
                    if (item == null)
                    {
                        if (store.IsLoadingModel(digest)) used.Add(digest);
                        continue;
                    }
                    used.Add(digest);
                    ready.Add(item);
                    signature[i] = key;
                }

            var next = string.Join("|", signature);
            if (next == avatar.Applied && avatar.Failed == null) return;
            if (avatar.Failed == next && (avatar.Attempts >= 4 || Time.unscaledTime < avatar.RetryAt)) return;
            if (avatar.Failed != next) avatar.Attempts = 0;
            try
            {
                if (ready.Count == 0)
                {
                    avatar.Placement?.Dispose();
                    avatar.Placement = null;
                }
                else
                {
                    avatar.Placement ??= new Placement(actor, skeleton);
                    avatar.Placement.Apply(ready);
                    context.Log("Applied " + string.Join(", ", ready.Select(item => item.Entry.Name)) + " to " +
                        actor.GetComponent<CozyPlayer>().playerName + ".");
                }
                avatar.Applied = next;
                avatar.Failed = null;
            }
            catch (Exception error)
            {
                avatar.Placement?.Dispose();
                avatar.Placement = null;
                avatar.Applied = AppearanceSlots.Empty;
                avatar.Failed = next;
                avatar.RetryAt = Time.unscaledTime + 0.75f;
                if (++avatar.Attempts == 4) context.Log("Appearance placement failed: " + error.Message);
            }
        }

        private void Dispose()
        {
            Application.quitting -= OnQuitting;
            if (Active == this) Active = null;
            foreach (var avatar in avatars.Values) avatar.Placement?.Dispose();
            avatars.Clear();
            room?.Dispose();
            // Unity releases the bundles during player shutdown, before some Mod teardown callbacks.
            if (!quitting) store?.Dispose();
        }
    }
}
