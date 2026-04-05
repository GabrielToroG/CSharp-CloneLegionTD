using Godot;
using LegionTDClone.Platform.Godot.Simulation;

namespace LegionTDClone.Platform.Godot.Presentation.Selection
{
    public static class SelectionViewFactory
    {
        public static SelectionViewData Build(Node3D selectedNode)
        {
            if (selectedNode is TowerAdapter tower && tower.Data != null)
            {
                return new SelectionViewData
                {
                    Title = tower.Data.TowerName,
                    Subtitle = "Tower",
                    RoleBadge = "Tower",
                    PortraitColor = new Color(0.18f, 0.16f, 0.1f, 0.95f),
                    CurrentHealth = tower.Data.Hp,
                    MaxHealth = tower.Data.Hp,
                    DamageText = $"DMG: {tower.Data.AttackDamage:0.#}",
                    RangeText = $"RNG: {tower.Data.AttackRange:0.#}",
                    SpeedText = $"SPD: {tower.Data.AttackSpeed:0.##}",
                    ArmorText = $"ARM: {tower.Data.Armor:0.#}",
                    DescriptionText = "Defensive structure ready for combat."
                };
            }

            if (selectedNode is UnitAdapter fighter && fighter.EntityState != null)
            {
                bool isEnemy = fighter.IsEnemy;
                return new SelectionViewData
                {
                    Title = isEnemy ? "Enemy Unit" : "Allied Defender",
                    Subtitle = isEnemy ? "Combat Unit • Enemy" : "Combat Unit • Allied",
                    RoleBadge = isEnemy ? "Enemy" : "Unit",
                    PortraitColor = isEnemy
                        ? new Color(0.22f, 0.1f, 0.1f, 0.95f)
                        : new Color(0.1f, 0.16f, 0.22f, 0.95f),
                    CurrentHealth = fighter.EntityState.CurrentHealth,
                    MaxHealth = fighter.EntityState.MaxHealth,
                    DamageText = $"DMG: {fighter.EntityState.AttackDamage:0.#}",
                    RangeText = $"RNG: {fighter.EntityState.AttackRange:0.#}",
                    SpeedText = $"SPD: {fighter.EntityState.AttackSpeed:0.##}",
                    ArmorText = $"ARM: {fighter.EntityState.Armor:0.#}",
                    DescriptionText = isEnemy
                        ? "Hostile target currently tracked on the battlefield."
                        : "Friendly defender currently active in combat."
                };
            }

            if (selectedNode is ConstructorAdapter constructor)
            {
                return new SelectionViewData
                {
                    Title = $"Constructor ({constructor.Team})",
                    Subtitle = "Builder • Non-combat",
                    RoleBadge = "Builder",
                    PortraitColor = new Color(0.09f, 0.18f, 0.12f, 0.95f),
                    CurrentHealth = 0f,
                    MaxHealth = 0f,
                    DamageText = "DMG: N/A",
                    RangeText = "RNG: N/A",
                    SpeedText = $"SPD: {constructor.MoveSpeed:0.#}",
                    ArmorText = "ARM: N/A",
                    DescriptionText = $"Movement: {constructor.MoveSpeed:0.0}\nUsed for construction and positioning."
                };
            }

            return new SelectionViewData();
        }
    }
}
