using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace CozyMods.Appearance
{
    internal static class AppearanceSlots
    {
        public static readonly string[] All = { "Body", "Hat", "Cloth", "Shoe", "Glove", "Knee" };
        public const string Empty = "|||||";

        public static string[] Parse(string selection)
        {
            var keys = selection?.Split('|');
            return keys?.Length == All.Length && keys.All(key => key.Length == 0 || AppearanceData.TryKey(key, out _, out _))
                ? keys : null;
        }
    }

    public sealed class AppearanceEntry
    {
        public string Id, Name, Slot, Prefab, Color, NativeOwner, NativeItem, BodyMaterial;
        public string[] HideBody;
        public Dictionary<string, AppearanceMaterial> Materials;
        public AppearancePart[] Parts;
    }

    public sealed class AppearanceMaterial
    {
        public long Prefab;
        public string Name;
        public string Color;
    }

    internal sealed class AppearanceCatalog
    {
        public int SchemaVersion { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Platform { get; set; }
        public string Bundle { get; set; }
        public bool Share { get; set; }
        public bool AllowSave { get; set; }
        public AppearanceEntry[] Entries { get; set; }
    }

    internal sealed class LoadedAppearance
    {
        public readonly AppearanceEntry Entry;
        public readonly GameObject Prefab;

        public LoadedAppearance(AppearanceEntry entry, GameObject prefab)
        {
            Entry = entry;
            Prefab = prefab;
        }
    }

    internal sealed class WardrobeItem
    {
        public string Key, Name, Author, Slot, Source;
        public bool Installed, Saved, CanEquip, Ready;
    }

    internal static class AppearanceData
    {
        public const int MaxCatalogBytes = 64 * 1024;
        public const int MaxPackBytes = 8 * 1024 * 1024;
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        public static string Platform => Application.platform == RuntimePlatform.WindowsPlayer ? "win64" :
            Application.platform == RuntimePlatform.OSXPlayer ? "macos" : null;

        public static string Digest(byte[] bytes)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        public static bool IsDigest(string value) => value?.Length == 64 && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

        private static bool IsSlug(string value) => value?.Length > 0 && value.Length <= 64
            && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'z' or '-' or '_' or '.');

        public static bool TryKey(string key, out string digest, out string entry)
        {
            digest = entry = null;
            if (key == null || key.Length < 66 || key.Length > 129 || key[64] != '/') return false;
            var hash = key.Substring(0, 64);
            var id = key.Substring(65);
            if (!IsDigest(hash) || !IsSlug(id)) return false;
            digest = hash;
            entry = id;
            return true;
        }

        public static AppearanceCatalog ReadCatalog(byte[] bytes, int offset, int length)
        {
            if (length <= 0 || length > MaxCatalogBytes) throw new InvalidDataException("Appearance catalog has an invalid size.");
            var json = Utf8.GetString(bytes, offset, length).TrimStart('\uFEFF');
            var catalog = JsonConvert.DeserializeObject<AppearanceCatalog>(json, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
                TypeNameHandling = TypeNameHandling.None,
                MaxDepth = 16
            });
            if (catalog == null || catalog.SchemaVersion != 1 || !IsSlug(catalog.Id) ||
                !ValidText(catalog.Name) || !ValidText(catalog.Author) ||
                catalog.Platform != Platform || !AssetPath(catalog.Bundle, ".bundle") ||
                catalog.Entries == null || catalog.Entries.Length == 0 || catalog.Entries.Length > 24)
                throw new InvalidDataException("Unsupported appearance catalog or platform.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in catalog.Entries)
            {
                if (entry == null || !IsSlug(entry.Id) || !ids.Add(entry.Id) || !ValidText(entry.Name) ||
                    !AppearanceSlots.All.Contains(entry.Slot) || !AssetPath(entry.Prefab, ".prefab") ||
                    (entry.Color?.Length ?? 0) > 64 || entry.Parts == null ||
                    entry.Parts.Length == 0 || entry.Parts.Length > 32)
                    throw new InvalidDataException("Invalid appearance entry.");
                if (entry.Materials != null && (entry.Materials.Count > 16 ||
                    entry.Materials.Any(pair => !ValidText(pair.Key) || pair.Value == null || pair.Value.Prefab <= 0 ||
                        !ValidText(pair.Value.Name) || pair.Value.Color?.Length > 64)))
                    throw new InvalidDataException("Invalid appearance palette.");
                bool body = entry.Slot == "Body";
                if ((entry.NativeOwner != null || entry.NativeItem != null) &&
                    (body || !IsSlug(entry.NativeOwner) || !IsSlug(entry.NativeItem)))
                    throw new InvalidDataException("Invalid native equipment mapping.");
                if ((entry.BodyMaterial != null && (!body || entry.Materials?.ContainsKey(entry.BodyMaterial) != true)) ||
                    (entry.HideBody != null && (!body || entry.HideBody.Length > 8 ||
                        entry.HideBody.Any(anchor => !IsBodyAnchor(anchor) && anchor != "DefaultClothing"))))
                    throw new InvalidDataException("Invalid body appearance options.");

                foreach (var part in entry.Parts)
                {
                    if (part == null || string.IsNullOrWhiteSpace(part.Path) || part.Path.Length > 192 ||
                        (part.Color?.Length ?? 0) > 64 ||
                        !Vector(part.Position, 3) || !Vector(part.Rotation, 4) || !Vector(part.Scale, 3))
                        throw new InvalidDataException("Invalid appearance part.");
                    if (body ? !IsBodyAnchor(part.Anchor) : part.Anchor != entry.Slot &&
                        (entry.Slot != "Cloth" || part.Anchor != "Sleeve"))
                        throw new InvalidDataException("Appearance part does not fit its slot.");
                    if ((part.NativeEquipment.HasValue && (body || part.NativeEquipment.Value <= 0)) ||
                        (part.FollowAnchor != null && (!body || !IsBodyAnchor(part.FollowAnchor))) ||
                        (part.FollowThrough != null && !body) ||
                        (part.BlinkBlendShape != null && (!body || !ValidText(part.BlinkBlendShape))) ||
                        (part.MaterialOverrides != null && (part.MaterialOverrides.Count > 16 ||
                            part.MaterialOverrides.Any(pair => !ValidText(pair.Key) || pair.Value == null ||
                                entry.Materials?.ContainsKey(pair.Value) != true))))
                        throw new InvalidDataException("Invalid appearance part options.");
                }
            }
            return catalog;
        }

        private static bool IsBodyAnchor(string anchor) =>
            anchor is "Face" or "Hair" or "Torso" or "BodyParts" or "BodyVisuals" or "Neck";

        private static bool ValidText(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 100 && !value.Any(char.IsControl);

        private static bool AssetPath(string path, string extension) => path != null && path.Length <= 192 &&
            path.StartsWith("Assets/", StringComparison.Ordinal) && path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) &&
            !path.Contains('\\') && !path.Contains(':') && path.Split('/').All(segment => segment.Length > 0 && segment != "." && segment != ".." && !segment.Any(char.IsControl));

        private static bool Vector(float[] values, int count) => values == null ||
            values.Length == count && values.All(value => !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) <= 100);
    }
}
