using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Billiard
{
    public class EnvironmentManager : MonoBehaviour
    {
        // enums
        public enum WhiteBallMode
        {
            GAME = 0,
            TABLE_CENTER = 1
        }
        
        public enum BallsMode
        {
            GAME = 0,
            RANDOM = 1
        }
        
        public enum TargetMode
        {
            GAME = 0,
            RANDOM = 1
        }
        
        // References
        public BallController m_Whiteball;
        public List<PocketController> m_Pockets;
        public List<BallController> m_Balls;
        public List<Transform> m_StartingPositions;

        public WhiteBallMode m_WhiteBallMode { get; set; }
        public BallsMode m_BallsMode { get; set; }
        public TargetMode m_TargetMode { get; set; } 

        // Configuration
        public float m_MaxShotPower = 2.0f;

        // Variables
        public List<BallController> m_LastTurnCaughtBalls { get; private set; } = new List<BallController>();
        public int m_TargetBallNumber { get; private set; }
        
        private List<Vector3> m_BallsInitPos = new List<Vector3>();
        private Vector3 m_WhiteBallInitPos;

        // Const
        public const float TABLE_WIDTH = 1.4f;
        public const float TABLE_LENGTH = 2.8f;

        private void Start()
        {
            // Storing Starting positions
            m_WhiteBallInitPos = m_Whiteball.transform.localPosition;
            m_Balls.ForEach(ball => m_BallsInitPos.Add(ball.transform.localPosition));
        
            // Assign handler for catching the ball
            m_Pockets.ForEach(pocket => pocket.OnBallCaught += OnBallCaughtHandler);
        }

        public bool WasTurnLegal()
        {
            // 1. Cue ball is not dead
            if (m_Whiteball.IsDead) 
                return false;

            // 2. Cue ball touched target ball first
            if (m_Whiteball.FirstHitTargetNum != m_TargetBallNumber)
                return false;
        
            // 3. At least one ball touched border 
            if (!WasBorderTouched())
                return false;
        
            return true;
        }

        private void RestoreWhiteBall(bool placeToInitialPosition = false)
        {
            var initPos = m_WhiteBallMode == WhiteBallMode.GAME 
                ? m_WhiteBallInitPos 
                : m_StartingPositions[0].localPosition;

            m_Whiteball.ResetBall(placeToInitialPosition ? initPos : GetSafePos());
        }
        
        private void RestoreBall(int id, bool placeToInitialPosition = false)
        {
            m_Balls[id].ResetBall(placeToInitialPosition ? m_BallsInitPos[id] : GetSafePos());
        }

        private void RestoreBalls(int qty = 9)
        {
            for (int i = 0; i < qty; i++)
            {
                m_Balls[i].ResetBall(m_BallsMode == BallsMode.GAME ? m_BallsInitPos[i] : GetSafePos());
            }

            if (qty >= 9) return;
            
            for (int i = qty-1; i < 9; i++) 
                OnBallCaughtHandler(m_Balls[i]);

        }

        public void ResetTurn()
        {
            if (m_Whiteball.IsDead) RestoreWhiteBall();
            if (m_Balls[8].IsDead) RestoreBall(8);

            m_Balls.ForEach(t => t.StopBall());
            m_Whiteball.StopBall();

            m_Whiteball.FirstHitTargetNum = -1;
            m_LastTurnCaughtBalls.Clear();
            m_TargetBallNumber = DefineTarget();
        }

        public void ResetGame(int ballsQuantity = 9)
        {
            RestoreWhiteBall(false);
            RestoreBalls(ballsQuantity);

            m_TargetBallNumber = DefineTarget();
        }
        
        private int DefineTarget()
        {
            if (m_TargetMode == TargetMode.GAME)
            {
                foreach (var ball in m_Balls.Where(ball => !ball.IsDead))
                    return ball.Number;
            }
            else
            {
                BallController ball;
                do
                {
                    ball = m_Balls[Random.Range(0, m_Balls.Count)];
                } while (ball.IsDead);
                return ball.Number;
            }

            throw new Exception("No available tragets");
        }

        private Vector3 GetSafePos()
        {
            Vector3 safePosition;
            Collider[] hitColliders;
            int layerMask = LayerMask.GetMask("Ball");

            do
            {
                float randomZ = Random.Range(-0.4f, 0.4f);
                float randomX = Random.Range(-1.0f, 1.0f);
                safePosition = new Vector3(randomX, m_WhiteBallInitPos.y, randomZ);

                hitColliders =
                    Physics.OverlapSphere(safePosition, m_Whiteball.GetComponentInChildren<SphereCollider>().radius, layerMask);
            } while (hitColliders.Length > 0);

            return safePosition;
        }

        public void SleepBalls()
        {
            m_Whiteball.Rb.Sleep();
            m_Balls.ForEach(b => b.Rb.Sleep());
        }

        public bool AreAllBallsStopped()
        {
            if (!m_Whiteball.IsDead && m_Whiteball.Rb.velocity.sqrMagnitude > 0.001f)
                return false;

            foreach (var ball in m_Balls)
            {
                if (!ball.IsDead && ball.Rb.velocity.sqrMagnitude > 0.001f)
                    return false;
            }

            return true;
        }
        
        private void OnBallCaughtHandler(BallController ballController)
        {
            ballController.IsDead = true;
            if (!ballController.IsWhite())
                m_LastTurnCaughtBalls.Add(ballController);
            
            ballController.gameObject.SetActive(false);
        }

        // TODO: implement border touching detection during the turn
        private bool WasBorderTouched()
        {
            return true;
        }

        public void Shot(Vector3 direction, float power)
        {
            power *= m_MaxShotPower;
            m_Whiteball.Rb.AddForce(direction * power, ForceMode.VelocityChange);
        }
        
    }
}
