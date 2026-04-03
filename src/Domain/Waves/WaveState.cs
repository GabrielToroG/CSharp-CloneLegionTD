using LegionTDClone.GameData.Definitions;

namespace LegionTDClone.Domain.Waves
{
    public class WaveState
    {
        public int CurrentWaveIndex { get; private set; } = 0;
        public int EnemiesRemainingToSpawn { get; private set; } = 0;
        public int ActiveEnemies { get; private set; } = 0;
        public bool IsWaveActive { get; private set; } = false;

        public WaveDefinition CurrentWaveConfig { get; private set; }

        public void StartWave(WaveDefinition config)
        {
            CurrentWaveConfig = config;
            EnemiesRemainingToSpawn = config.EnemyCount;
            ActiveEnemies = 0;
            IsWaveActive = true;
            CurrentWaveIndex++;
        }

        public void OnEnemySpawned()
        {
            if (EnemiesRemainingToSpawn > 0)
            {
                EnemiesRemainingToSpawn--;
                ActiveEnemies++;
            }
        }

        public void OnEnemyKilled()
        {
            if (ActiveEnemies > 0)
            {
                ActiveEnemies--;
            }
            CheckWaveEnd();
        }

        private void CheckWaveEnd()
        {
            if (EnemiesRemainingToSpawn == 0 && ActiveEnemies == 0)
            {
                IsWaveActive = false;
            }
        }
    }
}
