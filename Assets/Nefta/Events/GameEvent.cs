namespace Nefta.Events
{
    public abstract class GameEvent
    {
        internal abstract int _eventType { get; }
        internal abstract int _category { get; }
        internal abstract int _subCategory { get; }
        
        public string _name;
        /// <summary>
        /// Value field, must be non-negative.
        /// </summary>
        public long _value;
        public string _customString;
        
        public void Record()
        {
            Adapter.Record(_eventType, _category, _subCategory, _name, _value, _customString);
        }
    }
}