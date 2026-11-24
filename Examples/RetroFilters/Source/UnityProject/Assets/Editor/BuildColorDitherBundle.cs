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

    [MenuItem("Cozy Island/Build Windows Retro Filter Bundle")]
    public static void BuildWindows() => BuildBundle(BuildTarget.StandaloneWindows64, "colordither.bundle");

    [MenuItem("Cozy Island/Build macOS Retro Filter Bundle")]
    public static void BuildMac() => BuildBundle(BuildTarget.StandaloneOSX, "colordither-macos.bundle");

    private static void BuildBundle(BuildTarget target, string bundleName)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            throw new InvalidOperationException($"Install the Unity Editor build support for {target} before building the bundle.");
        if (EditorUserBuildSettings.activeBuildTarget != target)
            throw new InvalidOperationException($"Switch the active build target to {target} or pass -buildTarget before starting Unity.");
        if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
            throw new InvalidOperationException("Assign a URP Render Pipeline Asset before building.");

        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Check the import and compiler messages for " + ShaderPath);

        var project = Path.GetDirectoryName(Application.dataPath);
        var output = Path.Combine(project, "Temp", "RetroFilterBundles", target.ToString());
        Directory.CreateDirectory(output);
        var bundle = new AssetBundleBuild
        {
            assetBundleName = bundleName,
            assetNames = new[] { ShaderPath },
            addressableNames = new[] { ShaderAddress }
        };
        var result = BuildPipeline.BuildAssetBundles(output, new[] { bundle },
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, target);
        if (result == null) throw new InvalidOperationException("Could not build the color dither bundle.");

        var built = Path.Combine(output, bundleName);
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

        var destination = Path.GetFullPath(Path.Combine(project, "../../Assets", bundleName));
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        if (File.Exists(destination))
            File.Replace(built, destination, null);
        else
            File.Move(built, destination);
        Debug.Log($"Built {destination} for {target} with Unity {Application.unityVersion}.");
    }
}
