using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CozyMods.Appearance
{
    internal sealed class GlbModel : IDisposable
    {
        private readonly GameObject owner;
        private readonly List<Object> assets = new List<Object>();

        public GameObject Root { get; }

        // The store calls this from Unity's synchronization context. Only decoding is done on a worker.
        public static async Task<GlbModel> LoadAsync(byte[] glb, CancellationToken cancellationToken)
        {
            var data = await Task.Run(() => GlbData.Read(glb, cancellationToken), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return new GlbModel(data, cancellationToken);
        }

        private GlbModel(GlbData data, CancellationToken cancellationToken)
        {
            owner = new GameObject("GLB appearance assets") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                owner.SetActive(false);
                Object.DontDestroyOnLoad(owner);
                Root = new GameObject("Appearance model");
                Root.transform.SetParent(owner.transform, false);

                var materials = CreateMaterials(data, cancellationToken);
                var nodes = new Transform[data.Nodes.Length];
                for (int i = 0; i < nodes.Length; i++)
                {
                    nodes[i] = new GameObject(data.Nodes[i].Name).transform;
                    nodes[i].SetParent(Root.transform, false);
                }
                var meshes = new Dictionary<(int, int), Mesh>();
                for (int i = 0; i < nodes.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var source = data.Nodes[i];
                    var node = nodes[i];
                    node.SetParent(source.Parent < 0 ? Root.transform : nodes[source.Parent], false);
                    node.SetLocalPositionAndRotation(source.Position, source.Rotation);
                    node.localScale = source.Scale;
                }

                for (int i = 0; i < nodes.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var source = data.Nodes[i];
                    if (source.Mesh < 0) continue;
                    var node = nodes[i].gameObject;
                    var geometry = data.Meshes[source.Mesh];
                    var skin = source.Skin < 0 ? null : data.Skins[source.Skin];
                    if (!meshes.TryGetValue((source.Mesh, source.Skin), out var mesh))
                        meshes.Add((source.Mesh, source.Skin), mesh = CreateMesh(geometry, skin));
                    Renderer renderer;
                    if (skin == null && geometry.Morphs.Length == 0)
                    {
                        node.AddComponent<MeshFilter>().sharedMesh = mesh;
                        renderer = node.AddComponent<MeshRenderer>();
                    }
                    else
                    {
                        var skinned = node.AddComponent<SkinnedMeshRenderer>();
                        skinned.sharedMesh = mesh;
                        skinned.localBounds = mesh.bounds;
                        if (skin != null)
                        {
                            skinned.bones = skin.Joints.Select(joint => nodes[joint]).ToArray();
                            if (skin.Root >= 0) skinned.rootBone = nodes[skin.Root];
                        }
                        var weights = source.MorphWeights ?? geometry.MorphWeights;
                        for (int j = 0; j < weights.Length; j++)
                            skinned.SetBlendShapeWeight(j, weights[j] * 100);
                        renderer = skinned;
                    }
                    renderer.sharedMaterials = geometry.Materials.Select(index => index < 0 ? null : materials[index]).ToArray();
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private Material[] CreateMaterials(GlbData data, CancellationToken cancellationToken)
        {
            if (data.Materials.Length == 0) return Array.Empty<Material>();
            // These materials are only carriers for slot names and albedo. Placement renders with game shaders.
            var shader = Shader.Find("UI/Default") ?? Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidDataException("No game shader is available for GLB material data.");

            var textures = new Texture2D[data.Images.Length];
            var materials = new Material[data.Materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var source = data.Materials[i];
                var material = Own(new Material(shader));
                material.name = source.Name;
                if (source.Image >= 0)
                {
                    int index = source.Image;
                    if (textures[index] == null)
                    {
                        var image = data.Images[index];
                        var texture = Own(new Texture2D(2, 2, TextureFormat.RGBA32, false));
                        if (!ImageConversion.LoadImage(texture, image.Bytes, true) ||
                            texture.width != image.Width || texture.height != image.Height)
                            throw new InvalidDataException("GLB base color texture could not be decoded.");
                        textures[index] = texture;
                    }
                    material.mainTexture = textures[index];
                }
                materials[i] = material;
            }
            return materials;
        }

        private Mesh CreateMesh(GlbData.Geometry source, GlbData.Skin skin)
        {
            var mesh = Own(new Mesh());
            mesh.indexFormat = source.Vertices.Length > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = source.Vertices;
            if (source.Normals != null) mesh.normals = source.Normals;
            if (source.Uvs != null) mesh.uv = source.Uvs;
            mesh.subMeshCount = source.Indices.Length;
            for (int i = 0; i < source.Indices.Length; i++)
                mesh.SetIndices(source.Indices[i], MeshTopology.Triangles, i, false);
            if (source.Normals == null) mesh.RecalculateNormals();
            if (skin != null)
            {
                mesh.bindposes = skin.BindPoses;
                mesh.boneWeights = source.BoneWeights;
            }
            for (int i = 0; i < source.Morphs.Length; i++)
                mesh.AddBlendShapeFrame(source.Morphs[i].Name, 100, source.Morphs[i].Positions, source.Morphs[i].Normals, null);
            mesh.RecalculateBounds();
            return mesh;
        }

        private T Own<T>(T asset) where T : Object
        {
            assets.Add(asset);
            return asset;
        }

        public void Dispose()
        {
            if (owner != null) Object.Destroy(owner);
            for (int i = assets.Count - 1; i >= 0; i--) Object.Destroy(assets[i]);
            assets.Clear();
        }
    }
}
