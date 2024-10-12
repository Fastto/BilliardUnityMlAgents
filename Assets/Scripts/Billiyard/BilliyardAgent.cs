using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Billiyard;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class BilliyardAgent : Agent
{
    
    // References
    [SerializeField] BilliyardBall whiteBall_;
    [SerializeField] List<BilliyardPocket> pockets_;
    [SerializeField] List<BilliyardBall> targets_;

    // Configuration
    [SerializeField] float maxShotPower_ = 10.0f;

    // Variables
    private List<BilliyardBall> lastTurnCatchedBalls_ = new List<BilliyardBall>();
    private List<Vector3> ballsInitPos_ = new List<Vector3>();
    private Vector3 whiteBallInitPos_;
    private int targetBallNumber_;
    
    // Const
    const float TABLE_WIDTH = 1.2f;
    const float TABLE_HEIGHT = 2.5f;


    private void Start()
    {
        // Store Starting positions
        whiteBallInitPos_ = whiteBall_.transform.position;
        targets_.ForEach(ball => ballsInitPos_.Add(ball.transform.position));
        
        // Assign handler for catching the ball
        pockets_.ForEach(pocket => pocket.OnBallCaught += OnBallCaughtHandler);
    }

    public override void Initialize()
    {
        // EnvironmentParameters envParams = Academy.Instance.EnvironmentParameters;
        // env_penalty_reward_ = envParams.GetWithDefault("penalty_reward", 0.0f);
    }

    public override void OnEpisodeBegin()
    {
        StartCoroutine(EpisodeCycle());
    }

    private IEnumerator EpisodeCycle()
    {
        // true = Agent will make one more turn 
        // false = Agent want to start new Episode
        var performOneMoreStep = true; 
        
        while (performOneMoreStep)
        {
            ResetTurn();
            yield return new WaitForFixedUpdate();
            
            // ************************************ Performing shot 
            RequestDecision();

            // ************************************ Waiting rolling 
            do
            {
                yield return new WaitForFixedUpdate();
            } while (!AreAllBallsStopped());
            
            // ************************************ Results evaluation 

            // cue ball is dead = end of the turn
            if (!WasTurnLegal())
            {
                AddReward(-1f/9f);
                break;
            }
            
            // Reward for Legal shot
            AddReward(1f/18f);

            // Rewards for caught balls
            foreach (var ball in lastTurnCatchedBalls_)
            {
                if (ball.Number == 9)
                {
                    // Game won
                    SetReward(1f);
                    ResetGame();
                    break;
                }
                
                AddReward(1f/9f);
            }
        }
        
        EndEpisode();
    }

    private bool WasTurnLegal()
    {
        // 1. Cue ball is not dead
        if (whiteBall_.IsDead) 
            return false;

        // 2. Cue ball touched target ball first
        if (whiteBall_.FirstHitTargetNum != targetBallNumber_)
            return false;
        
        // 3. At least one ball touched border 
        if (!WasBorderTouched())
            return false;
        
        return true;
    }

    private void RestoreWhiteBall(bool placeToInitialPosition = false)
    {
        whiteBall_.gameObject.SetActive(true);
        whiteBall_.transform.position = placeToInitialPosition ? whiteBallInitPos_ : GetSafePos();
        whiteBall_.IsDead = false;
        whiteBall_.Rb.velocity = Vector3.zero;
        whiteBall_.Rb.angularVelocity = Vector3.zero;
    }
    
    private void RestoreBall(int id, bool placeToInitialPosition = false)
    {
        targets_[id].gameObject.SetActive(true);
        targets_[id].transform.position = placeToInitialPosition ? ballsInitPos_[id] : GetSafePos();
        targets_[id].IsDead = false;
        targets_[id].Rb.velocity = Vector3.zero;
        targets_[id].Rb.angularVelocity = Vector3.zero;
    }

    private void ResetTurn()
    {
        if (whiteBall_.IsDead) RestoreWhiteBall();
        if (targets_[8].IsDead) RestoreBall(8);
        
        if (whiteBall_.transform.localPosition.x < -0.7f || whiteBall_.transform.localPosition.x > 0.7f || whiteBall_.transform.localPosition.z < -1.5f || whiteBall_.transform.localPosition.z > 1.2f )
        {
            RestoreWhiteBall();
        }

        for (int i = 0; i < targets_.Count; i++)
        {
            if (!targets_[i].IsDead)
            {
                if (targets_[i].transform.localPosition.x < -0.7f || targets_[i].transform.localPosition.x > 0.7f || targets_[i].transform.localPosition.z < -1.5f || targets_[i].transform.localPosition.z > 1.2f )
                {
                    RestoreBall(i);
                }
            }
        }

        
        whiteBall_.FirstHitTargetNum = -1;
        lastTurnCatchedBalls_.Clear();
        targetBallNumber_ = DefineTarget();
    }

    private void ResetGame()
    {
        RestoreWhiteBall(true);
        for (int i = 0; i < ballsInitPos_.Count; i++) 
            RestoreBall(i, true);
        
        targetBallNumber_ = 1;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(whiteBall_.transform.localPosition.x / TABLE_HEIGHT);
        sensor.AddObservation(whiteBall_.transform.localPosition.z / TABLE_WIDTH);
        foreach (var pocket in pockets_)
        {
            sensor.AddObservation(pocket.transform.localPosition.x / TABLE_HEIGHT);
            sensor.AddObservation(pocket.transform.localPosition.z / TABLE_WIDTH);
        }
        foreach (var target in targets_)
        {
            sensor.AddObservation(target.IsDead);
            sensor.AddObservation(target.IsDead ? 0 : target.transform.localPosition.x / TABLE_HEIGHT);
            sensor.AddObservation(target.IsDead ? 0 : target.transform.localPosition.z / TABLE_WIDTH);
        }
        sensor.AddObservation(targetBallNumber_ / 10.0f);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float angleX = actionBuffers.ContinuousActions[0];
        float angleY = actionBuffers.ContinuousActions[1];
        var direction = new Vector3(angleX, 0, angleY).normalized;

        var power = ((actionBuffers.ContinuousActions[2] + 1.1f) * 0.5f) * maxShotPower_;
        whiteBall_.Rb.AddForce(direction * power, ForceMode.VelocityChange);
    }

    private int DefineTarget()
    {
        foreach (var ball in targets_.Where(ball => !ball.IsDead))
            return ball.Number;

        throw new Exception("No available tragets");
    }

    private Vector3 GetSafePos()
    {
        Vector3 safePosition;
        Collider[] hitColliders;
        int layerMask = LayerMask.GetMask("Ball");

        do
        {
            float randomX = Random.Range(-0.6f, 0.6f);
            float randomZ = Random.Range(-1.4f, 1.1f);
            safePosition = new Vector3(randomX, whiteBallInitPos_.y, randomZ);

            hitColliders =
                Physics.OverlapSphere(safePosition, whiteBall_.GetComponent<SphereCollider>().radius, layerMask);
        } while (hitColliders.Length > 0);

        return safePosition;
    }

    private bool AreAllBallsStopped()
    {
        if (!whiteBall_.IsDead && whiteBall_.Rb.velocity.sqrMagnitude > 0.001f)
            return false;

        foreach (var ball in targets_)
        {
            if (!ball.IsDead && ball.Rb.velocity.sqrMagnitude > 0.001f)
                return false;
        }

        return true;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        cont[0] = 0f;
        cont[1] = 1f;
        cont[2] = 1f;
    }

    private void OnBallCaughtHandler(BilliyardBall ball)
    {
        ball.gameObject.SetActive(false);
        ball.IsDead = true;
        if (ball.IsWhite()) return;
        
        lastTurnCatchedBalls_.Add(ball);
    }

    // TODO: implement border touching detection during the turn
    private bool WasBorderTouched()
    {
        return true;
    }
}