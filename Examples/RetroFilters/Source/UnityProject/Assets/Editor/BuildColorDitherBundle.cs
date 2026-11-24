using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class BuildColorDitherBundle
{
    private const string ShaderPath = "Assets/CozyColorDither.shader";
    private const string ShaderAddress = "assets/cozycolordither.shader";
    private const string BundleName = "colordither.bundle";

    [MenuItem("Cozy Island/Build Windows Retro Filter Bundle")]
    public static void BuildWindows()
    {
        if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
            throw new InvalidOperationException("Assign a URP Render Pipeline Asset before building.");

        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Check the import and compiler messages for " + ShaderPath);

        var project = Path.GetDirectoryName(Application.dataPath);
        var output = Path.Combine(project, "Temp", "RetroFilterBundles");
        Directory.CreateDirectory(output);
        var bundle = new AssetBundleBuild
        {
            assetBundleName = BundleName,
            assetNames = new[] { ShaderPath },
            addressableNames = new[] { ShaderAddress }
        };
        var result = BuildPipeline.BuildAssetBundles(output, new[] { bundle },
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
            BuildTarget.StandaloneWindows64);
        if (result == null) throw new InvalidOperationException("Could not build the color dither bundle.");

        var built = Path.Combine(output, BundleName);
        var assetBundle = AssetBundle.LoadFromFile(built);
        if (assetBundle == null) throw new InvalidDataException("Could not open the new retro filter bundle.");
        try
        {
            if (assetBundle.LoadAsset<Shader>(ShaderAddress) == null)
                throw new InvalidDataException("The new bundle is missing its filter shader.");
        }
        finally
        {
            assetBundle.Unload(true);
        }

        var destination = Path.GetFullPath(Path.Combine(project, "../../Assets", BundleName));
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.Copy(built, destination, overwrite: true);
        Debug.Log("Built retro filter bundle: " + destination);
    }
}
