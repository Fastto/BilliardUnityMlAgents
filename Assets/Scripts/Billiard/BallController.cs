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

        private void OnCollisionEnter(Collision other) {
            if(isWhite_ && FirstHitTargetNum == -1 && other.gameObject.tag == "targetBall")
            {
                BallController ballController = other.gameObject.GetComponent<BallController>();
                FirstHitTargetNum = ballController.Number;
            }
        }
    }
}
