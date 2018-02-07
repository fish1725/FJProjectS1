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

        public Dictionary<string, AssetBundleRequest> AssetLoadingOperations
        {
            get
            {
                return _assetLoadingOperations;
            }
        }

        private readonly Dictionary<string, UnityWebRequestAsyncOperation> _loadingAssetBundleOperations = new Dictionary<string, UnityWebRequestAsyncOperation>();

        public Dictionary<string, UnityWebRequestAsyncOperation> LoadingAssetBundleOperations
        {
            get
            {
                return _loadingAssetBundleOperations;
            }
        }

        private readonly Dictionary<string, AssetBundle> _loadedAssetBundles = new Dictionary<string, AssetBundle>();
        private readonly Dictionary<string, UnityEngine.Object> _loadedAssets = new Dictionary<string, UnityEngine.Object>();

        public static string Url = "http://localhost:7888/";

#if UNITY_EDITOR
        private const string _simulateAssetBundles = "SimulateAssetBundles";
#endif

        public static bool SimulateAssetBundleInEditor
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(_simulateAssetBundles, true);
#else
                return false;
#endif
            }
            set
            {
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetBool(_simulateAssetBundles, value);
#else

#endif
            }
        }

        public IEnumerator Init(Action<float> onProgress, Action<string> onError)
        {
            if (_manifest == null)
            {
                yield return StartCoroutine(LoadAssetAsync<AssetBundleManifest>(Utils.Utility.GetPlatformName(), "AssetBundleManifest", onProgress,
                manifest => { _manifest = manifest; }, onError, true));
            }
        }

        public IEnumerator LoadAssetAsync<T>(string assetBundleName, string assetName, Action<float> onProgress, Action<T> onComplete, Action<string> onError, bool forceReload = false) where T : UnityEngine.Object
        {
            T obj;
#if UNITY_EDITOR
            if (SimulateAssetBundleInEditor)
            {
                var paths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetBundleName, assetName);
                obj = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(paths[0]);
                yield return null;
            }
            else
#endif
            {
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

                        if (!_assetLoadingOperations.TryGetValue(key, out assetOper)) 
                        {
                            if (forceReload || !_loadedAssets.TryGetValue(key, out asset))
                            {
                                AssetBundle assetBundle;
                                if (_loadedAssetBundles.TryGetValue(assetBundleName, out assetBundle))
                                {
                                    Debug.LogFormat($"Load {assetName} from asset bundle {assetBundleName}");
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
                {
                    obj = (T)asset;
                }
                else
                {
                    try
                    {
                        if (_assetLoadingOperations.ContainsKey(key))
                        {
                            _assetLoadingOperations.Remove(key);
                        }
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
            }
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
                    Debug.LogFormat($"Download asset bundle {assetBundleName}");
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

                yield return null;

                if (_loadedAssetBundles.TryGetValue(assetBundleName, out assetBundle))
                {
                }
                else
                {
                    if (_loadingAssetBundleOperations.ContainsKey(assetBundleName))
                    {
                        _loadingAssetBundleOperations.Remove(assetBundleName);
                    }
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
