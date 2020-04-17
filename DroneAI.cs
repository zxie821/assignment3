using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DroneController))]
public class DroneAI : MonoBehaviour
{

    private DroneController m_Drone; // the car controller we want to use

    public GameObject my_goal_object;
    public GameObject terrain_manager_game_object;
    TerrainManager terrain_manager;
    TerrainInfo tInfo;
    private int ownership;
    private int pathIndex;
    private int currentI, currentJ;
    private int targetI, targetJ;
    private Vector3 targetPosition;
    private List<System.Tuple<int,int>> tPath;
    private List<Vector3> tPathV3;
    private OccupancyMap occupancyMap;
    private HighResMap mHighResMap;
    public GameObject[] friends; // use these to avoid collisions
    private DroneDecision mDecision;
    private UpScaledMap uMap;
    private bool ifMoveable;
    private bool ifFinished;
    private int myIndex;
    private void Start()
    {
        // get the drone controller
        m_Drone = GetComponent<DroneController>();
        friends = GameObject.FindGameObjectsWithTag("Drone");
        myIndex = -1;
        for (int idx = 0; idx < friends.Length; idx++)
        {
            if (friends[idx].transform == m_Drone.transform)
            {
                myIndex = idx;
                break;
            }
        }
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
        tInfo = terrain_manager.myInfo;
        Vector3 start_pos = m_Drone.transform.position;
        Vector3 goal_pos = my_goal_object.transform.position;
        mHighResMap = new HighResMap(ref tInfo, 1);
        AStar aster = new AStar(ref mHighResMap);
        occupancyMap = OccupancyMap.Instance;
        occupancyMap.init(ref terrain_manager, ref mHighResMap, friends);
        int i = mHighResMap.get_i_index(transform.position.x);
        int j = mHighResMap.get_j_index(transform.position.z);
        int iEnd = mHighResMap.get_i_index(goal_pos.x);
        int jEnd = mHighResMap.get_j_index(goal_pos.z);
        tPath = aster.ComputePath(i,j,iEnd,jEnd);
        // Plot your path to see if it makes sense
        Vector3 old_wp = start_pos;
        Vector3 wp = start_pos;
        uMap = new UpScaledMap(ref tInfo, tPath, 3);
        tPathV3 = uMap.getV3Path();
        tPathV3.Add(goal_pos);

        old_wp = tPathV3[0];
        // foreach (var tuplepoint in tPathV3)
        // {
        //     wp = tuplepoint;
        //     Debug.DrawLine(old_wp, wp, Color.red, 100f);
        //     old_wp = wp;
        // }
        mDecision = new DroneDecision(ref m_Drone,ref friends, tPathV3, tPath);
        //status init
        pathIndex = 0;
        ownership = 2;
        ifMoveable=false;
        ifFinished = false;
        targetPosition = tPathV3[pathIndex];
    }
    private void FixedUpdate()
    {
        if (ifMoveable)
        {
            if (Vector3.Distance(m_Drone.transform.position, my_goal_object.transform.position)<5f)
            {
                occupancyMap.releasePermission(myIndex);
                ifFinished = true;
                ifMoveable = false;
            }
            else
            {
                m_Drone.Move_vect(mDecision.getMove());    
                return;
            }
        }
        else if(!ifMoveable && !ifFinished)
        {
            ifMoveable = occupancyMap.getPermission(myIndex);
            if (ifMoveable)
            {
                m_Drone.Move_vect(mDecision.getMove());
                return;
            }
        }else if(ifFinished)
        {
            m_Drone.Move_vect(my_goal_object.transform.position-m_Drone.transform.position);
            return;
        }

        m_Drone.Move_vect(Vector3.zero);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
