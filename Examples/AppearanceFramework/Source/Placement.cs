using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ES3Internal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CozyMods.Appearance
{
    // One placement owns the complete visible selection of one Human skeleton.
    internal sealed class Placement : IDisposable
    {
        private readonly GameObject actor, skeleton;
        private readonly Transform face, hair, torso, bodyParts, bodyVisuals, neck;
        private readonly PCDEquipmentManager equipment;
        private readonly List<GameObject> roots = new List<GameObject>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<EquipmentVisual> equipmentVisuals = new List<EquipmentVisual>();
        private readonly HashSet<Renderer> fixedMasks = new HashSet<Renderer>();
        private readonly Dictionary<Renderer, bool> hidden = new Dictionary<Renderer, bool>();
        private readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        private Renderer[] nativeRenderers;
        private VisualImport import;
        private PlacementUpdater updater;
        private float nextMaskRefresh;

        public Placement(GameObject actor, GameObject skeleton)
        {
            this.actor = actor;
            this.skeleton = skeleton;
            equipment = actor.GetComponent<PCDEquipmentManager>();
            var bones = skeleton.GetComponent<PBSkeleton>();
            var head = bones?.GetBoneTransform("Head")?.Find("HeadGFX");
            var body = bones?.GetBoneTransform("Body");
            face = head?.Find("OriHeadGFX");
            hair = head?.Find("OriHatGFX");
            bodyParts = body?.Find("BodyParts");
            bodyVisuals = bodyParts?.Find("BodyHandle/BodyGFX/OriBodyGFX");
            torso = bodyVisuals?.Find("Human_Body");
            neck = body?.Find("BoneRenderer/Spine/Line3D_Mesh/Line3D_Skin");
            if (face == null || hair == null || torso == null || equipment == null)
                throw new InvalidDataException("Appearance target does not have the Human avatar anchors.");
        }

        public void Apply(IReadOnlyList<LoadedAppearance> appearances)
        {
            Dispose();
            if (appearances.Count == 0) return;
            nativeRenderers = skeleton.GetComponentsInChildren<Renderer>(true);
            import = new VisualImport();
            try
            {
                foreach (var appearance in appearances.OrderBy(value => value.Entry.Slot == "Body" ? 0 : 1))
                    ApplyParts(appearance);
                RefreshMasks();
                if (equipmentVisuals.Count != 0)
                {
                    var root = new GameObject("Appearance anchors");
                    root.transform.SetParent(skeleton.transform, false);
                    roots.Add(root);
                    updater = root.AddComponent<PlacementUpdater>();
                    updater.Refresh = RefreshWardrobe;
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void ApplyParts(LoadedAppearance appearance)
        {
            var entry = appearance.Entry;
            var palette = ReadMaterials(entry.Materials, appearance.UsesGlb);
            if (entry.Slot == "Body")
            {
                foreach (var anchor in entry.HideBody ?? Array.Empty<string>())
                {
                    if (anchor == "DefaultClothing")
                        fixedMasks.UnionWith(nativeRenderers.Where(IsDefaultClothing));
                    else
                        Mask(BodyAnchor(anchor));
                }
                if (entry.BodyMaterial != null)
                    ReplaceBodyMaterial(palette[entry.BodyMaterial]);
            }

            var baseMaterials = torso.GetComponent<Renderer>().sharedMaterials;
            foreach (var part in entry.Parts)
            {
                if (entry.Slot == "Body")
                {
                    var target = BodyAnchor(part.Anchor);
                    if (part.HideOriginal) Mask(target);
                    AttachPart(appearance, part, target, baseMaterials, palette);
                    continue;
                }

                var kind = ToEquipmentPart(part.Anchor);
                if (part.NativeEquipment is long prefabId)
                {
                    AttachUsingNativeEquipment(appearance, part, kind, prefabId, palette);
                    continue;
                }

                foreach (var handle in Handles(kind))
                {
                    var source = handle.oriEquipObj.SelectMany(item => item.GetComponentsInChildren<Renderer>(true))
                        .FirstOrDefault();
                    var stockMaterials = source == null ? baseMaterials : source.sharedMaterials;
                    AttachPart(appearance, part, CreateWardrobeAnchor(handle, part.HideOriginal), stockMaterials, palette);
                }
            }
        }

        private Transform BodyAnchor(string anchor) => anchor switch
        {
            "Face" => face,
            "Hair" => hair,
            "Torso" => torso,
            "BodyParts" => bodyParts,
            "BodyVisuals" => bodyVisuals,
            "Neck" => neck,
            _ => throw new InvalidDataException("Unknown body appearance anchor.")
        };

        private Dictionary<string, Material> ReadMaterials(Dictionary<string, AppearanceMaterial> definitions, bool usesGlb)
        {
            var palette = new Dictionary<string, Material>(StringComparer.Ordinal);
            if (definitions == null) return palette;
            foreach (var group in definitions.GroupBy(pair => pair.Value.Prefab))
            {
                var originals = ES3ReferenceMgrBase.Current.GetPrefab(group.Key, false)
                    .GetComponentsInChildren<Renderer>(true).SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null).ToLookup(material => material.name);
                foreach (var pair in group)
                    palette.Add(pair.Key, TintMaterial(originals[pair.Value.Name].First(), ReadTint(pair.Value.Color), usesGlb: usesGlb));
            }
            return palette;
        }

        private void ReplaceBodyMaterial(Material replacement)
        {
            foreach (var renderer in nativeRenderers)
            {
                var original = renderer.sharedMaterials;
                if (!original.Any(IsHumanSkin)) continue;
                originalMaterials.Add(renderer, original);
                renderer.sharedMaterials = original.Select(material => IsHumanSkin(material) ? replacement : material).ToArray();
            }
        }

        private void AttachUsingNativeEquipment(LoadedAppearance appearance, AppearancePart part, PCDEquipmentPart kind,
            long prefabId, Dictionary<string, Material> palette)
        {
            var stock = ES3ReferenceMgrBase.Current.GetPrefab(prefabId, false)
                .GetComponent<PCDEquipmentProp>().equipmentPrefab.GetComponent<PCDEquipment>();
            if (kind == PCDEquipmentPart.Sleeve)
                stock = stock.sleevePrefab.GetComponent<PCDEquipment>();
            if (stock.equipmentPart != kind)
                throw new InvalidDataException("The native equipment does not fit its appearance anchor.");

            var stockRenderer = stock.GetComponentInChildren<MeshRenderer>(true);
            var stockMaterials = stockRenderer.sharedMaterials;
            Func<Material, int, Material> material;
            if (appearance.UsesGlb)
                material = SlotMaterials(appearance, part, stockMaterials, palette);
            else
            {
                var color = TintMaterial(stockMaterials[0], ReadTint(part.Color ?? appearance.Entry.Color));
                material = (_, i) => i == 0 ? color : stockMaterials[Math.Min(i, stockMaterials.Length - 1)];
            }
            foreach (var handle in Handles(kind))
            {
                var anchor = CreateWardrobeAnchor(handle, part.HideOriginal || stock.needHideOri);
                var copy = Attach(appearance, part.Path, anchor, material);
                ApplyTransform(part, copy, stock.transform.InverseTransformPoint(stockRenderer.transform.position),
                    Quaternion.Inverse(stock.transform.rotation) * stockRenderer.transform.rotation,
                    RelativeScale(stockRenderer.transform, stock.transform));
            }
        }

        private Func<Material, int, Material> SlotMaterials(LoadedAppearance appearance, AppearancePart part,
            Material[] stockMaterials, Dictionary<string, Material> palette)
        {
            var variants = new Dictionary<(Material Stock, Texture2D Texture, Color? Tint), Material>();
            return (source, index) =>
            {
                if (source != null)
                {
                    var slot = source.name;
                    if (part.MaterialOverrides != null && part.MaterialOverrides.TryGetValue(slot, out var replacement))
                        return palette[replacement];
                    if (palette.TryGetValue(slot, out var selected)) return selected;
                }

                var regular = stockMaterials[Math.Min(index, stockMaterials.Length - 1)];
                if (regular == null) regular = torso.GetComponent<Renderer>().sharedMaterial;
                Texture2D texture = null;
                if (source != null)
                {
                    if (source.HasProperty("_AlbedoMap")) texture = source.GetTexture("_AlbedoMap") as Texture2D;
                    else if (source.HasProperty("_BaseMap")) texture = source.GetTexture("_BaseMap") as Texture2D;
                    else if (source.HasProperty("_MainTex")) texture = source.GetTexture("_MainTex") as Texture2D;
                }
                if (texture != null && (texture.width > 2048 || texture.height > 2048))
                    throw new InvalidDataException("Appearance texture exceeds 2048 pixels per side.");
                var tint = ReadTint(part.Color ?? appearance.Entry.Color);
                if (tint == null && appearance.UsesGlb && source != null) tint = source.color;
                var key = (regular, texture, tint);
                if (!variants.TryGetValue(key, out var material))
                    variants[key] = material = TintMaterial(regular, tint, texture, appearance.UsesGlb);
                return material;
            };
        }

        private void AttachPart(LoadedAppearance appearance, AppearancePart part, Transform target, Material[] stockMaterials,
            Dictionary<string, Material> palette)
        {
            var material = SlotMaterials(appearance, part, stockMaterials, palette);
            var copy = Attach(appearance, part.Path, target, material, part.FollowThrough == null);
            ApplyTransform(part, copy, copy.localPosition, copy.localRotation, copy.localScale);
            if (part.FollowAnchor != null)
                copy.SetParent(BodyAnchor(part.FollowAnchor), worldPositionStays: true);
            if (part.BlinkBlendShape != null)
                copy.gameObject.AddComponent<PlacementBlink>().Initialize(part.BlinkBlendShape);
            if (part.FollowThrough != null)
            {
                var body = actor.GetComponentInChildren<PCDHumanMgr>(true).skeleton.GetBone("Body").transform;
                PlacementTailMotion.Install(copy, body, part.FollowThrough);
                copy.gameObject.SetActive(true);
            }
        }

        private static void ApplyTransform(AppearancePart part, Transform copy, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            copy.SetLocalPositionAndRotation(VisualImport.Vector(part.Position, position), VisualImport.Rotation(part.Rotation, rotation));
            copy.localScale = VisualImport.Vector(part.Scale, scale);
        }

        private Material TintMaterial(Material material, Color? color, Texture2D texture = null, bool usesGlb = false)
        {
            if (color == null && texture == null) return material;
            var owned = new Material(material);
            materials.Add(owned);
            if (color is Color tint)
            {
                if (owned.HasProperty("_AlbedoColor")) owned.SetColor("_AlbedoColor", tint);
                if (owned.HasProperty("_Color")) owned.SetColor("_Color", tint);
                if (usesGlb && owned.HasProperty("_BaseColor")) owned.SetColor("_BaseColor", tint);
            }
            if (texture != null)
            {
                if (owned.HasProperty("_AlbedoMap")) owned.SetTexture("_AlbedoMap", texture);
                else if (owned.HasProperty("_MainTex")) owned.SetTexture("_MainTex", texture);
            }
            return owned;
        }

        private static Color? ReadTint(string value) => string.IsNullOrEmpty(value) ? (Color?)null : ParseColor(value);

        private static Color ParseColor(string value)
        {
            if (value.Length == 7 && value[0] == '#' && ColorUtility.TryParseHtmlString(value, out var color))
                return color;
            var channels = value.Split(',');
            if (channels.Length == 3)
            {
                var values = new float[3];
                for (int i = 0; i < values.Length; i++)
                    if (!float.TryParse(channels[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]) ||
                        !(values[i] >= 0 && values[i] <= 1))
                        throw new InvalidDataException("Appearance RGB channels must be in [0,1].");
                return new Color(values[0], values[1], values[2]);
            }
            throw new InvalidDataException("Appearance color must be #RRGGBB or r,g,b.");
        }

        private Transform Attach(LoadedAppearance appearance, string path, Transform target,
            Func<Material, int, Material> material, bool activate = true)
        {
            var root = appearance.Prefab.transform;
            var source = string.IsNullOrEmpty(path) ? root : root.Find(path);
            if (source == null || target == null)
                throw new InvalidDataException("Appearance part or its target anchor is missing.");
            var combined = import.Clone(source, target, material, activate, appearance.UsesGlb);
            roots.Add(combined.gameObject);
            return combined;
        }

        private Transform CreateWardrobeAnchor(PCDEquipmentHandleBone source, bool hideOriginal)
        {
            var anchor = new GameObject("Appearance " + source.boneName).transform;
            var visual = new EquipmentVisual(source, anchor, hideOriginal);
            roots.Add(anchor.gameObject);
            equipmentVisuals.Add(visual);
            visual.Place();
            return anchor;
        }

        private IEnumerable<PCDEquipmentHandleBone> Handles(PCDEquipmentPart kind)
        {
            var seen = new HashSet<PCDEquipmentHandleBone>();
            var remaining = new Queue<PCDEquipmentHandleBone>(equipment.GetBonesByType(kind));
            while (remaining.Count != 0)
            {
                var handle = remaining.Dequeue();
                if (handle == null || !handle.transform.IsChildOf(skeleton.transform) || !seen.Add(handle)) continue;
                yield return handle;
                foreach (var link in handle.linkHandleBones)
                    remaining.Enqueue(link);
            }
            if (seen.Count == 0) throw new InvalidDataException("The avatar equipment anchors are not ready.");
        }

        private static PCDEquipmentPart ToEquipmentPart(string anchor) => anchor switch
        {
            "Hat" => PCDEquipmentPart.Hat,
            "Cloth" => PCDEquipmentPart.Cloth,
            "Sleeve" => PCDEquipmentPart.Sleeve,
            "Shoe" => PCDEquipmentPart.Shoe,
            "Glove" => PCDEquipmentPart.Glove,
            "Knee" => PCDEquipmentPart.Knee,
            _ => throw new InvalidDataException("Unknown equipment appearance anchor.")
        };

        private void RefreshWardrobe()
        {
            foreach (var visual in equipmentVisuals) visual.Place();
            if (Time.unscaledTime < nextMaskRefresh) return;
            RefreshMasks();
            nextMaskRefresh = Time.unscaledTime + .15f;
        }

        private void RefreshMasks()
        {
            var desired = new HashSet<Renderer>(fixedMasks);
            foreach (var visual in equipmentVisuals)
            {
                var handle = visual.Handle;
                if (handle == null || !visual.Anchor.gameObject.activeInHierarchy) continue;
                desired.UnionWith(handle.GetComponentsInChildren<Renderer>(true));
                if (visual.HideOriginal)
                    foreach (var item in handle.oriEquipObj)
                        desired.UnionWith(item.GetComponentsInChildren<Renderer>(true));
            }
            foreach (var renderer in hidden.Keys.Where(renderer => renderer == null || !desired.Contains(renderer)).ToArray())
            {
                if (renderer != null) renderer.forceRenderingOff = hidden[renderer];
                hidden.Remove(renderer);
            }
            foreach (var renderer in desired)
            {
                if (renderer == null) continue;
                if (!hidden.ContainsKey(renderer)) hidden.Add(renderer, renderer.forceRenderingOff);
                renderer.forceRenderingOff = true;
            }
        }

        private void Mask(Transform anchor)
        {
            if (anchor != null)
                fixedMasks.UnionWith(nativeRenderers.Where(renderer => renderer.transform.IsChildOf(anchor)));
        }

        private static Vector3 RelativeScale(Transform value, Transform root)
        {
            var scale = Vector3.one;
            for (var current = value; current != root; current = current.parent)
                scale = Vector3.Scale(scale, current.localScale);
            return scale;
        }

        private static bool IsHumanSkin(Material material) =>
            material != null && (material.name == "Cozy-HumanSkin" || material.name == "Cozy-HumanSkin (Instance)");

        private static bool IsDefaultClothing(Renderer renderer)
        {
            if (renderer.name is "Human_Cloth_TShirt" or "Human_Cloth_Sleeve" or "Human-Shoe") return true;
            for (var parent = renderer.transform.parent; parent != null; parent = parent.parent)
                if (parent.name == "Clothes") return true;
            return false;
        }

        public void Dispose()
        {
            if (updater != null) updater.Refresh = null;
            foreach (var entry in hidden)
                if (entry.Key != null) entry.Key.forceRenderingOff = entry.Value;
            foreach (var entry in originalMaterials)
                if (entry.Key != null) entry.Key.sharedMaterials = entry.Value;
            foreach (var root in roots)
            {
                if (root == null) continue;
                root.SetActive(false);
                root.transform.SetParent(null, false);
                Object.Destroy(root);
            }
            foreach (var material in materials) Object.Destroy(material);
            hidden.Clear();
            fixedMasks.Clear();
            originalMaterials.Clear();
            roots.Clear();
            materials.Clear();
            equipmentVisuals.Clear();
            updater = null;
        }

        private sealed class EquipmentVisual
        {
            public readonly PCDEquipmentHandleBone Handle;
            public readonly Transform Anchor;
            public readonly bool HideOriginal;

            public EquipmentVisual(PCDEquipmentHandleBone handle, Transform anchor, bool hideOriginal)
            {
                Handle = handle;
                Anchor = anchor;
                HideOriginal = hideOriginal;
            }

            public void Place()
            {
                if (Handle == null)
                {
                    Anchor.gameObject.SetActive(false);
                    return;
                }
                var source = Handle.transform;
                if (Anchor.parent != source.parent) Anchor.SetParent(source.parent, false);
                Anchor.SetLocalPositionAndRotation(source.localPosition, source.localRotation);
                Anchor.localScale = source.localScale;
                Anchor.gameObject.SetActive(source.gameObject.activeSelf);
            }
        }
    }
}
