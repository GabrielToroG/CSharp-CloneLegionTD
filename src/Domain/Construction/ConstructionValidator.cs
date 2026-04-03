using LegionTDClone.Domain.Economy;
using LegionTDClone.Domain.Match;
using LegionTDClone.Queries.Board;

namespace LegionTDClone.Domain.Construction
{
    public class ConstructionValidator
    {
        public bool CanBuild(int cost, int gridX, int gridZ, EconomyState economy, TeamSide team, MatchState match, BoardQueryService boardQuery)
        {
            if (match.CurrentPhase != MatchPhase.BuildPhase) return false;
            if (economy.GetGold(team) < cost) return false;
            if (!boardQuery.CanPlaceAt(gridX, gridZ)) return false;

            return true;
        }
    }
}
