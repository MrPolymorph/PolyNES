using Poly6502.Microprocessor.Flags;

namespace PolyNES.Models
{
    public class WatchItem
    {
        public StateChangeType ChangeType { get; }
        public object Value { get; private set; }
        
        public WatchItem(StateChangeType stateType, object value)
        {
            ChangeType = stateType;
            Value = value;
        }

        public void Update(object newValue)
        {
            Value = Value;
        }
    }
}