using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Billiard
{
    public class BilliardAgent : Agent
    {
        // References
        [SerializeField] private EnvironmentManager m_Manager;

        // Config
        private float _waitBeforeShotTime = 2f;
        private float _collectObservationPeriod = 0.25f;
        
        // Variables
        private bool _doAction = false;
        
        // Rewards
        private float _victoryReward = 1.0f; //9th ball cought by legal shot
        private float _defeatPenalty = - 1.0f; //Panalty for the player who didnt caught 9th ball
        private float _legalShotReward; // rewarding for legal shot
        private float _legalShotBallCaughtReward; // any ball caught by legal shot
        private float _illegalShotPenalty; // penalty for illegal shot
 

        public override void Initialize()
        {
            EnvironmentParameters envParams = Academy.Instance.EnvironmentParameters;
            _victoryReward = envParams.GetWithDefault("victory_reward", 1.0f);
            _defeatPenalty = envParams.GetWithDefault("defeat_penalty", -1.0f);
            _legalShotReward = envParams.GetWithDefault("legal_shot_reward", 1f/18f);
            _legalShotBallCaughtReward = envParams.GetWithDefault("legal_shot_ball_caught_reward", 1f/9f);
            _illegalShotPenalty = envParams.GetWithDefault("illegal_shot_penalty", -1f/18f);
        }

        public override void OnEpisodeBegin()
        {
            StartCoroutine(EpisodeCycle());
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(m_Manager.m_Whiteball.transform.localPosition.x / EnvironmentManager.TABLE_HEIGHT);
            sensor.AddObservation(m_Manager.m_Whiteball.transform.localPosition.z / EnvironmentManager.TABLE_WIDTH);
            sensor.AddObservation(m_Manager.m_Whiteball.Rb.velocity);
            sensor.AddObservation(m_Manager.m_Whiteball.Rb.angularVelocity.z);
            foreach (var pocket in m_Manager.m_Pockets)
            {
                sensor.AddObservation(pocket.transform.localPosition.x / EnvironmentManager.TABLE_HEIGHT);
                sensor.AddObservation(pocket.transform.localPosition.z / EnvironmentManager.TABLE_WIDTH);
            }

            foreach (var target in m_Manager.m_Balls)
            {
                sensor.AddObservation(target.IsDead);
                sensor.AddObservation(target.IsDead
                    ? 0
                    : target.transform.localPosition.x / EnvironmentManager.TABLE_HEIGHT);
                sensor.AddObservation(target.IsDead
                    ? 0
                    : target.transform.localPosition.z / EnvironmentManager.TABLE_WIDTH);
                sensor.AddObservation(target.Rb.velocity);
                sensor.AddObservation(target.Rb.angularVelocity.z);
            }

            sensor.AddObservation(m_Manager.m_TargetBallNumber / 9.0f);
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            if (!_doAction)
                return;
            
            float angleX = actionBuffers.ContinuousActions[0];
            float angleY = actionBuffers.ContinuousActions[1];
            var direction = new Vector3(angleX, 0, angleY).normalized;
            var power = ((actionBuffers.ContinuousActions[1] + 1) / 2f) + 0.1f;

            m_Manager.Shot(direction, power);
            _doAction = false;
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var cont = actionsOut.ContinuousActions;
            cont[0] = .02f;
            cont[1] = .9f;
            cont[2] = 1f;
        }
        
        private IEnumerator EpisodeCycle()
        {
            var id = Random.Range(0, 9999);
            
            // true = Agent will make one more turn 
            // false = Agent want to start new Episode
            var performOneMoreStep = true;
            yield return new WaitForSeconds(_waitBeforeShotTime);
            
            while (performOneMoreStep)
            {
                m_Manager.ResetTurn();

                // ************************************ Performing shot 
                _doAction = true;
                RequestDecision();
                
                // ************************************ Waiting rolling starting
                var ballPos = m_Manager.m_Whiteball.transform.position;
                while ((m_Manager.m_Whiteball.transform.position - ballPos).magnitude < 0.1f && !m_Manager.m_Whiteball.IsDead)
                {
                    yield return null;
                }
                
                // ************************************ Waiting rolling stopping
                do
                {
                    yield return new WaitForSeconds(_collectObservationPeriod);
                    RequestDecision();
                } while (!m_Manager.AreAllBallsStopped() && m_Manager.AllBallsAreOnTable() && !m_Manager.m_Whiteball.IsDead);
                
                // ************************************ Stop All movements
                m_Manager.SleepBalls();

                if (!m_Manager.AllBallsAreOnTable())
                {
                    SetReward(_illegalShotPenalty);
                    m_Manager.ResetTurn();
                    break;
                }

                // ************************************ Results evaluation 

                // cue ball is dead = end of the turn
                if (!m_Manager.WasTurnLegal())
                {
                    AddReward(_illegalShotPenalty);
                    break;
                }

                // Reward for Legal shot
                AddReward(_legalShotReward);
                
                // Rewards for caught balls
                foreach (var ball in m_Manager.m_LastTurnCaughtBalls)
                {
                    if (ball.Number == 9)
                    {
                        // Game won
                        SetReward(_victoryReward);
                        m_Manager.ResetGame();
                        break;
                    }
                    
                    AddReward(_legalShotBallCaughtReward);
                }
            }
            
            EndEpisode();
        }

    }
}