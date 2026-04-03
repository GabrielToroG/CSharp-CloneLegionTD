using Godot;
using System;
using System.Collections.Generic;
using LegionTDClone.Application.Construction;
using LegionTDClone.Domain.Board;
using LegionTDClone.Domain.Economy;
using LegionTDClone.Domain.Match;
using LegionTDClone.Domain.Roster;
using LegionTDClone.Platform.Godot.Simulation;
using LegionTDClone.Queries.Board;

namespace LegionTDClone.Platform.Godot.Input
{
    public partial class ConstructionInput : Node
    {
        [Export] public Camera3D MainCamera;
        [Export] public Node HUDManager;
        [Export] public PackedScene[] Prefabs = new PackedScene[2];
        [Export] public PackedScene ConstructorPrefab;
        
        private MatchState _matchState;
        private EconomyState _economyState;
        private BoardState _boardState;
        private RosterState _rosterState;
        private BoardQueryService _boardQuery;
        private BuildTowerUseCase _buildUseCase;
        private GridAdapter _currentHoveredGrid;
        private readonly Dictionary<TeamSide, GridAdapter> _gridsByTeam = new Dictionary<TeamSide, GridAdapter>();
        private readonly Dictionary<TeamSide, ConstructorAdapter> _constructors = new Dictionary<TeamSide, ConstructorAdapter>();
        private readonly Dictionary<string, PendingBuildData> _pendingBuilds = new Dictionary<string, PendingBuildData>();
        private ConstructorAdapter _selectedConstructor;

        private MeshInstance3D _ghostMesh;
        private Vector3 _currentHoveredPosition;
        private Vector3 _currentBuildPosition;
        private int _hoveredGridX;
        private int _hoveredGridZ;
        private TeamSide _hoveredTeam = TeamSide.Left;
        private bool _isValidPlacement = false;
        private bool _isBuildPlacementMode = false;
        private int _selectedBuildTowerIndex = -1;

        private readonly struct PendingBuildData
        {
            public PendingBuildData(GridAdapter grid, Vector3 worldPosition)
            {
                Grid = grid;
                WorldPosition = worldPosition;
            }

            public GridAdapter Grid { get; }
            public Vector3 WorldPosition { get; }
        }

        public override void _Ready()
        {
            Prefabs[0] = GD.Load<PackedScene>("res://Scenes/TowerBase.tscn");
            Prefabs[1] = GD.Load<PackedScene>("res://Scenes/TowerBaseGreen.tscn");
            ConstructorPrefab ??= GD.Load<PackedScene>("res://Scenes/Constructor.tscn");

            // Placeholder for DI
            _matchState = CompositionRoot.App.Container.Resolve<MatchState>();
            _economyState = CompositionRoot.App.Container.Resolve<EconomyState>();
            _boardState = CompositionRoot.App.Container.Resolve<BoardState>();
            _rosterState = CompositionRoot.App.Container.Resolve<RosterState>();
            _boardQuery = CompositionRoot.App.Container.Resolve<BoardQueryService>();
            _buildUseCase = CompositionRoot.App.Container.Resolve<BuildTowerUseCase>();

            _buildUseCase.OnTowerBuilt += HandleTowerBuilt;

            InitializeGhostMesh();
            SpawnConstructors();
        }

        private void InitializeGhostMesh()
        {
            _ghostMesh = new MeshInstance3D();
            var boxMesh = new BoxMesh { Size = new Vector3(1.8f, 2f, 1.8f) };
            var material = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.2f, 0.8f, 1f, 0.5f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha
            };
            boxMesh.Material = material;
            _ghostMesh.Mesh = boxMesh;
            _ghostMesh.Visible = false;
            
            GetTree().Root.CallDeferred(MethodName.AddChild, _ghostMesh);
        }

        public override void _Process(double delta)
        {
            EnsureGridCache();

            if (_matchState.CurrentPhase != MatchPhase.BuildPhase || MainCamera == null)
            {
                if (_ghostMesh != null) _ghostMesh.Visible = false;
                return;
            }

            if (_selectedConstructor == null || !GodotObject.IsInstanceValid(_selectedConstructor))
            {
                if (_ghostMesh != null) _ghostMesh.Visible = false;
                return;
            }

            if (!_isBuildPlacementMode)
            {
                if (_ghostMesh != null) _ghostMesh.Visible = false;
                return;
            }

            if (_selectedBuildTowerIndex < 0)
            {
                if (_ghostMesh != null) _ghostMesh.Visible = false;
                return;
            }

            UpdateMouseRaycast();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseBlockEvent)
            {
                if (GetViewport().GuiGetHoveredControl() != null) return;
                if (HUDManager is LegionTDClone.Platform.Godot.Presentation.GameHUD hud && hud.IsPointOverUi(mouseBlockEvent.Position)) return;
            }

            if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                if (_matchState.CurrentPhase != MatchPhase.BuildPhase) return;

                if (_ghostMesh.Visible && _isValidPlacement)
                {
                    if (_selectedConstructor == null || !GodotObject.IsInstanceValid(_selectedConstructor)) return;
                    if (_selectedConstructor.Team != _hoveredTeam) return;

                    if (_constructors.TryGetValue(_hoveredTeam, out ConstructorAdapter constructor) && constructor != null)
                    {
                        if (constructor != _selectedConstructor) return;

                        int towerIndex = _selectedBuildTowerIndex;
                        string key = MakeBuildKey(_hoveredTeam, _hoveredGridX, _hoveredGridZ);
                        _pendingBuilds[key] = new PendingBuildData(_currentHoveredGrid, _currentBuildPosition);
                        constructor.IssueBuildOrder(_hoveredGridX, _hoveredGridZ, _currentBuildPosition, towerIndex);
                    }
                }
            }
            else if (@event is InputEventMouseButton moveEvent && moveEvent.ButtonIndex == MouseButton.Right && moveEvent.Pressed)
            {
                if (_selectedConstructor == null || !GodotObject.IsInstanceValid(_selectedConstructor)) return;

                if (TryGetPointOnSelectedGrid(moveEvent.Position, out GridAdapter grid, out Vector3 worldPosition))
                {
                    TeamSide team = _selectedConstructor.Team;

                    if (_selectedConstructor != null)
                    {
                        _selectedConstructor.CancelOrders();
                        RemovePendingBuildsForTeam(team);
                        _selectedConstructor.IssueMoveOrder(grid.SnapToGrid(worldPosition));
                    }
                }
            }
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                if (keyEvent.Keycode == Key.Key1) _rosterState.SelectTower(0);
                if (keyEvent.Keycode == Key.Key2) _rosterState.SelectTower(1);
            }
        }

        private void UpdateMouseRaycast()
        {
            var mousePos = GetViewport().GetMousePosition();
            if (_selectedConstructor == null || !GodotObject.IsInstanceValid(_selectedConstructor))
            {
                _ghostMesh.Visible = false;
                _isValidPlacement = false;
                return;
            }

            _hoveredTeam = _selectedConstructor.Team;
            if (!_gridsByTeam.TryGetValue(_hoveredTeam, out _currentHoveredGrid) || _currentHoveredGrid == null)
            {
                _ghostMesh.Visible = false;
                _isValidPlacement = false;
                return;
            }

            if (TryGetPointOnGrid(_currentHoveredGrid, mousePos, out Vector3 worldPos))
            {
                // Visual ghost follows mouse directly for precise cursor alignment.
                _currentHoveredPosition = worldPos;
                _ghostMesh.GlobalPosition = _currentHoveredPosition + new Vector3(0, 1f, 0);
                _ghostMesh.Visible = true;

                Vector3 localPos = _currentHoveredGrid.ToLocal(worldPos);
                _hoveredGridX = Mathf.FloorToInt(localPos.X / _currentHoveredGrid.CellSize) + _currentHoveredGrid.GridDimensions.X / 2;
                _hoveredGridZ = Mathf.FloorToInt(localPos.Z / _currentHoveredGrid.CellSize) + _currentHoveredGrid.GridDimensions.Y / 2;

                bool inBounds = _hoveredGridX >= 0 && _hoveredGridX < _currentHoveredGrid.GridDimensions.X &&
                                _hoveredGridZ >= 0 && _hoveredGridZ < _currentHoveredGrid.GridDimensions.Y;
                _isValidPlacement = inBounds && _boardQuery.CanPlaceAt(_hoveredGridX, _hoveredGridZ);
                _currentBuildPosition = _currentHoveredGrid.SnapToGrid(worldPos);

                var mat = (StandardMaterial3D)((BoxMesh)_ghostMesh.Mesh).Material;
                if (_isValidPlacement)
                {
                    mat.AlbedoColor = _selectedBuildTowerIndex == 0 ? new Color(0.2f, 0.8f, 1f, 0.5f) : new Color(0.2f, 1f, 0.2f, 0.5f);
                }
                else
                {
                    mat.AlbedoColor = new Color(1f, 0.2f, 0.2f, 0.5f);
                }
            }
            else
            {
                _ghostMesh.Visible = false;
                _isValidPlacement = false;
            }
        }

        private void HandleTowerBuilt(TeamSide team, int index, int gridX, int gridZ)
        {
            if (index < 0 || index >= Prefabs.Length) return;
            var prefab = Prefabs[index];
            if (prefab == null) return;

            string key = MakeBuildKey(team, gridX, gridZ);
            if (!_pendingBuilds.TryGetValue(key, out PendingBuildData pending)) return;

            Node3D newTower = (Node3D)prefab.Instantiate();
            pending.Grid?.RegisterPlacedUnit(newTower);
            newTower.GlobalPosition = pending.WorldPosition;
        }

        private static TeamSide ResolveTeamFromLane(Node3D laneNode)
        {
            if (laneNode == null) return TeamSide.Left;
            return laneNode.Name.ToString().Contains("Right") ? TeamSide.Right : TeamSide.Left;
        }

        private void SpawnConstructors()
        {
            if (ConstructorPrefab == null) return;

            TrySpawnConstructorForLane(TeamSide.Left, "../Lane_Left");
            TrySpawnConstructorForLane(TeamSide.Right, "../Lane_Right");
        }

        private void TrySpawnConstructorForLane(TeamSide team, string lanePath)
        {
            if (GetNodeOrNull<Node3D>(lanePath) is not Node3D laneNode) return;
            if (ConstructorPrefab.Instantiate() is not ConstructorAdapter constructor) return;
            var grid = laneNode.GetNodeOrNull<GridAdapter>("GridManager");
            if (grid != null)
            {
                _gridsByTeam[team] = grid;
            }

            laneNode.AddChild(constructor);
            constructor.GlobalPosition = laneNode.GlobalPosition + new Vector3(0, 0.5f, 18f);
            constructor.Initialize(team, _buildUseCase, _matchState);
            constructor.OnOrderResolved += HandleOrderResolved;
            _constructors[team] = constructor;
        }

        private void HandleOrderResolved(TeamSide team, int gridX, int gridZ)
        {
            _pendingBuilds.Remove(MakeBuildKey(team, gridX, gridZ));
        }

        private static string MakeBuildKey(TeamSide team, int gridX, int gridZ)
        {
            return $"{team}:{gridX}:{gridZ}";
        }

        private void RemovePendingBuildsForTeam(TeamSide team)
        {
            var keysToRemove = new System.Collections.Generic.List<string>();
            string prefix = $"{team}:";
            foreach (string key in _pendingBuilds.Keys)
            {
                if (key.StartsWith(prefix))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (string key in keysToRemove)
            {
                _pendingBuilds.Remove(key);
            }
        }

        private bool TryGetLanePointFromMouse(Vector2 mousePosition, out TeamSide team, out GridAdapter grid, out Vector3 worldPosition)
        {
            team = TeamSide.Left;
            grid = null;
            worldPosition = Vector3.Zero;
            if (_selectedConstructor == null || !GodotObject.IsInstanceValid(_selectedConstructor)) return false;
            team = _selectedConstructor.Team;
            if (!_gridsByTeam.TryGetValue(team, out grid) || grid == null) return false;
            return TryGetPointOnGrid(grid, mousePosition, out worldPosition);
        }

        public void SetBuildTowerFromActionPanel(int towerIndex)
        {
            _rosterState.SelectTower(towerIndex);
            _selectedBuildTowerIndex = towerIndex;
            _isBuildPlacementMode = true;
        }

        public void EnterBuildMenuForSelectedConstructor()
        {
            if (_selectedConstructor == null || !GodotObject.IsInstanceValid(_selectedConstructor))
            {
                _isBuildPlacementMode = false;
                _selectedBuildTowerIndex = -1;
                if (_ghostMesh != null) _ghostMesh.Visible = false;
                return;
            }

            _isBuildPlacementMode = false;
            _selectedBuildTowerIndex = -1;
            if (_ghostMesh != null) _ghostMesh.Visible = false;
        }

        public void ExitBuildPlacementMode()
        {
            _isBuildPlacementMode = false;
            _selectedBuildTowerIndex = -1;
            if (_ghostMesh != null) _ghostMesh.Visible = false;
        }

        public bool BeginBuildPlacementFromSelection(int towerIndex)
        {
            if (_selectedConstructor == null || !GodotObject.IsInstanceValid(_selectedConstructor))
            {
                _isBuildPlacementMode = false;
                if (_ghostMesh != null) _ghostMesh.Visible = false;
                return false;
            }

            _rosterState.SelectTower(towerIndex);
            _selectedBuildTowerIndex = towerIndex;
            _isBuildPlacementMode = true;
            return true;
        }

        public bool BeginBuildPlacementFromUi(Node3D selectedEntity, int towerIndex)
        {
            if (selectedEntity is not ConstructorAdapter constructor || !GodotObject.IsInstanceValid(constructor))
            {
                _isBuildPlacementMode = false;
                if (_ghostMesh != null) _ghostMesh.Visible = false;
                return false;
            }

            _selectedConstructor = constructor;
            _rosterState.SelectTower(towerIndex);
            _selectedBuildTowerIndex = towerIndex;
            _isBuildPlacementMode = true;
            return true;
        }

        public bool TrySellTower(TowerAdapter tower)
        {
            if (tower == null || tower.Data == null) return false;
            if (!TryResolveTowerPlacement(tower, out TeamSide team, out _, out int gridX, out int gridZ)) return false;

            int refund = Mathf.RoundToInt(tower.Data.Cost * tower.Data.SellRefundFactor);
            if (_boardState.TryFree(gridX, gridZ))
            {
                _economyState.AddGold(team, refund);
                tower.QueueFree();
                return true;
            }

            return false;
        }

        public bool TryUpgradeTower(TowerAdapter tower)
        {
            if (tower == null || tower.Data == null) return false;
            if (tower.Data.UpgradeScene == null || tower.Data.UpgradeCost <= 0) return false;
            if (!TryResolveTowerPlacement(tower, out TeamSide team, out GridAdapter grid, out _, out _)) return false;
            if (!_economyState.TrySpendGold(team, tower.Data.UpgradeCost)) return false;

            if (tower.Data.UpgradeScene.Instantiate() is not Node3D upgradedTower)
            {
                _economyState.AddGold(team, tower.Data.UpgradeCost);
                return false;
            }

            grid.RegisterPlacedUnit(upgradedTower);
            upgradedTower.GlobalPosition = tower.GlobalPosition;
            tower.QueueFree();
            return true;
        }

        private bool TryResolveTowerPlacement(TowerAdapter tower, out TeamSide team, out GridAdapter grid, out int gridX, out int gridZ)
        {
            team = TeamSide.Left;
            grid = tower.GetParent() as GridAdapter;
            gridX = 0;
            gridZ = 0;
            if (grid == null) return false;

            Node3D laneNode = grid.GetParent() as Node3D;
            team = ResolveTeamFromLane(laneNode);

            Vector3 localPos = grid.ToLocal(tower.GlobalPosition);
            gridX = Mathf.FloorToInt(localPos.X / grid.CellSize) + grid.GridDimensions.X / 2;
            gridZ = Mathf.FloorToInt(localPos.Z / grid.CellSize) + grid.GridDimensions.Y / 2;
            return true;
        }

        public void SetSelectedEntity(Node3D selectedEntity)
        {
            var nextConstructor = selectedEntity as ConstructorAdapter;

            // Preserve build placement state when the same constructor remains selected.
            if (nextConstructor != null && nextConstructor == _selectedConstructor) return;

            _selectedConstructor = nextConstructor;
            _isBuildPlacementMode = false;
            _selectedBuildTowerIndex = -1;
            if (_ghostMesh != null) _ghostMesh.Visible = false;
        }

        public void ClearSelectedEntity()
        {
            _selectedConstructor = null;
            _isBuildPlacementMode = false;
            _selectedBuildTowerIndex = -1;
            if (_ghostMesh != null) _ghostMesh.Visible = false;
        }

        private static bool TryResolveGridFromCollider(Node3D collider, out GridAdapter grid, out TeamSide team)
        {
            grid = null;
            team = TeamSide.Left;
            if (collider == null) return false;

            Node current = collider;
            while (current != null)
            {
                if (current is Node3D node3D)
                {
                    GridAdapter laneGrid = node3D.GetNodeOrNull<GridAdapter>("GridManager");
                    if (laneGrid != null)
                    {
                        grid = laneGrid;
                        team = ResolveTeamFromLane(node3D);
                        return true;
                    }

                    if (node3D is GridAdapter directGrid)
                    {
                        grid = directGrid;
                        team = ResolveTeamFromLane(directGrid.GetParent() as Node3D);
                        return true;
                    }
                }
                current = current.GetParent();
            }

            return false;
        }

        private bool TryGetPointOnSelectedGrid(Vector2 mousePosition, out GridAdapter grid, out Vector3 worldPosition)
        {
            grid = null;
            worldPosition = Vector3.Zero;
            if (_selectedConstructor == null || !GodotObject.IsInstanceValid(_selectedConstructor)) return false;
            if (!_gridsByTeam.TryGetValue(_selectedConstructor.Team, out grid) || grid == null) return false;
            return TryGetPointOnGrid(grid, mousePosition, out worldPosition);
        }

        private bool TryGetPointOnGrid(GridAdapter grid, Vector2 mousePosition, out Vector3 worldPosition)
        {
            worldPosition = Vector3.Zero;
            if (MainCamera == null || grid == null) return false;

            Vector3 rayOrigin = MainCamera.ProjectRayOrigin(mousePosition);
            Vector3 rayDir = MainCamera.ProjectRayNormal(mousePosition);
            float planeY = grid.GlobalPosition.Y;
            float denom = rayDir.Y;
            if (Mathf.Abs(denom) < 0.0001f) return false;

            float t = (planeY - rayOrigin.Y) / denom;
            if (t <= 0f) return false;

            worldPosition = rayOrigin + rayDir * t;
            return true;
        }

        private void EnsureGridCache()
        {
            if (_gridsByTeam.ContainsKey(TeamSide.Left) && _gridsByTeam.ContainsKey(TeamSide.Right)) return;

            foreach (Node node in GetTree().GetNodesInGroup("GridManagers"))
            {
                if (node is not GridAdapter grid) continue;
                TeamSide team = ResolveTeamFromLane(grid.GetParent() as Node3D);
                _gridsByTeam[team] = grid;
            }
        }
    }
}
