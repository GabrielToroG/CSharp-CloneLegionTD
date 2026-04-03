using Godot;
using System;
using LegionTDClone.Domain.Match;
using LegionTDClone.Application.Match;

namespace LegionTDClone.Platform.Godot.Simulation
{
    public partial class MatchAdapter : Node
    {
        public static MatchAdapter Instance { get; private set; }

        private MatchState _matchState;
        private MatchUseCase _matchUseCase;
        private WaveAdapter _waveAdapter;
        private bool _assaultAdvanceTriggered;

        // Configuration
        [Export] public double BuildPhaseDuration = 30.0;
        [Export] public NodePath WaveManagerPath = "../WaveManager";
        private double _phaseTimer = 0;
        public double BuildPhaseTimeRemaining => Math.Max(0.0, _phaseTimer);

        public override void _EnterTree()
        {
            if (Instance == null) Instance = this;
            else QueueFree();

            _matchState = CompositionRoot.App.Container.Resolve<MatchState>();
            _matchUseCase = CompositionRoot.App.Container.Resolve<MatchUseCase>();
            _matchUseCase.OnPhaseChanged += HandlePhaseChanged;
        }

        public override void _Ready()
        {
            _waveAdapter = GetNodeOrNull<WaveAdapter>(WaveManagerPath);
            if (_waveAdapter != null)
            {
                _waveAdapter.OnWaveFinished += HandleWaveFinished;
            }

            UnitAdapter.OnEnemyAggroAcquired += HandleEnemyAggroAcquired;

            if (Multiplayer.IsServer())
            {
                _matchUseCase.StartGame();
            }
        }

        public override void _ExitTree()
        {
            UnitAdapter.OnEnemyAggroAcquired -= HandleEnemyAggroAcquired;
        }

        public override void _Process(double delta)
        {
            if (!Multiplayer.IsServer()) return;

            if (_matchState.CurrentPhase == MatchPhase.BuildPhase)
            {
                _phaseTimer -= delta;
                if (_phaseTimer <= 0)
                {
                    _matchUseCase.AdvanceToCombat();
                }
            }
        }

        private void HandlePhaseChanged(MatchPhase newPhase)
        {
            if (newPhase == MatchPhase.BuildPhase)
            {
                _phaseTimer = BuildPhaseDuration;
                _assaultAdvanceTriggered = false;
                foreach (var node in GetTree().GetNodesInGroup("GridManagers"))
                {
                    if (node is GridAdapter grid)
                    {
                        grid.ClearFightersAndReset();
                    }
                }
            }
            else if (newPhase == MatchPhase.CombatPhase && Multiplayer.IsServer())
            {
                _assaultAdvanceTriggered = false;
                foreach (var node in GetTree().GetNodesInGroup("GridManagers"))
                {
                    if (node is GridAdapter grid)
                    {
                        grid.InstantiateFightersForCombat();
                    }
                }

                if (_waveAdapter != null)
                {
                    _waveAdapter.SpawnWave();
                }
                else
                {
                    GD.PrintErr("WaveManager not found. Cannot start combat wave.");
                }
            }
            Rpc(MethodName.RpcUpdatePhase, (int)newPhase);
        }

        private void HandleWaveFinished()
        {
            if (!Multiplayer.IsServer()) return;
            _matchUseCase.ReturnToBuildPhase();
        }

        private void HandleEnemyAggroAcquired(UnitAdapter enemyUnit)
        {
            if (!Multiplayer.IsServer()) return;
            if (_matchState.CurrentPhase != MatchPhase.CombatPhase) return;
            if (_assaultAdvanceTriggered) return;

            _assaultAdvanceTriggered = true;

            foreach (var node in GetTree().GetNodesInGroup("GridManagers"))
            {
                if (node is GridAdapter grid)
                {
                    grid.StartAssaultAdvance();
                }
            }
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
        private void RpcUpdatePhase(int phaseInt)
        {
            // Update UI here using Queries
            GD.Print($"Phase changed to: {(MatchPhase)phaseInt}");
        }
    }
}
