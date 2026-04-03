using Godot;
using System;

namespace LegionTDClone.Platform.Godot.Presentation
{
    public partial class LobbyUI : Control
    {
        // Reference to network adapter
        [Export] public Node NetworkManagerNode;
        
        private Button _testMapButton;

        public override void _Ready()
        {
            _testMapButton = GetNode<Button>("CenterContainer/VBoxContainer/TestMapButton");
            _testMapButton.Pressed += OnTestMapPressed;
        }

        private void OnTestMapPressed()
        {
            GetTree().ChangeSceneToFile("res://Scenes/TestGameMap.tscn");
        }
    }
}
