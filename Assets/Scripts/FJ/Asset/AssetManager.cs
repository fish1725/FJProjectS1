using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace FJ.Asset
{
    public class AssetManager : MonoBehaviour
    {
        private AssetBundleManifest _manifest;
        private readonly Dictionary<string, AssetBundleRequest> _assetLoadingOperations = new Dictionary<string, AssetBundleRequest>();
        private readonly Dictionary<string, UnityWebRequestAsyncOperation> _loadingAssetBundleOperations = new Dictionary<string, UnityWebRequestAsyncOperation>();
        private readonly Dictionary<string, AssetBundle> _loadedAssetBundles = new Dictionary<string, AssetBundle>();
        private readonly Dictionary<string, UnityEngine.Object> _loadedAssets = new Dictionary<string, UnityEngine.Object>();

        public static string Url = "http://localhost:7888/";

        public IEnumerator Init(Action<float> onProgress, Action<string> onError)
        {
            if (_manifest == null)
            {
                yield return StartCoroutine(LoadAssetAsync<AssetBundleManifest>(Utility.GetPlatformName(), "AssetBundleManifest", onProgress,
                manifest => { _manifest = manifest; }, onError, true));
            }
        }

        public IEnumerator LoadAssetAsync<T>(string assetBundleName, string assetName, Action<float> onProgress, Action<T> onComplete, Action<string> onError, bool forceReload = false) where T : UnityEngine.Object
        {
            T obj;
#if UNITY_EDITOR
            var paths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetBundleName, assetName);
            obj = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(paths[0]);
            yield return null;
#else
            if (!forceReload)
            {
                if (_manifest == null)
                {
                    yield return StartCoroutine(Init(onProgress, onError));
                }
                if (_manifest == null)
                {
                    onError?.Invoke("Manifest is null.");
                    yield break;
                }
            }

            var key = $"{assetBundleName}.{assetName}";
            AssetBundleRequest assetOper;
            UnityEngine.Object asset;
            if (!_assetLoadingOperations.TryGetValue(key, out assetOper))
            {
                if (forceReload || !_loadedAssets.TryGetValue(key, out asset))
                {
                    yield return StartCoroutine(LoadAssetBundleAsync(assetBundleName, onProgress, null, onError, forceReload));

                    AssetBundle assetBundle;
                    if (_loadedAssetBundles.TryGetValue(assetBundleName, out assetBundle))
                    {
                        assetOper = assetBundle.LoadAssetAsync<T>(assetName);
                        _assetLoadingOperations[key] = assetOper;
                    }
                    else
                    {
                        onError?.Invoke($"AssetBundle {assetBundleName} load error");
                        yield break;
                    }
                }
            }
            while (assetOper != null && !assetOper.isDone)
            {
                try
                {
                    onProgress?.Invoke(assetOper.progress);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                yield return null;
            }

            if (_assetLoadingOperations.ContainsKey(key))
            {
                _assetLoadingOperations.Remove(key);
            }
            if (_loadedAssets.TryGetValue(key, out asset))
            {
                obj = (T)asset;
            }
            else
            {
                try
                {
                    if (assetOper != null)
                    {
                        obj = (T)assetOper.asset;
                        _loadedAssets[key] = obj;
                    }
                    else
                    {
                        onError?.Invoke("Asset load operation is null.");
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    try
                    {
                        onError?.Invoke(ex.ToString());
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    yield break;
                }
            }
#endif
            onComplete?.Invoke(obj);
        }

        public IEnumerator LoadAssetBundleAsync(string assetBundleName, Action<float> onProgress, Action<AssetBundle> onComplete, Action<string> onError, bool forceReload = false)
        {
            AssetBundle assetBundle;
            if (forceReload || !_loadedAssetBundles.TryGetValue(assetBundleName, out assetBundle))
            {
                UnityWebRequestAsyncOperation oper;
                if (!_loadingAssetBundleOperations.TryGetValue(assetBundleName, out oper))
                {
                    if (forceReload)
                    {
                        oper = UnityWebRequest.GetAssetBundle(System.IO.Path.Combine(Url, assetBundleName))
                            .SendWebRequest();
                    }
                    else
                    {
                        oper = UnityWebRequest.GetAssetBundle(System.IO.Path.Combine(Url, assetBundleName), _manifest.GetAssetBundleHash(assetBundleName), 0).SendWebRequest();
                    }

                    _loadingAssetBundleOperations[assetBundleName] = oper;
                }
                while (!oper.isDone)
                {
                    try
                    {
                        onProgress?.Invoke(oper.progress);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                    yield return null;
                }

                if (_loadingAssetBundleOperations.ContainsKey(assetBundleName))
                {
                    _loadingAssetBundleOperations.Remove(assetBundleName);
                }

                if (_loadedAssetBundles.TryGetValue(assetBundleName, out assetBundle))
                {
                }
                else
                {
                    if (string.IsNullOrEmpty(oper.webRequest.error))
                    {
                        try
                        {
                            assetBundle = DownloadHandlerAssetBundle.GetContent(oper.webRequest);
                            _loadedAssetBundles[assetBundleName] = assetBundle;
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            try
                            {
                                onError?.Invoke(e.ToString());
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                            }
                            yield break;
                        }
                    }
                    else
                    {
                        try
                        {
                            onError?.Invoke(oper.webRequest.error);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                        yield break;
                    }
                }
            }
            onComplete?.Invoke(assetBundle);
        }
    }
}
