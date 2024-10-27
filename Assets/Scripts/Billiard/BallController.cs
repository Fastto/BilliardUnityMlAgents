using System;
using UnityEngine;

namespace Billiard
{
    public class BallController : MonoBehaviour
    {
        public int Number;

        [SerializeField]
        bool isWhite_;

        public Rigidbody Rb;
        public bool IsDead;
        public int FirstHitTargetNum = -1;

        void Start()
        {
            Rb = GetComponent<Rigidbody>();
        }

        public bool IsWhite()
        {
            return isWhite_;
        }
        
        void FixedUpdate()
        {
            // var rollingFriction = 0.1f;
            // // Check if the ball is moving but not rolling
            // if (Rb.velocity.magnitude > 0.1f && Rb.angularVelocity.magnitude < Rb.velocity.magnitude)
            // {
            //     // Apply rolling friction to simulate realistic rolling
            //     Vector3 rollingTorque = Vector3.Cross(Rb.velocity.normalized, Vector3.up) * rollingFriction;
            //     Rb.AddTorque(rollingTorque, ForceMode.Force);
            // }
        }

        private void OnCollisionEnter(Collision other) {
            if(isWhite_ && FirstHitTargetNum == -1 && other.gameObject.tag == "targetBall")
            {
                BallController ballController = other.transform.GetComponent<BallController>();
                FirstHitTargetNum = ballController.Number;
            }
        }

        public void ResetBall(Vector3 position)
        {
            transform.localPosition = position;
            IsDead = false;
            StopBall();
            gameObject.SetActive(true);
        }
        
        public void HideBall()
        {
            IsDead = true;
            gameObject.SetActive(false);
        }

        public void StopBall()
        {
            Rb.velocity = Vector3.zero;
            Rb.angularVelocity = Vector3.zero;
            Rb.Sleep();
        }
    }
}
