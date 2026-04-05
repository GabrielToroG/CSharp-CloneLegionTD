using LegionTDClone.Application.Construction;
using LegionTDClone.Application.Match;
using LegionTDClone.Application.Waves;
using LegionTDClone.Domain.Board;
using LegionTDClone.Domain.Economy;
using LegionTDClone.Domain.Match;
using LegionTDClone.Domain.Roster;
using LegionTDClone.Domain.Waves;
using LegionTDClone.Queries.Board;
using LegionTDClone.Queries.Economy;
using LegionTDClone.Queries.Match;

namespace LegionTDClone.CompositionRoot
{
    public static class App
    {
        private static Container _container;

        public static Container Container
        {
            get
            {
                if (_container == null)
                {
                    _container = new Container();
                    RegisterServices();
                }
                return _container;
            }
        }

        private static void RegisterServices()
        {
            // Domain States
            var matchState = new MatchState();
            var economyState = new EconomyState(10000);
            var rosterState = new RosterState();
            var boardState = new BoardState(7, 20);
            var waveState = new WaveState();

            _container.RegisterSingleton(matchState);
            _container.RegisterSingleton(economyState);
            _container.RegisterSingleton(rosterState);
            _container.RegisterSingleton(boardState);
            _container.RegisterSingleton(waveState);

            // Queries
            var boardQuery = new BoardQueryService(boardState);
            var economyQuery = new EconomyQueryService(economyState);
            var matchQuery = new MatchQueryService(matchState);
            _container.RegisterSingleton(boardQuery);
            _container.RegisterSingleton(economyQuery);
            _container.RegisterSingleton(matchQuery);

            // Application Use Cases
            var matchUseCase = new MatchUseCase(matchState);
            var buildTowerUseCase = new BuildTowerUseCase(economyState, matchState, rosterState, boardState, boardQuery);
            var waveUseCase = new WaveUseCase(waveState);

            _container.RegisterSingleton(matchUseCase);
            _container.RegisterSingleton(buildTowerUseCase);
            _container.RegisterSingleton(waveUseCase);
        }
    }
}
