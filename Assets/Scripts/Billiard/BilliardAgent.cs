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
        private float _collectObservationPeriod = 0.2f;
        
        // Variables
        private bool _doAction = false;
        private bool _resetedOnStart = false;
        
        // Rewards
        private int _whiteBallStartingPos;
        private int _ballsStartingPos;
        private int _targetMode;
        private bool _onlyOneAttempt;
        
        private float _victoryReward; //9th ball cought by legal shot
        private float _defeatPenalty; //Panalty for the player who didnt caught 9th ball
        private float _legalShotReward; // rewarding for legal shot
        private float _legalShotBallCaughtReward; // any ball caught by legal shot
        private float _illegalShotPenalty; // penalty for illegal shot
 

        public override void Initialize()
        {
            EnvironmentParameters envParams = Academy.Instance.EnvironmentParameters;

            _whiteBallStartingPos = Mathf.RoundToInt(envParams.GetWithDefault("white_ball_starting_pos", 0));
            _ballsStartingPos = Mathf.RoundToInt(envParams.GetWithDefault("balls_starting_pos", 0));
            _targetMode = Mathf.RoundToInt(envParams.GetWithDefault("target_mode", 0));
            _onlyOneAttempt = Mathf.RoundToInt(envParams.GetWithDefault("only_one_attempt", 0)) == 1;

            m_Manager.m_WhiteBallMode = (EnvironmentManager.WhiteBallMode) _whiteBallStartingPos;
            m_Manager.m_BallsMode  = (EnvironmentManager.BallsMode) _ballsStartingPos;
            m_Manager.m_TargetMode  = (EnvironmentManager.TargetMode) _targetMode;

            _victoryReward = envParams.GetWithDefault("victory_reward", 1.0f);
            _defeatPenalty = envParams.GetWithDefault("defeat_penalty", -1.0f);
            _legalShotReward = envParams.GetWithDefault("legal_shot_reward", 1f/36f);
            _legalShotBallCaughtReward = envParams.GetWithDefault("legal_shot_ball_caught_reward", 1f/9f);
            _illegalShotPenalty = envParams.GetWithDefault("illegal_shot_penalty", -1f/36f);
        }

        public override void OnEpisodeBegin()
        {
            if (!_resetedOnStart)
            {
                _resetedOnStart = true;
                m_Manager.ResetGame();
            }
            
            StartCoroutine(EpisodeCycle());
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // the real position of ball is -1.15 .. 1.45
            // to normalize value we have to - 0.15, result height -1.3...1.3
            // TABLE_HEIGHT = 2.5, to make observation -1...1 we have to device TABLE_HEIGHT / 2
            var baseHeight = (EnvironmentManager.TABLE_HEIGHT + .2f) / 2f;
            var baseWidth = (EnvironmentManager.TABLE_WIDTH + .2f) / 2f;
            
            sensor.AddObservation((m_Manager.m_Whiteball.transform.localPosition.x - 0.15f) / baseHeight);
            sensor.AddObservation(m_Manager.m_Whiteball.transform.localPosition.z / baseWidth);

            foreach (var pocket in m_Manager.m_Pockets)
            {
                sensor.AddObservation((pocket.transform.localPosition.x - 0.15f) / baseHeight);
                sensor.AddObservation(pocket.transform.localPosition.z / baseWidth);
            }

            foreach (var target in m_Manager.m_Balls)
            {
                sensor.AddObservation(target.IsDead);
                sensor.AddObservation(target.IsDead ? 0
                    : (target.transform.localPosition.x - 0.15f) / baseHeight);
                sensor.AddObservation(target.IsDead ? 0
                    : target.transform.localPosition.z / baseWidth);
                
                // instead of 1/ball_ID -separated observation for narking target
                sensor.AddObservation(target.Number == m_Manager.m_TargetBallNumber );
            }

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
            cont[1] = .02f;
            cont[0] = .9f;
            cont[2] = 1f;
        }
        
        private IEnumerator EpisodeCycle()
        {
            // true = Agent will make one more turn 
            // false = Agent want to start new Episode
            var performOneMoreStep = true;
            yield return null;
            while (performOneMoreStep)
            {
                m_Manager.ResetTurn();

                // ************************************ Performing shot 
                _doAction = true;
                RequestDecision();
                
                // ************************************ Waiting rolling starting
                var ballPos = m_Manager.m_Whiteball.transform.localPosition;
                while ((m_Manager.m_Whiteball.transform.localPosition - ballPos).magnitude < 0.1f 
                       && !m_Manager.m_Whiteball.IsDead)
                {
                    yield return null;
                }
                
                // ************************************ Waiting rolling stopping
                do
                {
                    yield return new WaitForSeconds(_collectObservationPeriod);
                    //RequestDecision();
                } while (!m_Manager.AreAllBallsStopped() && m_Manager.AllBallsAreOnTable() && !m_Manager.m_Whiteball.IsDead);
                
                // ************************************ Stop All movements
                m_Manager.SleepBalls();

                if (!m_Manager.AllBallsAreOnTable())
                {
                    SetReward(_illegalShotPenalty);
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

                if (_onlyOneAttempt)
                {
                    break;
                }
            }
            
            if (_onlyOneAttempt)
            {
                m_Manager.ResetGame();
            }
            
            EndEpisode();
        }

    }
}