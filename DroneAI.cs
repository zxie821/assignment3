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
    private OccupancyMap occupancyMap;
    private HighResMap mHighResMap;
    private DroneDecision mDecision;
    private void Start()
    {
        // get the drone controller
        m_Drone = GetComponent<DroneController>();
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
        tInfo = terrain_manager.myInfo;
        Vector3 start_pos = m_Drone.transform.position;
        Vector3 goal_pos = my_goal_object.transform.position;

        List<Vector3> my_path_v3 = new List<Vector3>();
        mHighResMap = new HighResMap(ref tInfo, 1);
        AStar aster = new AStar(ref mHighResMap);
        occupancyMap = OccupancyMap.Instance;
        occupancyMap.init(ref terrain_manager, ref mHighResMap);
        int i = mHighResMap.get_i_index(transform.position.x);
        int j = mHighResMap.get_j_index(transform.position.z);
        int iEnd = mHighResMap.get_i_index(goal_pos.x);
        int jEnd = mHighResMap.get_j_index(goal_pos.z);
        tPath = aster.ComputePath(i,j,iEnd,jEnd);
        // Plot your path to see if it makes sense
        Vector3 old_wp = start_pos;
        Vector3 wp = start_pos;
        foreach (var tuplepoint in tPath)
        {
            wp = new Vector3(
                mHighResMap.get_x_pos(tuplepoint.Item1),0f, 
                mHighResMap.get_z_pos(tuplepoint.Item2));
            my_path_v3.Add(wp);
            Debug.DrawLine(old_wp, wp, Color.red, 100f);
            old_wp = wp;
        }
        mDecision = new DroneDecision(ref m_Drone, my_path_v3);

        //status init
        pathIndex = 0;
        ownership = 0;
        targetI = tPath[pathIndex].Item1;
        targetJ = tPath[pathIndex].Item2;
        targetPosition = new Vector3(mHighResMap.get_x_pos(targetI), 0f, mHighResMap.get_z_pos(targetJ));
        currentI = mHighResMap.get_i_index(transform.position.x);
        currentJ = mHighResMap.get_j_index(transform.position.z);
    }


    private void FixedUpdate()
    {
        int i = mHighResMap.get_i_index(transform.position.x);
        int j = mHighResMap.get_j_index(transform.position.z);

        if (i==targetI && j == targetJ && pathIndex<tPath.Count-1)  //到达目标点，准备更新
        {
            if ((transform.position - targetPosition).magnitude < 6f)
            {
                var newTargetI = tPath[pathIndex+1].Item1;
                var newTargetJ = tPath[pathIndex+1].Item2;
                Vector3 newTargetPosition = new Vector3(mHighResMap.get_x_pos(newTargetI), 0f, mHighResMap.get_z_pos(newTargetJ));
                Debug.DrawLine(transform.position, newTargetPosition, Color.yellow);
                if (ownership == 1)
                {
                    occupancyMap.releaseCell(targetI, targetJ);
                }
                // 没有抢占成功，试着停下来,即保持原有目标点
                if(occupancyMap.occupyCell(targetI, targetJ, newTargetI, newTargetJ))  // 抢占成功，继续运动
                {
                    ownership = 1;
                    pathIndex++;
                    currentI = targetI;
                    currentJ = targetJ;
                    targetI = newTargetI;
                    targetJ = newTargetJ;
                    Vector3 forwardDirection = newTargetPosition - targetPosition;
                    Vector3 rightDirection = Vector3.Cross(forwardDirection, Vector3.up);
                    //targetPosition = newTargetPosition - rightDirection.normalized * 5f;
                    targetPosition = newTargetPosition;
                }
                else if(occupancyMap.checkOccupancyint(targetI, targetJ, newTargetI, newTargetJ)==1)
                {
                    ownership = 0;
                    pathIndex++;
                    currentI = targetI;
                    currentJ = targetJ;
                    targetI = newTargetI;
                    targetJ = newTargetJ;
                    Vector3 forwardDirection = newTargetPosition - targetPosition;
                    Vector3 rightDirection = Vector3.Cross(forwardDirection, Vector3.up);
                    //targetPosition = newTargetPosition - rightDirection.normalized * 5f;
                    targetPosition = newTargetPosition;
                }
            }
        }
        else if (ownership==0)   // 没到目标点，如果没有占有点，尝试占有
        {
            if (occupancyMap.occupyCell(currentI, currentJ, targetI, targetJ))
            {
                ownership = 1;
            }
        }else if (pathIndex==tPath.Count-1)
        {
            targetPosition = my_goal_object.transform.position;
        }
        
        Vector3 relVect = targetPosition - transform.position;
        Debug.DrawLine(transform.position, targetPosition, Color.cyan);
        var decision = mDecision.getMove(pathIndex);
        //m_Drone.Move(decision.x,decision.z);
        // TODO:防止碰撞
        m_Drone.Move_vect(decision*15f);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
