using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SkinBundleBuilder
{
    private const string MenuPath = "Cozy Island/Build Selected Prefab Bundle (Windows)";

    [MenuItem(MenuPath, true)]
    private static bool CanBuild() => Selection.activeObject is GameObject &&
        AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);

    [MenuItem(MenuPath)]
    private static void BuildSelected()
    {
        string prefab = AssetDatabase.GetAssetPath(Selection.activeObject);
        string name = Path.GetFileNameWithoutExtension(prefab).ToLowerInvariant();
        string destination = EditorUtility.SaveFilePanel("Save prefab bundle", "", name, "bundle");
        if (destination.Length != 0) BuildWindows(prefab, destination);
    }

    public static void BuildWindows(string prefab, string destination)
    {
        string project = Path.GetDirectoryName(Application.dataPath);
        string work = Path.Combine(project, "Temp", "CozyIslandAssetBundles");
        Directory.CreateDirectory(work);
        string name = Path.GetFileName(destination).ToLowerInvariant();
        var build = new AssetBundleBuild { assetBundleName = name, assetNames = new[] { prefab } };
        var manifest = BuildPipeline.BuildAssetBundles(work, new[] { build },
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
            BuildTarget.StandaloneWindows64);
        if (manifest == null) throw new InvalidOperationException("Could not build the bundle for " + prefab);

        string output = Path.Combine(work, name);
        var bundle = AssetBundle.LoadFromFile(output);
        if (bundle == null) throw new InvalidDataException("Could not reopen the built bundle.");
        try
        {
            if (bundle.LoadAsset<GameObject>(prefab) == null)
                throw new InvalidDataException("Bundle does not contain " + prefab);
        }
        finally
        {
            bundle.Unload(true);
        }

        File.Copy(output, destination, overwrite: true);
        Debug.Log($"Built {destination} with prefab {prefab}");
    }
}
