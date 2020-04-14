using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

    public class HighResMap
    {
        private TerrainInfo rawTerrainInfo;
        private int upXRatio;
        private int upZRatio;
        public int x_N;
        public int z_N;
        public float[,] traversability;

        public HighResMap(ref TerrainInfo info, int upRatio){
            rawTerrainInfo = info;
            upXRatio = upZRatio = upRatio;
            x_N = rawTerrainInfo.x_N * upXRatio;
            z_N = rawTerrainInfo.z_N * upZRatio;
            traversability = new float[x_N, z_N];
            updateMap();
        }
        private void updateMap()
        {
            for (int x = 0; x < x_N; x++)
            {
                for (int z = 0; z < z_N; z++)
                {
                    traversability[x, z] = rawTerrainInfo.traversability[x/upXRatio, z/upZRatio];
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
            if (index < 0)
            {
                index = 0;
            }else if (index > x_N - 1)
            {
                index = x_N - 1;
            }
            return index;

        }
        public int get_j_index(float z) // get index of given coordinate
        {
            int index = (int)Mathf.Floor(z_N * (z - rawTerrainInfo.z_low) / (rawTerrainInfo.z_high - rawTerrainInfo.z_low));
            if (index < 0)
            {
                index = 0;
            }
            else if (index > z_N - 1)
            {
                index = z_N - 1;
            }
            return index;
        }
    }
