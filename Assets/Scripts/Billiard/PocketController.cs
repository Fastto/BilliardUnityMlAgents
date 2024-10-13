using System;
using UnityEngine;

namespace Billiard
{
    public class PocketController : MonoBehaviour
    {
        public Action<BallController> OnBallCaught;

        void OnTriggerEnter(Collider other) {
            if(other.TryGetComponent(out BallController ball)){
                OnBallCaught?.Invoke(ball);
            }
        }
    }
}
