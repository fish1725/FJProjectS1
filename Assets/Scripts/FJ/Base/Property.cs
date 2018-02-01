namespace FJ.Base
{
    public class Property<T>
    {
        public delegate void PropertyOnValueChanged(Property<T> property);

        private T _value;

        public PropertyOnValueChanged OnValueChanged;

        public T Value
        {
            get { return _value; }
            set
            {
                if (_value == null ? value != null : !_value.Equals(value))
                {
                    _value = value;
                    OnValueChanged?.Invoke(this);
                }
            }
        }
    }
}