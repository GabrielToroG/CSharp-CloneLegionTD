using Godot;

namespace LegionTDClone.Platform.Godot.Presentation.Actions
{
    public sealed class ActionSlotViewData
    {
        public bool Visible { get; init; }
        public bool Disabled { get; init; } = true;
        public string Text { get; init; } = "";
        public Color? HighlightColor { get; init; }
    }
}
