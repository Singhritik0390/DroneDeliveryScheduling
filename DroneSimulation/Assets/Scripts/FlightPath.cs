using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlightPath
{
    private ISet<Vector2Int> open;
    private ISet<Vector2Int> closed;
    private Vector2Int start;
    private Vector2Int goal;
    private Dictionary<Vector2Int, Vector2Int> parent;
    private Dictionary<Vector2Int, float> g;
    private Dictionary<Vector2Int, float> f;

    private List<Vector3> path;
    private bool isPathSet = false;
    private float length = 0;
    private bool isLengthSet = false;

    private bool[,] isNFZ;
    private float height;
    private float buildingSizeX;
    private float buildingSizeZ;
    private int heightGain;
    private int heightLoss;

    public FlightPath(Vector2Int start, Vector2Int goal, bool[,] isNFZ, float height, float buildingSizeX, float buildingSizeZ, int heightGain, int heightLoss)
    {
        open = new HashSet<Vector2Int>();
        closed = new HashSet<Vector2Int>();
        g = new Dictionary<Vector2Int, float>();
        f = new Dictionary<Vector2Int, float>();
        parent = new Dictionary<Vector2Int, Vector2Int>();
        path = new List<Vector3>();
        this.start = start;
        this.goal = goal;
        this.isNFZ = isNFZ;
        this.height = height;
        this.buildingSizeX = buildingSizeX;
        this.buildingSizeZ = buildingSizeZ;
        this.heightGain = heightGain;
        this.heightLoss = heightLoss;
    }

    public List<Vector3> GetPath()
    {

        if (isPathSet)
        {
            return path;
        }

        if (lazyThetaStar())
        {
            Vector2Int cur = goal;
            path.Add(getWorldCoords(cur));
            while (parent.ContainsKey(cur) && cur != start)
            {
                cur = parent[cur];
                path.Add(getWorldCoords(cur));
            }
            path.Reverse();
            isPathSet = true;
        }

        return path;
    }

    public float GetPathLength()
    {
        // TODO: Maybe this can be taken directly from g[goal] * 5
        if (isLengthSet)
        {
            return length;
        }

        if (!isPathSet)
        {
            GetPath();
        }

        if (path.Count == 1)
        {
            length = float.PositiveInfinity;
        }
        else
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                length += Vector3.Distance(path[i], path[i + 1]);
            }
            length += (heightGain + heightLoss);
        }

        isLengthSet = true;
        return length;
    }

    public void DrawPath()
    {
        if (!isPathSet)
        {
            GetPath();
        }

        if (path.Count == 1)
        {
            return;
        }
        for (int i = 0; i < path.Count - 1; i++)
        {
            Debug.DrawLine(path[i], path[i + 1], Color.cyan, 60, false);
        }
    }

    private bool lazyThetaStar()
    {

        g.Add(start, 0.0f);
        parent.Add(start, start);
        f.Add(start, h(start));
        open.Add(start);
        
        while (open.Count > 0)
        {
            Vector2Int s = pop();
            setVertex(s);
            if (s == goal)
            {
                return true;
            }
            closed.Add(s);
            HashSet<Vector2Int> neighbours;
            calculateVisibleNeighbours(s, out neighbours, isNFZ);
            
            foreach (Vector2Int n in neighbours)
            {
                if (!closed.Contains(n))
                {
                    if (!open.Contains(n))
                    {
                        g.Add(n, float.PositiveInfinity);
                    }
                    updateVertex(s, n);
                }
            }
        }

        return false;
    }

    private void updateVertex(Vector2Int s, Vector2Int n)
    {
        float gOld = g[n];
        computeCost(s, n);
        if (g[n] < gOld)
        {
            if (open.Contains(n))
            {
                open.Remove(n);
                f.Remove(n);
            }
            open.Add(n);
            f.Add(n, g[n] + h(n));
        }
    }

    private void computeCost(Vector2Int s, Vector2Int n)
    {
        if (g[parent[s]] + Vector2Int.Distance(parent[s], n) < g[n])
        {
            parent[n] = parent[s];
            g[n] = g[parent[s]] + Vector2Int.Distance(parent[s], n);
        }
    }

    private void setVertex(Vector2Int s)
    {
        if (!lineOfSight(parent[s], s))
        {
            HashSet<Vector2Int> closedNeighbours;
            calculateVisibleNeighbours(s, out closedNeighbours, isNFZ);
            
            closedNeighbours.IntersectWith(closed);

            float min = float.PositiveInfinity;
            Vector2Int cur = new Vector2Int();

            foreach (Vector2Int n in closedNeighbours)
            {
                float score = g[n] + Vector2Int.Distance(n, s);
                if (score < min)
                {
                    min = score;
                    cur = n;
                }
            }

            parent[s] = cur;
            g[s] = min;
            
        }
    }

    private bool lineOfSight(Vector2Int s, Vector2Int n)
    {
        Vector3 sPos = getWorldCoords(s);
        Vector3 nPos = getWorldCoords(n);
        int layerMask = 1 << 9;
        return !Physics.Raycast(sPos, nPos - sPos, Vector3.Distance(sPos, nPos), layerMask);
    }

    private Vector2Int pop()
    {
        float min = float.PositiveInfinity;
        Vector2Int cur = new Vector2Int();

        foreach (var item in f)
        {
            if (item.Value < min)
            {
                cur = item.Key;
                min = item.Value;
            }
        }

        f.Remove(cur);
        open.Remove(cur);

        return cur;
    }

    private void calculateVisibleNeighbours(Vector2Int location, out HashSet<Vector2Int> neighbours, bool[,] isNFZ)
    {
        neighbours = new HashSet<Vector2Int>();
        for (int i = -1; i < 2; i++)
        {
            if (location.x + i < 0 || location.x + i >= isNFZ.GetLength(0))
            {
                continue;
            }
            for (int j = -1; j < 2; j++)
            {
                if (location.y + j < 0 || location.y + j >= isNFZ.GetLength(1))
                {
                    continue;
                }
                if (!isNFZ[location.x + i, location.y + j])
                {
                    neighbours.Add(new Vector2Int(location.x + i, location.y + j));
                }

            }
        }
    }

    private float h(Vector2Int vertex)
    {
        return Vector2Int.Distance(vertex, goal);
    }

    private Vector3 getWorldCoords(Vector2Int vertex)
    {
        float x = vertex.x * buildingSizeX;
        float z = vertex.y * buildingSizeZ;
        return new Vector3(x, height, z);
    }

}



