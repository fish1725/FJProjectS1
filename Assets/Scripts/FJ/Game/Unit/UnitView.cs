using FJ.Base;
using UnityEngine;

namespace FJ.Game.Unit
{
    public class UnitView : View<Unit>
    {
        public Transform ModelContent;

        protected override void InitListeners()
        {
            base.InitListeners();
            Model.ModelName.OnValueChanged += OnUnitModelChanged;
        }

        private void OnUnitModelChanged(Property<string> property)
        {
            Debug.LogFormat("Detect property change {0}", property.Value);
        }
    }
}
