using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WitchRobeBundle
{
    private const string Root = "Assets/WitchRobe/";
    private const string PrefabPath = Root + "WitchRobe.prefab";
    private const string BundleName = "witch-robe-win64.bundle";

    [MenuItem("Cozy Island/Create Witch Robe Prefab")]
    public static void CreatePrefab()
    {
        var cloth = PreviewMaterial("ClothTeal", new Color32(38, 123, 115, 255));
        var trim = PreviewMaterial("TrimGold", new Color(0.75f, 0.57f, 0.3f));
        var lining = PreviewMaterial("LiningPurple", new Color(0.1727848f, 0.1063291f, 0.3f));
        var buckle = PreviewMaterial("BuckleSilver", new Color(0.75f, 0.75f, 0.75f));

        var root = new GameObject("WitchRobe");
        try
        {
            AddPart(root.transform, "Robe", "CozyCloth-MagicCloth.fbx",
                new Vector3(0, -0.26f, 0), new Quaternion(-0.7071057f, 0, 0, 0.7071079f),
                Vector3.one * 0.9156065f, cloth);
            AddPart(root.transform, "Sleeve", "Human-Sleeve-1-3.fbx",
                Vector3.zero, new Quaternion(0, -1, 0, 0.0000000745058f), Vector3.one, cloth);
            AddPart(root.transform, "Hat", "CozyMagicHat-1.fbx",
                new Vector3(0, 0.25f, -0.099998474f), new Quaternion(-0.8191521f, 0, 0, 0.57357645f),
                new Vector3(1.05f, 1.0500001f, 1.0500001f), cloth, trim, lining, buckle);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [MenuItem("Cozy Island/Build Witch Robe Bundle (Windows)")]
    public static void BuildWindows()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) CreatePrefab();
        string project = Path.GetDirectoryName(Application.dataPath);
        string work = Path.Combine(project, "Temp", "WitchRobeBundles");
        Directory.CreateDirectory(work);
        var build = new AssetBundleBuild { assetBundleName = BundleName, assetNames = new[] { PrefabPath } };
        if (BuildPipeline.BuildAssetBundles(work, new[] { build },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                BuildTarget.StandaloneWindows64) == null)
            throw new InvalidOperationException("Unity could not build the witch robe AssetBundle.");

        string path = Path.Combine(work, BundleName);
        Verify(path);
        string output = Path.GetFullPath(Path.Combine(project, "../../Assets"));
        Directory.CreateDirectory(output);
        string destination = Path.Combine(output, BundleName);
        File.Copy(path, destination, true);
        Debug.Log("Built " + destination);
    }

    private static Material PreviewMaterial(string name, Color color)
    {
        string path = Root + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Standard")) { name = name, color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void AddPart(Transform root, string name, string source, Vector3 position,
        Quaternion rotation, Vector3 scale, params Material[] materials)
    {
        var part = new GameObject(name);
        part.transform.SetParent(root, false);
        part.transform.SetLocalPositionAndRotation(position, rotation);
        part.transform.localScale = scale;
        var mesh = AssetDatabase.LoadAllAssetsAtPath(Root + "Models/" + source).OfType<Mesh>().Single();
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        part.AddComponent<MeshRenderer>().sharedMaterials = materials;
    }

    private static void Verify(string path)
    {
        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null) throw new InvalidDataException("Could not reopen the witch robe AssetBundle.");
        try
        {
            var prefab = bundle.LoadAsset<GameObject>(PrefabPath);
            foreach (string partName in new[] { "Robe", "Sleeve", "Hat" })
            {
                var part = prefab.transform.Find(partName);
                var mesh = part.GetComponent<MeshFilter>().sharedMesh;
                var materials = part.GetComponent<MeshRenderer>().sharedMaterials;
                int expectedSlots = partName == "Hat" ? 4 : 1;
                if (mesh == null || mesh.vertexCount == 0 || materials.Length != expectedSlots)
                    throw new InvalidDataException("Invalid witch robe model or material slots: " + partName);
                Debug.Log($"{partName}: {mesh.name}, vertices={mesh.vertexCount}, submeshes={mesh.subMeshCount}, materials={materials.Length}, bounds={mesh.bounds}");
            }
            if (prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                throw new InvalidDataException("Witch robe must be a visual-only prefab.");
        }
        finally
        {
            bundle.Unload(true);
        }
    }
}
