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
    public Vector3 otherGoalUp, otherGoalDown;
    public Vector3 myPosition,ballPosition,ballVelocity, ballPositionPredict;
    public Vector3 defendPoint1, defendPoint2, myDefencePoint;
    public Vector3 attackDirection;
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
        {
            enemy_tag = "Red";
            attackDirection = Vector3.right;
        }
        else
        {
            enemy_tag = "Blue";
            attackDirection = Vector3.left;
        }
        friends = GameObject.FindGameObjectsWithTag(friend_tag);
        enemies = GameObject.FindGameObjectsWithTag(enemy_tag);

        initAcc();
        ball = GameObject.FindGameObjectWithTag("Ball");
        myDefencePoint = new Vector3(185f, 0f,100f);
        for (playerNum = 0; friends[playerNum].transform != m_Drone.transform; playerNum++){}
        time = Time.fixedDeltaTime * 50f;
        //FixedUpdate();
    }
    [Task]
    public bool IsStrikerG12(){return playerNum==2;}
    [Task]
    public bool IsInterceptorG12(){return playerNum==1;}
    [Task]
    public bool IsGoalKeeperG12(){return playerNum==0;}
    
    [Task]
    public void AdjustPositionG12()
    {
        
        Vector3 goal2ball = (ballPosition - otherGoal).normalized;
        var goal2ballUp = (ballPosition - otherGoalUp).normalized;
        var goal2ballDown = (ballPosition - otherGoalDown).normalized;

        Vector3 idealPosition = ballPosition + goal2ball * 10f;
        Debug.DrawRay(otherGoalUp, goal2ballUp*1000f, Color.black);
        Debug.DrawRay(otherGoalDown, goal2ballDown*1000f, Color.black);
        Debug.DrawLine(myPosition, idealPosition, Color.red);
        goToPosition(idealPosition, true, true);

        if (ifGoodShootAngle())
        {
            Task.current.Succeed();
        }
    }

    [Task]
    public void ShootG12()
    {
        //goToPosition(otherGoal, false, false);
        m_Drone.Move_vect(getInterceptAcc(ballPosition));
        if (Vector3.Distance(ballPosition, otherGoal)<3f)
        {
            Task.current.Succeed();
        }
        else if(!ifGoodShootAngle())
        {
            Task.current.Fail();
            pbTree.Reset();
        }
    }
    [Task]
    public bool IsBallNearG12()
    {
        return Math.Abs(myDefencePoint.x - ballPositionPredict.x)<40f;
    }
    [Task]
    public void InterceptG12()
    {
        Vector3 me2ball = (ballPositionPredict - myPosition).normalized;
        if (Vector3.Angle(me2ball, attackDirection)<90)
        {
            m_Drone.Move_vect(getInterceptAcc(ballPosition));

        }
        else
        {
            Vector3 closestEnemy = Vector3.zero;
            float distanceMin = 10000000;
            for (int idx = 0; idx< enemies.Length; idx++)
            {
                Vector3 enemyPosition = enemies[idx].transform.position;
                enemyPosition.y = 0;
                float enemy2ballDis = (ballPosition- enemyPosition).magnitude;
                if (enemy2ballDis<distanceMin)
                {
                    closestEnemy = enemyPosition;
                    distanceMin = enemy2ballDis;
                }
            }
            m_Drone.Move_vect(getInterceptAcc(closestEnemy));
        }
        if (!IsBallNearG12())
        {
            Task.current.Succeed();
        }

    }
    [Task]
    public void goBackG12()
    {
        goToPosition(myDefencePoint, true, false, false);
        if (Vector3.Distance(myPosition, myDefencePoint)<4f)
        {
            Task.current.Succeed();
        }else if(IsBallNearG12())
        {
            Task.current.Fail();
            pbTree.Reset();
        }
    }
    [Task]
    public void BlockG12()
    {
        if (Vector3.Distance(ballPositionPredict, ownGoal)<35f)
        {
            m_Drone.Move_vect(getInterceptAcc(ballPosition));
        }
        else
        {
            Vector3 defendPoint = new Vector3(0f,0f,0f);
            defendPoint.x = (ownGoal+attackDirection*3f).x;
            defendPoint.z = ballPositionPredict.z;
            if (defendPoint.z>ownGoal.z+16f)
            {
                defendPoint.z = ownGoal.z+16f;
            }
            else if(defendPoint.z < ownGoal.z-16f)
            {
                defendPoint.z = ownGoal.z -16f;
            }
            goToPosition(defendPoint,false, false, false);
        }

    }

    private bool ifGoodShootAngle()
    {
        Vector3 ball2goalUp = (otherGoalUp - ballPosition).normalized;
        Vector3 ball2goalDown = (otherGoalDown - ballPosition).normalized;
        Vector3 me2ball = (ballPosition - myPosition).normalized;

        float cosGoal = Vector3.Dot(ball2goalUp, ball2goalDown);
        float cosMeUp = Vector3.Dot(me2ball, ball2goalUp);
        float cosMeDown = Vector3.Dot(me2ball, ball2goalDown);
        if (cosMeUp>cosGoal && cosMeDown>cosGoal && ball.transform.position.y<7f)
        {
            return true;
        }
        return false;
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
    private void goToPosition(Vector3 target, bool avoidBall, bool avoidAgents, bool speedFirst=true)
    {
        int bestAcc = -1;
        float bestAccScore = -100000;
        for (int i = 0; i < possibleAcceleration.Count; i++)
        {
            float score = evaluateAcceleration(target, possibleAcceleration[i], avoidBall, avoidAgents, speedFirst);
            if (score>bestAccScore)
            {
                bestAcc = i;
                bestAccScore = score;
            }
        }
        Debug.DrawRay(myPosition, possibleAcceleration[bestAcc]*10f, Color.green);
        m_Drone.Move_vect(possibleAcceleration[bestAcc]);
    }
    private Vector3 getInterceptAcc(Vector3 target)
    {
        Vector3 me2target = (target - myPosition).normalized;
        int bestIndex= 0;
        float bestAccAngle = 360f;
        for (int idx = 0; idx < possibleAcceleration.Count; idx++)
        {
            Vector3 newSpeed = m_Drone.velocity + possibleAcceleration[idx] * time*10;
            var speedError = newSpeed - ballVelocity;
            float angle = Vector3.Angle(speedError.normalized, me2target.normalized);
            //float speed = Vector3.Project()
            if (angle<bestAccAngle)
            {
                bestIndex = idx;
                bestAccAngle = angle;
            }
        }
        return possibleAcceleration[bestIndex];
    }

    private float evaluateAcceleration(Vector3 target, Vector3 acceleration, bool avoidBall, bool avoidAgents, bool speedFirst=true)
    {
        float score;
        Vector3 newSpeed;
        if (speedFirst)
        {
            Vector3 me2target = (target - myPosition).normalized;
            newSpeed = m_Drone.velocity + acceleration * time*10;
            score = 360f - Vector3.Angle(newSpeed, me2target.normalized);
        }
        else
        {
            Vector3 estimatedDestination = m_Drone.velocity*time + 0.5f*acceleration*time*time + myPosition;
            newSpeed = m_Drone.velocity + acceleration * time;
            score = 10000f - Vector3.Distance(target, estimatedDestination);
        }
        
        if (avoidAgents)
        {
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
        Vector3 friendPosition = friend.transform.position;
        friendPosition.y = 0f;
        Vector3 friendLocation = friendPosition- myPosition;
        Vector3 speedError = mySpeed - friendVelocity;
        //float tolerance = (speedError - Vector3.Project(speedError, friendLocation)).magnitude;
        if (Vector3.Angle(speedError.normalized, friendLocation.normalized)<5f)
        {
            float timeToHit = Vector3.Project(speedError, friendLocation).magnitude / mySpeed.magnitude;
            if (timeToHit<5f)
            {
                return true;
            }
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
        otherGoalUp = otherGoal;
        otherGoalDown = otherGoal;
        if (Math.Abs(otherGoal.x-ballPosition.x)<50f)
        {
            otherGoalUp.z += 15f;
            otherGoalDown.z -= 15f;
        }
        else
        {
            otherGoalUp.z += 50f;
            otherGoalDown.z -= 50f;
        }

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
