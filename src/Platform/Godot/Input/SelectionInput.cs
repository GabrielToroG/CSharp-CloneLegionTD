using Godot;
using LegionTDClone.Platform.Godot.Simulation;

namespace LegionTDClone.Platform.Godot.Input
{
    public partial class SelectionInput : Node
    {
        [Export] public Camera3D MainCamera;
        [Export] public Node HUDManager; // Replace with UI integration later
        [Export] public Node BuilderController;

        public override void _Ready()
        {
            HUDManager ??= GetNodeOrNull<Node>("../GameHUD");
            BuilderController ??= GetNodeOrNull<Node>("../BuilderController");
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
            {
                if (GetViewport().GuiGetHoveredControl() != null) return;
                if (HUDManager is LegionTDClone.Platform.Godot.Presentation.GameHUD hud && hud.IsPointOverUi(mouseEvent.Position)) return;

                if (mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    PerformRaycast(mouseEvent.Position);
                }
            }
            if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
            {
                 ClearSelection();
            }
        }

        private void PerformRaycast(Vector2 mousePos)
        {
            if (MainCamera == null) return;

            var spaceState = MainCamera.GetWorld3D().DirectSpaceState;
            var rayOrigin = MainCamera.ProjectRayOrigin(mousePos);
            var rayEnd = rayOrigin + MainCamera.ProjectRayNormal(mousePos) * 2000;

            var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
            var result = spaceState.IntersectRay(query);

            if (result.Count > 0)
            {
                var collider = (Node3D)result["collider"];
                Node3D toSelect = TryFindSelectable(collider);
                
                if (toSelect != null && HUDManager != null)
                {
                    HUDManager.Call("SetSelection", toSelect);
                    BuilderController?.Call("SetSelectedEntity", toSelect);
                    return;
                }
            }

            // Keep current selection when clicking non-selectable world areas.
            // Selection is explicitly cleared with Esc.
        }

        private Node3D TryFindSelectable(Node3D startNode)
        {
            Node current = startNode;
            while (current != null)
            {
                if (current is TowerAdapter || current.GetType().Name == "UnitAdapter" || current.GetType().Name == "ConstructorAdapter")
                {
                    return (Node3D)current;
                }
                current = current.GetParent();
            }
            return null;
        }

        private void ClearSelection()
        {
            if (HUDManager != null)
            {
                HUDManager.Call("ClearSelection");
            }
            BuilderController?.Call("ClearSelectedEntity");
        }
    }
}
