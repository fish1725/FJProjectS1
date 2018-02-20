using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace FJ.Utils
{
    public static class Utility
    {
        public const string AssetBundlesOutputPath = "AssetBundles";

        public static string GetPlatformName()
        {
#if UNITY_EDITOR
            return GetPlatformForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
#else
            return GetPlatformForAssetBundles(Application.platform);
#endif
        }

#if UNITY_EDITOR
        private static string GetPlatformForAssetBundles(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return "Android";
#if UNITY_TVOS
                case BuildTarget.tvOS:
                    return "tvOS";
#endif
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.WebGL:
                    return "WebGL";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                case BuildTarget.StandaloneOSX:
                    return "OSX";
                // Add more build targets for your own.
                // If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
                default:
                    return null;
            }
        }
#endif

        private static string GetPlatformForAssetBundles(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
#if UNITY_TVOS
                case RuntimePlatform.tvOS:
                    return "tvOS";
#endif
                case RuntimePlatform.WebGLPlayer:
                    return "WebGL";
                case RuntimePlatform.WindowsPlayer:
                    return "Windows";
                case RuntimePlatform.OSXPlayer:
                    return "OSX";
                // Add more build targets for your own.
                // If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
                default:
                    return null;
            }
        }

        public static float Sum(float[] array)
        {
            var sum = 0f;
            for (var i = 0; i < array.Length; i++)
            {
                sum += array[i];
            }
            return sum;
        }

        public static Coroutine WhenAll(this MonoBehaviour mono, IEnumerable<IEnumerator> coroutines, Action onComplete = null)
        {
            return mono.StartCoroutine(mono.StartCoroutineAll(coroutines, onComplete));
        }

        public static Coroutine When(this MonoBehaviour mono, IEnumerator coroutine, Action onComplete)
        {
            return mono.StartCoroutine(mono.StartCoroutine(coroutine, onComplete));
        }

        public static IEnumerator StartCoroutineAll(this MonoBehaviour mono, IEnumerable<IEnumerator> coroutines, Action onComplete)
        {
            int completed = 0;
            int i = 0;
            foreach (var coroutine in coroutines)
            {
                i++;
                mono.When(coroutine, () =>
                {
                    completed += 1;
                });
            }
            while (completed < i)
            {
                yield return null;
            }
            onComplete?.Invoke();
        }

        public static IEnumerator StartCoroutine(this MonoBehaviour mono, IEnumerator coroutine, Action onComplete)
        {
            var co = mono.StartCoroutine(coroutine);
            yield return co;
            onComplete?.Invoke();
        }
    }
}