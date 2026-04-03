namespace LegionTDClone.Domain.Combat
{
    public class CombatService
    {
        public void ExecuteAttack(CombatEntity attacker, CombatEntity target)
        {
            if (attacker.IsDead || target.IsDead) return;

            target.TakeDamage(attacker.AttackDamage);
        }
    }
}
