using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Panda;

//namespace UnityStandardAssets.Vehicles.Car
//{
[RequireComponent(typeof(DroneController))]
public class DroneAISoccer_red : MonoBehaviour
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
    float time;
    public int counter;
    private readonly float WORST_SCORE = -1000;
    public Vector3 otherGoal,ownGoal;
    public Vector3 myPosition,ballPosition,ballVelocity, ballPositionPredict;
    public Vector3 defendPoint1, defendPoint2, myDefencePoint;
    public List<Vector3> possibleAcceleration;
    private void Start()
    {
        // get the car controller
        m_Drone = GetComponent<DroneController>();
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
        pbTree =  GetComponent<PandaBehaviour>();

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
        defendPoint1 = new Vector3(220f, 0f,100f);
        defendPoint2 = new Vector3(180f, 0f, 100f);
        for (playerNum = 0; friends[playerNum].transform != m_Drone.transform; playerNum++){}
        myDefencePoint = defendPoint2;
        if (playerNum==1)
        {
            myDefencePoint = defendPoint1;
        }
        time = Time.fixedDeltaTime * 50f;
        //FixedUpdate();
    }
    [Task]
    public bool IsStriker(){return playerNum==0;}
    [Task]
    public void AdjustPosition()
    {
        Vector3 goal2ball = (ballPositionPredict - otherGoal).normalized;
        Vector3 me2ball = (ballPositionPredict - myPosition).normalized;
        Vector3 ball2me = -me2ball;
        Vector3 ball2goal = -goal2ball;

        Vector3 idealPosition = ballPositionPredict + goal2ball*4f;
        goToPosition(idealPosition, true, true);
        if ( Vector3.Distance(myPosition, idealPosition)< 5f&&Vector3.Dot(ball2me, ball2goal)<-.9f)
        {
            Task.current.Succeed();
        }
    }

    [Task]
    public void Shoot()
    {
        goToPosition(otherGoal, false, false);
        if (Vector3.Distance(ballPosition, otherGoal)<3f)
        {
            Task.current.Succeed();
        }
        else if(Vector3.Distance(myPosition, ballPosition)>5f)
        {
            Task.current.Fail();
            pbTree.Reset();
        }
    }
    [Task]
    public bool IsBallNear()
    {
        return (myDefencePoint.x - ballPositionPredict.x)<20f;
    }
    [Task]
    public void Intercept()
    {
        m_Drone.Move_vect(getInterceptAcc());
        if (!IsBallNear())
        {
            Task.current.Succeed();
        }
    }
    [Task]
    public void goBack()
    {
        goToPosition(myDefencePoint, true, true);
        if (Vector3.Distance(myPosition, myDefencePoint)<4f)
        {
            Task.current.Succeed();
        }
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
    private void goToPosition(Vector3 target, bool avoidBall, bool avoidAgents)
    {
        int bestAcc = -1;
        float bestAccScore = -100000;
        for (int i = 0; i < possibleAcceleration.Count; i++)
        {
            float score = evaluateAcceleration(target, possibleAcceleration[i], avoidBall, avoidAgents);
            if (score>bestAccScore)
            {
                bestAcc = i;
                bestAccScore = score;
            }
        }
        Debug.DrawRay(myPosition, possibleAcceleration[bestAcc]*10f, Color.green);
        m_Drone.Move_vect(possibleAcceleration[bestAcc]);
    }
    private Vector3 getInterceptAcc()
    {
        Vector3 ball2goal = (otherGoal - ballPosition).normalized;
        Vector3 me2ball = (ballPosition - myPosition).normalized;
        int bestIndex= 0;
        float bestAccScore = WORST_SCORE;
        for (int idx = 0; idx < possibleAcceleration.Count; idx++)
        {
            Vector3 newSpeed = m_Drone.velocity + possibleAcceleration[idx] * time;
            var speedError = newSpeed - ballVelocity;
            float score = Vector3.Dot(speedError.normalized, me2ball.normalized);
            if (score>bestAccScore)
            {
                bestIndex = idx;
                bestAccScore = score;
            }
        }
        return possibleAcceleration[bestIndex];
    }

    private float evaluateAcceleration(Vector3 target, Vector3 acceleration, bool avoidBall, bool avoidAgents)
    {
        Vector3 estimatedDestination = m_Drone.velocity*time + 0.5f*acceleration*time*time + myPosition;
        Vector3 newSpeed = m_Drone.velocity + acceleration * time;
        float score = 10000f - Vector3.Distance(target, estimatedDestination);
        if (avoidAgents)
        {
            for (int idx = 0; idx < enemies.Length; idx++)
            {
                if (collisionDetect(enemies[idx], newSpeed))
                {
                    return WORST_SCORE;
                }
            }    
            for (int idx = 0; idx < friends.Length; idx++)
            {
                var friend = friends[idx];
                if (friend.transform == m_Drone.transform)
                {
                    continue;
                }
                if (collisionDetect(friend, newSpeed))
                {
                    return WORST_SCORE;
                }
            }   
        }
        if (avoidBall&&collisionDetect(ball, newSpeed))
        {
            return WORST_SCORE;
        }
        return score;
    }
    private bool collisionDetect(GameObject friend, Vector3 mySpeed)
    {
        Vector3 friendVelocity = friend.GetComponent<Rigidbody>().velocity;
        Vector3 friendLocation = friend.transform.position - m_Drone.transform.position;
        Vector3 speedError = mySpeed - friendVelocity;
        if (speedError.magnitude<1f || friendLocation.magnitude>10f)
        {
            return false;
        }
        float tolerance = (speedError - Vector3.Project(speedError, friendLocation)).magnitude;
        if (tolerance<4f)
        {
            return true;
        }
        return false;
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

        ballPositionPredict = ballPosition + ballVelocity*Time.fixedDeltaTime*30f;
        Debug.DrawLine(ballPosition, ballPositionPredict, Color.cyan);
    }
    
    private void Update()
    {
        //pbTree.Reset();
        pbTree.Tick();
    }
}
//}
