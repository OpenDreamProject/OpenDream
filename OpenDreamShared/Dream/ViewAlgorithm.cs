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
        public bool Dense;
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

        public bool IsAudible = false;
    }

    private static readonly List<Tile> BoundaryTiles = new();

// heavily based on CalculateVisibility, but opacity is replaced with density.
    public static void CalculateAudibility(Tile?[,] tiles) {
        var width = tiles.GetLength(0);
        var height = tiles.GetLength(1);
        Tile? ear = null;

        // Step 2
        var highestMaxDelta = 0;
        var highestSumDelta = 0;
        foreach (var tile in tiles) {
            if (tile == null)
                continue;

            highestMaxDelta = Math.Max(highestMaxDelta, tile.MaxDelta);
            highestSumDelta = Math.Max(highestSumDelta, tile.SumDelta);

            tile.Vis = 0; // half wanted to rename these but there's no point really.
            tile.Vis2 = 0;
            tile.IsAudible = false;

            if (tile.DeltaX == 0 && tile.DeltaY == 0)
                ear = tile;
        }

        // TODO: Lummox mentions an optimization for viewers() in step 3 and 4. Probably worthwhile.

        // Step 3, Diagonal shadow loop
        for (int d = 0; d < highestMaxDelta; d++) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    var tile = tiles[x, y];
                    if (tile == null)
                        continue;

                    if (tile.MaxDelta == d + 1 && CheckNeighborsVis(tiles, true, x, y, d)) {
                        tile.Vis2 = (tile.Dense) ? -1 : d + 1;
                    }
                }
            }
        }

        // Step 4, Straight shadow loop
        for (int d = 0; d < highestSumDelta; d++) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    var tile = tiles[x, y];
                    if (tile == null)
                        continue;

                    if (tile.SumDelta == d + 1 && CheckNeighborsVis(tiles, false, x, y, d)) {
                        if (tile.Dense) {
                            tile.Vis = -1;
                        } else if (tile.Vis2 != 0) {
                            // Lummox says "set vis=d" but I think this is a typo?
                            tile.Vis = d + 1;
                        }
                    }
                }
            }
        }

        // Step 5
        if (ear != null)
            ear.Vis = 1;

        // we skip steps 6 and 7 because hearing works in the dark

        // Step 8
        foreach (var tile in tiles) {
            if (tile == null)
                continue;

            tile.Vis2 = tile.Vis;

        }

        // Step 9
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                var tile = tiles[x, y];
                if (tile == null)
                    continue;
                if (!tile.Dense || tile.Vis != 0)
                    continue;

                // Can't have both east and west audible if you're on the east or west border
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

                // Can't have both north and south audible if you're on the north or south border
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

        // Make all wall/corner tiles audible
        foreach (var boundary in BoundaryTiles) {
            boundary.Vis = -1;
        }

        BoundaryTiles.Clear();

        // Step 10
        foreach (var tile in tiles) {
            if (tile == null)
                continue;

            if (tile.Vis2 != 0) // using the equivalent of LOS_VIS.
                tile.IsAudible = true;
        }
    }

    // As described by https://www.byond.com/forum/post/2130277#comment20659267
    // Step 1 is done by the calling code so this can be shared between server and client
    public static void CalculateVisibility(Tile?[,] tiles) {
        var width = tiles.GetLength(0);
        var height = tiles.GetLength(1);
        Tile? eye = null;

        // Step 2
        var highestMaxDelta = 0;
        var highestSumDelta = 0;
        foreach (var tile in tiles) {
            if (tile == null)
                continue;

            highestMaxDelta = Math.Max(highestMaxDelta, tile.MaxDelta);
            highestSumDelta = Math.Max(highestSumDelta, tile.SumDelta);

            tile.Vis = 0;
            tile.Vis2 = 0;
            tile.Visibility = 0;

            if (tile.DeltaX == 0 && tile.DeltaY == 0)
                eye = tile;
        }

        // TODO: Lummox mentions an optimization in step 3 and 4. Probably worthwhile.

        // Step 3, Diagonal shadow loop
        for (int d = 0; d < highestMaxDelta; d++) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    var tile = tiles[x, y];
                    if (tile == null)
                        continue;

                    if (tile.MaxDelta == d + 1 && CheckNeighborsVis(tiles, true, x, y, d)) {
                        tile.Vis2 = (tile.Opaque) ? -1 : d + 1;
                    }
                }
            }
        }

        // Step 4, Straight shadow loop
        for (int d = 0; d < highestSumDelta; d++) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    var tile = tiles[x, y];
                    if (tile == null)
                        continue;

                    if (tile.SumDelta == d + 1 && CheckNeighborsVis(tiles, false, x, y, d)) {
                        if (tile.Opaque) {
                            tile.Vis = -1;
                        } else if (tile.Vis2 != 0) {
                            // Lummox says "set vis=d" but I think this is a typo?
                            tile.Vis = d + 1;
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
