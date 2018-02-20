using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
using Utility = FJ.Utils.Utility;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FJ.Asset
{
    public class AssetLoadOperation : CustomYieldInstruction
    {
        public static float WebRequestProgressThreshold = 0.3f;

        public AssetBundleRequest AssetBundleRequest { get; set; }
        public UnityWebRequestAsyncOperation WebRequest { get; set; }

        public override bool keepWaiting => !IsDone;

        public bool IsWebRequestDone => WebRequest?.isDone ?? false;
        public bool IsAssetBundleRequestDone => AssetBundleRequest?.isDone ?? false;

        public float WebRequestProgress => WebRequest?.progress ?? 1;
        public float AssetBundleRequestProgress => AssetBundleRequest?.progress ?? 0;

        public bool IsDone => WebRequest == null ? IsAssetBundleRequestDone : IsWebRequestDone | IsAssetBundleRequestDone;

        public float Progress => WebRequestProgressThreshold * WebRequestProgress +
                                 (1 - WebRequestProgressThreshold) * AssetBundleRequestProgress;

        public string Error => WebRequest?.webRequest.error;
    }

    public class AssetManager2 : MonoBehaviour
    {
        public static string Url = "http://localhost:7888/";

        private readonly Dictionary<string, AssetLoadOperation> _assetLoadOperations = new Dictionary<string, AssetLoadOperation>();
        private readonly Dictionary<string, AssetBundle> _assetBundles = new Dictionary<string, AssetBundle>();

        public Dictionary<string, AssetBundleRequest> AssetLoadingOperations { get; } =
            new Dictionary<string, AssetBundleRequest>();

        public Dictionary<string, UnityWebRequestAsyncOperation> WebRequestAsyncOperations { get; } =
            new Dictionary<string, UnityWebRequestAsyncOperation>();

        public AssetLoadOperation LoadAssetAsync<T>(string assetBundleName, string assetName) where T : Object
        {
            AssetLoadOperation operation;
            var key = $"{assetBundleName}.{assetName}";
            if (_assetLoadOperations.TryGetValue(key, out operation))
            {
                return operation;
            }

            operation = new AssetLoadOperation();
            _assetLoadOperations[key] = operation;
            AssetBundle assetBundle;
            if (!_assetBundles.TryGetValue(assetBundleName, out assetBundle))
            {
                StartCoroutine(LoadAssetBundle(assetBundleName, operation, assetBundleLoaded =>
                {
                    StartCoroutine(LoadAsset<T>(assetBundleLoaded, assetName, operation, obj =>
                    {

                    }));
                }));
            }
            else
            {
                StartCoroutine(LoadAsset<T>(assetBundle, assetName, operation, obj =>
                {

                }));
            }

            return operation;
        }

        private IEnumerator LoadAssetBundle(string assetBundleName, AssetLoadOperation operation, Action<AssetBundle> onComplete)
        {
            UnityWebRequestAsyncOperation webRequestAsyncOperation;
            if (!WebRequestAsyncOperations.TryGetValue(assetBundleName, out webRequestAsyncOperation))
            {
                webRequestAsyncOperation = UnityWebRequest.GetAssetBundle(Path.Combine(Url, assetBundleName)).SendWebRequest();
                WebRequestAsyncOperations[assetBundleName] = webRequestAsyncOperation;
            }
            operation.WebRequest = webRequestAsyncOperation;

            while (!webRequestAsyncOperation.isDone)
            {
                yield return null;
            }

            if (WebRequestAsyncOperations.ContainsKey(assetBundleName))
            {
                WebRequestAsyncOperations.Remove(assetBundleName);
                if (webRequestAsyncOperation.webRequest.isHttpError || webRequestAsyncOperation.webRequest.isNetworkError)
                {
                    Debug.LogErrorFormat($"[AssetManager] Load asset bundle error: {webRequestAsyncOperation.webRequest.error}");
                }
                else
                {
                    var assetBundle = DownloadHandlerAssetBundle.GetContent(webRequestAsyncOperation.webRequest);
                    _assetBundles[assetBundleName] = assetBundle;
                }
            }

            if (webRequestAsyncOperation.webRequest.isHttpError || webRequestAsyncOperation.webRequest.isNetworkError)
            {
            }
            else
            {
                onComplete?.Invoke(_assetBundles[assetBundleName]);
            }

        }

        private IEnumerator LoadAsset<T>(AssetBundle assetBundle, string assetName, AssetLoadOperation operation, Action<T> onComplete) where T : Object
        {
            var key = $"{assetBundle.name}.{assetName}";
            AssetBundleRequest assetBundleRequest;
            if (!AssetLoadingOperations.TryGetValue(key, out assetBundleRequest))
            {
                assetBundleRequest = assetBundle.LoadAssetAsync(assetName);
                AssetLoadingOperations[assetName] = assetBundleRequest;
            }
            operation.AssetBundleRequest = assetBundleRequest;

            while (!assetBundleRequest.isDone)
            {
                yield return null;
            }

            if (AssetLoadingOperations.ContainsKey(assetName))
            {
                AssetLoadingOperations.Remove(assetName);

            }

            onComplete?.Invoke((T)assetBundleRequest.asset);
        }
    }
}