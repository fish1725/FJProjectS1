using UnityEngine;

namespace FJ.Base
{
    public abstract class View<T> : MonoBehaviour where T : Model
    {
        private T _model;
        public T Model
        {
            get
            {
                return _model;
            }
            set
            {
                if (_model != null)
                {
                    RemoveListeners();
                }
                _model = value;
                if (_model != null) 
                {
                    AddListeners();
                    _model.InvokeAllPropertyValueChanged();    
                }
            }
        }

        protected virtual void AddListeners()
        {

        }

        protected virtual void RemoveListeners()
        {

        }
    }
}
