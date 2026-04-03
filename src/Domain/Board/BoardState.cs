using System;

namespace LegionTDClone.Domain.Board
{
    public class BoardState
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        
        // Logical representation of the board, e.g. holding unit IDs or status
        // A true value here means occupied. A full entity system would use EntityId.
        public bool[,] OccupancyGrid { get; private set; }

        public BoardState(int width = 7, int height = 20)
        {
            Width = width;
            Height = height;
            OccupancyGrid = new bool[Width, Height];
        }

        public bool IsOccupied(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Height)
                return true; // Out of bounds are considered "occupied" / not placable

            return OccupancyGrid[x, z];
        }

        public bool TryOccupy(int x, int z)
        {
            if (IsOccupied(x, z)) return false;

            OccupancyGrid[x, z] = true;
            return true;
        }

        public bool TryFree(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Height)
                return false;

            if (!OccupancyGrid[x, z]) return false;

            OccupancyGrid[x, z] = false;
            return true;
        }

        public void ClearBoard()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Height; z++)
                {
                    OccupancyGrid[x, z] = false;
                }
            }
        }
    }
}
