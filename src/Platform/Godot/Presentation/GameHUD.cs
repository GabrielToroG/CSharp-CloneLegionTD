using Godot;
using LegionTDClone.Domain.Economy;
using LegionTDClone.Domain.Match;
using LegionTDClone.Domain.Roster;
using LegionTDClone.Platform.Godot.Input;
using LegionTDClone.Platform.Godot.Simulation;

namespace LegionTDClone.Platform.Godot.Presentation
{
    public partial class GameHUD : CanvasLayer
    {
        [Export] public Node BuilderController;

        private Label _goldLabel;
        private Label _buildTimerLabel;
        private Control _inspectorPanel;
        private Label _titleLabel;
        private Label _statsLabel;
        private Control _actionPanel;
        private Button _actionButton1;
        private Button _actionButton2;
        private Button _actionButton3;
        private Button _actionButton4;
        private Node3D _selectedNode;
        private bool _isConstructorBuildMenuOpen;
        private int _selectedConstructorTowerIndex = -1;

        private EconomyState _economyState;
        private MatchState _matchState;
        private RosterState _rosterState;

        public override void _Ready()
        {
            _economyState = CompositionRoot.App.Container.Resolve<EconomyState>();
            _matchState = CompositionRoot.App.Container.Resolve<MatchState>();
            _rosterState = CompositionRoot.App.Container.Resolve<RosterState>();
            BuilderController ??= GetNodeOrNull<Node>("../BuilderController");

            _goldLabel = GetNode<Label>("GoldLabel");
            _buildTimerLabel = GetNode<Label>("BuildTimerLabel");
            _inspectorPanel = GetNode<Control>("InspectorPanel");
            _titleLabel = GetNode<Label>("InspectorPanel/Margin/VBox/TitleLabel");
            _statsLabel = GetNode<Label>("InspectorPanel/Margin/VBox/StatsLabel");
            _actionPanel = GetNode<Control>("ActionPanel");
            _actionButton1 = GetNode<Button>("ActionPanel/BtnAction1");
            _actionButton2 = GetNode<Button>("ActionPanel/BtnAction2");
            _actionButton3 = GetNode<Button>("ActionPanel/BtnAction3");
            _actionButton4 = GetNode<Button>("ActionPanel/BtnAction4");

            // UI in bottom-right must stay above world interactions.
            _inspectorPanel.MouseFilter = Control.MouseFilterEnum.Stop;
            _actionPanel.MouseFilter = Control.MouseFilterEnum.Stop;
            _actionButton1.MouseFilter = Control.MouseFilterEnum.Stop;
            _actionButton2.MouseFilter = Control.MouseFilterEnum.Stop;
            _actionButton3.MouseFilter = Control.MouseFilterEnum.Stop;
            _actionButton4.MouseFilter = Control.MouseFilterEnum.Stop;

            _actionButton1.ButtonDown += HandleAction1Pressed;
            _actionButton2.ButtonDown += HandleAction2Pressed;
            _actionButton3.ButtonDown += HandleAction3Pressed;
            _actionButton4.ButtonDown += HandleAction4Pressed;

            HideAllActions();
            _actionPanel.Visible = false;
        }

        public override void _Process(double delta)
        {
            _goldLabel.Text = $"Oro Izq: {_economyState.GetGold(TeamSide.Left)} | Oro Der: {_economyState.GetGold(TeamSide.Right)}";
            if (_matchState.CurrentPhase == MatchPhase.BuildPhase)
            {
                _buildTimerLabel.Visible = true;
                double remaining = MatchAdapter.Instance?.BuildPhaseTimeRemaining ?? 0.0;
                _buildTimerLabel.Text = $"Construccion: {Mathf.CeilToInt((float)remaining)}s";
            }
            else
            {
                _buildTimerLabel.Visible = false;
            }

            if (_selectedNode != null && !GodotObject.IsInstanceValid(_selectedNode))
            {
                ClearSelection();
                return;
            }

            if (_selectedNode != null)
            {
                UpdateInspectorDisplay();
                UpdateActionPanel();
            }
        }

        public void SetSelection(Node3D entity)
        {
            bool selectionChanged = _selectedNode != entity;
            _selectedNode = entity;

            if (BuilderController is ConstructionInput builder)
            {
                builder.SetSelectedEntity(entity);
            }
            else
            {
                BuilderController?.Call("SetSelectedEntity", entity);
            }

            if (selectionChanged)
            {
                _isConstructorBuildMenuOpen = false;
                _selectedConstructorTowerIndex = -1;
            }
            _inspectorPanel.Visible = true;
            _actionPanel.Visible = true;
            UpdateInspectorDisplay();
            UpdateActionPanel();
        }

        public void ClearSelection()
        {
            _selectedNode = null;

            if (BuilderController is ConstructionInput builder)
            {
                builder.ClearSelectedEntity();
            }
            else
            {
                BuilderController?.Call("ClearSelectedEntity");
            }

            _isConstructorBuildMenuOpen = false;
            _selectedConstructorTowerIndex = -1;
            _inspectorPanel.Visible = false;
            _actionPanel.Visible = false;
            HideAllActions();
        }

        private void UpdateInspectorDisplay()
        {
            if (_selectedNode is TowerAdapter tower && tower.Data != null)
            {
                _titleLabel.Text = tower.Data.TowerName;
                _statsLabel.Text = $"HP: {tower.Data.Hp}/{tower.Data.Hp} | DMG: {tower.Data.AttackDamage}\nRNG: {tower.Data.AttackRange} | ATK SPD: {tower.Data.AttackSpeed}\nARMOR: {tower.Data.Armor}";
            }
            else if (_selectedNode is UnitAdapter fighter && fighter.EntityState != null)
            {
                _titleLabel.Text = fighter.IsEnemy ? "Enemy Unit" : "Allied Defender";
                _statsLabel.Text = $"HP: {fighter.EntityState.CurrentHealth:0.0}/{fighter.EntityState.MaxHealth} | DMG: {fighter.EntityState.AttackDamage}\nRNG: {fighter.EntityState.AttackRange} | ATK SPD: {fighter.EntityState.AttackSpeed}\nARMOR: {fighter.EntityState.Armor}";
            }
            else if (_selectedNode is ConstructorAdapter constructor)
            {
                _titleLabel.Text = $"Constructor ({constructor.Team})";
                _statsLabel.Text = $"Velocidad: {constructor.MoveSpeed:0.0}\nRol: Construccion (no combate)";
            }
            else
            {
                _titleLabel.Text = "Selected Entity";
                _statsLabel.Text = "Stats...";
            }
        }

        private void UpdateActionPanel()
        {
            HideAllActions();
            ClearBuildButtonHighlights();

            if (_selectedNode is TowerAdapter tower && tower.Data != null)
            {
                TeamSide team = ResolveTeamFromNode(tower);
                int refund = Mathf.RoundToInt(tower.Data.Cost * tower.Data.SellRefundFactor);
                _actionButton1.Visible = true;
                _actionButton1.Disabled = false;
                _actionButton1.Text = $"Vender (+{refund}g)";

                bool hasUpgrade = tower.Data.UpgradeScene != null && tower.Data.UpgradeCost > 0;
                _actionButton2.Visible = true;
                _actionButton2.Text = hasUpgrade ? $"Mejorar ({tower.Data.UpgradeCost}g)" : "Mejorar (sin evolucion)";
                _actionButton2.Disabled = !hasUpgrade || _economyState.GetGold(team) < tower.Data.UpgradeCost;
                return;
            }

            if (_selectedNode is ConstructorAdapter)
            {
                if (_isConstructorBuildMenuOpen)
                {
                    TeamSide team = ResolveTeamFromNode(_selectedNode);
                    int teamGold = _economyState.GetGold(team);
                    int blueCost = _rosterState.GetTowerCost(0);
                    int greenCost = _rosterState.GetTowerCost(1);

                    _actionButton1.Visible = true;
                    _actionButton1.Disabled = teamGold < blueCost;
                    _actionButton1.Text = $"Torre Azul ({blueCost}g)";
                    if (_selectedConstructorTowerIndex == 0) ApplySelectedStyle(_actionButton1, new Color(0.25f, 0.75f, 1f, 1f));

                    _actionButton2.Visible = true;
                    _actionButton2.Disabled = teamGold < greenCost;
                    _actionButton2.Text = $"Torre Verde ({greenCost}g)";
                    if (_selectedConstructorTowerIndex == 1) ApplySelectedStyle(_actionButton2, new Color(0.25f, 1f, 0.4f, 1f));

                    _actionButton3.Visible = true;
                    _actionButton3.Disabled = false;
                    _actionButton3.Text = "Volver";
                    return;
                }

                _actionButton1.Visible = true;
                _actionButton1.Disabled = false;
                _actionButton1.Text = "Construir";
                return;
            }

            if (_isConstructorBuildMenuOpen)
            {
                _isConstructorBuildMenuOpen = false;
            }

        }

        private void HideAllActions()
        {
            _actionButton1.Visible = false;
            _actionButton2.Visible = false;
            _actionButton3.Visible = false;
            _actionButton4.Visible = false;
        }

        private void HandleAction1Pressed()
        {
            if (_selectedNode is TowerAdapter tower)
            {
                if (BuilderController is ConstructionInput builder)
                {
                    builder.TrySellTower(tower);
                }
                else
                {
                    BuilderController?.Call("TrySellTower", tower);
                }
                ClearSelection();
                return;
            }

            if (_selectedNode is ConstructorAdapter)
            {
                if (_isConstructorBuildMenuOpen)
                {
                    if (BuilderController is ConstructionInput builder)
                    {
                        builder.BeginBuildPlacementFromUi(_selectedNode, 0);
                    }
                    else
                    {
                        BuilderController?.Call("SetSelectedEntity", _selectedNode);
                        BuilderController?.Call("BeginBuildPlacementFromSelection", 0);
                    }
                    _selectedConstructorTowerIndex = 0;
                    UpdateActionPanel();
                }
                else
                {
                    _isConstructorBuildMenuOpen = true;
                    _selectedConstructorTowerIndex = -1;
                    if (BuilderController is ConstructionInput builder)
                    {
                        builder.EnterBuildMenuForSelectedConstructor();
                    }
                    UpdateActionPanel();
                }
            }
        }

        private void HandleAction2Pressed()
        {
            if (_selectedNode is TowerAdapter tower)
            {
                if (BuilderController is ConstructionInput builder)
                {
                    builder.TryUpgradeTower(tower);
                }
                else
                {
                    BuilderController?.Call("TryUpgradeTower", tower);
                }
                UpdateActionPanel();
                UpdateInspectorDisplay();
                return;
            }

            if (_selectedNode is ConstructorAdapter)
            {
                if (_isConstructorBuildMenuOpen)
                {
                    if (BuilderController is ConstructionInput builder)
                    {
                        builder.BeginBuildPlacementFromUi(_selectedNode, 1);
                    }
                    else
                    {
                        BuilderController?.Call("SetSelectedEntity", _selectedNode);
                        BuilderController?.Call("BeginBuildPlacementFromSelection", 1);
                    }
                    _selectedConstructorTowerIndex = 1;
                    UpdateActionPanel();
                }
            }
        }

        private void HandleAction3Pressed()
        {
            if (_selectedNode is ConstructorAdapter && _isConstructorBuildMenuOpen)
            {
                _isConstructorBuildMenuOpen = false;
                _selectedConstructorTowerIndex = -1;
                if (BuilderController is ConstructionInput builder)
                {
                    builder.ExitBuildPlacementMode();
                }
                else
                {
                    BuilderController?.Call("ExitBuildPlacementMode");
                }
                UpdateActionPanel();
            }
        }

        private void HandleAction4Pressed() { }

        private static TeamSide ResolveTeamFromNode(Node3D node)
        {
            Node current = node;
            while (current != null)
            {
                string name = current.Name.ToString();
                if (name == "Lane_Right") return TeamSide.Right;
                if (name == "Lane_Left") return TeamSide.Left;
                current = current.GetParent();
            }

            return TeamSide.Left;
        }

        public bool IsPointOverUi(Vector2 screenPoint)
        {
            if (_actionPanel != null && _actionPanel.Visible)
            {
                var actionRect = new Rect2(_actionPanel.GlobalPosition, _actionPanel.Size);
                if (actionRect.HasPoint(screenPoint)) return true;
            }

            if (_inspectorPanel != null && _inspectorPanel.Visible)
            {
                var inspectorRect = new Rect2(_inspectorPanel.GlobalPosition, _inspectorPanel.Size);
                if (inspectorRect.HasPoint(screenPoint)) return true;
            }

            return false;
        }

        private void ApplySelectedStyle(Button button, Color borderColor)
        {
            var style = new StyleBoxFlat
            {
                BgColor = new Color(0.13f, 0.13f, 0.13f, 0.95f),
                BorderColor = borderColor,
                BorderWidthLeft = 3,
                BorderWidthTop = 3,
                BorderWidthRight = 3,
                BorderWidthBottom = 3,
                CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6,
                CornerRadiusBottomRight = 6,
                CornerRadiusBottomLeft = 6
            };

            button.AddThemeStyleboxOverride("normal", style);
            button.AddThemeStyleboxOverride("hover", style);
            button.AddThemeStyleboxOverride("pressed", style);
            button.AddThemeStyleboxOverride("focus", style);
        }

        private void ClearBuildButtonHighlights()
        {
            _actionButton1.RemoveThemeStyleboxOverride("normal");
            _actionButton1.RemoveThemeStyleboxOverride("hover");
            _actionButton1.RemoveThemeStyleboxOverride("pressed");
            _actionButton1.RemoveThemeStyleboxOverride("focus");

            _actionButton2.RemoveThemeStyleboxOverride("normal");
            _actionButton2.RemoveThemeStyleboxOverride("hover");
            _actionButton2.RemoveThemeStyleboxOverride("pressed");
            _actionButton2.RemoveThemeStyleboxOverride("focus");
        }
    }
}
