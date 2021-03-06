using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class DroneDecision
{
    private Vector3 mCurrentPosition;
    List<Vector3> mPath = new List<Vector3>();
    List<Tuple<int,int>> mPathII = new List<Tuple<int, int>>();
    int[] visit_list;
    float higher_limit=10f;
    float lower_limit=8f;
    float stuckTime = 0f;
    bool backWard = false;
    private OccupancyMap occupancyMap;
    private int mCurrentTargetIndex;
    GameObject[] mFriends;
    
    DroneController mDrone;
    public DroneDecision(ref DroneController m_Drone, ref GameObject[] friends, List<Vector3> path, List<Tuple<int,int>> pathII)
    {
        mPath = path;
        mPathII = pathII;
        mDrone = m_Drone;
        mFriends = friends;
        occupancyMap = OccupancyMap.Instance;
        mCurrentTargetIndex=0;
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
    public Vector3 getMove()
    {
        mCurrentPosition = mDrone.transform.position;
        mCurrentPosition.y = 0f;
        // 寻找远和近目标点，并在地图中用白线标出
        //updateTargetIndex();
        // 速度计算
        Vector3 speed_vector = mDrone.velocity;
        float max_acceleration_speed = mDrone.max_acceleration * Time.fixedDeltaTime;
        int target_index = searchClosestTargetIndex();
        var targetDistance = Vector3.Distance(mPath[target_index],mCurrentPosition);
        if (speed_vector.magnitude<0.5f&&!backWard)
        {
            stuckTime += Time.fixedDeltaTime;
        }
        if (stuckTime>2f&&!backWard)
        {
            stuckTime-=Time.fixedDeltaTime*2f;
            backWard=true;
        }else if(stuckTime>0&&backWard){
            stuckTime-=Time.fixedDeltaTime*2f;
        }else if(stuckTime<=0)
        {
            backWard=false;
        }
        if (backWard&&target_index>1)
        {
            target_index--;
        }
        else if(targetDistance<5f && target_index < mPath.Count-1 && tryAhead(target_index))
        {
            target_index++;
        }
        Vector3 target_point = mPath[target_index];
        Debug.DrawLine(mCurrentPosition, target_point, Color.cyan);
        float steer_angle = getSteerAngle(target_point);
        Vector3 remote_target_point = mPath[target_index<mPath.Count-4?target_index+4:mPath.Count-1];

        float remote_steer_angle = getSteerAngle(remote_target_point);
        float speed_limit = getSpeedLimit(target_point,steer_angle,remote_steer_angle);

        Vector3 move_direction = target_point - mCurrentPosition;
        Vector3 target_speed_direction = move_direction;
        Vector3 current_speed_target_direct = Vector3.Project(speed_vector, target_speed_direction);
        Vector3 current_speed_orth_target_direct = speed_vector - current_speed_target_direct;
        Vector3 update_acceleration;
        if(speed_vector.magnitude>speed_limit)
        {
            update_acceleration = speed_vector*-1;
        }
        else if(current_speed_orth_target_direct.magnitude > max_acceleration_speed)
        {
            update_acceleration = current_speed_orth_target_direct*-1;
        }
        else
        {
            Vector3 orth_acc_speed = current_speed_orth_target_direct * -1;
            Vector3 target_acc_speed = target_speed_direction*
            (float)(Math.Sqrt(Math.Pow(max_acceleration_speed,2f) - Math.Pow(orth_acc_speed.magnitude,2f)));
            update_acceleration = (orth_acc_speed + target_acc_speed);
        }
        update_acceleration = avoidFriends(update_acceleration.normalized);
        //Debug.DrawRay(mCurrentPosition, update_acceleration*10f, Color.black);
        return update_acceleration;
    }
    public Vector3 getMove(Vector3 goal)
    {
        mCurrentPosition = mDrone.transform.position;
        mCurrentPosition.y = 0f;
        Vector3 target_point = goal;
        Vector3 move_direction = target_point - mCurrentPosition;
        Vector3 target_speed_direction = move_direction;
        Vector3 speed_vector = mDrone.velocity;
        Vector3 current_speed_target_direct = Vector3.Project(speed_vector, target_speed_direction);
        Vector3 current_speed_orth_target_direct = speed_vector - current_speed_target_direct;
        Vector3 update_acceleration;
        float max_acceleration_speed = mDrone.max_acceleration * Time.fixedDeltaTime;
        if(current_speed_orth_target_direct.magnitude > max_acceleration_speed)
        {
            update_acceleration = current_speed_orth_target_direct*-1;
        }else{
            Vector3 orth_acc_speed = current_speed_orth_target_direct * -1;
            Vector3 target_acc_speed = target_speed_direction*
            (float)(Math.Sqrt(Math.Pow(max_acceleration_speed,2f) - Math.Pow(orth_acc_speed.magnitude,2f)));
            update_acceleration = (orth_acc_speed + target_acc_speed);
        }
        return avoidFriendsSimple(update_acceleration.normalized);
    }
    private float getSteerAngle(Vector3 target_point)
    {
        // 角度计算
        Vector3 z0 = new Vector3(0,0,3);
        float target_angle = Vector3.Angle( target_point-mDrone.transform.position,z0);
        if(mDrone.transform.position.x>target_point.x)
        {
            target_angle *= -1f;
            target_angle += 360f;
        }
        float speed_angle = mDrone.transform.localEulerAngles.y;
        float steer_angle = target_angle - speed_angle;
        if(steer_angle <0)
            steer_angle += 360f;
        if ( steer_angle > 180f)
        {
            steer_angle -=360f;
        }
        return steer_angle;
    }
    private float getSpeedLimit(Vector3 target_point, float steer_angle, float remote_steer_angle)
    {
        float angle_diff = Math.Abs(remote_steer_angle-steer_angle);
        // if (Vector3.Distance(target_point, mCurrentPosition)<2f)
        // {
        //     return lower_limit/5;
        // }
        // 即将转弯
        if( angle_diff>10f)
            return lower_limit;
        // 长直线路径
        return higher_limit;
    }
    private bool tryAhead(int targetIndex)
    {
        int counter =0;
        for (int i = 0; i < mFriends.Length; i++)
        {
            if ((mFriends[i].transform != mDrone.transform)&&
            (Vector3.Distance(mFriends[i].transform.position, mDrone.transform.position)<30f)&&
            occupancyMap.playerStatus[i])
            {
                counter++;
            }
        }
        if (counter==0)
        {
            //Debug.Log("no one is around");
            return true;
        }
        int nextStatus = occupancyMap.checkOccupancy(mPath[targetIndex], mPathII);
        if (nextStatus==0 && occupancyMap.occupyCell(mPath[targetIndex], mPathII, mCurrentPosition))
        {
            return true;
        }else if(nextStatus!=0)
        {
            return true;
        }
        return false;
    }
    // private void updateTargetIndex()
    // {
    //     var targetDistance = Vector3.Distance(mPath[mCurrentTargetIndex],mCurrentPosition);
    //     if (targetDistance>12f)
    //     {
    //         searchClosestTargetIndex();
    //     }else if(targetDistance<5f&&mCurrentTargetIndex < mPath.Count-1 && tryAhead())
    //     {
    //         mCurrentTargetIndex++;
    //     }
    // }
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
    private Vector3 avoidFriends(Vector3 rawAcceleration)
    {
        Vector3 repulsiveForce = Vector3.zero;
        Vector3 newSpeed = rawAcceleration*Time.fixedDeltaTime + mDrone.velocity;
        for (int idx = 0; idx < mFriends.Length; idx++)
        {
            if (!occupancyMap.playerStatus[idx])
            {
                continue;
            }
            var friend= mFriends[idx];
            Vector3 friendVelocity = friend.GetComponent<Rigidbody>().velocity;
            Vector3 friendLocation = friend.transform.position - mDrone.transform.position;
            if (friendLocation.magnitude<7f && Vector3.Dot(friendLocation.normalized, newSpeed.normalized) >0.9f)
            {
                if (false&&Vector3.Dot(newSpeed, friendVelocity)<0f)
                {
                    Vector3 leftRepulsive = Vector3.Cross(friendLocation, Vector3.down).normalized;
                    repulsiveForce -= (friendLocation.normalized+leftRepulsive.normalized).normalized;
                }
                else
                {
                    return -mDrone.velocity.normalized;
                }
            }
        }
        rawAcceleration += repulsiveForce.normalized ;
        return rawAcceleration.normalized;
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
                repulsiveForce -= (friendLocation.normalized+leftRepulsive.normalized).normalized;
            }
        }
        rawAcceleration += repulsiveForce.normalized ;
        return rawAcceleration.normalized;
    }

}
