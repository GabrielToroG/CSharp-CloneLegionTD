using LegionTDClone.Domain.Match;

namespace LegionTDClone.Queries.Match
{
    public class MatchQueryService
    {
        private readonly MatchState _matchState;

        public MatchQueryService(MatchState matchState)
        {
            _matchState = matchState;
        }

        public MatchPhase GetCurrentPhase()
        {
            return _matchState.CurrentPhase;
        }

        public bool IsBuildPhase()
        {
            return _matchState.CurrentPhase == MatchPhase.BuildPhase;
        }
    }
}
