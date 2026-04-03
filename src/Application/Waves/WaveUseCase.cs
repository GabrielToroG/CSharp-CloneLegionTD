using System;
using LegionTDClone.Domain.Waves;
using LegionTDClone.GameData.Definitions;

namespace LegionTDClone.Application.Waves
{
    public class WaveUseCase
    {
        private readonly WaveState _waveState;

        // In a strictly decoupled setup, config would be loaded from GameData/Registry rather than passed from UI
        public event Action<WaveDefinition> OnWaveStarted;
        public event Action OnWaveCompleted;

        public WaveUseCase(WaveState waveState)
        {
            _waveState = waveState;
        }

        public void TriggerSpawnWave(WaveDefinition config)
        {
            if (_waveState.IsWaveActive) return;

            _waveState.StartWave(config);
            OnWaveStarted?.Invoke(config);
        }

        public void EnemySpawned()
        {
            _waveState.OnEnemySpawned();
        }

        public void EnemyDied()
        {
            _waveState.OnEnemyKilled();
            if (!_waveState.IsWaveActive)
            {
                OnWaveCompleted?.Invoke();
            }
        }
    }
}
