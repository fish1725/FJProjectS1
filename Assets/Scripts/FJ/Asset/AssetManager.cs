using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
using FJ.Utils;
using Utility = FJ.Utils.Utility;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FJ.Asset
{
    public class AssetManager : MonoBehaviour
    {

        public static string Url = "http://localhost:7888/";

        private readonly Dictionary<string, AssetBundle> _loadedAssetBundles = new Dictionary<string, AssetBundle>();

        public Dictionary<string, AssetBundle> LoadedAssetBundles
        {
            get
            {
                return _loadedAssetBundles;
            }
        }

        private readonly Dictionary<string, Object> _loadedAssets = new Dictionary<string, Object>();

        public Dictionary<string, Object> LoadedAssets
        {
            get
            {
                return _loadedAssets;
            }
        }

        private AssetBundleManifest _manifest;

        public Dictionary<string, AssetBundleRequest> LoadingAssetOperations { get; } =
            new Dictionary<string, AssetBundleRequest>();

        public Dictionary<string, UnityWebRequestAsyncOperation> LoadingAssetBundleOperations { get; } =
            new Dictionary<string, UnityWebRequestAsyncOperation>();

#if UNITY_EDITOR
        private const string SimulateAssetBundles = "SimulateAssetBundles";
        public static bool SimulateAssetBundleInEditor
        {
            get
            {
                return EditorPrefs.GetBool(SimulateAssetBundles, true);
            }
            set
            {
                EditorPrefs.SetBool(SimulateAssetBundles, value);
            }
        }
#endif

        public IEnumerator LoadManifestAsync(Action<float> onProgress, Action<AssetBundleManifest> onComplete, Action<string> onError)
        {
#if UNITY_EDITOR
            if (SimulateAssetBundleInEditor)
            {
                onComplete?.Invoke(null);
                yield break;
            }
            else
#endif
            {
                if (_manifest == null)
                    yield return StartCoroutine(LoadAssetAsync<AssetBundleManifest>(Utility.GetPlatformName(),
                        "AssetBundleManifest", onProgress,
                        manifest =>
                        {
                            _manifest = manifest;
                            onComplete?.Invoke(_manifest);
                        }, onError, true, false));
            }
        }

        public string[] GetAllAssetBundleNames()
        {
#if UNITY_EDITOR
            return AssetDatabase.GetAllAssetBundleNames();
#else
            if (_manifest == null)
                throw new Exception("Manifest is missing.");
            return _manifest.GetAllAssetBundles();
#endif
        }

        public IEnumerator GetAllAssetNames(string assetBundleName, Action<string[]> onComplete, Action<string> onError)
        {
#if UNITY_EDITOR
            try
            {
                var paths = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
                var names = new string[paths.Length];
                for (var i = 0; i < paths.Length; i++)
                {
                    var path = paths[i];
                    names[i] = AssetDatabase.LoadAssetAtPath<Object>(path).name;
                }
                onComplete?.Invoke(names);
            }
            catch (Exception e)
            {
                onError?.Invoke(e.ToString());
            }
            yield break;
#else
            yield return StartCoroutine(LoadAssetBundleAsync(assetBundleName, null, bundle =>
            {
                onComplete?.Invoke(bundle.GetAllAssetNames());
            }, onError));
#endif
        }

        public IEnumerator LoadAssetAsync<T>(string assetBundleName, string assetName, Action<float> onProgress,
            Action<T> onComplete, Action<string> onError, bool forceReload = false, bool cache = true) where T : Object
        {
            T obj;
#if UNITY_EDITOR
            if (SimulateAssetBundleInEditor)
            {
                var paths = AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetBundleName, assetName);
                obj = AssetDatabase.LoadAssetAtPath<T>(paths[0]);
                yield return null;
            }
            else
#endif
            {
                if (!forceReload)
                {
                    if (_manifest == null)
                        yield return StartCoroutine(LoadManifestAsync(onProgress, null, onError));
                    if (_manifest == null)
                    {
                        onError?.Invoke("Manifest is null.");
                        yield break;
                    }
                }

                var key = $"{assetBundleName}.{assetName}";
                AssetBundleRequest assetOper;
                AssetBundle assetBundle = null;
                Object asset;
                if (!LoadingAssetOperations.TryGetValue(key, out assetOper))
                    if (forceReload || !_loadedAssets.TryGetValue(key, out asset))
                    {
                        yield return StartCoroutine(LoadAssetBundleAsync(assetBundleName, onProgress, (AssetBundle ab) =>
                        {
                            assetBundle = ab;
                        }, onError,
                                                                             forceReload, cache));

                        if (!LoadingAssetOperations.TryGetValue(key, out assetOper))
                            if (forceReload || !_loadedAssets.TryGetValue(key, out asset))
                            {
                                if (assetBundle != null)
                                {
                                    Debug.LogFormat($"Load {assetName} from asset bundle {assetBundleName}");
                                    assetOper = assetBundle.LoadAssetAsync<T>(assetName);
                                    LoadingAssetOperations[key] = assetOper;
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

                yield return null;

                if (_loadedAssets.TryGetValue(key, out asset))
                    obj = (T)asset;
                else
                    try
                    {
                        if (LoadingAssetOperations.ContainsKey(key))
                            LoadingAssetOperations.Remove(key);
                        if (assetOper != null)
                        {
                            obj = (T)assetOper.asset;
                            if (cache)
                            {
                                _loadedAssets[key] = obj;
                            }
                            else
                            {
                                if (assetBundle != null)
                                {
                                    assetBundle.Unload(false);
                                    Debug.LogFormat($"Unload asset bundle {assetBundleName}");
                                }
                            }
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
            onComplete?.Invoke(obj);
        }

        public IEnumerator LoadAssetBundleAsync(string assetBundleName, Action<float> onProgress,
            Action<AssetBundle> onComplete, Action<string> onError, bool forceReload = false, bool cache = true)
        {
            AssetBundle assetBundle;
            if (forceReload || !_loadedAssetBundles.TryGetValue(assetBundleName, out assetBundle))
            {
                UnityWebRequestAsyncOperation oper;
                if (!LoadingAssetBundleOperations.TryGetValue(assetBundleName, out oper))
                {
                    Debug.LogFormat($"Download asset bundle {assetBundleName}");
                    if (forceReload)
                        oper = UnityWebRequest.GetAssetBundle(Path.Combine(Url, assetBundleName))
                            .SendWebRequest();
                    else
                        oper = UnityWebRequest
                            .GetAssetBundle(Path.Combine(Url, assetBundleName),
                                _manifest.GetAssetBundleHash(assetBundleName), 0).SendWebRequest();

                    LoadingAssetBundleOperations[assetBundleName] = oper;
                }
                yield return null;
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

                if (_loadedAssetBundles.TryGetValue(assetBundleName, out assetBundle))
                {
                }
                else
                {
                    if (LoadingAssetBundleOperations.ContainsKey(assetBundleName))
                        LoadingAssetBundleOperations.Remove(assetBundleName);
                    if (oper.webRequest.isHttpError || oper.webRequest.isNetworkError)
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
                    else
                    {
                        try
                        {
                            assetBundle = DownloadHandlerAssetBundle.GetContent(oper.webRequest);
                            if (cache)
                            {
                                _loadedAssetBundles[assetBundleName] = assetBundle;
                                Debug.LogFormat($"Cache asset bundle {assetBundleName}");
                            }
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
                }
            }
            onComplete?.Invoke(assetBundle);
        }

        public IEnumerator LoadAllAssetBundleAsync(Action<float> onProgress,
            Action<AssetBundle[]> onComplete, Action<string> onError)
        {
#if UNITY_EDITOR
            if (SimulateAssetBundleInEditor)
            {
                onComplete?.Invoke(null);
                yield break;
            }
            else
#endif
            {
                if (_manifest == null)
                    yield return StartCoroutine(LoadManifestAsync(onProgress, null, onError));
                if (_manifest == null)
                {
                    onError?.Invoke("Manifest is null.");
                    yield break;
                }
                var assetBundleNames = GetAllAssetBundleNames();
                float[] progress = new float[assetBundleNames.Length];
                AssetBundle[] assetBundles = new AssetBundle[assetBundleNames.Length];
                int error = 0;
                IEnumerator[] loads = new IEnumerator[assetBundleNames.Length];

                for (var i = 0; i < assetBundleNames.Length; i++)
                {
                    var _i = i;
                    var assetBundleName = assetBundleNames[i];
                    loads[_i] = LoadAssetBundleAsync(assetBundleName, (float obj) =>
                    {
                        progress[_i] = obj;
                        onProgress?.Invoke(Utility.Sum(progress) / assetBundleNames.Length);
                    }, (AssetBundle obj) =>
                    {
                        assetBundles[_i] = obj;
                    }, (string obj) =>
                    {
                        onError?.Invoke(obj);
                        error += 1;
                    });
                }
                yield return this.WhenAll(loads);

                if (error > 0)
                {
                    yield break;
                }
                onComplete?.Invoke(assetBundles);
            }
        }
    }
}