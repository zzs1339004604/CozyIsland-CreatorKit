using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CozyMods.Appearance
{
    // This reader handles the self-contained glTF 2.0 subset defined by the appearance format.
    // All input-controlled ranges and allocation sizes are checked before Unity sees the data.
    internal sealed class GlbData
    {
        public const int MaxBytes = 8 * 1024 * 1024;
        private const int MaxVertices = 160000, MaxIndices = 960000, MaxMorphVertices = 320000;
        private const int Float = 5126, UnsignedByte = 5121, UnsignedShort = 5123, UnsignedInt = 5125;

        internal sealed class Node
        {
            public string Name;
            public int Parent = -1, Mesh, Skin;
            public Vector3 Position, Scale;
            public Quaternion Rotation;
            public float[] MorphWeights;
        }

        internal sealed class Geometry
        {
            public Vector3[] Vertices, Normals;
            public Vector2[] Uvs;
            public BoneWeight[] BoneWeights;
            public int[][] Indices;
            public int[] Materials;
            public Morph[] Morphs;
            public float[] MorphWeights;
            public int MaxJoint;
        }

        internal sealed class Morph
        {
            public string Name;
            public Vector3[] Positions, Normals;
        }

        internal sealed class Skin
        {
            public int[] Joints;
            public int Root;
            public Matrix4x4[] BindPoses;
        }

        internal sealed class Surface
        {
            public string Name;
            public int Image;
            public Color BaseColor;
        }

        internal sealed class Image
        {
            public byte[] Bytes;
            public int Width, Height;
        }

        public Node[] Nodes { get; private set; }
        public Geometry[] Meshes { get; private set; }
        public Skin[] Skins { get; private set; }
        public Surface[] Materials { get; private set; }
        public Image[] Images { get; private set; }

        private readonly byte[] bytes;
        private readonly CancellationToken cancellation;
        private readonly JObject document;
        private readonly (int Offset, int Length, int Stride)[] views;
        private readonly JArray entries;
        private readonly Dictionary<int, Accessor> accessors = new Dictionary<int, Accessor>();
        private int totalVertices, totalIndices, totalMorphVertices, totalSparseIndices;

        public static GlbData Read(byte[] bytes, CancellationToken cancellation) =>
            new GlbData(bytes, cancellation);

        private GlbData(byte[] bytes, CancellationToken cancellation)
        {
            this.bytes = bytes;
            this.cancellation = cancellation;
            Require(bytes != null && bytes.Length >= 28 && bytes.Length <= MaxBytes, "invalid file size");
            Require(U32(0) == 0x46546c67 && U32(4) == 2 && U32(8) == bytes.Length, "invalid GLB 2.0 header");
            int jsonSize = Int32Checked(U32(12), "JSON chunk");
            Require(jsonSize > 0 && jsonSize % 4 == 0 && jsonSize <= 512 * 1024 &&
                jsonSize <= bytes.Length - 28 && U32(16) == 0x4e4f534a, "invalid JSON chunk");
            int binHeader = 20 + jsonSize;
            int binSize = Int32Checked(U32(binHeader), "binary chunk");
            int binStart = binHeader + 8;
            Require(binSize == bytes.Length - binStart && binSize % 4 == 0 &&
                U32(binHeader + 4) == 0x004e4942, "invalid binary chunk");

            using var json = new JsonTextReader(new StringReader(new UTF8Encoding(false, true).GetString(bytes, 20, jsonSize)))
                { MaxDepth = 48, DateParseHandling = DateParseHandling.None };
            document = JObject.Load(json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            Require(!json.Read() && (string)((document["asset"] as JObject)?["version"]) == "2.0", "invalid glTF document");
            Require(Array(document, "extensionsUsed", 0).Count == 0 && Array(document, "extensionsRequired", 0).Count == 0,
                "glTF extensions are unsupported");

            var buffers = Array(document, "buffers", 1);
            Require(buffers.Count == 1, "the model must have one embedded buffer");
            var buffer = Object(buffers[0], "buffer");
            int bufferSize = Integer(buffer["byteLength"], "buffer length");
            Require(buffer["uri"] == null && bufferSize > 0 && bufferSize <= binSize && binSize - bufferSize <= 3,
                "the model must have one embedded buffer");

            views = Array(document, "bufferViews", 2048).Select((token, index) =>
            {
                var view = Object(token, "buffer view");
                int offset = Optional(view["byteOffset"], 0, "buffer view offset");
                int length = Integer(view["byteLength"], "buffer view length");
                int stride = Optional(view["byteStride"], 0, "buffer view stride");
                Require(Integer(view["buffer"], "buffer") == 0 && offset >= 0 && length > 0 &&
                    offset <= bufferSize - length && (stride == 0 || (stride >= 4 && stride <= 252 && stride % 4 == 0)),
                    "invalid buffer view " + index);
                return (binStart + offset, length, stride);
            }).ToArray();
            entries = Array(document, "accessors", 4096);

            Images = ReadImages();
            Materials = ReadMaterials();
            Meshes = Array(document, "meshes", 256).Select(ReadMesh).ToArray();
            Nodes = ReadNodes();
            Skins = Array(document, "skins", 64).Select(ReadSkin).ToArray();
            ValidateRenderers();
        }

        private Node[] ReadNodes()
        {
            var entries = Array(document, "nodes", 511);
            Require(entries.Count > 0, "the model has no nodes");
            var nodes = entries.Select((token, i) =>
            {
                var node = Object(token, "node");
                Require(node["matrix"] == null, "node matrices are unsupported; export translation, rotation and scale");
                var rotation = Values(node["rotation"], 4, new[] { 0f, 0f, 0f, 1f }, -1.001f, 1.001f);
                double length = Math.Sqrt(rotation.Sum(value => (double)value * value));
                Require(Math.Abs(length - 1) < 0.01, "invalid node rotation");
                var position = Values(node["translation"], 3, new float[3], -32, 32);
                var scale = Values(node["scale"], 3, new[] { 1f, 1f, 1f }, -32, 32);
                int mesh = OptionalIndex(node["mesh"], Meshes.Length, "node mesh");
                return new Node
                {
                    Name = Name(node["name"], "Node " + i, true),
                    Mesh = mesh,
                    Skin = Optional(node["skin"], -1, "node skin"),
                    Position = new Vector3(-position[0], position[1], position[2]),
                    Rotation = new Quaternion((float)(rotation[0] / length), (float)(-rotation[1] / length),
                        (float)(-rotation[2] / length), (float)(rotation[3] / length)),
                    Scale = new Vector3(scale[0], scale[1], scale[2]),
                    MorphWeights = node["weights"] == null || mesh < 0 ? null :
                        Values(node["weights"], Meshes[mesh].Morphs.Length, null, -4, 4)
                };
            }).ToArray();
            var scenes = Array(document, "scenes", 1);
            Require(scenes.Count == 1 && Optional(document["scene"], 0, "scene") == 0, "export exactly one scene");
            var roots = Array(Object(scenes[0], "scene"), "nodes", nodes.Length);
            var visited = new bool[nodes.Length];
            void Visit(int index, int parent, int depth)
            {
                cancellation.ThrowIfCancellationRequested();
                Require(!visited[index] && depth <= 39, "invalid or oversized node hierarchy");
                visited[index] = true;
                nodes[index].Parent = parent;
                foreach (var child in Array(Object(entries[index], "node"), "children", nodes.Length))
                    Visit(Index(child, nodes.Length, "child node"), index, depth + 1);
            }
            foreach (var root in roots) Visit(Index(root, nodes.Length, "scene root"), -1, 1);
            Require(visited.All(value => value), "all model nodes must be in the scene");
            return nodes;
        }

        private Geometry ReadMesh(JToken token)
        {
            cancellation.ThrowIfCancellationRequested();
            var mesh = Object(token, "mesh");
            var primitives = Array(mesh, "primitives", 16);
            Require(primitives.Count > 0, "mesh has no primitives");
            var first = Object(primitives[0], "primitive");
            int shapes = Array(first, "targets", 16).Count;
            var names = Array(mesh["extras"] as JObject, "targetNames", 16);
            Require(names.Count == 0 || names.Count == shapes, "invalid morph names");
            var morphNames = Enumerable.Range(0, shapes)
                .Select(i => Name(names.Count == 0 ? null : names[i], "Shape " + i)).ToArray();
            Require(morphNames.Distinct(StringComparer.Ordinal).Count() == shapes, "duplicate morph name");

            var geometry = new Geometry
            {
                Indices = new int[primitives.Count][],
                Materials = new int[primitives.Count],
                MorphWeights = Values(mesh["weights"], shapes, new float[shapes], -4, 4)
            };
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var weights = new List<BoneWeight>();
            var deltas = morphNames.Select(_ => new List<Vector3>()).ToArray();
            var normalDeltas = morphNames.Select(_ => new List<Vector3>()).ToArray();
            bool allNormals = true, anyUv = false, anyWeights = false;

            for (int i = 0; i < primitives.Count; i++)
            {
                var part = Object(primitives[i], "primitive");
                Require(Optional(part["mode"], 4, "primitive mode") == 4 && part["extensions"] == null,
                    "only triangle primitives are supported");
                var attributes = Object(part["attributes"], "attributes");
                var vertices = ReadAccessor(attributes["POSITION"], "VEC3", MaxVertices, Float);
                int count = vertices.Count;
                int start = positions.Count;
                Require((totalVertices += count) <= MaxVertices &&
                    (totalMorphVertices += count * shapes) <= MaxMorphVertices, "mesh geometry budget exceeded");
                var normal = Attribute(attributes, "NORMAL", "VEC3", count, Float);
                var uv = Attribute(attributes, "TEXCOORD_0", "VEC2", count, Float, UnsignedByte, UnsignedShort);
                var joints = Attribute(attributes, "JOINTS_0", "VEC4", count, UnsignedByte, UnsignedShort);
                var influences = Attribute(attributes, "WEIGHTS_0", "VEC4", count, Float, UnsignedByte, UnsignedShort);
                Require((joints == null) == (influences == null) && attributes["JOINTS_1"] == null &&
                    attributes["WEIGHTS_1"] == null && joints?.Normalized != true, "maximum four skin influences per vertex");
                anyWeights |= joints != null;
                allNormals &= normal != null;
                anyUv |= uv != null;

                for (int j = 0; j < count; j++)
                {
                    if ((j & 2047) == 0) cancellation.ThrowIfCancellationRequested();
                    positions.Add(vertices.Vector(j, 32));
                    normals.Add(normal == null ? Vector3.zero : normal.Vector(j, 2));
                    uvs.Add(uv == null ? Vector2.zero : new Vector2(uv.Value(j, 0, -1024, 1024), 1 - uv.Value(j, 1, -1024, 1024)));
                    if (joints != null) weights.Add(ReadWeights(joints, influences, j, ref geometry.MaxJoint));
                }

                var indices = part["indices"] == null ? null :
                    ReadAccessor(part["indices"], "SCALAR", MaxIndices, UnsignedByte, UnsignedShort, UnsignedInt);
                int indexCount = indices?.Count ?? count;
                Require(indexCount % 3 == 0 && indices?.Normalized != true &&
                    (totalIndices += indexCount) <= MaxIndices, "mesh index budget exceeded or invalid triangles");
                var triangles = new int[indexCount];
                for (int j = 0; j < indexCount; j += 3)
                {
                    triangles[j] = ReadIndex(j);
                    triangles[j + 1] = ReadIndex(j + 2);
                    triangles[j + 2] = ReadIndex(j + 1);
                }
                int ReadIndex(int element)
                {
                    uint vertex = indices == null ? (uint)element : indices.RawInt(element, 0);
                    Require(vertex < count, "triangle index is outside its primitive");
                    return start + (int)vertex;
                }
                geometry.Indices[i] = triangles;
                geometry.Materials[i] = OptionalIndex(part["material"], Materials.Length, "primitive material");

                var targets = Array(part, "targets", 16);
                Require(targets.Count == shapes, "mesh primitives must have matching morphs");
                for (int j = 0; j < shapes; j++)
                {
                    var target = Object(targets[j], "morph target");
                    var delta = Attribute(target, "POSITION", "VEC3", count, Float);
                    var deltaNormal = Attribute(target, "NORMAL", "VEC3", count, Float);
                    for (int k = 0; k < count; k++)
                    {
                        deltas[j].Add(delta == null ? Vector3.zero : delta.Vector(k, 64));
                        normalDeltas[j].Add(deltaNormal == null ? Vector3.zero : deltaNormal.Vector(k, 4));
                    }
                }
            }

            Require(!anyWeights || weights.Count == positions.Count, "skin weights must cover all primitives");
            geometry.Vertices = positions.ToArray();
            geometry.Normals = allNormals ? normals.ToArray() : null;
            geometry.Uvs = anyUv ? uvs.ToArray() : null;
            geometry.BoneWeights = anyWeights ? weights.ToArray() : null;
            geometry.Morphs = morphNames.Select((name, i) => new Morph
            {
                Name = name,
                Positions = deltas[i].ToArray(),
                Normals = normalDeltas[i].ToArray()
            }).ToArray();
            return geometry;
        }

        private static BoneWeight ReadWeights(Accessor joints, Accessor weights, int i, ref int maxJoint)
        {
            var j0 = (int)joints.RawInt(i, 0);
            var j1 = (int)joints.RawInt(i, 1);
            var j2 = (int)joints.RawInt(i, 2);
            var j3 = (int)joints.RawInt(i, 3);
            var w0 = weights.Value(i, 0, 0, 1);
            var w1 = weights.Value(i, 1, 0, 1);
            var w2 = weights.Value(i, 2, 0, 1);
            var w3 = weights.Value(i, 3, 0, 1);
            // Unity expects influences in descending weight order.
            Sort(ref w0, ref j0, ref w1, ref j1);
            Sort(ref w2, ref j2, ref w3, ref j3);
            Sort(ref w0, ref j0, ref w2, ref j2);
            Sort(ref w1, ref j1, ref w3, ref j3);
            Sort(ref w1, ref j1, ref w2, ref j2);
            float sum = w0 + w1 + w2 + w3;
            Require(sum > 0, "skinned vertex has no joint weight");
            maxJoint = Math.Max(maxJoint, Math.Max(Math.Max(j0, j1), Math.Max(j2, j3)));
            return new BoneWeight
            {
                boneIndex0 = j0, boneIndex1 = j1, boneIndex2 = j2, boneIndex3 = j3,
                weight0 = w0 / sum, weight1 = w1 / sum, weight2 = w2 / sum, weight3 = w3 / sum
            };
        }

        private static void Sort(ref float first, ref int firstJoint, ref float second, ref int secondJoint)
        {
            if (first >= second) return;
            (first, second) = (second, first);
            (firstJoint, secondJoint) = (secondJoint, firstJoint);
        }

        private Skin ReadSkin(JToken token)
        {
            var skin = Object(token, "skin");
            var joints = Array(skin, "joints", 192).Select(joint => Index(joint, Nodes.Length, "skin joint")).ToArray();
            Require(joints.Length > 0 && joints.Distinct().Count() == joints.Length, "invalid skin joints");
            var binds = new Matrix4x4[joints.Length];
            var matrices = skin["inverseBindMatrices"] == null ? null :
                ReadAccessor(skin["inverseBindMatrices"], "MAT4", 192, Float);
            Require(matrices == null || matrices.Count == joints.Length, "skin bind pose count differs from joint count");
            for (int i = 0; i < binds.Length; i++)
            {
                var matrix = Matrix4x4.identity;
                if (matrices != null)
                    for (int c = 0; c < 4; c++)
                        for (int r = 0; r < 4; r++)
                        {
                            float value = matrices.Value(i, c * 4 + r, -1024, 1024);
                            matrix[r, c] = (r == 0) != (c == 0) ? -value : value;
                        }
                Require(matrix.m30 == 0 && matrix.m31 == 0 && matrix.m32 == 0 && matrix.m33 == 1,
                    "invalid affine skin bind pose");
                binds[i] = matrix;
            }
            return new Skin { Joints = joints, Root = OptionalIndex(skin["skeleton"], Nodes.Length, "skin root"), BindPoses = binds };
        }

        private void ValidateRenderers()
        {
            var shared = new HashSet<(int, int)>();
            int vertices = 0, indices = 0, morphs = 0;
            foreach (var node in Nodes)
            {
                Require(node.Skin >= -1 && node.Skin < Skins.Length && (node.Mesh >= 0 || node.Skin < 0), "invalid node skin");
                if (node.Mesh < 0) continue;
                var mesh = Meshes[node.Mesh];
                if (node.Skin >= 0)
                    Require(mesh.BoneWeights != null && mesh.MaxJoint < Skins[node.Skin].Joints.Length,
                        "mesh joints are outside the renderer skin");
                else
                    Require(mesh.BoneWeights == null, "skinned mesh has no node skin");
                if (!shared.Add((node.Mesh, node.Skin))) continue;
                Require((vertices += mesh.Vertices.Length) <= MaxVertices &&
                    (indices += mesh.Indices.Sum(array => array.Length)) <= MaxIndices &&
                    (morphs += mesh.Vertices.Length * mesh.Morphs.Length) <= MaxMorphVertices,
                    "instanced mesh budget exceeded");
            }
        }

        private Surface[] ReadMaterials()
        {
            var textures = Array(document, "textures", 32).Select(texture =>
                Index(Object(texture, "texture")["source"], Images.Length, "texture image")).ToArray();
            return Array(document, "materials", 64).Select((token, i) =>
            {
                var material = Object(token, "material");
                var pbr = material["pbrMetallicRoughness"] as JObject;
                var factor = Values(pbr?["baseColorFactor"], 4, new[] { 1f, 1f, 1f, 1f }, 0, 1);
                var info = pbr?["baseColorTexture"] as JObject;
                Require(Optional(info?["texCoord"], 0, "texture coordinates") == 0, "base color needs TEXCOORD_0");
                return new Surface
                {
                    Name = Name(material["name"], "Material " + i),
                    Image = info == null ? -1 : textures[Index(info["index"], textures.Length, "base color texture")],
                    BaseColor = new Color(factor[0], factor[1], factor[2], factor[3])
                };
            }).ToArray();
        }

        private Image[] ReadImages()
        {
            long pixels = 0, imageBytes = 0;
            return Array(document, "images", 16).Select(token =>
            {
                var image = Object(token, "image");
                Require(image["uri"] == null, "images must be embedded");
                string type = (string)image["mimeType"];
                var view = views[Index(image["bufferView"], views.Length, "image buffer view")];
                Require((imageBytes += view.Length) <= MaxBytes, "image byte budget exceeded");
                var data = new ReadOnlySpan<byte>(bytes, view.Offset, view.Length);
                var size = GlbImageSize.Read(data, type);
                Require(size.Width > 0 && size.Width <= 2048 && size.Height > 0 && size.Height <= 2048 &&
                    (pixels += (long)size.Width * size.Height) <= 8 * 1024 * 1024, "image pixel budget exceeded");
                return new Image { Bytes = data.ToArray(), Width = size.Width, Height = size.Height };
            }).ToArray();
        }

        private Accessor Attribute(JObject attributes, string name, string type, int count, params int[] components)
        {
            if (attributes[name] == null) return null;
            var result = ReadAccessor(attributes[name], type, count, components);
            Require(result.Count == count, name + " count differs from POSITION");
            Require((name != "TEXCOORD_0" && name != "WEIGHTS_0") || result.Component == Float ||
                result.Normalized, "integer UVs and skin weights must be normalized");
            return result;
        }

        private Accessor ReadAccessor(JToken token, string type, int limit, params int[] components)
        {
            int index = Index(token, entries.Count, "accessor");
            if (!accessors.TryGetValue(index, out var accessor))
            {
                var entry = Object(entries[index], "accessor");
                accessor = new Accessor(this, entry);
                accessors.Add(index, accessor);
            }
            Require(accessor.Type == type && accessor.Count <= limit && components.Contains(accessor.Component),
                "unexpected or oversized " + type + " accessor");
            return accessor;
        }

        private sealed class Accessor
        {
            private readonly GlbData model;
            private readonly int size, offset, stride, sparseOffset, elementSize;
            private readonly int[] sparseIndices;
            public string Type { get; }
            public int Component { get; }
            public int Count { get; }
            public bool Normalized { get; }

            public Accessor(GlbData model, JObject entry)
            {
                this.model = model;
                Type = (string)entry["type"];
                int channels = Type switch { "SCALAR" => 1, "VEC2" => 2, "VEC3" => 3, "VEC4" => 4, "MAT4" => 16,
                    _ => throw Invalid("unsupported accessor type") };
                Component = Integer(entry["componentType"], "accessor component");
                size = ComponentSize(Component);
                elementSize = channels * size;
                Count = Integer(entry["count"], "accessor count");
                Require(Count > 0 && Count <= MaxIndices &&
                    (entry["normalized"] == null || entry["normalized"].Type == JTokenType.Boolean),
                    "invalid accessor count or normalization");
                Normalized = (bool?)entry["normalized"] ?? false;
                Require(!Normalized || Component == UnsignedByte || Component == UnsignedShort, "invalid normalized accessor");
                int local = Optional(entry["byteOffset"], 0, "accessor offset");
                offset = -1;
                if (entry["bufferView"] != null)
                    (offset, stride) = model.Block(entry["bufferView"], local, Count, elementSize, size, true);
                else Require(local == 0, "zero-filled accessor cannot have a byte offset");

                if (entry["sparse"] == null) return;
                var sparse = Object(entry["sparse"], "sparse accessor");
                int sparseCount = Integer(sparse["count"], "sparse count");
                Require(sparseCount > 0 && sparseCount <= Count &&
                    (model.totalSparseIndices += sparseCount) <= MaxMorphVertices, "sparse accessor budget exceeded");
                var indices = Object(sparse["indices"], "sparse indices");
                var values = Object(sparse["values"], "sparse values");
                int indexType = Integer(indices["componentType"], "sparse index component");
                Require(indexType != Float, "invalid sparse index format");
                int indexSize = ComponentSize(indexType);
                var source = model.Block(indices["bufferView"], Optional(indices["byteOffset"], 0, "sparse index offset"),
                    sparseCount, indexSize, indexSize, false).Offset;
                sparseOffset = model.Block(values["bufferView"], Optional(values["byteOffset"], 0, "sparse value offset"),
                    sparseCount, elementSize, size, false).Offset;
                sparseIndices = new int[sparseCount];
                int previous = -1;
                for (int i = 0; i < sparseCount; i++)
                {
                    uint next = model.ReadInt(source + i * indexSize, indexType);
                    Require(next < Count && (int)next > previous, "sparse indices must be sorted and in range");
                    previous = sparseIndices[i] = (int)next;
                }
            }

            private int Address(int element, int channel)
            {
                if (sparseIndices != null)
                {
                    int sparse = System.Array.BinarySearch(sparseIndices, element);
                    if (sparse >= 0) return sparseOffset + sparse * elementSize + channel * size;
                }
                return offset < 0 ? -1 : offset + element * stride + channel * size;
            }

            public uint RawInt(int element, int channel)
            {
                int address = Address(element, channel);
                return address < 0 ? 0 : model.ReadInt(address, Component);
            }

            public float Value(int element, int channel, float min, float max)
            {
                int address = Address(element, channel);
                float value = address < 0 ? 0 : Component == Float ?
                    BitConverter.Int32BitsToSingle(unchecked((int)model.U32(address))) :
                    model.ReadInt(address, Component) / (Normalized ? (size == 1 ? 255f : 65535f) : 1);
                Require(value >= min && value <= max, "accessor value is outside permitted bounds");
                return value;
            }

            public Vector3 Vector(int element, float limit) =>
                new Vector3(-Value(element, 0, -limit, limit), Value(element, 1, -limit, limit), Value(element, 2, -limit, limit));
        }

        private (int Offset, int Stride) Block(JToken reference, int offset, int count, int element, int alignment, bool allowStride)
        {
            var view = views[Index(reference, views.Length, "buffer view")];
            int stride = view.Stride == 0 ? element : view.Stride;
            Require(offset >= 0 && offset % alignment == 0 && view.Offset % alignment == 0 &&
                (allowStride || view.Stride == 0) && stride >= element && stride % alignment == 0 &&
                (long)offset + (long)(count - 1) * stride + element <= view.Length, "accessor exceeds its buffer view");
            return (view.Offset + offset, stride);
        }

        private static int ComponentSize(int component) => component switch
        {
            UnsignedByte => 1,
            UnsignedShort => 2,
            UnsignedInt or Float => 4,
            _ => throw Invalid("unsupported accessor component")
        };

        private uint ReadInt(int address, int component) => component switch
        {
            UnsignedByte => bytes[address],
            UnsignedShort => BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(address, 2)),
            UnsignedInt => U32(address),
            _ => throw Invalid("unsupported integer component")
        };

        private uint U32(int address) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(address, 4));

        private static JObject Object(JToken token, string name) =>
            token as JObject ?? throw Invalid("missing or invalid " + name);

        private static JArray Array(JObject parent, string property, int max)
        {
            var token = parent?[property];
            if (token == null) return new JArray();
            Require(token is JArray entries && entries.Count <= max, "invalid or oversized " + property);
            return (JArray)token;
        }

        private static string Name(JToken token, string fallback, bool pathSegment = false)
        {
            if (token == null) return fallback;
            var name = (string)token;
            Require(!string.IsNullOrWhiteSpace(name) && name.Length <= 100 && !name.Any(char.IsControl) &&
                (!pathSegment || (!name.Contains('/') && name != "." && name != "..")), "invalid asset name");
            return name;
        }

        private static int Integer(JToken token, string name)
        {
            Require(token?.Type == JTokenType.Integer, "missing or invalid " + name);
            long value = token.Value<long>();
            Require(value >= int.MinValue && value <= int.MaxValue, "invalid " + name);
            return (int)value;
        }

        private static int Optional(JToken token, int fallback, string name) => token == null ? fallback : Integer(token, name);
        private static int OptionalIndex(JToken token, int count, string name) => token == null ? -1 : Index(token, count, name);

        private static int Index(JToken token, int count, string name)
        {
            int index = Integer(token, name);
            Require(index >= 0 && index < count, "missing or invalid " + name);
            return index;
        }

        private static int Int32Checked(uint value, string name)
        {
            Require(value <= int.MaxValue, "invalid " + name);
            return (int)value;
        }

        private static float[] Values(JToken token, int count, float[] fallback, float min, float max)
        {
            if (token == null) return fallback;
            Require(token is JArray values && values.Count == count, "unexpected vector size");
            return ((JArray)token).Select(entry =>
            {
                Require(entry.Type == JTokenType.Integer || entry.Type == JTokenType.Float, "expected numeric value");
                var value = entry.Value<float>();
                Require(value >= min && value <= max, "numeric value is outside permitted bounds");
                return value;
            }).ToArray();
        }

        private static InvalidDataException Invalid(string message) => new InvalidDataException("GLB appearance: " + message + ".");

        private static void Require(bool valid, string message)
        {
            if (!valid) throw Invalid(message);
        }
    }
}
