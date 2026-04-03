namespace LegionTDClone.Domain.Match
{
    public class MatchState
    {
        public MatchPhase CurrentPhase { get; private set; } = MatchPhase.WaitingForPlayers;

        public void ChangePhase(MatchPhase newPhase)
        {
            CurrentPhase = newPhase;
        }
    }
}
