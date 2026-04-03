using System.Collections.Generic;
using LegionTDClone.Domain.Match;

namespace LegionTDClone.Domain.Economy
{
    public class EconomyState
    {
        private readonly Dictionary<TeamSide, int> _goldByTeam = new Dictionary<TeamSide, int>();

        public EconomyState(int initialGoldPerTeam = 10000)
        {
            _goldByTeam[TeamSide.Left] = initialGoldPerTeam;
            _goldByTeam[TeamSide.Right] = initialGoldPerTeam;
        }

        public int GetGold(TeamSide team)
        {
            return _goldByTeam[team];
        }

        public void AddGold(TeamSide team, int amount)
        {
            if (amount > 0)
            {
                _goldByTeam[team] += amount;
            }
        }

        public bool TrySpendGold(TeamSide team, int amount)
        {
            if (amount > 0 && _goldByTeam[team] >= amount)
            {
                _goldByTeam[team] -= amount;
                return true;
            }
            return false;
        }
    }
}
