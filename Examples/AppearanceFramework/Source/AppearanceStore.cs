using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CozyIsland.Modding;
using Newtonsoft.Json;
using UnityEngine;

namespace CozyMods.Appearance
{
    internal sealed class AppearanceStore : IDisposable
    {
        private const long MaxCacheBytes = 128L * 1024 * 1024;
        private sealed class Pack
        {
            public string Digest, Path;
            public AppearanceCatalog Catalog;
            public bool Saved, Installed;
            public SharedBundle Bundle;
            public GlbModel Model;
            public CancellationTokenSource ModelRequest;
            public bool LoadFailed;
            public readonly HashSet<string> FailedEntries = new HashSet<string>();
            public readonly Dictionary<string, LoadedAppearance> Loaded = new Dictionary<string, LoadedAppearance>();
        }

        private sealed class SharedBundle
        {
            public string Digest;
            public AssetBundle Value;
            public int Users;
            public bool Borrowed;
        }

        private sealed class InstalledSource
        {
            public string Digest, PayloadPath;
            public DateTime CatalogTime, PayloadTime, UpgradesTime;
            public string[] PreviousDigests = Array.Empty<string>();
        }

        private readonly Dictionary<string, Pack> known = new Dictionary<string, Pack>(StringComparer.Ordinal);
        private readonly Dictionary<string, SharedBundle> bundles = new Dictionary<string, SharedBundle>(StringComparer.Ordinal);
        private readonly Dictionary<string, InstalledSource> installed = new Dictionary<string, InstalledSource>(StringComparer.OrdinalIgnoreCase);
        private readonly string cache, library;
        private readonly Action<string> log;
        private readonly SemaphoreSlim modelImports = new SemaphoreSlim(1);
        private Pack preview;

        public AppearanceStore(string root, Action<string> log)
        {
            this.log = log;
            cache = Path.Combine(root, "Cache");
            library = Path.Combine(root, "Library");
            Directory.CreateDirectory(cache);
            Directory.CreateDirectory(library);
            foreach (var path in Directory.GetFiles(library, "*.appearance"))
            {
                var digest = Path.GetFileNameWithoutExtension(path);
                if (AppearanceData.IsDigest(digest) && TryReadFile(digest, path, out var pack) && !pack.Catalog.UsesGlb)
                {
                    pack.Saved = true;
                    known[digest] = pack;
                }
            }
        }

        public void RefreshInstalled(IEnumerable<string> folders)
        {
            var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var folder in folders)
            {
                if (string.IsNullOrEmpty(folder)) continue;
                var path = Path.Combine(folder, "appearance.json");
                if (!File.Exists(path)) continue;
                present.Add(folder);
                var time = File.GetLastWriteTimeUtc(path);
                var upgradesPath = Path.Combine(folder, "appearance-upgrades.json");
                var upgradesTime = File.GetLastWriteTimeUtc(upgradesPath);
                if (installed.TryGetValue(folder, out var source) && source.CatalogTime == time &&
                    source.UpgradesTime == upgradesTime &&
                    (source.PayloadPath == null || source.PayloadTime == File.GetLastWriteTimeUtc(source.PayloadPath))) continue;

                var next = new InstalledSource { CatalogTime = time, UpgradesTime = upgradesTime };
                installed[folder] = next;
                try
                {
                    if (File.Exists(upgradesPath))
                    {
                        next.PreviousDigests = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(upgradesPath));
                        if (next.PreviousDigests == null || next.PreviousDigests.Any(digest => !AppearanceData.IsDigest(digest)))
                            throw new InvalidDataException("Invalid appearance upgrade digests.");
                    }
                    if (new FileInfo(path).Length > AppearanceData.MaxCatalogBytes)
                        throw new InvalidDataException("The appearance catalog is too large.");
                    var json = File.ReadAllBytes(path);
                    var catalog = AppearanceData.ReadCatalog(json, 0, json.Length);
                    // The framework can load a local .glb even with a Player that predates Workshop delivery for the extension.
                    var payloadPath = catalog.UsesGlb ? CozyModFiles.Resolve(folder, catalog.Model, false) :
                        Path.Combine(folder, catalog.Bundle);
                    next.PayloadPath = payloadPath;
                    next.PayloadTime = File.GetLastWriteTimeUtc(payloadPath);
                    var size = new FileInfo(payloadPath).Length;
                    if (size < 1 || size + json.Length + 4 > AppearanceData.MaxPackBytes)
                        throw new InvalidDataException("The appearance pack exceeds 8 MiB.");
                    var bytes = new byte[4 + json.Length + (int)size];
                    Buffer.BlockCopy(BitConverter.GetBytes(json.Length), 0, bytes, 0, 4);
                    Buffer.BlockCopy(json, 0, bytes, 4, json.Length);
                    var payload = File.ReadAllBytes(payloadPath);
                    if (payload.Length != size) throw new IOException("The appearance payload changed while it was being read.");
                    Buffer.BlockCopy(payload, 0, bytes, 4 + json.Length, payload.Length);
                    var digest = AppearanceData.Digest(bytes);
                    Register(digest, bytes, catalog);
                    next.Digest = digest;
                }
                catch (Exception error)
                {
                    log("Appearance could not be read from " + folder + ": " + error.Message);
                }
            }

            foreach (var removed in installed.Keys.Where(key => !present.Contains(key)).ToArray())
                installed.Remove(removed);
            foreach (var pack in known.Values) pack.Installed = false;
            foreach (var entry in installed.Values)
                if (entry.Digest != null && known.TryGetValue(entry.Digest, out var pack)) pack.Installed = true;
        }

        public bool HasPack(string digest) => Find(digest) != null;

        public bool IsShareable(string digest) => Find(digest)?.Catalog.Share == true;

        public string UpgradeSelection(string key, string slot)
        {
            if (!AppearanceData.TryKey(key, out var digest, out var id) ||
                known.TryGetValue(digest, out var pack) && pack.Installed)
                return key;

            // An installed pack may replace an earlier packet with the same entry IDs.
            var candidates = installed.Values
                .Where(source => source.Digest != null && source.PreviousDigests.Contains(digest))
                .Select(source => source.Digest).Distinct().Take(2).ToArray();
            if (candidates.Length != 1) return key;

            var updated = candidates[0] + "/" + id;
            return CanSelect(updated, slot) ? updated : key;
        }

        public string NativeAppearance(GameObject actor, string slot)
        {
            if (!Enum.TryParse<PCDEquipmentPart>(slot, out var part)) return null;
            var manager = actor.GetComponent<PCDEquipmentManager>();
            var prop = manager?.GetBonesByType(part)?.FirstOrDefault()?.curHandlejEquipment?.equipmentPropPrefab;
            if (prop == null) return null;
            foreach (var pack in known.Values)
            {
                if (!pack.Installed) continue;
                foreach (var entry in pack.Catalog.Entries)
                    if (entry.Slot == slot && entry.NativeOwner != null &&
                        CozyModRegistry.FindPrefab(entry.NativeOwner, entry.NativeItem) == prop)
                        return pack.Digest + "/" + entry.Id;
            }
            return null;
        }

        public bool CanSelect(string key, string slot)
        {
            var pack = FindEntry(key, out var entry);
            return pack != null && entry.Slot == slot && (pack.Installed || pack.Saved);
        }

        public bool CanShare(string key, string slot)
        {
            var pack = FindEntry(key, out var entry);
            return pack != null && entry.Slot == slot && pack.Catalog.Share && (pack.Installed || pack.Saved);
        }

        public bool Save(string key)
        {
            var pack = FindEntry(key, out _);
            if (pack == null || !pack.Catalog.AllowSave) return false;
            if (pack.Saved || pack.Installed) return true;
            try
            {
                var path = Path.Combine(library, pack.Digest + ".appearance");
                var data = ReadShareable(pack.Digest);
                if (data == null) return false;
                AtomicWrite(path, data);
                pack.Path = path;
                pack.Saved = true;
                return true;
            }
            catch (IOException error) { log("Appearance could not be saved: " + error.Message); return false; }
            catch (UnauthorizedAccessException error) { log("Appearance could not be saved: " + error.Message); return false; }
        }

        public byte[] ReadShareable(string digest)
        {
            var pack = Find(digest);
            if (pack == null || !pack.Catalog.Share) return null;
            try
            {
                var bytes = File.ReadAllBytes(pack.Path);
                return bytes.Length <= AppearanceData.MaxPackBytes && AppearanceData.Digest(bytes) == digest ? bytes : null;
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }

        public bool Accept(string digest, byte[] bytes)
        {
            try
            {
                if (!AppearanceData.IsDigest(digest) || bytes == null || bytes.Length > AppearanceData.MaxPackBytes ||
                    AppearanceData.Digest(bytes) != digest) return false;
                var catalog = ReadHeader(bytes);
                if (!catalog.Share) return false;
                Register(digest, bytes, catalog);
                log("Received appearance " + catalog.Name + " (" + bytes.Length + " bytes).");
                return true;
            }
            catch (Exception error) { log("Appearance transfer rejected: " + error.Message); return false; }
        }

        public LoadedAppearance Load(string key, string slot)
        {
            var pack = FindEntry(key, out var entry);
            if (pack == null || entry.Slot != slot || pack.LoadFailed) return null;
            if (pack.Loaded.TryGetValue(entry.Id, out var loaded)) return loaded;
            if (pack.Catalog.UsesGlb) return GetModelEntry(pack, entry);
            try
            {
                if (pack.Bundle == null)
                {
                    var packet = File.ReadAllBytes(pack.Path);
                    var start = 4 + BitConverter.ToInt32(packet, 0);
                    var bundle = new byte[packet.Length - start];
                    Buffer.BlockCopy(packet, start, bundle, 0, bundle.Length);
                    var digest = AppearanceData.Digest(bundle);
                    if (!bundles.TryGetValue(digest, out var reference))
                    {
                        var native = FindNativeBundle(pack.Catalog.Bundle, entry, digest, bundle.Length);
                        var asset = native != null ? native : AssetBundle.LoadFromMemory(bundle);
                        if (asset == null) throw new InvalidDataException("Unity could not load the appearance bundle.");
                        reference = new SharedBundle { Digest = digest, Value = asset, Borrowed = native != null };
                        bundles.Add(digest, reference);
                    }
                    reference.Users++;
                    pack.Bundle = reference;
                }

                var prefab = pack.Bundle.Value.LoadAsset<GameObject>(entry.Prefab);
                if (prefab == null) throw new InvalidDataException("The appearance bundle is missing " + entry.Prefab + ".");
                loaded = new LoadedAppearance(entry, prefab);
                pack.Loaded[entry.Id] = loaded;
                return loaded;
            }
            catch (Exception error)
            {
                pack.LoadFailed = true;
                log("Appearance could not be loaded: " + error.Message);
                return null;
            }
        }

        public void Preview(string key)
        {
            var pack = FindEntry(key, out var entry);
            preview = pack != null && pack.Installed && pack.Catalog.UsesGlb ? pack : null;
            if (preview != null && !pack.LoadFailed && !pack.Loaded.ContainsKey(entry.Id))
                GetModelEntry(pack, entry);
        }

        public bool IsLoadingModel(string digest) =>
            known.TryGetValue(digest, out var pack) && pack.ModelRequest != null;

        private LoadedAppearance GetModelEntry(Pack pack, AppearanceEntry entry)
        {
            if (!pack.Installed || pack.FailedEntries.Contains(entry.Id)) return null;
            if (pack.Model == null)
            {
                if (pack.ModelRequest == null)
                {
                    var request = new CancellationTokenSource();
                    pack.ModelRequest = request;
                    _ = LoadModelAsync(pack, request);
                }
                return null;
            }

            var root = pack.Model.Root.transform;
            if (entry.Prefab != null) root = root.Find(entry.Prefab);
            if (root == null || entry.Parts.Any(part => root.Find(part.Path) == null))
            {
                pack.FailedEntries.Add(entry.Id);
                log("Appearance model does not contain the configured root or parts for " + entry.Id + ".");
                return null;
            }

            var loaded = new LoadedAppearance(entry, root.gameObject);
            pack.Loaded[entry.Id] = loaded;
            return loaded;
        }

        private async Task LoadModelAsync(Pack pack, CancellationTokenSource request)
        {
            GlbModel model = null;
            bool entered = false;
            try
            {
                await modelImports.WaitAsync(request.Token);
                entered = true;
                var path = pack.Path;
                var bytes = await Task.Run(() => ReadModel(path), request.Token);
                model = await GlbModel.LoadAsync(bytes, request.Token);
                if (request.IsCancellationRequested || !pack.Installed) return;
                pack.Model = model;
                model = null;
            }
            catch (OperationCanceledException) when (request.IsCancellationRequested) { }
            catch (Exception error)
            {
                if (!request.IsCancellationRequested)
                {
                    pack.LoadFailed = true;
                    log("Appearance model " + pack.Catalog.Name + " could not be loaded: " + error.Message);
                }
            }
            finally
            {
                if (pack.ModelRequest == request) pack.ModelRequest = null;
                model?.Dispose();
                if (entered) modelImports.Release();
                request.Dispose();
            }
        }

        private static byte[] ReadModel(string path)
        {
            using var input = File.OpenRead(path);
            using var reader = new BinaryReader(input);
            if (input.Length < 5 || input.Length > AppearanceData.MaxPackBytes)
                throw new InvalidDataException("Invalid appearance model packet size.");
            var header = reader.ReadInt32();
            var length = (int)input.Length - 4 - header;
            if (header <= 0 || header > AppearanceData.MaxCatalogBytes || length <= 0)
                throw new InvalidDataException("Invalid appearance model packet.");
            input.Seek(header, SeekOrigin.Current);
            var bytes = reader.ReadBytes(length);
            if (bytes.Length != length) throw new IOException("Incomplete appearance model payload.");
            return bytes;
        }

        private static AssetBundle FindNativeBundle(string relativePath, AppearanceEntry entry, string digest, int length)
        {
            if (entry.NativeOwner == null) return null;
            var service = CozyWorkshopService.Instance;
            if (service == null) return null;

            // A native equipment Mod can already own these exact bundle bytes.
            foreach (var mod in service.Mods)
            {
                if (!mod.enabled || string.IsNullOrEmpty(mod.directory) ||
                    File.Exists(Path.Combine(mod.directory, "appearance.json"))) continue;
                var file = new FileInfo(Path.Combine(mod.directory, relativePath));
                if (!file.Exists || file.Length != length || AppearanceData.Digest(File.ReadAllBytes(file.FullName)) != digest)
                    continue;
                return AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(bundle =>
                    bundle.GetAllAssetNames().Contains(entry.Prefab, StringComparer.OrdinalIgnoreCase));
            }
            return null;
        }

        public IReadOnlyList<WardrobeItem> Items(IEnumerable<(string Key, string Slot)> roomKeys)
        {
            var items = new List<WardrobeItem>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pack in known.Values.Where(pack => pack.Installed || pack.Saved).ToArray())
                foreach (var entry in pack.Catalog.Entries)
                {
                    var key = pack.Digest + "/" + entry.Id;
                    if (UpgradeSelection(key, entry.Slot) != key) continue;
                    keys.Add(key);
                    items.Add(ToItem(pack, entry, key));
                }

            foreach (var (key, slot) in roomKeys)
            {
                if (!keys.Add(key)) continue;
                var pack = FindEntry(key, out var entry);
                if (pack != null && entry.Slot == slot) items.Add(ToItem(pack, entry, key));
                else if (AppearanceData.TryKey(key, out _, out var id))
                    items.Add(new WardrobeItem { Key = key, Slot = slot, Name = id, Source = "房间获取" });
            }
            return items;
        }

        private static WardrobeItem ToItem(Pack pack, AppearanceEntry entry, string key) =>
            new WardrobeItem
            {
                Key = key, Name = entry.Name, Author = pack.Catalog.Author, Slot = entry.Slot,
                Source = pack.Installed ? "已安装" : pack.Saved ? "已保存" : "房间获取",
                Installed = pack.Installed, Saved = pack.Saved,
                CanEquip = pack.Installed || pack.Saved || pack.Catalog.AllowSave,
                Ready = !pack.LoadFailed && (!pack.Catalog.UsesGlb || pack.Loaded.ContainsKey(entry.Id)),
                IsModel = pack.Catalog.UsesGlb,
                LoadFailed = pack.LoadFailed || pack.FailedEntries.Contains(entry.Id)
            };

        public void TrimLoaded(ISet<string> inUse)
        {
            foreach (var pack in known.Values)
                if ((pack.Bundle != null || pack.Model != null || pack.ModelRequest != null) &&
                    !inUse.Contains(pack.Digest) && (pack != preview || !pack.Installed)) Unload(pack);
        }

        public void TrimCache(ISet<string> requested)
        {
            var files = new DirectoryInfo(cache).GetFiles("*.appearance")
                .Where(file => AppearanceData.IsDigest(Path.GetFileNameWithoutExtension(file.Name)))
                .OrderBy(file => file.LastWriteTimeUtc).ToArray();
            var remaining = files.Sum(file => file.Length);
            foreach (var file in files)
            {
                if (remaining <= MaxCacheBytes) break;
                var digest = Path.GetFileNameWithoutExtension(file.Name);
                known.TryGetValue(digest, out var pack);
                if (requested.Contains(digest) || pack?.Installed == true || pack?.Bundle != null ||
                    pack?.Model != null || pack?.ModelRequest != null) continue;
                var size = file.Length;
                try { file.Delete(); }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }
                if (pack?.Saved != true) known.Remove(digest);
                remaining -= size;
            }
        }

        public void Dispose()
        {
            preview = null;
            foreach (var pack in known.Values) Unload(pack);
            known.Clear();
        }

        public void CancelModelImports()
        {
            foreach (var pack in known.Values) CancelModelImport(pack);
        }

        private void Unload(Pack pack)
        {
            CancelModelImport(pack);
            pack.Loaded.Clear();
            pack.Model?.Dispose();
            pack.Model = null;
            if (pack.Bundle != null && --pack.Bundle.Users == 0)
            {
                if (!pack.Bundle.Borrowed && pack.Bundle.Value != null) pack.Bundle.Value.Unload(true);
                bundles.Remove(pack.Bundle.Digest);
            }
            pack.Bundle = null;
        }

        private static void CancelModelImport(Pack pack)
        {
            var request = pack.ModelRequest;
            pack.ModelRequest = null;
            request?.Cancel();
        }

        private Pack FindEntry(string key, out AppearanceEntry entry)
        {
            entry = null;
            if (!AppearanceData.TryKey(key, out var digest, out var id)) return null;
            var pack = Find(digest);
            entry = pack?.Catalog.Entries.FirstOrDefault(candidate => candidate.Id == id);
            return entry == null ? null : pack;
        }

        private Pack Find(string digest)
        {
            if (!AppearanceData.IsDigest(digest)) return null;
            if (known.TryGetValue(digest, out var pack)) return pack;
            var path = Path.Combine(cache, digest + ".appearance");
            if (!File.Exists(path) || !TryReadFile(digest, path, out pack)) return null;
            known[digest] = pack;
            return pack;
        }

        private bool TryReadFile(string digest, string path, out Pack pack)
        {
            pack = null;
            try
            {
                if (new FileInfo(path).Length > AppearanceData.MaxPackBytes) return false;
                var bytes = File.ReadAllBytes(path);
                if (AppearanceData.Digest(bytes) != digest) return false;
                pack = new Pack { Digest = digest, Path = path, Catalog = ReadHeader(bytes) };
                return true;
            }
            catch (Exception error)
            {
                log("Appearance cache could not be read: " + error.Message);
                return false;
            }
        }

        private static AppearanceCatalog ReadHeader(byte[] bytes)
        {
            if (bytes.Length < 5) throw new InvalidDataException("Incomplete appearance pack.");
            var length = BitConverter.ToInt32(bytes, 0);
            if (length > bytes.Length - 5) throw new InvalidDataException("Incomplete appearance bundle.");
            return AppearanceData.ReadCatalog(bytes, 4, length);
        }

        private void Register(string digest, byte[] bytes, AppearanceCatalog catalog)
        {
            if (known.ContainsKey(digest)) return;
            var path = Path.Combine(cache, digest + ".appearance");
            AtomicWrite(path, bytes);
            known[digest] = new Pack { Digest = digest, Path = path, Catalog = catalog };
        }

        private static void AtomicWrite(string path, byte[] bytes)
        {
            var temporary = path + ".tmp";
            File.WriteAllBytes(temporary, bytes);
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
    }
}
