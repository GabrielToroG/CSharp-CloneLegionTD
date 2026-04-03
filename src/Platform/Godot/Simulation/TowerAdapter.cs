using Godot;

namespace LegionTDClone.Platform.Godot.Simulation
{
    public partial class TowerAdapter : Node3D
    {
        [Export] public GameData.Definitions.TowerDefinition Data { get; set; }
        [Export] public PackedScene FighterPrefab { get; set; }

        public override void _Ready()
        {
            // Towers should always keep their visual shadows enabled.
            foreach (Node child in GetChildren())
            {
                if (child is MeshInstance3D mesh)
                {
                    mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
                }
            }
        }

        public void SetInteractive(bool active)
        {
            Visible = active;
            var col = GetNodeOrNull<StaticBody3D>("StaticBody");
            if (col != null)
            {
                col.CollisionLayer = active ? 1u : 0u;
                col.CollisionMask = active ? 1u : 0u;
            }
        }
    }
}
