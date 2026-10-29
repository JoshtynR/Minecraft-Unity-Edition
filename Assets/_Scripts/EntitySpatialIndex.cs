using System;
using System.Collections.Generic;
using UnityEngine;

// Shared broad phase; gameplay stores remain responsible for identity and validity.
public sealed class EntitySpatialIndex<T>
{
    private readonly Dictionary<Vector3Int, HashSet<int>> buckets = new Dictionary<Vector3Int, HashSet<int>>();
    private readonly Dictionary<int, Vector3Int> cells = new Dictionary<int, Vector3Int>();
    public int LastCandidates { get; private set; }
    private static Vector3Int Cell(Vector3 position) => Vector3Int.FloorToInt(position * .125f);
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    public void Remove(int id)
    {
        if (!cells.TryGetValue(id, out var cell)) return;
        if (buckets.TryGetValue(cell, out var bucket))
        {
            bucket.Remove(id);
            if (bucket.Count == 0) buckets.Remove(cell);
        }
        cells.Remove(id);
    }
    public void Update(int id, Vector3 position)
    {
        if (!Finite(position.x) || !Finite(position.y) || !Finite(position.z)) { Remove(id); return; }
        var cell = Cell(position);
        if (cells.TryGetValue(id, out var previous) && previous.Equals(cell)) return;
        Remove(id);
        if (!buckets.TryGetValue(cell, out var bucket)) buckets[cell] = bucket = new HashSet<int>();
        bucket.Add(id);
        cells[id] = cell;
    }
    public void Query(Vector3 position, float radius, Dictionary<int, T> entities, List<T> output)
    {
        output.Clear();
        LastCandidates = 0;
        if (!Finite(position.x) || !Finite(position.y) || !Finite(position.z) || !Finite(radius) || radius < 0) return;
        var extent = new Vector3(radius, radius, radius);
        var min = Cell(position - extent);
        var max = Cell(position + extent);
        for (int x = min.x; x <= max.x; x++)
        for (int y = min.y; y <= max.y; y++)
        for (int z = min.z; z <= max.z; z++)
        {
            if (!buckets.TryGetValue(new Vector3Int(x, y, z), out var bucket)) continue;
            foreach (int id in bucket)
            {
                LastCandidates++;
                if (entities.TryGetValue(id, out var entity)) output.Add(entity);
            }
        }
    }
    public void Clear() { buckets.Clear(); cells.Clear(); LastCandidates = 0; }
}
