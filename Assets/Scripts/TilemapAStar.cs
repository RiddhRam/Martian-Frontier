using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Mobile‑friendly A* path‑finder for Unity Tilemaps.
/// Uses a binary‑heap OPEN list (O(log n) push/pop) and re‑uses
/// internal collections to minimise allocations.
/// </summary>
public class TilemapAStar : MonoBehaviour
{
    [Header("Outputs")]
    public List<Vector3> Waypoints = new List<Vector3>();
    public bool PathFound = false;
    public NPCMovement nPCMovement;

    public bool generating = false;

    public MineRenderer mineRenderer;

    /* ---------- reusable scratch data ---------- */
    private readonly BinaryHeap<Node> open = new BinaryHeap<Node>(128);
    private readonly HashSet<Vector3Int> closed = new HashSet<Vector3Int>();
    private readonly Dictionary<Vector3Int, Node> nodePool = new Dictionary<Vector3Int, Node>();

    Color randomColor = Color.red;

    void Start()
    {
        randomColor = Random.ColorHSV();
    }

    /* ---------- const data ---------- */
    private static readonly Vector3Int[] Dir4 =
    {
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(1, 0, 0)
    };

    /// <summary>Attempts to build a path from <paramref name="startWorld"/> to <paramref name="endWorld"/>.</summary>
    public void GeneratePath(Vector3 startWorld, Vector3 endWorld, int width = 1)
    {
        if (!mineRenderer) {
            mineRenderer = nPCMovement.nPCManager.mineRenderer;
        }

        if (generating) {
            return;
        }

        generating = true;

        Waypoints.Clear();
        PathFound = false;
        open.Clear();
        closed.Clear();
        nodePool.Clear();

        Vector2Int startTileMapPos = mineRenderer.CalculateTileMapPos(new((int)startWorld.x, (int)startWorld.y));
        Vector2Int endTileMapPos   = mineRenderer.CalculateTileMapPos(new((int)endWorld.x,   (int)endWorld.y));

        Vector3Int start = mineRenderer.tilemaps[startTileMapPos.x, startTileMapPos.y].WorldToCell(startWorld);
        Vector3Int goal  = mineRenderer.tilemaps[endTileMapPos.x,   endTileMapPos.y]  .WorldToCell(endWorld);

        // Abort early if either point is blocked for the given footprint width.
        if (!IsWalkable(start, width) || !IsWalkable(goal, width)) {
            generating = false;
            return;
        }

        Node startNode = new Node(start, 0, Heuristic(start, goal), null);
        open.Add(startNode);
        nodePool[start] = startNode;

        while (open.Count > 0)
        {
            Node current = open.RemoveFirst();
            closed.Add(current.pos);

            if (current.pos == goal)
            {
                Reconstruct(current);

                // Remove all points in the spawn
                Waypoints.RemoveAll(wp => wp.y > -3);

                PathFound = true;
                generating = false;
                return;
            }

            foreach (Vector3Int dir in Dir4)
            {
                Vector3Int nPos = current.pos + dir;
                if (closed.Contains(nPos) || !IsWalkable(nPos, width)) continue;

                int gCost = current.gCost + 1;

                if (nodePool.TryGetValue(nPos, out Node neighbour))
                {
                    if (gCost < neighbour.gCost)
                    {
                        neighbour.gCost = gCost;
                        neighbour.parent = current;
                        open.UpdateItem(neighbour);        // decrease‑key
                    }
                }
                else
                {
                    neighbour = new Node(nPos, gCost, Heuristic(nPos, goal), current);
                    nodePool[nPos] = neighbour;
                    open.Add(neighbour);
                }
            }
        }

        generating = false;
    }

    /* ---------- helpers ---------- */
    private void Reconstruct(Node goalNode)
    {
        Waypoints.Clear();
        for (Node cur = goalNode; cur != null; cur = cur.parent)
        {
            Vector2Int mp = mineRenderer.CalculateTileMapPos(new(cur.pos.x, cur.pos.y));
            Waypoints.Add(mineRenderer.tilemaps[mp.x, mp.y].GetCellCenterWorld(cur.pos));
        }
        Waypoints.Reverse();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = randomColor;

        foreach (Vector3 waypoint in Waypoints)
        {
            Gizmos.DrawSphere(waypoint, 1);
        }
    }

    private static int Heuristic(Vector3Int a, Vector3Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    private bool IsWalkable(Vector3Int cell, int width)
    {
        int half = width / 2;
        for (int x = -half; x <= half; x++)
            for (int y = -half; y <= half; y++)
            {
                Vector3Int check = new Vector3Int(cell.x + x, cell.y + y, cell.z);
                Vector2Int mp = mineRenderer.CalculateTileMapPos(new(check.x, check.y));
                if (!mineRenderer.tilemaps[mp.x, mp.y]) {
                    return false;
                }

                if (mineRenderer.tilemaps[mp.x, mp.y].HasTile(check)) return false;
            }
        return true;
    }

    /* ---------- nested types ---------- */
    private class Node : IHeapItem<Node>
    {
        public Vector3Int pos;
        public int gCost;
        public int hCost;
        public int fCost => gCost + hCost;
        public Node parent;

        /* heap plumbing */
        public int HeapIndex { get; set; }

        public Node(Vector3Int p, int g, int h, Node parent)
        {
            pos = p;
            gCost = g;
            hCost = h;
            this.parent = parent;
        }

        public int CompareTo(Node other)
            => fCost == other.fCost ? hCost.CompareTo(other.hCost) : fCost.CompareTo(other.fCost);
    }

    /// <summary>Binary min‑heap with O(log n) Add / RemoveFirst / UpdateItem.</summary>
    private class BinaryHeap<T> where T : IHeapItem<T>
    {
        private List<T> items;

        public int Count => items.Count;

        public BinaryHeap(int capacity) => items = new List<T>(capacity);

        public void Clear() => items.Clear();

        public void Add(T item)
        {
            item.HeapIndex = items.Count;
            items.Add(item);
            SortUp(item);
        }

        public T RemoveFirst()
        {
            T first = items[0];
            int lastIndex = items.Count - 1;

            if (lastIndex == 0)          // only one item in the heap
            {
                items.Clear();           // just empty the list
                return first;
            }

            items[0] = items[lastIndex]; // move last to root
            items[0].HeapIndex = 0;
            items.RemoveAt(lastIndex);

            SortDown(items[0]);          // re‑heapify
            return first;
        }


        public void UpdateItem(T item) => SortUp(item);

        /* ---- private heap helpers ---- */
        private void SortUp(T item)
        {
            while (true)
            {
                int parentIndex = (item.HeapIndex - 1) >> 1;
                if (parentIndex < 0) break;

                T parentItem = items[parentIndex];
                if (item.CompareTo(parentItem) < 0)
                {
                    Swap(item, parentItem);
                }
                else break;
            }
        }

        private void SortDown(T item)
        {
            while (true)
            {
                int left = (item.HeapIndex << 1) + 1;
                int right = left + 1;

                if (left >= items.Count) break;

                int swapIndex = right < items.Count && items[right].CompareTo(items[left]) < 0 ? right : left;

                if (items[swapIndex].CompareTo(item) < 0)
                {
                    Swap(item, items[swapIndex]);
                }
                else break;
            }
        }

        private void Swap(T a, T b)
        {
            items[a.HeapIndex] = b;
            items[b.HeapIndex] = a;
            int temp = a.HeapIndex;
            a.HeapIndex = b.HeapIndex;
            b.HeapIndex = temp;
        }
    }

    /// <summary>Interface for heap items so the heap can update indices in‑place.</summary>
    private interface IHeapItem<in T> : System.IComparable<T>
    {
        int HeapIndex { get; set; }
    }
}
