using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
public class AStar
{
    private float[,] traversabilityMatrix;
    private readonly int[,] mOffset = new int[,]{{1,0},{-1,0},{0,1},{0,-1}};

    public AStar(float[,] traversabilityMatrix)
    {
        this.traversabilityMatrix = traversabilityMatrix;
    }

    public List<Tuple<int,int>> ComputePath(int iStart, int jStart, int iEnd, int jEnd)
    {
        List<Tuple<int, int>> toExplore = new List<Tuple<int, int>>();
        toExplore.Add(new Tuple<int, int>(iStart, jStart));
        Dictionary<Tuple<int, int>, Tuple<int, int>> ancestors = new Dictionary<Tuple<int, int>, Tuple<int, int>>();
        ancestors.Add(toExplore[0], null);
        bool[,] explored = new bool[traversabilityMatrix.GetLength(0), traversabilityMatrix.GetLength(1)];
        int[,] cost = new int[traversabilityMatrix.GetLength(0), traversabilityMatrix.GetLength(1)];
        for(int i=0; i<cost.GetLength(0); i++)
        {
            for(int j=0; j < cost.GetLength(1); j++)
            {
                explored[i, j] = (traversabilityMatrix[i, j] != 0f);
                cost[i, j] = 0;
            }
        }
        explored[iStart, jStart] = true;
        bool pathFound = (iStart == iEnd && jStart == jEnd);
        Tuple<int, int> lastNode = pathFound ? new Tuple<int,int>(iEnd,jEnd) : null;
        while (toExplore.Count>0 && !pathFound)
        {
            var currentNode = toExplore[0];
            toExplore.RemoveAt(0);
            for (int t = 0; t < mOffset.GetLength(0); t++)
            {
                int neighborsI = currentNode.Item1 + mOffset[t,0];
                int neighborsJ = currentNode.Item2 + mOffset[t,1];
                if (!explored[neighborsI, neighborsJ])
                {
                    Tuple<int, int> neighbor = new Tuple<int, int>(neighborsI, neighborsJ);
                    explored[neighborsI, neighborsJ] = true;
                    if (neighborsI == iEnd && neighborsJ == jEnd)
                    {
                        pathFound = true;
                        lastNode = neighbor;
                    }
                    cost[neighborsI, neighborsJ] = cost[currentNode.Item1, currentNode.Item2] + 1;
                    toExplore.Add(neighbor);
                    ancestors.Add(neighbor, currentNode);
                }
            }
            toExplore.Sort((a, b) =>
            {
                int costA = cost[a.Item1, a.Item2] + Math.Abs(a.Item1 - iEnd) + Math.Abs(a.Item2 - jEnd);
                int costB = cost[b.Item1, b.Item2] + Math.Abs(b.Item1 - iEnd) + Math.Abs(b.Item2 - jEnd);
                return  costA.CompareTo(costB);
            });
        }
        List<Tuple<int, int>> path = new List<Tuple<int, int>>();
        if (!pathFound)
            Debug.Log($"AStar failed");
        while (lastNode != null)
        {
            path.Insert(0, lastNode);
            lastNode = ancestors[lastNode];
        }

        return path;
    }
}
