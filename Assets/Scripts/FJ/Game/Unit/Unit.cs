using FJ.Base;

namespace FJ.Game.Unit
{
    public class Unit : Model
    {
        public Property<string> ModelName { get; set; } = new Property<string>();
    }
}