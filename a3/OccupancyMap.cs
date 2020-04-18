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
    private GameObject[] mFriends;
    private int[,] mOccupyMap;
    private float cdTime = 1f;
    private float[,] coldDownMap;
    private int maxRunningAgent=50;
    public bool[] playerStatus;
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
    public void init(ref TerrainManager terrainManager, ref HighResMap hResMap, GameObject[] friends)
    {
        lock (padlock)
        {
            if (!solved)
            {
                mTerrainManager = terrainManager;
                mInfo = mTerrainManager.myInfo;
                mHighResMap = hResMap;
                mFriends = friends;
                playerStatus=new bool[100];
                initOccupyMap();
                solved = true;
            }
        }
    }
    private int numOfFriend(int i, int j, Vector3 myPosition)
    {
        int counter=0;
        for (int idx = 0; idx < mFriends.Length; idx++)
        {
            var friend = mFriends[idx];
            var friendLocation = friend.transform.position;
            friendLocation.y = 0;
            if(i==mHighResMap.get_i_index(friendLocation.x) &&
                j==mHighResMap.get_j_index(friendLocation.z) &&
                playerStatus[idx])
            {
                counter++;
            } 
        }
        return counter;
    }
    private bool occupy(int i, int j, int label)
    {
        if (Time.time - coldDownMap[i, j]>cdTime)
        {
            mOccupyMap[i, j] = label;
            coldDownMap[i, j] = Time.time;
            return true;  // occupy successfully
        }
        return false;
    }
    public bool getPermission(int playerIndex)
    {
        lock(padlock){
            if (maxRunningAgent>0)
            {
                if (UnityEngine.Random.Range(0f,1f)>0.5f)
                {
                    return false;
                }
                maxRunningAgent--;
                playerStatus[playerIndex] = true;
                return true;
            }
            return false;
        }
    }
    public void releasePermission(int playerIndex)
    {
        lock (padlock)
        {
            playerStatus[playerIndex] = false;
            maxRunningAgent++;
        }
    }
    private int getLabel(int i, int j, int iEnd, int jEnd)
    {
        if (i==iEnd)
        {
            if (j>jEnd)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }
        else
        {
            if (i>iEnd)
            {
                return 3;
            }
            else
            {
                return 4;
            }
        }
        throw new Exception("Error 1");
    }
    private int getLabel(int i, int j, int iEnd, int jEnd,int iEndNext, int jEndNext)
    {
        var firstLabel = getLabel(i,j,iEnd, jEnd);
        if (iEndNext==-1||jEndNext==-1)  // 等于-1就不能算label了
        {
            return firstLabel;
        }
        var secondLabel = getLabel(iEnd, jEnd, iEndNext, jEndNext);
        if (firstLabel == secondLabel)
        {
            return firstLabel;
        }
        return firstLabel*10 + secondLabel;
    }
    public int checkOccupancy(int i, int j, int iEnd, int jEnd,int iEndNext, int jEndNext){
        int tempLabel = getLabel(i,j,iEnd,jEnd, iEndNext, jEndNext);
        int[,] pairs = {{1,2},{3,4},{14,32},{42,13},{41,23},{24,31}};
        if (tempLabel == mOccupyMap[iEnd, jEnd])
        {
            //coldDownMap[iEnd, jEnd] = Time.time;
            return 1;  // match
        }
        for (int idx = 0; idx < pairs.GetLength(0); idx++)
        {
            if((tempLabel==pairs[idx,0] && mOccupyMap[iEnd,jEnd]==pairs[idx,1])||
            (tempLabel==pairs[idx,1] && mOccupyMap[iEnd,jEnd]==pairs[idx,0]))
            {
                //coldDownMap[iEnd, jEnd] = Time.time;
                return 2;
            }
        }
        return 0;
    }
    
    public bool occupyCell(int i, int j, int iEnd, int jEnd,int iEndNext, int jEndNext, Vector3 myPosition){
        if (numOfFriend(iEnd, jEnd, myPosition)==0)
        {
            lock (padlock)
            {
                if (numOfFriend(iEnd, jEnd, myPosition)==0)
                {
                    return occupy(iEnd, jEnd, getLabel(i,j,iEnd,jEnd, iEndNext, jEndNext));
                }
            }
        }
        return false;  // failed
    }

    public bool occupyCell(Vector3 node, List<Tuple<int,int>> mPathII, Vector3 myPosition)
    {
        var i1 = mHighResMap.get_i_index(node.x);
        var j1 = mHighResMap.get_j_index(node.z);
        int originalIndex = -1;
        for (int i = 0; i < mPathII.Count; i++)
        {
            if (mPathII[i].Item1==i1 && mPathII[i].Item2==j1)
            {
                originalIndex = i;
            }
        }
        if (originalIndex==-1)
        {
            throw new Exception("Cannot find node!");
        }
        if (originalIndex==mPathII.Count - 1)
        {
            return true;
        }
        
        int i2 = mPathII[originalIndex+1].Item1;
        int j2 = mPathII[originalIndex+1].Item2;
        int i3 = -1;
        int j3 = -1;
        if (originalIndex<mPathII.Count - 2)
        {
            i3 = mPathII[originalIndex+2].Item1;
            j3 = mPathII[originalIndex+2].Item2;
        }
        return occupyCell(i1,j1,i2,j2,i3,j3, myPosition);
    }
    public int checkOccupancy(Vector3 node, List<Tuple<int,int>> mPathII)
    {
        var i1 = mHighResMap.get_i_index(node.x);
        var j1 = mHighResMap.get_j_index(node.z);
        int originalIndex = -1;
        for (int i = 0; i < mPathII.Count; i++)
        {
            if (mPathII[i].Item1==i1 && mPathII[i].Item2==j1)
            {
                originalIndex = i;
            }
        }
        if (originalIndex==-1)
        {
            throw new Exception("Cannot find node!");
        }
        if (originalIndex==mPathII.Count - 1)
        {
            return 0;
        }
        
        int i2 = mPathII[originalIndex+1].Item1;
        int j2 = mPathII[originalIndex+1].Item2;
        int i3 = -1;
        int j3 = -1;
        if (originalIndex<mPathII.Count - 2)
        {
            i3 = mPathII[originalIndex+2].Item1;
            j3 = mPathII[originalIndex+2].Item2;
        }
        return checkOccupancy(i1,j1,i2,j2,i3,j3);
    }
    private void initOccupyMap()
    {
        float[,] traversability = mHighResMap.traversability;
        mOccupyMap = new int[mHighResMap.x_N, mHighResMap.z_N];
        coldDownMap = new float[mHighResMap.x_N, mHighResMap.z_N];
        for (int i = 0; i < mHighResMap.x_N; i++)
        {
            for (int j = 0; j < mHighResMap.z_N; j++)
            {
                if (traversability[i, j] == 0)
                {
                    mOccupyMap[i, j] = 0; // mark traversabile grids with -1
                    coldDownMap[i,j] = 0;
                }
                else{
                    mOccupyMap[i,j] = -1;
                    coldDownMap[i,j] = -1;
                }
            }
        }
    }
    
}