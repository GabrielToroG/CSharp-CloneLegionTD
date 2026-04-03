using System;

namespace LegionTDClone.Domain.Combat
{
    public class CombatEntity
    {
        public string Id { get; private set; }
        public bool IsEnemy { get; private set; }
        
        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; private set; }
        public float AttackDamage { get; private set; }
        public float AttackRange { get; private set; }
        public float AttackSpeed { get; private set; }
        public float Armor { get; private set; }
        public float MovementSpeed { get; private set; }
        
        public bool IsDead => CurrentHealth <= 0;

        public event Action OnDeath;

        public CombatEntity(string id, bool isEnemy, float maxHealth, float attackDamage, float attackRange, float attackSpeed, float armor, float movementSpeed)
        {
            Id = id;
            IsEnemy = isEnemy;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            AttackDamage = attackDamage;
            AttackRange = attackRange;
            AttackSpeed = attackSpeed;
            Armor = armor;
            MovementSpeed = movementSpeed;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            float mitigatedDamage = Math.Max(1f, amount - Armor);
            CurrentHealth -= mitigatedDamage;

            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                OnDeath?.Invoke();
            }
        }
    }
}
