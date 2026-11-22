using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using Object = UnityEngine.Object;

public static class BuildCamp
{
    static readonly string[] PrefabPaths = { "Assets/camp.prefab", "Assets/ration.prefab" };

    public static void BuildWindows() => BuildBundle(BuildTarget.StandaloneWindows64, "researchcamp-win64.bundle");

    public static void BuildMac() => BuildBundle(BuildTarget.StandaloneOSX, "researchcamp-macos.bundle");

    static void BuildBundle(BuildTarget target, string bundleName)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            throw new InvalidOperationException($"Install the Unity Editor build support for {target} before building the bundle.");
        if (EditorUserBuildSettings.activeBuildTarget != target)
            throw new InvalidOperationException($"Switch the active build target to {target} or pass -buildTarget before starting Unity.");

        foreach (var path in PrefabPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                throw new InvalidDataException($"Missing research camp prefab: {path}");
        }

        // Publish only a newly built bundle. Keep the existing runtime copy on failure.
        var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Assets"));
        Directory.CreateDirectory(output);
        var staging = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp", "researchcamp-build-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(staging);
        try
        {
            var builds = new[] { new AssetBundleBuild { assetBundleName = bundleName, assetNames = PrefabPaths } };
            var manifest = BuildPipeline.BuildAssetBundles(staging, builds, BuildAssetBundleOptions.ChunkBasedCompression, target);
            var built = Path.Combine(staging, bundleName);
            if (manifest == null || Array.IndexOf(manifest.GetAllAssetBundles(), bundleName) < 0 ||
                !File.Exists(built) || new FileInfo(built).Length == 0)
                throw new InvalidDataException($"Unity did not build {bundleName} for {target}. See the Editor log.");

            var destination = Path.Combine(output, bundleName);
            if (File.Exists(destination))
                File.Replace(built, destination, null);
            else
                File.Move(built, destination);
            Debug.Log($"Built {destination} for {target} with Unity {Application.unityVersion}.");
        }
        finally
        {
            Directory.Delete(staging, true);
        }
    }

    static Material Mat(string name, Color c)
    {
        var m = new Material(Shader.Find("ResearchCamp/VertexTint"));
        m.color = c;
        AssetDatabase.CreateAsset(m, "Assets/" + name + ".mat");
        return m;
    }

    static void Part(GameObject root, string name, PrimitiveType shape, Vector3 pos, Vector3 scale, Material m)
    {
        var g = GameObject.CreatePrimitive(shape);
        g.name = name;
        g.transform.SetParent(root.transform, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.GetComponent<Renderer>().sharedMaterial = m;
    }

    // Recreates the stock example assets. BuildBundle never calls this reset method.
    public static void RegenerateExampleAssets()
    {
        var wood = Mat("Wood", new Color(.42f, .23f, .11f));
        var teal = Mat("Teal", new Color(.08f, .55f, .5f));
        var gold = Mat("Gold", new Color(1, .72f, .2f));
        var paper = Mat("Paper", new Color(.91f, .88f, .69f));
        var g = new GameObject("CampVisual");
        Part(g, "SurveyDesk", PrimitiveType.Cube, new Vector3(0, .85f, 0), new Vector3(1.4f, .14f, .7f), wood);
        for (int i = 0; i < 4; i++)
            Part(g, "Leg" + i, PrimitiveType.Cube, new Vector3(i % 2 == 0 ? -.57f : .57f, .4f, i < 2 ? -.23f : .23f), new Vector3(.1f, .8f, .1f), wood);
        Part(g, "MapBoard", PrimitiveType.Cube, new Vector3(0, .95f, 0), new Vector3(.7f, .02f, .45f), paper);
        Part(g, "SampleJar", PrimitiveType.Cylinder, new Vector3(.47f, 1.05f, 0), new Vector3(.2f, .13f, .2f), teal);
        Part(g, "Mast", PrimitiveType.Cylinder, new Vector3(-.65f, 1.5f, .3f), new Vector3(.05f, .8f, .05f), wood);
        Part(g, "Flag", PrimitiveType.Cube, new Vector3(-.37f, 2.18f, .3f), new Vector3(.55f, .32f, .04f), teal);
        Part(g, "Compass", PrimitiveType.Sphere, new Vector3(-.15f, 1, .05f), new Vector3(.2f, .07f, .2f), gold);
        Save(g, "camp");
        g = new GameObject("RationVisual");
        Part(g, "Tin", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.26f, .06f, .26f), teal);
        Part(g, "Lid", PrimitiveType.Cylinder, new Vector3(0, .07f, 0), new Vector3(.27f, .012f, .27f), gold);
        Save(g, "ration");
        AssetDatabase.SaveAssets();
    }

    static void Save(GameObject g, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(g, "Assets/" + name + ".prefab");
        Object.DestroyImmediate(g);
    }
}
