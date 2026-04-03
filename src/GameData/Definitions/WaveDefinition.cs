using Godot;

namespace LegionTDClone.GameData.Definitions
{
    [GlobalClass]
    public partial class WaveDefinition : Resource
    {
        [Export] public int Level { get; set; } = 1;

        // Idealmente en el futuro esto se cambia por un EnemyId (string) para que no dependa de PackedScene (Godot)
        [Export] public PackedScene EnemyScene { get; set; }
        
        [Export] public int EnemyCount { get; set; } = 10;
        [Export] public float SpawnInterval { get; set; } = 1.0f;

        [ExportCategory("Enemy Stats (Overrides Prefab)")]
        [Export] public float Hp { get; set; } = 100f;
        [Export] public float AttackDamage { get; set; } = 10f;
        [Export] public float AttackRange { get; set; } = 2f;
        [Export] public float AttackSpeed { get; set; } = 1f;
        [Export] public float Armor { get; set; } = 0f;
        [Export] public float MovementSpeed { get; set; } = 3f;
    }
}
