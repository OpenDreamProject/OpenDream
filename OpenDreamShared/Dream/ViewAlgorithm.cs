using System;
using System.Collections.Generic;

namespace OpenDreamShared.Dream;

public static class ViewAlgorithm {
    [Flags]
    public enum VisibilityFlags {
        LineOfSight = 1,
        Normal = 2,
        Infrared = 4
    }

    public sealed class Tile {
        public bool Opaque;
        public int Luminosity;
        public int InfraredLuminosity;

        /// <summary>
        /// The distance from the eye in tiles
        /// </summary>
        public int DeltaX, DeltaY;

        private int AbsDeltaX => Math.Abs(DeltaX);
        private int AbsDeltaY => Math.Abs(DeltaY);

        public int MaxDelta => Math.Max(AbsDeltaX, AbsDeltaY);
        public int SumDelta => AbsDeltaX + AbsDeltaY;

        // Values used during calculation
        public int Vis;
        public int Vis2;

        public VisibilityFlags Visibility;
        public bool IsVisible => ((Visibility & VisibilityFlags.Normal) != 0);
    }

    private static readonly List<Tile> BoundaryTiles = new();

    // As described by https://www.byond.com/forum/post/2130277#comment20659267
    // Step 1 is done by the calling code so this can be shared between server and client
    public static void CalculateVisibility(Tile?[,] tiles, bool ignoreLight = false) { // ignore light is used for hearers()
        var width = tiles.GetLength(0);
        var height = tiles.GetLength(1);
        var viewRange = new ViewRange(width, height);

        // Step 2
        // Assume the delta values are done by calling code, and that the eye is in the center
        var (eyeX, eyeY) = viewRange.Center;
        var eye = tiles[eyeX, eyeY];
        var highestMaxDelta = Math.Max(width - eyeX, height - eyeY);
        var highestSumDelta = (width - eyeX) + (height - eyeY);

        // Step 3, Diagonal shadow loop
        // Radiates out from the eye and updates Vis2:
        // 2 2 2 2 2
        // 2 1 1 1 2
        // 2 1   1 2
        // 2 1 1 1 2
        // 2 2 2 2 2
        for (int d = 0; d < highestMaxDelta; d++) {
            var lowerX = eyeX - d - 1;
            var upperX = eyeX + d + 1;

            for (int x = Math.Max(lowerX, 0); x <= Math.Min(upperX, width - 1); x++) {
                var y = eyeY - d - 1;
                var upperY = eyeY + d + 1;
                var skipMiddle = x != lowerX && x != upperX;

                if (y < 0)
                    y = skipMiddle ? upperY : 0;

                while (y <= upperY && y < height) {
                    var tile = tiles[x, y];
                    if (tile != null && CheckNeighborsVis(tiles, true, x, y, d)) {
                        tile.Vis2 = (tile.Opaque) ? -1 : d + 1;
                    }

                    // Jump to the other side if we're not on the left or right
                    if (skipMiddle && y != upperY) {
                        y = upperY;
                    } else {
                        y++;
                    }
                }
            }
        }

        // Step 4, Straight shadow loop
        // Radiates out from the eye in a diamond shape and updates Vis:
        //     2
        //   2 1 2
        // 2 1   1 2
        //   2 1 2
        //     2
        for (int d = 0; d < highestSumDelta; d++) {
            var lowerX = Math.Max(eyeX - d - 1, 0);
            var upperX = Math.Min(eyeX + d + 1, width - 1);

            for (int x = lowerX; x <= upperX; x++) {
                var offsetY = (d + 1) - Math.Abs(eyeX - x);

                UpdateTile(eyeY - offsetY);
                if (offsetY != 0)
                    UpdateTile(eyeY + offsetY);

                continue;

                void UpdateTile(int y) {
                    if (y >= 0 && y < height && tiles[x, y] is { } tile) {
                        if (CheckNeighborsVis(tiles, false, x, y, d)) {
                            if (tile.Opaque) {
                                tile.Vis = -1;
                            } else if (tile.Vis2 != 0) {
                                // Lummox says "set vis=d" but that's a typo
                                tile.Vis = d + 1;
                            }
                        }
                    }
                }
            }
        }

        // Step 5
        if (eye != null)
            eye.Vis = 1;

        // TODO: Step 6, Light loop

        // TODO: Step 7, Infrared sight

        // Step 8
        foreach (var tile in tiles) {
            if (tile == null)
                continue;

            tile.Vis2 = tile.Vis;

            // TODO: Luminosity & lit area check
        }

        // Step 9
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                var tile = tiles[x, y];
                if (tile == null)
                    continue;
                if (!tile.Opaque || tile.Vis != 0)
                    continue;

                // Can't have both east and west visible if you're on the east or west border
                if (x != 0 && x != width - 1) {
                    var east = tiles[x - 1, y];
                    var west = tiles[x + 1, y];

                    if (east != null && west != null) {
                        if (east.Vis != 0 && west.Vis != 0) {
                            // Considered a "wall"
                            BoundaryTiles.Add(tile);

                            continue;
                        }
                    }
                }

                // Can't have both north and south visible if you're on the north or south border
                if (y != 0 && y != height - 1) {
                    var north = tiles[x, y - 1];
                    var south = tiles[x, y + 1];

                    if (north != null && south != null) {
                        if (north.Vis != 0 && south.Vis != 0) {
                            // Considered a "wall"
                            BoundaryTiles.Add(tile);

                            continue;
                        }
                    }
                }

                if (IsCorner(tiles, x, y, 1, 1) ||
                    IsCorner(tiles, x, y, 1, -1) ||
                    IsCorner(tiles, x, y, -1, -1) ||
                    IsCorner(tiles, x, y, -1, 1)) {
                    BoundaryTiles.Add(tile);
                }
            }
        }

        // Make all wall/corner tiles visible
        foreach (var boundary in BoundaryTiles) {
            boundary.Vis = -1;
        }

        BoundaryTiles.Clear();

        // Step 10
        foreach (var tile in tiles) {
            if (tile == null)
                continue;

            if (tile.Vis != 0)
                tile.Visibility |= VisibilityFlags.Normal;
            if (tile.Vis2 != 0)
                tile.Visibility |= VisibilityFlags.LineOfSight;
            if (tile.InfraredLuminosity != 0)
                tile.Visibility |= VisibilityFlags.Infrared;
        }
    }

    /// <summary>
    /// Checks if any of a tile's neighbors have Vis == d or Vis2 == d
    /// </summary>
    private static bool CheckNeighborsVis(Tile?[,] tiles, bool checkingVis2, int x, int y, int d) {
        var width = tiles.GetLength(0);
        var height = tiles.GetLength(1);

        for (int neighborX = Math.Max(x - 1, 0); neighborX < Math.Min(x + 2, width); neighborX++) {
            for (int neighborY = Math.Max(y - 1, 0); neighborY < Math.Min(y + 2, height); neighborY++) {
                if (neighborX == x && neighborY == y) // Not a neighbor
                    continue;

                var tile = tiles[neighborX, neighborY];
                if (tile == null)
                    continue;

                if (checkingVis2 && tile.Vis2 == d)
                    return true;
                if (!checkingVis2 && tile.Vis == d)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether this tile fits the definition of a "corner"
    /// </summary>
    private static bool IsCorner(Tile?[,] tiles, int x, int y, int deltaX, int deltaY) {
        int diagonalX = x + deltaX;
        int diagonalY = y + deltaY;
        if (diagonalX < 0 || diagonalY < 0 || diagonalX >= tiles.GetLength(0) || diagonalY >= tiles.GetLength(1))
            return false; // Out of bounds

        var diagonal = tiles[diagonalX, diagonalY];
        var cardinal1 = tiles[x, diagonalY];
        var cardinal2 = tiles[diagonalX, y];
        if (diagonal == null)
            return false;

        return diagonal.Vis != 0 && cardinal1!.Vis != 0 && cardinal2!.Vis != 0 &&
               cardinal1.Opaque && cardinal2.Opaque && !diagonal.Opaque;
    }
}
