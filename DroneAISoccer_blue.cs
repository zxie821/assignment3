using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Panda;

//namespace UnityStandardAssets.Vehicles.Car
//{
[RequireComponent(typeof(DroneController))]
public class DroneAISoccer_blue : MonoBehaviour
{
    private DroneController m_Drone; // the drone controller we want to use

    public GameObject terrain_manager_game_object;
    TerrainManager terrain_manager;

    public GameObject[] friends;
    public string friend_tag;
    public GameObject[] enemies;
    public string enemy_tag;

    public GameObject own_goal;
    public GameObject other_goal;
    public GameObject ball;
    public PandaBehaviour pbTree;
    public int playerNum;
    public int counter;
    public Vector3 otherGoal,ownGoal;
    public Vector3 myPosition,ballPosition,ballVelocity, ballPositionPredict;
    public Vector3 defendPoint1, defendPoint2, myDefencePoint;
    public List<Vector3> possibleAcceleration;
    private void Start()
    {
        // get the car controller
        m_Drone = GetComponent<DroneController>();
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
        //pbTree =  GetComponent<PandaBehaviour>();

        // note that both arrays will have holes when objects are destroyed
        // but for initial planning they should work
        friend_tag = gameObject.tag;
        if (friend_tag == "Blue")
            enemy_tag = "Red";
        else
            enemy_tag = "Blue";

        friends = GameObject.FindGameObjectsWithTag(friend_tag);
        enemies = GameObject.FindGameObjectsWithTag(enemy_tag);

        initAcc();
        ball = GameObject.FindGameObjectWithTag("Ball");
        defendPoint1 = new Vector3(150f, 0f,110f);
        defendPoint2 = new Vector3(130f, 0f, 90f);
        for (playerNum = 0; friends[playerNum].transform != m_Drone.transform; playerNum++){}
        myDefencePoint = defendPoint2;
        if (playerNum==1)
        {
            myDefencePoint = defendPoint1;
        }
        FixedUpdate();
    }
    
    private void initAcc()
    {
        var dummy = new Vector3(0f,0f,1f);
        Quaternion rotation = Quaternion.Euler(0,10,0);
        for (int idx = 0; idx < 36; idx++)
        {
            possibleAcceleration.Add(dummy);
            dummy = rotation * dummy;
        }
    }
    private void FixedUpdate(){
        myPosition =  m_Drone.transform.position;
        ownGoal = own_goal.transform.position;
        otherGoal = other_goal.transform.position;
        ballPosition = ball.transform.position;
        
        ballVelocity = ball.GetComponent<Rigidbody>().velocity;

        myPosition.y = 0f;
        ownGoal.y = 0f;
        otherGoal.y = 0f;
        ballPosition.y = 0f;
        ballVelocity.y = 0f;

        ballPositionPredict = ballPosition + ballVelocity*Time.fixedDeltaTime*20f;
        Debug.DrawLine(ballPosition, ballPositionPredict, Color.cyan);
    }
    
    private void Update()
    {
        //pbTree.Reset();
        //pbTree.Tick();
    }
}
//}
