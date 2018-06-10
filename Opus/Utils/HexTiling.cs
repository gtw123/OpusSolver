using System;
using System.Collections.Generic;

namespace Opus
{
    /// <summary>
    /// Partitions a hex grid into large hexagonal tiles of equal size.
    /// For Size = 2, the tiling looks something like this:
    ///                     (0, 1) 
    ///                     
    ///                     . . . 
    ///                    . . . .
    ///             o o o . . . . .
    ///            o o o o . . . . + + +
    ///  (-1, 1)  o o o o o . . . + + + +  (1, 0)
    ///            o o o o * * * + + + + +
    ///             o o o * * * * + + + +
    ///            + + + * * * * * + + +
    ///           + + + + * * * * o o o
    ///          + + + + + * * * o o o o
    ///           + + + + . . . o o o o o
    ///   (-1, 0)  + + + . . . . o o o o  (1, -1)
    ///                 . . . . . o o o
    ///                  . . . .  
    ///                   . . .
    ///                   
    ///                   (0, -1)
    ///          
    /// </summary>
    public class HexTiling
    {
        /// <summary>
        /// The "radius" of each tile, excluding the middle cell.
        /// Equivalent to (length - 1) of the side of each tile.
        /// </summary>
        public int Size { get; private set; }

        public HexTiling(int size)
        {
            if (size < 1)
            {
                throw new ArgumentException("size must be greater than 0.");
            }

            Size = size;
        }

        public Vector2 GetCenterCell(Vector2 tile)
        {
            return new Vector2((Size + 1) * tile.X - Size * tile.Y,
                Size * tile.X + (2 * Size + 1) * tile.Y);
        }

        public IEnumerable<Vector2> GetCellsForTile(Vector2 tile)
        {
            var center = GetCenterCell(tile);
            for (int y = -Size; y <= Size; y++)
            {
                for (int x = -Size; x <= Size; x++)
                {
                    if (x + y >= -Size && x + y <= Size)
                    {
                        yield return center.Add(x, y);
                    }
                }
            }
        }

        public Vector2 GetTileForCell(Vector2 cell)
        {
            //  Do the inverse transform of GetCenterCell
            double determinant = (Size + 1) * (2 * Size + 1) + Size * Size;
            var tile = new Vector2((2 * Size + 1) * cell.X + Size * cell.Y,
                - Size * cell.X + (Size + 1) * cell.Y);
            tile.X = (int)Math.Round(tile.X / determinant);
            tile.Y = (int)Math.Round(tile.Y / determinant);

            // Fix up cells near the edge of a tile
            var offset = cell - GetCenterCell(tile);
            if (offset.X <= 0 && offset.Y > Size)
            {
                tile.Y += 1;
            }
            else if (offset.X > 0 && offset.X + offset.Y > Size)
            {
                tile.X += 1;
            }
            else if (offset.X >= 0 && offset.Y < -Size)
            {
                tile.Y -= 1;
            }
            else if (offset.X < 0 && offset.X + offset.Y < -Size)
            {
                tile.X -= 1;
            }

            return tile;
        }
    }
}
