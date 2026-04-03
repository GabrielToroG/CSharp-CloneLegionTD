using Godot;

namespace LegionTDClone.GameData.Definitions
{
    [GlobalClass]
    public partial class TowerDefinition : Resource
    {
        [Export] public string TowerName { get; set; } = "Basic Tower";
        [Export] public int Cost { get; set; } = 20;
        [Export] public PackedScene UpgradeScene { get; set; }
        [Export] public int UpgradeCost { get; set; } = 0;
        [Export] public float SellRefundFactor { get; set; } = 0.75f;

        [ExportCategory("Combat Stats")]
        [Export] public float Hp { get; set; } = 100f;
        [Export] public float AttackDamage { get; set; } = 15f;
        [Export] public float AttackRange { get; set; } = 1.2f;
        [Export] public float AttackSpeed { get; set; } = 1f;
        [Export] public float Armor { get; set; } = 5f;
        [Export] public float MovementSpeed { get; set; } = 2f;
        [Export] public float AggroRange { get; set; } = 8f;
    }
}
