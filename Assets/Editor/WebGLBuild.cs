using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Passo 4: gera o build WebGL em Builds/WebGL (pasta que vai, zipada, para o itch.io).
public static class WebGLBuild
{
    public const string OutputPath = "Builds/WebGL";

    [MenuItem("Poeira Estelar/4. Build WebGL")]
    public static void Build()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        });

        var summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"[Build] WebGL OK: {summary.totalSize / (1024f * 1024f):0.0} MB em {summary.totalTime.TotalSeconds:0}s -> {OutputPath}");
        else
            Debug.LogError($"[Build] WebGL falhou: {summary.result}, {summary.totalErrors} erro(s)");

        if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
