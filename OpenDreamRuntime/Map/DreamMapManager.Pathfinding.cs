using System.Diagnostics.CodeAnalysis;
using OpenDreamRuntime.Procs.Native;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Map;

public partial class DreamMapManager {
    private sealed class PathFindNode : IDisposable, IEquatable<PathFindNode> {
        private static readonly Stack<PathFindNode> Pool = new();

        public int X, Y;
        public PathFindNode? Parent;
        public int NeededSteps;

        public static PathFindNode GetNode(int x, int y) {
            if (!Pool.TryPop(out var node)) {
                node = new();
            }

            node.Parent = null;
            node.X = x;
            node.Y = y;
            node.NeededSteps = 0;
            return node;
        }

        public void Dispose() {
            Pool.Push(this);
        }

        public bool Equals(PathFindNode? other) {
            if (other is null)
                return false;
            return X == other.X && Y == other.Y;
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode() {
            return HashCode.Combine(X, Y);
        }
    }

    public IEnumerable<AtomDirection> CalculateSteps((int X, int Y, int Z) loc, (int X, int Y, int Z) dest, int distance) {
        int z = loc.Z;
        if (z != dest.Z) // Different Z-levels are unreachable
            yield break;

        HashSet<PathFindNode> explored = new();
        Queue<PathFindNode> toExplore = new();

        toExplore.Enqueue(PathFindNode.GetNode(loc.X, loc.Y));

        void Explore(PathFindNode current, int offsetX, int offsetY, int currentStep) {
            var next = PathFindNode.GetNode(current.X + offsetX, current.Y + offsetY);
            if (explored.Contains(next))
                return;
            if (!TryGetCellAt(new(next.X, next.Y), z, out var cell) || cell.Turf.IsDense)
                return;

            if (!toExplore.Contains(next))
                toExplore.Enqueue(next);
            else if (next.NeededSteps >= currentStep + 1)
                return;

            next.NeededSteps = currentStep + 1;
            next.Parent = current;
        }

        while (toExplore.TryDequeue(out var node)) {
            var distX = node.X - dest.X;
            var distY = node.Y - dest.Y;
            if (Math.Sqrt(distX * distX + distY * distY) <= distance) { // Path to the destination was found
                Stack<AtomDirection> path = new();

                while (node.Parent != null) {
                    var stepDir = DreamProcNativeHelpers.GetDir((node.Parent.X, node.Parent.Y, z), (node.X, node.Y, z));

                    node = node.Parent;
                    path.Push(stepDir);
                }

                while (path.TryPop(out var step)) {
                    yield return step;
                }

                break;
            }

            explored.Add(node);
            Explore(node,  1,  0, node.NeededSteps);
            Explore(node,  1,  1, node.NeededSteps);
            Explore(node,  0,  1, node.NeededSteps);
            Explore(node, -1,  1, node.NeededSteps);
            Explore(node, -1,  0, node.NeededSteps);
            Explore(node, -1, -1, node.NeededSteps);
            Explore(node,  0, -1, node.NeededSteps);
            Explore(node,  1, -1, node.NeededSteps);
        }

        foreach (var node in explored)
            node.Dispose();
        foreach (var node in toExplore)
            node.Dispose();
    }
}
