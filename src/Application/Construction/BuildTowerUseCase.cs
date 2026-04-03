using System;
using LegionTDClone.Domain.Construction;
using LegionTDClone.Domain.Economy;
using LegionTDClone.Domain.Match;
using LegionTDClone.Domain.Roster;
using LegionTDClone.Domain.Board;
using LegionTDClone.Queries.Board;

namespace LegionTDClone.Application.Construction
{
    public class BuildTowerUseCase
    {
        private readonly EconomyState _economyState;
        private readonly MatchState _matchState;
        private readonly RosterState _rosterState;
        private readonly BoardState _boardState;
        private readonly BoardQueryService _boardQuery;
        private readonly ConstructionValidator _validator;

        // Fired when a build is successfully validated and state updated.
        // Platform subscribes to this to instantiate the Node3D.
        public event Action<TeamSide, int, int, int> OnTowerBuilt; // team, index, x, z

        public BuildTowerUseCase(
            EconomyState economyState, 
            MatchState matchState, 
            RosterState rosterState, 
            BoardState boardState, 
            BoardQueryService boardQuery)
        {
            _economyState = economyState;
            _matchState = matchState;
            _rosterState = rosterState;
            _boardState = boardState;
            _boardQuery = boardQuery;
            _validator = new ConstructionValidator();
        }

        public bool TryBuildTower(TeamSide team, int gridX, int gridZ, int towerIndex)
        {
            int cost = _rosterState.GetTowerCost(towerIndex);
            
            if (_validator.CanBuild(cost, gridX, gridZ, _economyState, team, _matchState, _boardQuery))
            {
                if (_economyState.TrySpendGold(team, cost))
                {
                    if (_boardState.TryOccupy(gridX, gridZ))
                    {
                        OnTowerBuilt?.Invoke(team, towerIndex, gridX, gridZ);
                        return true;
                    }
                    else
                    {
                        // Rollback gold if occupancy failed unexpectedly
                        _economyState.AddGold(team, cost);
                    }
                }
            }
            return false;
        }
    }
}
