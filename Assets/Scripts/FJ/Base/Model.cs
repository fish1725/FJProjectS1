using System;
using UnityEngine;

namespace FJ.Base
{
    public abstract class Model
    {
        public virtual void OnViewInited()
        {
            var props = GetType().GetProperties();
            foreach (var propertyInfo in props)
            {
                dynamic propValue = propertyInfo.GetValue(this);
                try
                {
                    propValue.OnValueChanged?.Invoke(propValue);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}