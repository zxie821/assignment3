using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class OccupancyMap{

    private static OccupancyMap instance=null;
    private TerrainManager mTerrainManager;
    private HighResMap mHighResMap;
    private static readonly object padlock = new object();
    private bool solved = false;
    private GameObject[] allCars;
    private int[,] mOccupyMap;
    private int[,] extraCostMap;
    private TerrainInfo mInfo;
    public static OccupancyMap Instance{
        get{
            if (instance==null)
            {
                lock (padlock)
                {
                    if (instance ==null)
                    {
                        instance = new OccupancyMap();
                    }
                }
            }
            return instance;
        }
    }
    public void init(ref TerrainManager terrainManager, ref HighResMap hResMap)
    {
        lock (padlock)
        {
            if (!solved)
            {
                mTerrainManager = terrainManager;
                mInfo = mTerrainManager.myInfo;
                mHighResMap = hResMap;
                initOccupyMap();
                solved = true;
            }
        }
    }
    public int releaseCell(int i, int j){
        mOccupyMap[i,j] = 0;
        return 1;
    }
    public int checkOccupancyint(int i, int j, int iEnd, int jEnd){

        if (
            (i==iEnd && mOccupyMap[iEnd, jEnd] == 1) ||
            (j==jEnd && mOccupyMap[iEnd, jEnd]==2))
        {
            return 1;  // not need to occupy
        }
        return 0;

    }
    public bool occupyCell(int i, int j, int iEnd, int jEnd){
        if(mOccupyMap[iEnd, jEnd]==0)
        {
            lock (padlock)
            {
                if (mOccupyMap[iEnd, jEnd]==0)
                {
                    if (i==iEnd)
                    {
                        mOccupyMap[iEnd, jEnd] = 1;
                    }
                    else
                    {
                        mOccupyMap[iEnd, jEnd] = 2;
                    }
                    return true;  // occupy successfully
                }   
            }
        }
        return false;  // failed
    }
    private void initOccupyMap()
    {
        float[,] traversability = mHighResMap.traversability;
        mOccupyMap = new int[mHighResMap.x_N, mHighResMap.z_N];
        extraCostMap = new int[mHighResMap.x_N, mHighResMap.z_N];
        for (int i = 0; i < mHighResMap.x_N; i++)
        {
            for (int j = 0; j < mHighResMap.z_N; j++)
            {
                if (traversability[i, j] == 0)
                {
                    mOccupyMap[i, j] = 0; // mark traversabile grids with -1
                }
                else{
                    mOccupyMap[i,j] = -1;
                }
                extraCostMap[i,j] = 0;
            }
        }
    }
    
}