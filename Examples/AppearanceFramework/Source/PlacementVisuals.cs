using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CozyMods.Appearance
{
    public sealed class AppearancePart
    {
        public string Path;
        public string Anchor;
        public string FollowAnchor;
        public string Color;
        public long? NativeEquipment;
        public Dictionary<string, string> MaterialOverrides;
        public float[] Position;
        public float[] Rotation;
        public float[] Scale;
        public bool HideOriginal;
        public string BlinkBlendShape;
        public AppearanceFollowThrough FollowThrough;
    }

    public sealed class AppearanceFollowThrough
    {
        public string Root;
        public string Target;
        public string[] Bones;
        public float[] EulerOffset;
        public float JointScale;
        public float[][] ScaleKeys;
    }

    // Import only visual data. An appearance prefab is never instantiated.
    internal sealed class VisualImport
    {
        private int nodes, vertices;

        public Transform Clone(Transform source, Transform parent, Func<Material, int, Material> material, bool activate,
            bool preserveMorphWeights)
        {
            var hierarchy = new Dictionary<Transform, Transform>();
            int startingVertices = vertices;
            GameObject root = new GameObject(source.name);
            root.SetActive(false);
            try
            {
                CopyTransforms(source, root.transform, parent, hierarchy, 0);
                foreach (var node in hierarchy)
                {
                    var original = node.Key;
                    var copy = node.Value.gameObject;
                    if (original.TryGetComponent<MeshRenderer>(out var regular) &&
                        original.TryGetComponent<MeshFilter>(out var filter))
                    {
                        var mesh = filter.sharedMesh;
                        CheckMesh(mesh);
                        copy.AddComponent<MeshFilter>().sharedMesh = mesh;
                        SetRenderer(copy.AddComponent<MeshRenderer>(), regular, mesh, material);
                    }
                    if (original.TryGetComponent<SkinnedMeshRenderer>(out var skinned))
                    {
                        var mesh = skinned.sharedMesh;
                        CheckMesh(mesh);
                        var output = copy.AddComponent<SkinnedMeshRenderer>();
                        output.sharedMesh = mesh;
                        var bones = skinned.bones;
                        if (bones.Length > 192) throw new InvalidDataException("Appearance part exceeds the skinning bone limit.");
                        output.bones = bones.Select(bone => MapBone(hierarchy, bone)).ToArray();
                        output.rootBone = skinned.rootBone == null ? null : MapBone(hierarchy, skinned.rootBone);
                        CheckVector(skinned.localBounds.center, 32);
                        CheckVector(skinned.localBounds.extents, 32);
                        output.localBounds = skinned.localBounds;
                        if (preserveMorphWeights)
                            for (int i = 0; i < mesh.blendShapeCount; i++)
                                output.SetBlendShapeWeight(i, skinned.GetBlendShapeWeight(i));
                        SetRenderer(output, skinned, mesh, material);
                    }
                }
                if (vertices == startingVertices) throw new InvalidDataException("Appearance part has no renderable mesh.");
                root.SetActive(activate && source.gameObject.activeSelf);
                return root.transform;
            }
            catch
            {
                root.transform.SetParent(null, false);
                Object.Destroy(root);
                throw;
            }
        }

        private void CopyTransforms(Transform source, Transform target, Transform parent,
            Dictionary<Transform, Transform> hierarchy, int depth)
        {
            if (++nodes > 512 || depth > 40)
                throw new InvalidDataException("Appearance hierarchy is too large.");
            CheckVector(source.localPosition, 32);
            CheckVector(source.localScale, 32);
            target.SetParent(parent, false);
            target.SetLocalPositionAndRotation(source.localPosition, Rotation(source.localRotation));
            target.localScale = source.localScale;
            hierarchy.Add(source, target);
            foreach (Transform child in source)
            {
                var childCopy = new GameObject(child.name);
                childCopy.transform.SetParent(target, false);
                childCopy.SetActive(child.gameObject.activeSelf);
                CopyTransforms(child, childCopy.transform, target, hierarchy, depth + 1);
            }
        }

        private static Transform MapBone(Dictionary<Transform, Transform> hierarchy, Transform bone)
        {
            if (bone != null && hierarchy.TryGetValue(bone, out var copy)) return copy;
            throw new InvalidDataException("The skinning bones must be inside the appearance part.");
        }

        private void CheckMesh(Mesh mesh)
        {
            if (mesh == null || mesh.vertexCount == 0 || mesh.subMeshCount < 1 || mesh.subMeshCount > 16 ||
                (vertices += mesh.vertexCount) > 160000)
                throw new InvalidDataException("Appearance mesh is missing or exceeds the geometry limit.");
            CheckVector(mesh.bounds.center, 32);
            CheckVector(mesh.bounds.extents, 32);
            ulong indices = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                indices += mesh.GetIndexCount(i);
            if (indices > 960000)
                throw new InvalidDataException("Appearance mesh exceeds the index limit.");
        }

        private static void SetRenderer(Renderer output, Renderer input, Mesh mesh, Func<Material, int, Material> material)
        {
            var source = input.sharedMaterials;
            var target = new Material[mesh.subMeshCount];
            for (int i = 0; i < target.Length; i++)
                target[i] = material(i < source.Length ? source[i] : null, i);
            output.sharedMaterials = target;
            output.enabled = input.enabled;
            output.shadowCastingMode = input.shadowCastingMode;
            output.receiveShadows = input.receiveShadows;
        }

        public static Vector3 Vector(float[] value, Vector3 fallback)
        {
            if (value == null) return fallback;
            if (value.Length != 3) throw new InvalidDataException("Appearance transform needs three values.");
            var result = new Vector3(value[0], value[1], value[2]);
            CheckVector(result, 32);
            return result;
        }

        public static Quaternion Rotation(float[] value, Quaternion fallback)
        {
            if (value == null) return fallback;
            if (value.Length != 4) throw new InvalidDataException("Appearance rotation needs a quaternion.");
            return Rotation(new Quaternion(value[0], value[1], value[2], value[3]));
        }

        private static Quaternion Rotation(Quaternion value)
        {
            CheckVector(new Vector3(value.x, value.y, value.z), 2);
            if (!(Mathf.Abs(value.w) <= 2) || Quaternion.Dot(value, value) < .001f)
                throw new InvalidDataException("Appearance rotation is invalid.");
            return value.normalized;
        }

        private static void CheckVector(Vector3 value, float limit)
        {
            if (!(Mathf.Abs(value.x) <= limit && Mathf.Abs(value.y) <= limit && Mathf.Abs(value.z) <= limit))
                throw new InvalidDataException("Appearance geometry has an invalid transform or bounds.");
        }
    }

    internal sealed class PlacementUpdater : MonoBehaviour
    {
        internal Action Refresh;
        private void LateUpdate() => Refresh?.Invoke();
    }

    internal sealed class PlacementBlink : MonoBehaviour
    {
        private (SkinnedMeshRenderer Renderer, int Index)[] eyes;
        internal void Initialize(string blendShape)
        {
            eyes = GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Select(r => (Renderer: r, Index: r.sharedMesh.GetBlendShapeIndex(blendShape)))
                .Where(x => x.Index >= 0).ToArray();
        }

        private void Update()
        {
            float time = Time.time % 5f;
            float weight = time < 1f / 3f ? 70 * Mathf.SmoothStep(0, 1, 1 - Mathf.Abs(time * 6 - 1)) : 0;
            foreach (var eye in eyes)
                eye.Renderer.SetBlendShapeWeight(eye.Index, weight);
        }
    }

    internal sealed class PlacementTailMotion : MonoBehaviour
    {
        private Transform modelBody, tailBase;
        private Vector3 restPosition, modelOffset;
        private Quaternion modelRotation;
        private float maxSwing, maxLengthChange;

        // Install while the part is inactive so PCDIK.Awake sees the configured chain.
        internal static void Install(Transform part, Transform body, AppearanceFollowThrough config)
        {
            var root = FindDescendant(part, config.Root);
            var target = FindDescendant(part, config.Target);
            if (body == null || root == target ||
                !(Mathf.Abs(root.lossyScale.x) >= .0001f && Mathf.Abs(root.lossyScale.x) < float.PositiveInfinity))
                throw new InvalidDataException("Appearance follow-through has invalid anchors.");
            if (config.Bones is not { Length: >= 3 and <= 16 } ||
                config.Bones.Any(string.IsNullOrWhiteSpace) || config.Bones.Distinct(StringComparer.Ordinal).Count() != config.Bones.Length ||
                config.EulerOffset is not { Length: 3 } || config.EulerOffset.Any(value => !(Mathf.Abs(value) <= 360)) ||
                !(config.JointScale > 0 && config.JointScale <= 32) ||
                config.ScaleKeys is not { Length: >= 2 and <= 16 } || config.ScaleKeys.Any(key => !IsScaleKey(key)))
                throw new InvalidDataException("Appearance follow-through has an invalid chain or scale curve.");

            var available = part.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SelectMany(renderer => renderer.bones).Distinct().ToLookup(bone => bone.name, StringComparer.Ordinal);
            var matches = config.Bones.Select(name => available[name]).ToArray();
            if (matches.Any(match => match.Count() != 1))
                throw new InvalidDataException("Each appearance follow-through bone must name one skinned mesh bone.");
            var bones = matches.Select(match => match.First()).ToArray();

            var chain = root.gameObject.AddComponent<PCDIK>();
            chain.root = root;
            chain.startBone = root.parent;
            chain.endBone = target;
            chain.start = bones[0];
            chain.joints = bones.Skip(1).Take(bones.Length - 2).ToArray();
            chain.end = bones[bones.Length - 1];
            chain.jointEulerOffset = new Vector3(config.EulerOffset[0], config.EulerOffset[1], config.EulerOffset[2]);
            chain.jointScale = config.JointScale;
            chain.jointScaleCurve = new AnimationCurve(config.ScaleKeys.Select(key => new Keyframe(key[0], key[1], key[2], key[3])).ToArray());
            chain.jointScaleScaleCurve = AnimationCurve.Constant(0, 1, 1);
            chain.debugMode = true;
            target.gameObject.AddComponent<PlacementTailMotion>().Follow(body, chain);
        }

        private static Transform FindDescendant(Transform part, string path)
        {
            var result = string.IsNullOrWhiteSpace(path) || path.Length > 192 ? null : part.Find(path);
            if (result == null || result == part || !result.IsChildOf(part))
                throw new InvalidDataException("Appearance follow-through anchor must be inside its part.");
            return result;
        }

        private static bool IsScaleKey(float[] key) => key is { Length: 4 } &&
            key[0] >= 0 && key[0] <= 1 && key[1] >= 0 && key[1] <= 32 &&
            Mathf.Abs(key[2]) <= 128 && Mathf.Abs(key[3]) <= 128;

        private void Follow(Transform body, PCDIK chain)
        {
            modelBody = body;
            tailBase = chain.startBone;
            restPosition = transform.localPosition;
            modelOffset = body.InverseTransformPoint(transform.position);
            modelRotation = Quaternion.Inverse(body.rotation) * transform.rotation;
            float length = Vector3.Distance(tailBase.position, transform.position);
            chain.oriLength = length * 0.995f / chain.root.lossyScale.x;
            maxSwing = length / 5f;
            maxLengthChange = length / 32f;
        }

        private void Update()
        {
            if (modelBody == null || tailBase == null) return;
            Vector3 rest = transform.parent.TransformPoint(restPosition);
            Vector3 axis = (rest - tailBase.position).normalized;
            Vector3 offset = modelBody.TransformPoint(modelOffset) - rest;
            float along = Vector3.Dot(offset, axis);
            Vector3 swing = Vector3.ClampMagnitude(offset - axis * along, maxSwing);
            transform.position = rest + swing + axis * Mathf.Clamp(along, -maxLengthChange, maxLengthChange);
            transform.rotation = Quaternion.Slerp(transform.rotation, modelBody.rotation * modelRotation, 25f * Time.deltaTime);
        }
    }
}
