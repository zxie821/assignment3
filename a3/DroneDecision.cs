using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class DroneDecision
{
    private Vector3 mCurrentPosition;
    List<Vector3> mPath = new List<Vector3>();
    int[] visit_list;
    float higher_limit=15f;
    float lower_limit=15f;
    DroneController mDrone;
    public DroneDecision(ref DroneController m_Drone, List<Vector3> path)
    {
        mPath = path;
        mDrone = m_Drone;
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
    private float getSpeedLimit(float steer_angle, float remote_steer_angle)
    {
        float angle_diff = Math.Abs(remote_steer_angle-steer_angle);
        // 即将转弯
        if( angle_diff>1f)
            return lower_limit;
        // 长直线路径
        return higher_limit;
    }
    public Vector3 getMove(int target_index)
    {
        // 寻找远和近目标点，并在地图中用白线标出
        Vector3 target_point = mPath[target_index];
        if (target_index > 0)
        {
            Vector3 forwardDirection = target_point - mPath[target_index-1];
            Vector3 rightDirection = Vector3.Cross(forwardDirection, Vector3.up);
            target_point = target_point - rightDirection.normalized * 5f;
        }
        Vector3 my_position = mDrone.transform.position;
        my_position.y = 0f;
        Debug.DrawLine(my_position, target_point, Color.black);
        float steer_angle = getSteerAngle(target_point);
        Vector3 remote_target_point;
        if (target_index<mPath.Count-1)
        {
            remote_target_point = mPath[target_index+1];
            Vector3 forwardDirection = remote_target_point - mPath[target_index];
            Vector3 rightDirection = Vector3.Cross(forwardDirection, Vector3.up);
            remote_target_point = remote_target_point - rightDirection.normalized * 5f;
        }
        else
        {
            remote_target_point = mPath[target_index];
        }
        float remote_steer_angle = getSteerAngle(remote_target_point);

        float speed_limit = getSpeedLimit(steer_angle,remote_steer_angle);
        // 速度计算
        Vector3 speed_vector = mDrone.velocity;

        float max_acceleration_speed = mDrone.max_acceleration * Time.fixedDeltaTime;
        higher_limit = mDrone.max_acceleration-1;
        lower_limit = higher_limit/2;

        Vector3 move_direction = target_point - my_position;
        //Debug.Log(move_direction);
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
        return update_acceleration.normalized;
    }
    
}
