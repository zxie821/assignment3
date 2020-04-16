using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class UpScaledMap
{
    private TerrainInfo rawTerrainInfo;
    private int mUpRatio;
    private int printFlag;
    public int x_N;
    public int z_N;
    public float[,] traversability;
    private int remainArea;
    private readonly int[,] mOffset = new int[,]{{1,0},{0,-1},{0,1},{-1,0}};
    private List<Tuple<int,int>> mDFSPath;

    public UpScaledMap(ref TerrainInfo info, List<Tuple<int,int>> path, int upRatio){
        rawTerrainInfo = info;
        mDFSPath = path;
        mUpRatio = upRatio;
        remainArea = 0;
        x_N = rawTerrainInfo.x_N * mUpRatio;
        z_N = rawTerrainInfo.z_N * mUpRatio;
        traversability = new float[x_N, z_N];
        initMap();
        buildWall();
    }
    private void buildWall(){
        for (int i = 0; i < mDFSPath.Count-1; i++)
        {
            updateWithTwoRawPoint(mDFSPath[i], mDFSPath[i+1]);
        }
        for (int x = 0; x < x_N; x++)
        {
            for (int z = 0; z < z_N; z++)
            {
                if (traversability[x,z]==0f)
                {
                    remainArea++;
                }
            }
        }
    }
    public List<Vector3> getV3Path(){
        int indexI = get_i_index(rawTerrainInfo.get_x_pos(mDFSPath[0].Item1));
        int indexJ = get_j_index(rawTerrainInfo.get_z_pos(mDFSPath[0].Item2));
        
        List<Vector3> path = new List<Vector3>();
        var neigbour = getNeigbour(indexI, indexJ);
        int mOffsetType = neigbour[0];
        indexI += mOffset[mOffsetType,0];
        indexJ += mOffset[mOffsetType,1];
        path.Add(new Vector3(get_x_pos(indexI), 0f, get_z_pos(indexJ)));
        traversability[indexI,indexJ] = 1f;
        remainArea--;
        int whileCounter = 0;
        while(remainArea>0 && whileCounter < 10000){
            whileCounter++;
            neigbour = getNeigbour(indexI, indexJ);
            if (neigbour.Count==1)
            {
                mOffsetType = neigbour[0];
                indexI += mOffset[mOffsetType,0];
                indexJ += mOffset[mOffsetType,1];
                path.Add(new Vector3(get_x_pos(indexI), 0f, get_z_pos(indexJ)));
                traversability[indexI,indexJ] = 1f;
                remainArea--;
            }
            else if(neigbour.Count>1)
            {
                bool ifAddPoint = false;
                foreach (var t in neigbour)
                {
                    int pIndexI_P1 = rawTerrainInfo.get_i_index(get_x_pos(indexI));
                    int pIndexJ_P1 = rawTerrainInfo.get_j_index(get_z_pos(indexJ));
                    int pIndexI_P2 = rawTerrainInfo.get_i_index(get_x_pos(indexI+mOffset[t,0]));
                    int pIndexJ_P2 = rawTerrainInfo.get_j_index(get_z_pos(indexJ+mOffset[t,1]));
                    if (pIndexI_P1==pIndexI_P2 && pIndexJ_P1==pIndexJ_P2)
                    {
                        indexI += mOffset[t,0];
                        indexJ += mOffset[t,1];
                        mOffsetType = t;
                        path.Add(new Vector3(get_x_pos(indexI), 0f, get_z_pos(indexJ)));
                        traversability[indexI,indexJ] = 1f;
                        remainArea--;
                        ifAddPoint = true;
                        break;
                    }
                }
                if (!ifAddPoint && neigbour.Exists( x=>x==mOffsetType))
                {
                    indexI += mOffset[mOffsetType,0];
                    indexJ += mOffset[mOffsetType,1];
                    path.Add(new Vector3(get_x_pos(indexI), 0f, get_z_pos(indexJ)));
                    traversability[indexI,indexJ] = 1f;
                    remainArea--;
                    ifAddPoint = true;
                }
            }
            else{
                break;
            }
        }
        path = splitPath(path);
        for (int i = 0; i < path.Count; i++)
        {
            float ori_center_x = rawTerrainInfo.get_x_pos(rawTerrainInfo.get_i_index(path[i].x));
            float ori_center_z = rawTerrainInfo.get_z_pos(rawTerrainInfo.get_j_index(path[i].z));
            Vector3 centerV3 = new Vector3(ori_center_x, 0f, ori_center_z);
            path[i] += (centerV3-path[i])*0.5f;
        }
        return path;
    }
    private List<Vector3> splitPath(List<Vector3> path, int tryIndex=1)
    {
        
        int pathLength = path.Count;
        if (tryIndex==mDFSPath.Count)
        {
            throw new Exception("split path error");
        }
        var firstHalf = path.GetRange(0,pathLength/2);
        var secondHalf = path.GetRange(pathLength/2, pathLength/2);
        Vector3 firstPoint = new Vector3();
        Vector3 secondPoint=new Vector3();

        firstPoint.x = rawTerrainInfo.get_x_pos(mDFSPath[tryIndex].Item1);
        firstPoint.z = rawTerrainInfo.get_z_pos(mDFSPath[tryIndex].Item2);
        secondPoint.x = rawTerrainInfo.get_x_pos(mDFSPath[tryIndex+1].Item1);
        secondPoint.z = rawTerrainInfo.get_z_pos(mDFSPath[tryIndex+1].Item2);

        Vector3 forwardDirection = secondPoint - firstPoint;
        Vector3 rightDirection = Vector3.Cross(forwardDirection, Vector3.up);
        secondPoint = secondPoint - rightDirection.normalized * 5f;
        if (firstHalf.Contains(secondPoint))
        {
            return firstHalf;
        }
        else if(secondHalf.Contains(secondPoint))
        {
            secondHalf.Reverse();
            return secondHalf;
        }
        return splitPath(path, tryIndex+2);

    }
    private List<int> getNeigbour(int i, int j){
        List<int> neigbour = new List<int>();
        for (int idx = 0; idx < mOffset.GetLength(0); idx++)
        {
            if (traversability[i+mOffset[idx,0], j+mOffset[idx, 1]]==0f)
            {
                neigbour.Add(idx);
            }
        }
        return neigbour;
    }
    private void updateWithTwoRawPoint(Tuple<int,int> p1, Tuple<int,int> p2){
        int indexIP1 = get_i_index(rawTerrainInfo.get_x_pos(p1.Item1));
        int indexJP1 = get_i_index(rawTerrainInfo.get_z_pos(p1.Item2));
        int indexIP2 = get_i_index(rawTerrainInfo.get_x_pos(p2.Item1));
        int indexJP2 = get_i_index(rawTerrainInfo.get_z_pos(p2.Item2));
        for (int i = Math.Min(indexIP2,indexIP1); i <= Math.Max(indexIP2,indexIP1); i++)
        {
            for (int j = Math.Min(indexJP2,indexJP1); j <= Math.Max(indexJP2,indexJP1); j++)
            {
                traversability[i,j] = 1f;
            }
        }
    }
    private void initMap()
    {
        foreach (var p in mDFSPath)
        {
            for (int i = 0; i < mUpRatio; i++)
            {
                for (int j = 0; j < mUpRatio; j++)
                {
                    traversability[p.Item1*mUpRatio + i, p.Item2*mUpRatio + j] = 1f;
                }
            }
        }
        for (int x = 0; x < x_N; x++)
        {
            for (int z = 0; z < z_N; z++)
            {
                if (traversability[x,z]==1f)
                {
                    traversability[x, z] = 0f;
                }
                else
                {
                    traversability[x,z] = 1f;
                }
            }
        }
        //printMap();
    }
    
    public void printMap()
    {
        float step = (rawTerrainInfo.x_high - rawTerrainInfo.x_low) / x_N;
        float[] row = new float[z_N];
        for (int i = 0; i < x_N; i++)
        {
            for (int j = 0; j < z_N; j++)
            {
                if (traversability[i,j]==1f)
                {
                    Debug.DrawLine(new Vector3(get_x_pos(i)+step/2,0f,get_z_pos(j)-step/2), new Vector3(get_x_pos(i)-step/2,0f,get_z_pos(j)+step/2),Color.black,100f);
                }
            }
            
        }
    }
    public float get_x_pos(int i)
    {
        float step = (rawTerrainInfo.x_high - rawTerrainInfo.x_low) / x_N;
        return rawTerrainInfo.x_low + step / 2 + step * i;
    }
    public float get_z_pos(int j)
    {
        float step = (rawTerrainInfo.z_high - rawTerrainInfo.z_low) / z_N;
        return rawTerrainInfo.z_low + step / 2 + step * j;
    }
    public int get_i_index(float x)
    {
        int index = (int) Mathf.Floor(x_N * (x - rawTerrainInfo.x_low) / (rawTerrainInfo.x_high - rawTerrainInfo.x_low));
        if (index < 0 || index > x_N - 1)
        {
            throw new IndexOutOfRangeException();
        }
        return index;

    }
    public int get_j_index(float z) // get index of given coordinate
    {
        int index = (int)Mathf.Floor(z_N * (z - rawTerrainInfo.z_low) / (rawTerrainInfo.z_high - rawTerrainInfo.z_low));
        if (index < 0 || index > z_N - 1)
        {
            throw new IndexOutOfRangeException();
        }
        return index;
    }
}
