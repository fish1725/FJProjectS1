using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;

public class AssetManager : MonoBehaviour
{
    private AssetBundleManifest _manifest;
    private UnityWebRequestAsyncOperation _initOperation;
    private AssetBundleRequest _initAssetBundleOperation;
    private Dictionary<string, AssetBundleRequest> _assetLoadingOperations = new Dictionary<string, AssetBundleRequest>();
    private Dictionary<string, UnityWebRequestAsyncOperation> _loadingAssetBundleOperations = new Dictionary<string, UnityWebRequestAsyncOperation>();
    private Dictionary<string, AssetBundle> _loadedAssetBundles = new Dictionary<string, AssetBundle>();
    private Dictionary<string, UnityEngine.Object> _loadedAssets = new Dictionary<string, UnityEngine.Object>();

    public static string URL = "http://localhost:7888/";

    public IEnumerator Init(Action<float> onProgress, Action<string> onError)
    {
        if(_manifest == null) 
        {
            if(_initOperation == null)
            {
                Debug.LogFormat("Download AssetBundleManifest");
                _initOperation = UnityWebRequest.GetAssetBundle(System.IO.Path.Combine(URL, AssetBundles.Utility.GetPlatformName())).SendWebRequest();
            }
            var oper = _initOperation;
            while(!oper.isDone)
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

            if(_manifest == null)
            {
                if(_initAssetBundleOperation == null) 
                {
                    if (string.IsNullOrEmpty(_initOperation.webRequest.error))
                    {
                        Debug.LogFormat("Load AssetBundleManifest");
                        var ab = DownloadHandlerAssetBundle.GetContent(_initOperation.webRequest);
                        _initAssetBundleOperation = ab.LoadAssetAsync("AssetBundleManifest");
                        _initOperation = null;
                    }
                    else
                    {
                        onError?.Invoke(_initOperation.webRequest.error);
                        _initOperation = null;
                        yield break;
                    }
                }
                var assetOper = _initAssetBundleOperation;
                while(!assetOper.isDone)
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

                try
                {
                    _manifest = _initAssetBundleOperation.asset as AssetBundleManifest;
                    _initAssetBundleOperation = null;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    onError?.Invoke(ex.ToString());
                    _manifest = null;
                    _initOperation = null;
                    _initAssetBundleOperation = null;
                }
            }
            else
            {
                yield break;
            }
        }
        else 
        {
            yield break;
        }

    }

    private IEnumerator Test()
    {
        yield return null;
    }

    public IEnumerator LoadAssetAsync<T>(string assetBundleName, string assetName, Action<float> onProgress, Action<T> onComplete, Action<string> onError) where T : UnityEngine.Object
    {
        T obj;
#if UNITY_EDITOR && ASSETBUNDLE_SIMU
        var paths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetBundleName, assetName);
        obj = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(paths[0]);
#else
        if(_manifest == null)
        {
            yield return StartCoroutine(Init(onProgress, onError));
        }
        if(_manifest == null)
        {
            onError?.Invoke("Manifest is null.");
            yield break;
        }
        var key = string.Format("{0}.{1}", assetBundleName, assetName);
        AssetBundleRequest assetOper;
        UnityEngine.Object asset;
        if (!_assetLoadingOperations.TryGetValue(key, out assetOper))
        {
            if(!_loadedAssets.TryGetValue(key, out asset)) 
            {
                yield return StartCoroutine(LoadAssetBundleAsync(assetBundleName, onProgress, null, onError));

                AssetBundle assetBundle;
                if (_loadedAssetBundles.TryGetValue(assetBundleName, out assetBundle))
                {
                    assetOper = assetBundle.LoadAssetAsync<T>(assetName);
                    _assetLoadingOperations[key] = assetOper;
                }
                else
                {
                    yield break;
                }
            }
            else
            {
                obj = (T)asset;
            }
        }
        while(!assetOper.isDone)
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

        if(_assetLoadingOperations.ContainsKey(key)) 
        {
            _assetLoadingOperations.Remove(key);
        }
        if(_loadedAssets.TryGetValue(key, out asset)) 
        {
            obj = (T)asset;
        }
        else
        {
            try
            {
                obj = (T)assetOper.asset;
                _loadedAssets[key] = obj;
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
        yield break;
    }

    public IEnumerator LoadAssetBundleAsync(string assetBundleName, Action<float> onProgress, Action<AssetBundle> onComplete, Action<string> onError) 
    {
        AssetBundle assetBundle;
        if (!_loadedAssetBundles.TryGetValue(assetBundleName, out assetBundle))
        {
            UnityWebRequestAsyncOperation oper;
            if (!_loadingAssetBundleOperations.TryGetValue(assetBundleName, out oper))
            {
                oper = UnityWebRequest.GetAssetBundle(System.IO.Path.Combine(URL, assetBundleName), _manifest.GetAssetBundleHash(assetBundleName), 0).SendWebRequest();
                _loadingAssetBundleOperations[assetBundleName] = oper;
            }
            while(!oper.isDone) 
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
           
            if(_loadingAssetBundleOperations.ContainsKey(assetBundleName)) 
            {
                _loadingAssetBundleOperations.Remove(assetBundleName);
            }

            if(_loadedAssetBundles.TryGetValue(assetBundleName, out assetBundle)) 
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
