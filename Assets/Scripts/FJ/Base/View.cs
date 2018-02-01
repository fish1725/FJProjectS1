using UnityEngine;

namespace FJ.Base
{
    public abstract class View<T> : MonoBehaviour where T : Model
    {
        protected T Model { get; private set; }

        protected virtual void InitListeners()
        {

        }

        public void SetModel(T model)
        {
            Model = model;
            InitListeners();
            Model.OnViewInited();
        }
    }
}
