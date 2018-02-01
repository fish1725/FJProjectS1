using System.Collections;
using FJ.Game.Unit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FJ.Editor
{
    public class BaseTest
    {

        [Test]
        public void BaseTestSimplePasses()
        {
            var u = new Unit();
            u.ModelName.Value = "123123";

            var uv = new GameObject().AddComponent<UnitView>();
            uv.SetModel(u);
            u.ModelName.Value = "123123";
            u.ModelName.Value = "1";
        }

        // A UnityTest behaves like a coroutine in PlayMode
        // and allows you to yield null to skip a frame in EditMode
        [UnityTest]
        public IEnumerator BaseTestWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // yield to skip a frame
            yield return null;
        }
    }
}
