using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class DroneDecisionSimple
{
    private Vector3 mCurrentPosition;
    List<Vector3> mPath = new List<Vector3>();
    List<Tuple<int,int>> mPathII = new List<Tuple<int, int>>();
    int[] visit_list;
    float higher_limit=10f;
    float lower_limit=8f;
    float stuckTime = 0f;
    private int mCurrentTargetIndex;
    GameObject[] mFriends;
    HighResMap mHighResMap;
    DroneController mDrone;
    public DroneDecisionSimple(ref DroneController m_Drone, ref GameObject[] friends, ref HighResMap highRes, List<Tuple<int,int>> pathII)
    {
        mPathII = pathII;
        mHighResMap = highRes;
        mDrone = m_Drone;
        mFriends = friends;
        mCurrentTargetIndex=0;
        foreach (var p in mPathII)
        {
            mPath.Add(new Vector3(mHighResMap.get_x_pos(p.Item1),0f,mHighResMap.get_z_pos(p.Item2)));
        }
        printPath();
    }
    public Vector3 getMove()
    {

        mCurrentPosition = mDrone.transform.position;
        mCurrentPosition.y = 0f;
        var dis = Vector3.Distance(mPath[mCurrentTargetIndex],mCurrentPosition);
        if (dis>30f)
        {
            mCurrentTargetIndex = searchClosestTargetIndex();
        }
        else if(dis<20f && mCurrentTargetIndex<mPath.Count-1)
        {
            mCurrentTargetIndex++;
        }
        int target_index = mCurrentTargetIndex;
        float max_acceleration_speed = mDrone.max_acceleration * Time.fixedDeltaTime;

        Vector3 target_point = mPath[target_index];
        Vector3 move_direction = target_point - mCurrentPosition;
        Vector3 current_speed_target_direct = Vector3.Project(mDrone.velocity, move_direction);
        Vector3 current_speed_orth_target_direct = mDrone.velocity - current_speed_target_direct;
        Vector3 update_acceleration;
        if(current_speed_orth_target_direct.magnitude > max_acceleration_speed)
        {
            update_acceleration = current_speed_orth_target_direct*-1;
        }else{
            Vector3 orth_acc_speed = current_speed_orth_target_direct * -1;
            Vector3 target_acc_speed = move_direction*
            (float)(Math.Sqrt(Math.Pow(max_acceleration_speed,2f) - Math.Pow(orth_acc_speed.magnitude,2f)));
            update_acceleration = (orth_acc_speed + target_acc_speed);
        }
        update_acceleration = avoidFriendsSimple(update_acceleration.normalized);
        //Debug.DrawRay(mCurrentPosition, update_acceleration*10f, Color.black);
        return update_acceleration.normalized;
    }
    public void printPath(float time=200f)
    {
        Vector3 old_wp = mPath[0];
        foreach (var wp in mPath)
        {
            Debug.DrawLine(old_wp, wp, Color.red, time);
            old_wp = wp;
        }
    }
    private int searchClosestTargetIndex()
    {
        int bestIndex= -1;
        float bestDistance = 100000000;
        for (int i = 0; i < mPath.Count; i++)
        {
            var point = mPath[i];
            float distance = Vector3.Distance(point, mCurrentPosition);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }
        return bestIndex;
    }
    private Vector3 avoidFriendsSimple(Vector3 rawAcceleration)
    {
        Vector3 repulsiveForce = Vector3.zero;
        Vector3 newSpeed = rawAcceleration*Time.fixedDeltaTime + mDrone.velocity;
        for (int idx = 0; idx < mFriends.Length; idx++)
        {
            var friend= mFriends[idx];
            Vector3 friendVelocity = friend.GetComponent<Rigidbody>().velocity;
            Vector3 friendLocation = friend.transform.position - mDrone.transform.position;
            if (friendLocation.magnitude<7f)
            {
                Vector3 leftRepulsive = Vector3.Cross(friendLocation, Vector3.down).normalized;
                repulsiveForce -= (leftRepulsive.normalized+0.5f*friendLocation.normalized).normalized*(20f - friendLocation.magnitude);
            }
        }
        rawAcceleration += repulsiveForce.normalized;
        return rawAcceleration.normalized;
    }

}
