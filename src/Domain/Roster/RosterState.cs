namespace LegionTDClone.Domain.Roster
{
    public class RosterState
    {
        // For MVP, just storing an index. Later this should be a UnitDefinitionId.
        public int SelectedTowerIndex { get; private set; } = 0;

        public void SelectTower(int index)
        {
            SelectedTowerIndex = index;
        }

        // Ideally returns TowerDefinition.Cost
        public int GetCurrentTowerCost()
        {
            return GetTowerCost(SelectedTowerIndex);
        }

        public int GetTowerCost(int towerIndex)
        {
            return towerIndex == 0 ? 20 : 30; // Hardcoded fallback for now
        }
    }
}
