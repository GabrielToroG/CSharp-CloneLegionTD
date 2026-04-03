namespace LegionTDClone.Domain.Base
{
    public class BaseState
    {
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }

        public bool IsAlive => Health > 0;

        public BaseState(float maxHealth = 1000f)
        {
            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (amount > 0)
            {
                Health -= amount;
                if (Health < 0) Health = 0;
            }
        }
    }
}
