using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using FJ.Asset;

public class AssetLoadTest {

	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator AssetLoadTestWithEnumeratorPasses() {
        var isLoading = 0;
        var timer = 0f;
        var assetBundleNames = UnityEditor.AssetDatabase.GetAllAssetBundleNames();
        var assetBundleName = assetBundleNames[Random.Range(0, assetBundleNames.Length)];
        //Debug.LogFormat($"Start to load {assetBundleName} and instantiate");
        var paths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);

        var manager = new GameObject("AssetManager").AddComponent<AssetManager>();
        var un = new WaitUntil(()=>timer >= 5f);

        while(un.keepWaiting)
        {
            timer += Time.deltaTime;
            for (var i = 0; i < 10; i++)
            {
                if(Random.value < 0.2f) {
                    var assetName = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(paths[Random.Range(0, paths.Length)]).name;
                    isLoading += 1;
                    manager.StartCoroutine(manager.LoadAssetAsync<GameObject>(assetBundleName, assetName,
                                                  null,
                                                  (obj) =>
                                                  {
                                                      var go = Object.Instantiate(obj);
                                                      go.name = obj.name;
                                                      isLoading -= 1;
                                                  },
                                                                              (obj) => {
                                                                                  isLoading -= 1;
                                                                                  Debug.LogError(obj);
                                                                              }));
                }

            }
        }


        while (isLoading > 0)
        {
            yield return null;
        }
        Assert.Zero(manager.AssetLoadingOperations.Count);
        Assert.Zero(manager.LoadingAssetBundleOperations.Count);
	}
}
