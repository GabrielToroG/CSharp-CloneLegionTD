using Godot;
using LegionTDClone.Platform.Godot.Presentation;

namespace LegionTDClone.Platform.Godot.Input
{
    public partial class RtsCameraController : Camera3D
    {
        [Export] public Node HUDManager;
        [Export] public float EdgeScrollMargin = 18f;
        [Export] public float EdgeScrollSpeed = 22f;
        [Export] public float MinX = -18f;
        [Export] public float MaxX = 18f;
        [Export] public float MinZ = -28f;
        [Export] public float MaxZ = 26f;
        [Export] public float FixedHeight = 24f;
        [Export] public float FixedPitchDegrees = -52f;
        [Export] public float DefaultYawDegrees = 0f;
        [Export] public float FieldOfView = 58f;

        public override void _Ready()
        {
            HUDManager ??= GetNodeOrNull<Node>("../GameHUD");

            Current = true;
            Fov = FieldOfView;
            RotationDegrees = new Vector3(FixedPitchDegrees, DefaultYawDegrees, 0f);

            Vector3 position = Position;
            position.Y = FixedHeight;
            Position = ClampPosition(position);
        }

        public override void _Process(double delta)
        {
            Vector2 mousePosition = GetViewport().GetMousePosition();
            Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
            bool isNearEdge =
                mousePosition.X <= EdgeScrollMargin ||
                mousePosition.X >= viewportSize.X - EdgeScrollMargin ||
                mousePosition.Y <= EdgeScrollMargin ||
                mousePosition.Y >= viewportSize.Y - EdgeScrollMargin;

            if (!isNearEdge)
            {
                if (GetViewport().GuiGetHoveredControl() != null) return;
                if (HUDManager is GameHUD hud && hud.IsPointOverUi(mousePosition)) return;
            }

            Vector3 move = Vector3.Zero;

            if (mousePosition.X <= EdgeScrollMargin) move.X -= 1f;
            else if (mousePosition.X >= viewportSize.X - EdgeScrollMargin) move.X += 1f;

            if (mousePosition.Y <= EdgeScrollMargin) move.Z -= 1f;
            else if (mousePosition.Y >= viewportSize.Y - EdgeScrollMargin) move.Z += 1f;

            if (move == Vector3.Zero) return;

            move = move.Normalized() * EdgeScrollSpeed * (float)delta;
            Position = ClampPosition(Position + move);
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            position.X = Mathf.Clamp(position.X, MinX, MaxX);
            position.Y = FixedHeight;
            position.Z = Mathf.Clamp(position.Z, MinZ, MaxZ);
            return position;
        }
    }
}
