using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FJ.Asset.Test
{
    public class AssetLoadTest
    {
        [UnityTest]
        public IEnumerator AssetLoadTestWithEnumeratorPasses()
        {
            var manager = new GameObject("AssetManager").AddComponent<AssetManager>();
            int[] isLoading = { 0 };
            var timer = 0f;
            var assetBundleNames = manager.GetAllAssetBundleNames();
            var assetBundleName = assetBundleNames[Random.Range(0, assetBundleNames.Length)];
            string[] assetNames = null;
            manager.GetAllAssetNames(assetBundleName, strings =>
            {
                assetNames = strings;
            }, Assert.Fail);

            while (assetNames == null)
            {
                yield return null;
            }

            while (timer < 5)
            {
                timer += Time.deltaTime;
                for (var i = 0; i < 10; i++)
                    if (Random.value < 0.2f)
                    {
                        isLoading[0] += 1;
                        manager.StartCoroutine(manager.LoadAssetAsync<GameObject>(assetBundleName, assetNames[Random.Range(0, assetNames.Length)],
                            null,
                            obj =>
                            {
                                var go = Object.Instantiate(obj);
                                go.name = obj.name;
                                isLoading[0] -= 1;
                            },
                            obj =>
                            {
                                isLoading[0] -= 1;
                                Debug.LogError(obj);
                            }));
                    }
                yield return null;
            }


            while (isLoading[0] > 0)
                yield return null;
            Assert.Zero(manager.AssetLoadingOperations.Count);
            Assert.Zero(manager.LoadingAssetBundleOperations.Count);
        }

        [UnityTest]
        public IEnumerator AssetLoadTestWithEnumeratorPasses2()
        {
            var manager = new GameObject("AssetManager2").AddComponent<AssetManager2>();
            var oper = manager.LoadAssetAsync<GameObject>("man", "M_civilian_01");
            while (!oper.IsDone)
            {
                Debug.Log(oper.Progress);
                yield return null;
            }
            Debug.Log(oper.Error);
            var go = Object.Instantiate((GameObject)oper.AssetBundleRequest.asset);
            Assert.IsNotNull(go);
        }
    }
}