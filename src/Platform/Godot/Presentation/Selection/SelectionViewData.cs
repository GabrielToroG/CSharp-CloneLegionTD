using Godot;

namespace LegionTDClone.Platform.Godot.Presentation.Selection
{
    public sealed class SelectionViewData
    {
        public string Title { get; init; } = "Selected Entity";
        public string Subtitle { get; init; } = "Awaiting selection";
        public string RoleBadge { get; init; } = "None";
        public Color PortraitColor { get; init; } = new(0.1f, 0.18f, 0.16f, 0.95f);
        public float CurrentHealth { get; init; }
        public float MaxHealth { get; init; }
        public string DamageText { get; init; } = "DMG: --";
        public string RangeText { get; init; } = "RNG: --";
        public string SpeedText { get; init; } = "SPD: --";
        public string ArmorText { get; init; } = "ARM: --";
        public string DescriptionText { get; init; } = "Stats...";
    }
}
