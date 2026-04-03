using System;
using Godot;
using LegionTDClone.Application.Construction;
using LegionTDClone.Domain.Match;

namespace LegionTDClone.Platform.Godot.Simulation
{
    public partial class ConstructorAdapter : CharacterBody3D
    {
        [Export] public TeamSide Team = TeamSide.Left;
        [Export] public float MoveSpeed = 6f;
        [Export] public float BuildDistance = 0.6f;

        public event Action<TeamSide, int, int> OnOrderResolved; // team, x, z

        private BuildTowerUseCase _buildUseCase;
        private MatchState _matchState;

        private bool _hasOrder;
        private int _orderGridX;
        private int _orderGridZ;
        private int _orderTowerIndex;
        private Vector3 _orderWorldPosition;
        private bool _hasMoveOrder;
        private Vector3 _moveWorldPosition;

        public override void _Ready()
        {
            // Non-combat utility unit: selectable but not physically blocking/blocked.
            CollisionLayer = 8u;
            CollisionMask = 0u;
            AddToGroup("Constructors");
        }

        public void Initialize(TeamSide team, BuildTowerUseCase buildUseCase, MatchState matchState)
        {
            Team = team;
            _buildUseCase = buildUseCase;
            _matchState = matchState;
        }

        public void IssueBuildOrder(int gridX, int gridZ, Vector3 worldPosition, int towerIndex)
        {
            _orderGridX = gridX;
            _orderGridZ = gridZ;
            _orderTowerIndex = towerIndex;
            _orderWorldPosition = worldPosition;
            _hasOrder = true;
            _hasMoveOrder = false;
        }

        public void IssueMoveOrder(Vector3 worldPosition)
        {
            _moveWorldPosition = worldPosition;
            _hasMoveOrder = true;
            _hasOrder = false;
        }

        public void CancelOrders()
        {
            _hasOrder = false;
            _hasMoveOrder = false;
            Velocity = Vector3.Zero;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_matchState == null) return;

            if (_hasOrder)
            {
                // Build orders are only valid during build phase.
                if (_matchState.CurrentPhase == MatchPhase.BuildPhase)
                {
                    ProcessBuildOrder();
                    return;
                }

                _hasOrder = false;
            }

            if (_hasMoveOrder)
            {
                ProcessMoveOrder();
            }
        }

        private void ProcessBuildOrder()
        {
            if (_buildUseCase == null) return;

            Vector3 target = new Vector3(_orderWorldPosition.X, GlobalPosition.Y, _orderWorldPosition.Z);
            float distance = GlobalPosition.DistanceTo(target);

            if (distance <= BuildDistance)
            {
                _buildUseCase.TryBuildTower(Team, _orderGridX, _orderGridZ, _orderTowerIndex);
                _hasOrder = false;
                Velocity = Vector3.Zero;
                OnOrderResolved?.Invoke(Team, _orderGridX, _orderGridZ);
                return;
            }

            MoveTowards(target);
        }

        private void ProcessMoveOrder()
        {
            Vector3 target = new Vector3(_moveWorldPosition.X, GlobalPosition.Y, _moveWorldPosition.Z);
            float distance = GlobalPosition.DistanceTo(target);
            if (distance <= BuildDistance)
            {
                _hasMoveOrder = false;
                Velocity = Vector3.Zero;
                return;
            }

            MoveTowards(target);
        }

        private void MoveTowards(Vector3 target)
        {
            Vector3 direction = GlobalPosition.DirectionTo(target);
            Velocity = direction * MoveSpeed;
            MoveAndSlide();

            if (direction.LengthSquared() > 0.001f)
            {
                Vector3 lookTarget = GlobalPosition + direction;
                try { LookAt(lookTarget, Vector3.Up); } catch { }
            }
        }
    }
}
