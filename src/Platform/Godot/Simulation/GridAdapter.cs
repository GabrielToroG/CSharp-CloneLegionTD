using Godot;
using System;
using System.Collections.Generic;
using LegionTDClone.Queries.Board;
using LegionTDClone.Domain.Board;

namespace LegionTDClone.Platform.Godot.Simulation
{
    public partial class GridAdapter : Node3D
    {
        [Export] public float CellSize = 2f;
        [Export] public Vector2I GridDimensions = new Vector2I(7, 20); // 14x40 green zone
        [Export] public Color GridColor = new Color(0.55f, 0.55f, 0.55f, 0.9f);
        [Export] public float GridHeightOffset = 0.03f;
        
        [Export] public PackedScene DefenderPrefab;
        private global::Godot.Collections.Array<Node3D> _spawnedInstances = new global::Godot.Collections.Array<Node3D>();
        private readonly Dictionary<TowerAdapter, UnitAdapter> _towerFighters = new Dictionary<TowerAdapter, UnitAdapter>();
        private MeshInstance3D _gridOverlay;
        
        // This will eventually be injected from CompositionRoot
        private BoardState _boardState;
        private BoardQueryService _boardQuery;

        public override void _Ready()
        {
            AddToGroup("GridManagers");
            // Minimal setup for intermediate testing (without full DI yet)
            _boardState = CompositionRoot.App.Container.Resolve<BoardState>();
            _boardQuery = CompositionRoot.App.Container.Resolve<BoardQueryService>();
            BuildGridOverlay();
        }

        public Vector3 SnapToGrid(Vector3 worldPosition)
        {
            Vector3 localPosition = ToLocal(worldPosition);
            
            int gridX = Mathf.FloorToInt(localPosition.X / CellSize) + GridDimensions.X / 2;
            int gridZ = Mathf.FloorToInt(localPosition.Z / CellSize) + GridDimensions.Y / 2;

            gridX = Math.Clamp(gridX, 0, GridDimensions.X - 1);
            gridZ = Math.Clamp(gridZ, 0, GridDimensions.Y - 1);

            float localXPos = (gridX - GridDimensions.X / 2f + 0.5f) * CellSize;
            float localZPos = (gridZ - GridDimensions.Y / 2f + 0.5f) * CellSize;

            Vector3 snappedLocalPos = new Vector3(localXPos, localPosition.Y, localZPos);
            return ToGlobal(snappedLocalPos);
        }

        public bool TryPlaceUnit(Vector3 worldPosition, Node3D unitInstance)
        {
            Vector3 localPosition = ToLocal(worldPosition);

            int localX = Mathf.FloorToInt(localPosition.X / CellSize) + GridDimensions.X / 2;
            int localZ = Mathf.FloorToInt(localPosition.Z / CellSize) + GridDimensions.Y / 2;

            if (_boardQuery.CanPlaceAt(localX, localZ))
            {
                if (_boardState.TryOccupy(localX, localZ))
                {
                    RegisterPlacedUnit(unitInstance);
                    unitInstance.GlobalPosition = SnapToGrid(worldPosition);
                    return true;
                }
            }
            return false;
        }

        public void RegisterPlacedUnit(Node3D unitInstance)
        {
            AddChild(unitInstance);
            _spawnedInstances.Add(unitInstance);

            if (unitInstance is TowerAdapter tower)
            {
                EnsureTowerCombatFighter(tower);
            }
        }

        // Logic here to iterate over all instantiated prefabs and toggle combat mode 
        // will be expanded in Combat Phase (Fase 5). For now, preserving signature.
        public void InstantiateFightersForCombat()
        {
            foreach (var instance in _spawnedInstances)
            {
                if (instance is not TowerAdapter tower) continue;
                UnitAdapter fighter = EnsureTowerCombatFighter(tower);
                if (fighter == null) continue;

                tower.SetInteractive(false);
                fighter.GlobalPosition = tower.GlobalPosition;
                SetFighterActive(fighter, true);
            }
        }

        public void ClearFightersAndReset()
        {
            foreach (var kvp in _towerFighters)
            {
                TowerAdapter tower = kvp.Key;
                UnitAdapter fighter = kvp.Value;
                if (tower != null && GodotObject.IsInstanceValid(tower))
                {
                    tower.SetInteractive(true);
                }
                if (fighter != null && GodotObject.IsInstanceValid(fighter))
                {
                    SetFighterActive(fighter, false);
                }
            }
        }

        private UnitAdapter EnsureTowerCombatFighter(TowerAdapter tower)
        {
            if (tower == null || !GodotObject.IsInstanceValid(tower)) return null;
            if (_towerFighters.TryGetValue(tower, out UnitAdapter existing) && existing != null && GodotObject.IsInstanceValid(existing))
            {
                return existing;
            }

            if (tower.FighterPrefab == null) return null;
            if (tower.FighterPrefab.Instantiate() is not UnitAdapter fighter) return null;

            AddChild(fighter);
            fighter.GlobalPosition = tower.GlobalPosition;
            SetFighterActive(fighter, false);
            _towerFighters[tower] = fighter;

            tower.TreeExiting += () =>
            {
                if (_towerFighters.TryGetValue(tower, out UnitAdapter linkedFighter))
                {
                    if (linkedFighter != null && GodotObject.IsInstanceValid(linkedFighter))
                    {
                        linkedFighter.QueueFree();
                    }
                    _towerFighters.Remove(tower);
                }
            };

            return fighter;
        }

        private static void SetFighterActive(UnitAdapter fighter, bool active)
        {
            if (fighter == null || !GodotObject.IsInstanceValid(fighter)) return;

            fighter.Visible = active;
            fighter.SetPhysicsProcess(active);
            fighter.SetProcess(active);
            fighter.CollisionLayer = active ? 1u : 0u;
            fighter.CollisionMask = active ? 1u : 0u;
        }

        public void StartAssaultAdvance()
        {
            Vector3 destination = ResolveForwardAssaultDestination();
            foreach (var kvp in _towerFighters)
            {
                UnitAdapter fighter = kvp.Value;
                if (fighter == null || !GodotObject.IsInstanceValid(fighter)) continue;
                if (!fighter.Visible) continue;

                fighter.HasTargetDestination = true;
                fighter.FinalDestination = destination;
            }
        }

        private Vector3 ResolveForwardAssaultDestination()
        {
            Node3D lane = GetParent() as Node3D;
            if (lane == null) return GlobalPosition;

            foreach (Node child in lane.GetChildren())
            {
                if (child is Marker3D marker && marker.Name.ToString().StartsWith("Spawn"))
                {
                    return marker.GlobalPosition;
                }
            }

            return GlobalPosition;
        }

        private void BuildGridOverlay()
        {
            _gridOverlay?.QueueFree();

            var mesh = new ImmediateMesh();
            var material = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                AlbedoColor = GridColor
            };

            float halfWidth = GridDimensions.X * CellSize * 0.5f;
            float halfDepth = GridDimensions.Y * CellSize * 0.5f;
            float y = GridHeightOffset;

            mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, material);

            for (int x = 0; x <= GridDimensions.X; x++)
            {
                float xPos = -halfWidth + (x * CellSize);
                mesh.SurfaceAddVertex(new Vector3(xPos, y, -halfDepth));
                mesh.SurfaceAddVertex(new Vector3(xPos, y, halfDepth));
            }

            for (int z = 0; z <= GridDimensions.Y; z++)
            {
                float zPos = -halfDepth + (z * CellSize);
                mesh.SurfaceAddVertex(new Vector3(-halfWidth, y, zPos));
                mesh.SurfaceAddVertex(new Vector3(halfWidth, y, zPos));
            }

            mesh.SurfaceEnd();

            _gridOverlay = new MeshInstance3D
            {
                Name = "GridOverlay",
                Mesh = mesh,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            };

            AddChild(_gridOverlay);
        }
    }
}
