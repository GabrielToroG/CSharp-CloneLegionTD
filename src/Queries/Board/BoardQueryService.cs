namespace LegionTDClone.Queries.Board
{
    public class BoardQueryService
    {
        private readonly Domain.Board.BoardState _boardState;

        public BoardQueryService(Domain.Board.BoardState boardState)
        {
            _boardState = boardState;
        }

        public bool CanPlaceAt(int x, int z)
        {
            return !_boardState.IsOccupied(x, z);
        }
    }
}
