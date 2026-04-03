using Godot;
using LegionTDClone.Application.Waves;
using LegionTDClone.Domain.Waves;
using LegionTDClone.GameData.Definitions;

namespace LegionTDClone.Platform.Godot.Simulation
{
    public partial class WaveAdapter : Node
    {
        public static WaveAdapter Instance { get; private set; }

        [Export] public global::Godot.Collections.Array<WaveDefinition> Waves = new global::Godot.Collections.Array<WaveDefinition>();
        [Export] public global::Godot.Collections.Array<Node3D> SpawnPoints = new global::Godot.Collections.Array<Node3D>();
        [Export] public global::Godot.Collections.Array<Node3D> TargetPoints = new global::Godot.Collections.Array<Node3D>();

        private WaveState _waveState;
        private WaveUseCase _waveUseCase;

        // Timers for Godot loop
        private float _spawnTimer = 0;
        private float _spawnInterval = 1.0f;
        private PackedScene _enemyScene;

        [Signal] public delegate void OnWaveFinishedEventHandler();

        public override void _EnterTree()
        {
            if (Instance == null) Instance = this;
            else QueueFree();

            _waveState = CompositionRoot.App.Container.Resolve<WaveState>();
            _waveUseCase = CompositionRoot.App.Container.Resolve<WaveUseCase>();

            _waveUseCase.OnWaveStarted += HandleWaveStarted;
            _waveUseCase.OnWaveCompleted += HandleWaveCompleted;
        }

        public void SpawnWave()
        {
            if (!Multiplayer.IsServer()) return;

            int currentIdx = _waveState.CurrentWaveIndex;
            if (currentIdx >= Waves.Count)
            {
                GD.PrintErr("No more waves configured!");
                EmitSignal(SignalName.OnWaveFinished);
                return;
            }

            _waveUseCase.TriggerSpawnWave(Waves[currentIdx]);
        }

        private void HandleWaveStarted(WaveDefinition config)
        {
            _enemyScene = config.EnemyScene;
            _spawnInterval = config.SpawnInterval;
            _spawnTimer = 0;
            Rpc(MethodName.RpcUpdateWaveUI, _waveState.CurrentWaveIndex);
        }

        private void HandleWaveCompleted()
        {
            EmitSignal(SignalName.OnWaveFinished);
        }

        public override void _Process(double delta)
        {
            if (!Multiplayer.IsServer()) return;

            if (_waveState.IsWaveActive && _waveState.EnemiesRemainingToSpawn > 0)
            {
                _spawnTimer -= (float)delta;
                if (_spawnTimer <= 0)
                {
                    InstantiateEnemy();
                    _waveUseCase.EnemySpawned();
                    _spawnTimer = _spawnInterval;
                }
            }
        }

        private void InstantiateEnemy()
        {
            if (_enemyScene == null) return;
            if (SpawnPoints == null || SpawnPoints.Count == 0) return;

            for (int i = 0; i < SpawnPoints.Count; i++)
            {
                var point = SpawnPoints[i];
                if (_enemyScene.Instantiate() is UnitAdapter enemy)
                {
                    var conf = _waveState.CurrentWaveConfig;
                    enemy.MaxHealth = conf.Hp;
                    enemy.AttackDamage = conf.AttackDamage;
                    enemy.AttackRange = conf.AttackRange;
                    enemy.AttackSpeed = conf.AttackSpeed;
                    enemy.Armor = conf.Armor;
                    enemy.MovementSpeed = conf.MovementSpeed;
                    enemy.IsEnemy = true;
                    
                    point.AddChild(enemy);

                    // Add listener to count deaths correctly in domain
                    enemy.TreeExiting += () => _waveUseCase.EnemyDied();
                    
                    float randomX = (float)GD.RandRange(-5.0, 5.0);
                    float randomZ = (float)GD.RandRange(-1.0, 1.0);
                    enemy.GlobalPosition = point.GlobalPosition + new Vector3(randomX, 0, randomZ);
                    
                    if (TargetPoints != null && TargetPoints.Count > i && TargetPoints[i] != null)
                    {
                        enemy.HasTargetDestination = true;
                        enemy.FinalDestination = TargetPoints[i].GlobalPosition;
                    }
                }
            }
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
        private void RpcUpdateWaveUI(int waveNum)
        {
            GD.Print($"Client UI updated for Wave: {waveNum}");
        }
    }
}
