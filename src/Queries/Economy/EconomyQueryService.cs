using LegionTDClone.Domain.Economy;
using LegionTDClone.Domain.Match;

namespace LegionTDClone.Queries.Economy
{
    public class EconomyQueryService
    {
        private readonly EconomyState _economyState;

        public EconomyQueryService(EconomyState economyState)
        {
            _economyState = economyState;
        }

        public int GetGold(TeamSide team)
        {
            return _economyState.GetGold(team);
        }
    }
}
