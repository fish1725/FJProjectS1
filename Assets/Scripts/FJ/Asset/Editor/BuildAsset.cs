using System;
using System.Collections.Generic;
using System.IO;
using FJ.Utils;
using UnityEditor;
using UnityEngine;

namespace FJ.Asset.Editor
{
    public class BuildAsset
    {
        public static string CreateAssetBundleDirectory()
        {
            // Choose the output path according to the build target.
            var outputPath = Path.Combine(Utility.AssetBundlesOutputPath, Utility.GetPlatformName());
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            return outputPath;
        }

        public static void BuildAssetBundles()
        {
            BuildAssetBundles(null);
        }

        public static void BuildAssetBundles(AssetBundleBuild[] builds)
        {
            // Choose the output path according to the build target.
            var outputPath = CreateAssetBundleDirectory();

            var options = BuildAssetBundleOptions.None;

            var shouldCheckODR = EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS;
#if UNITY_TVOS
            shouldCheckODR |= EditorUserBuildSettings.activeBuildTarget == BuildTarget.tvOS;
#endif
            if (shouldCheckODR)
            {
#if ENABLE_IOS_ON_DEMAND_RESOURCES
                if (PlayerSettings.iOS.useOnDemandResources)
                    options |= BuildAssetBundleOptions.UncompressedAssetBundle;
#endif
#if ENABLE_IOS_APP_SLICING
                options |= BuildAssetBundleOptions.UncompressedAssetBundle;
#endif
            }

            if (builds == null || builds.Length == 0)
                BuildPipeline.BuildAssetBundles(outputPath, options, EditorUserBuildSettings.activeBuildTarget);
            else
                BuildPipeline.BuildAssetBundles(outputPath, builds, options, EditorUserBuildSettings.activeBuildTarget);
        }

        public static void BuildPlayer()
        {
            var outputPath = EditorUtility.SaveFolderPanel("Choose Location of the Built Game", "", "");
            if (outputPath.Length == 0)
                return;

            var levels = GetLevelsFromBuildSettings();
            if (levels.Length == 0)
            {
                Debug.Log("Nothing to build.");
                return;
            }

            var targetName = GetBuildTargetName(EditorUserBuildSettings.activeBuildTarget);
            if (targetName == null)
                return;

            // Build and copy AssetBundles.
            BuildAssetBundles();

#if UNITY_5_4 || UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0
            BuildOptions option = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None;
            BuildPipeline.BuildPlayer(levels, outputPath + targetName, EditorUserBuildSettings.activeBuildTarget, option);
#else
            var buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = levels;
            buildPlayerOptions.locationPathName = outputPath + targetName;
            buildPlayerOptions.assetBundleManifestPath = GetAssetBundleManifestFilePath();
            buildPlayerOptions.target = EditorUserBuildSettings.activeBuildTarget;
            buildPlayerOptions.options = EditorUserBuildSettings.development
                ? BuildOptions.Development
                : BuildOptions.None;
            BuildPipeline.BuildPlayer(buildPlayerOptions);
#endif
        }

        public static void BuildStandalonePlayer()
        {
            var outputPath = EditorUtility.SaveFolderPanel("Choose Location of the Built Game", "", "");
            if (outputPath.Length == 0)
                return;

            var levels = GetLevelsFromBuildSettings();
            if (levels.Length == 0)
            {
                Debug.Log("Nothing to build.");
                return;
            }

            var targetName = GetBuildTargetName(EditorUserBuildSettings.activeBuildTarget);
            if (targetName == null)
                return;

            // Build and copy AssetBundles.
            BuildAssetBundles();
            CopyAssetBundlesTo(Path.Combine(Application.streamingAssetsPath, Utility.AssetBundlesOutputPath));
            AssetDatabase.Refresh();

#if UNITY_5_4 || UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0
            BuildOptions option = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None;
            BuildPipeline.BuildPlayer(levels, outputPath + targetName, EditorUserBuildSettings.activeBuildTarget, option);
#else
            var buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = levels;
            buildPlayerOptions.locationPathName = outputPath + targetName;
            buildPlayerOptions.assetBundleManifestPath = GetAssetBundleManifestFilePath();
            buildPlayerOptions.target = EditorUserBuildSettings.activeBuildTarget;
            buildPlayerOptions.options = EditorUserBuildSettings.development
                ? BuildOptions.Development
                : BuildOptions.None;
            BuildPipeline.BuildPlayer(buildPlayerOptions);
#endif
        }

        public static string GetBuildTargetName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return "/test.apk";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "/test.exe";
#if UNITY_5_4_OR_NEWER
#else
                case BuildTarget.StandaloneOSXIntel:
                case BuildTarget.StandaloneOSXIntel64:
#endif
                case BuildTarget.StandaloneOSX:
                    return "/test.app";
#if UNITY_5_4_OR_NEWER
#else
                case BuildTarget.WebPlayer:
                case BuildTarget.WebPlayerStreamed:
#endif
                case BuildTarget.WebGL:
                case BuildTarget.iOS:
                    return "";
                // Add more build targets for your own.
                default:
                    Debug.Log("Target not implemented.");
                    return null;
            }
        }

        private static void CopyAssetBundlesTo(string outputPath)
        {
            // Clear streaming assets folder.
            FileUtil.DeleteFileOrDirectory(Application.streamingAssetsPath);
            Directory.CreateDirectory(outputPath);

            var outputFolder = Utility.GetPlatformName();

            // Setup the source folder for assetbundles.
            var source = Path.Combine(Path.Combine(Environment.CurrentDirectory, Utility.AssetBundlesOutputPath),
                outputFolder);
            if (!Directory.Exists(source))
                Debug.Log("No assetBundle output folder, try to build the assetBundles first.");

            // Setup the destination folder for assetbundles.
            var destination = Path.Combine(outputPath, outputFolder);
            if (Directory.Exists(destination))
                FileUtil.DeleteFileOrDirectory(destination);

            FileUtil.CopyFileOrDirectory(source, destination);
        }

        private static string[] GetLevelsFromBuildSettings()
        {
            var levels = new List<string>();
            for (var i = 0; i < EditorBuildSettings.scenes.Length; ++i)
                if (EditorBuildSettings.scenes[i].enabled)
                    levels.Add(EditorBuildSettings.scenes[i].path);

            return levels.ToArray();
        }

        private static string GetAssetBundleManifestFilePath()
        {
            var relativeAssetBundlesOutputPathForPlatform =
                Path.Combine(Utility.AssetBundlesOutputPath, Utility.GetPlatformName());
            return Path.Combine(relativeAssetBundlesOutputPathForPlatform, Utility.GetPlatformName()) + ".manifest";
        }
    }
}