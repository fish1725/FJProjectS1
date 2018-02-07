using UnityEngine;
using System.Collections;
using FJ.Asset;

public class Test : MonoBehaviour
{
    public AssetManager manager;
    public float threshold = 10;

    private float _timer;
    private string[] _assetBundleNames;

    // Use this for initialization
    void Start()
    {
        _assetBundleNames = UnityEditor.AssetDatabase.GetAllAssetBundleNames();
    }

    // Update is called once per frame
    void Update()
    {
        if(_timer <= threshold) {
            if(Random.value < 0.2f) {
                var assetBundleName = _assetBundleNames[Random.Range(0, _assetBundleNames.Length)];
                //Debug.LogFormat($"Start to load {assetBundleName} and instantiate");
                var paths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
                var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(paths[Random.Range(0, paths.Length)]);
                LoadAndInstantiate(assetBundleName, obj.name);
            }
        }
    }

    public void LoadAndInstantiate(string assetBundleName, string assetName)
    {

        StartCoroutine(manager.LoadAssetAsync<GameObject>(assetBundleName, assetName,
                                              null,
                                              (obj) => {
                                                  //Debug.LogFormat("Complete {0}", obj.name); 
                                                  Instantiate(obj);
                                              },
        (obj) => Debug.LogError(obj)));
    }
}
