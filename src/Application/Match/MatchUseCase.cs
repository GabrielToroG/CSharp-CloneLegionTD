using System;
using LegionTDClone.Domain.Match;


namespace LegionTDClone.Application.Match
{
    public class MatchUseCase
    {
        private readonly MatchState _matchState;
        
        // This is a placeholder design for Eventing; we'd inject an EventBus
        public event Action<MatchPhase> OnPhaseChanged;

        public MatchUseCase(MatchState matchState)
        {
            _matchState = matchState;
        }

        public void StartGame()
        {
            if (_matchState.CurrentPhase != MatchPhase.WaitingForPlayers) return;
            ChangePhase(MatchPhase.BuildPhase);
        }

        public void AdvanceToCombat()
        {
            if (_matchState.CurrentPhase == MatchPhase.BuildPhase)
            {
                ChangePhase(MatchPhase.CombatPhase);
            }
        }

        public void ReturnToBuildPhase()
        {
            if (_matchState.CurrentPhase == MatchPhase.CombatPhase)
            {
                ChangePhase(MatchPhase.BuildPhase);
            }
        }

        private void ChangePhase(MatchPhase newPhase)
        {
            _matchState.ChangePhase(newPhase);
            OnPhaseChanged?.Invoke(newPhase);
        }
    }
}
