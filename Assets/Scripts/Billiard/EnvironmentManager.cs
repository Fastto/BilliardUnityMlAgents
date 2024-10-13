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
        // References
        public BallController m_Whiteball;
        public List<PocketController> m_Pockets;
        public List<BallController> m_Balls;

        // Configuration
        public float m_MaxShotPower = 2.0f;
        public float m_MaxDistanceToTable = 5f;

        // Variables
        public List<BallController> m_LastTurnCaughtBalls { get; private set; } = new List<BallController>();
        public int m_TargetBallNumber { get; private set; }
        
        private List<Vector3> m_BallsInitPos = new List<Vector3>();
        private Vector3 m_WhiteBallInitPos;

        // Const
        public const float TABLE_WIDTH = 1.2f;
        public const float TABLE_HEIGHT = 2.5f;
        
        private void Start()
        {
            // Storing Starting positions
            m_WhiteBallInitPos = m_Whiteball.transform.position;
            m_Balls.ForEach(ball => m_BallsInitPos.Add(ball.transform.position));
        
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
            m_Whiteball.gameObject.SetActive(true);
            m_Whiteball.transform.position = placeToInitialPosition ? m_WhiteBallInitPos : GetSafePos();
            m_Whiteball.IsDead = false;
            m_Whiteball.Rb.velocity = Vector3.zero;
            m_Whiteball.Rb.angularVelocity = Vector3.zero;
        }
    
        private void RestoreBall(int id, bool placeToInitialPosition = false)
        {
            m_Balls[id].gameObject.SetActive(true);
            m_Balls[id].transform.position = placeToInitialPosition ? m_BallsInitPos[id] : GetSafePos();
            m_Balls[id].IsDead = false;
            m_Balls[id].Rb.velocity = Vector3.zero;
            m_Balls[id].Rb.angularVelocity = Vector3.zero;
        }

        public void ResetTurn()
        {
            if (m_Whiteball.IsDead) RestoreWhiteBall();
            if (m_Balls[8].IsDead) RestoreBall(8);

            if ((m_Whiteball.transform.position - transform.position).magnitude > m_MaxDistanceToTable)
                RestoreWhiteBall();

            for (int i = 0; i < m_Balls.Count; i++)
            {
                if(!m_Balls[i].IsDead && (m_Balls[i].transform.position - transform.position).magnitude > m_MaxDistanceToTable)
                    RestoreBall(i);
            }
            
            m_Balls.ForEach(t => {
                t.Rb.velocity = Vector3.zero;
                t.Rb.angularVelocity = Vector3.zero;
                t.Rb.Sleep();
            });

            m_Whiteball.Rb.velocity = Vector3.zero;
            m_Whiteball.Rb.angularVelocity = Vector3.zero;
            m_Whiteball.Rb.Sleep();

            m_Whiteball.FirstHitTargetNum = -1;
            m_LastTurnCaughtBalls.Clear();
            m_TargetBallNumber = DefineTarget();
        }

        public void ResetGame()
        {
            RestoreWhiteBall(true);
            for (int i = 0; i < m_BallsInitPos.Count; i++) 
                RestoreBall(i, true);
        
            m_TargetBallNumber = 1;
        }
        
        private int DefineTarget()
        {
            foreach (var ball in m_Balls.Where(ball => !ball.IsDead))
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
                float randomX = Random.Range(-0.5f, 0.5f);
                float randomZ = Random.Range(-1.3f, 1.0f);
                safePosition = new Vector3(randomX, m_WhiteBallInitPos.y, randomZ);

                hitColliders =
                    Physics.OverlapSphere(safePosition, m_Whiteball.GetComponent<SphereCollider>().radius, layerMask);
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

        public bool AllBallsAreOnTable()
        {
            if ((m_Whiteball.transform.position - transform.position).magnitude > m_MaxDistanceToTable)
                return false;

            foreach (var ball in m_Balls)
            {
                if(!ball.IsDead && (ball.transform.position - transform.position).magnitude > m_MaxDistanceToTable)
                    return false;
            }
            
            return true;
        }
    }
}
