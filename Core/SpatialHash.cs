using System.Numerics;

namespace CrowdCheckpointSim.Core;

/// <summary>
/// Uniform-grid spatial index used to keep local SFM neighbourhood queries close to O(n).
/// </summary>
internal sealed class SpatialHash
{
    private readonly float _cellSize;
    private readonly Dictionary<(int X, int Y), List<int>> _cells = new();

    public SpatialHash(float cellSize)
    {
        _cellSize = Math.Max(0.25f, cellSize);
    }

    public void Rebuild(IReadOnlyList<Pedestrian> pedestrians)
    {
        _cells.Clear();
        for (int i = 0; i < pedestrians.Count; i++)
        {
            (int X, int Y) cell = CellFor(pedestrians[i].Position);
            if (!_cells.TryGetValue(cell, out List<int>? indices))
            {
                indices = new List<int>();
                _cells.Add(cell, indices);
            }

            indices.Add(i);
        }
    }

    public IEnumerable<int> Query(Vector2 point, float radius)
    {
        int range = Math.Max(1, (int)MathF.Ceiling(radius / _cellSize));
        (int cx, int cy) = CellFor(point);

        for (int y = cy - range; y <= cy + range; y++)
        {
            for (int x = cx - range; x <= cx + range; x++)
            {
                if (_cells.TryGetValue((x, y), out List<int>? indices))
                {
                    foreach (int index in indices)
                    {
                        yield return index;
                    }
                }
            }
        }
    }

    private (int X, int Y) CellFor(Vector2 point) =>
        ((int)MathF.Floor(point.X / _cellSize), (int)MathF.Floor(point.Y / _cellSize));
}
