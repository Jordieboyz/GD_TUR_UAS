using UnityEngine.Events;

namespace DataStructures
{
    public class Resource
    {
        public int Amount { get; private set; }
        public readonly UnityEvent<int> OnValueChanged = new();
        
        public Resource(){}
        
        public Resource(int amount)
        {
            Amount = amount;
        }
        
        public void Set(int amount){
            Amount = amount;
            OnValueChanged.Invoke(Amount);
        }

        public void Add(int amount)
        {
            Amount += amount;
            OnValueChanged.Invoke(Amount);
        }

        public void Remove(int amount)
        {
            Amount -= amount;
            OnValueChanged.Invoke(Amount);
        }
        
        public bool HasEnough(int amount)
        {
            return Amount >= amount;
        }
        
        public override string ToString()
        {
            return Amount.ToString();
        }
        
        public static implicit operator int(Resource resource)
        {
            return resource.Amount;
        }

        // This is for debug purposes only for now.
        public static implicit operator Resource(int amount)
        {
            return new Resource(amount);
        }
    }
}