using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FJ.Utils;
using System.Collections.Generic;

namespace FJ.Asset.Test
{
    public class AssetLoadTest
    {
        [UnityTest]
        public IEnumerator AssetLoadTestWithEnumeratorPasses()
        {
            var manager = new GameObject("AssetManager").AddComponent<AssetManager>();
            yield return manager.StartCoroutine(manager.LoadManifestAsync(null, null, Assert.Fail));
            Assert.AreEqual(0, manager.LoadedAssetBundles.Count);

            yield return manager.StartCoroutine(manager.LoadAllAssetBundleAsync(null, null, Assert.Fail));
            var assetBundleNames = manager.GetAllAssetBundleNames();
#if UNITY_EDITOR
            if (AssetManager.SimulateAssetBundleInEditor)
            {
                Assert.AreEqual(0, manager.LoadedAssetBundles.Count);
            }
            else
#endif
            {
                Assert.AreEqual(assetBundleNames.Length, manager.LoadedAssetBundles.Count);    
            }



            var assetBundleName = assetBundleNames[Random.Range(0, assetBundleNames.Length)];
            List<IEnumerator> _loads = new List<IEnumerator>();
            string[] assetNames = null;
            yield return manager.GetAllAssetNames(assetBundleName, strings =>
            {
                assetNames = strings;
            }, Assert.Fail);

            if (assetNames == null)
            {
                Assert.Fail("Assets is missing.");
            }

            for (var j = 0; j < 2; j++)
                for (var i = 0; i < assetNames.Length; i++)
                {
                    var assetName = assetNames[i];
                    _loads.Add(manager.LoadAssetAsync<GameObject>(assetBundleName, assetName,
                        null,
                        obj =>
                        {
                            var go = Object.Instantiate(obj);
                            go.name = obj.name;
                        },
                        obj =>
                        {
                            Debug.LogError(obj);
                        }));
                }

            yield return manager.WhenAll(_loads);


            Assert.Zero(manager.LoadingAssetOperations.Count);
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