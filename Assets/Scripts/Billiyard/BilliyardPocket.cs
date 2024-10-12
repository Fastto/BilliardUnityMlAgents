using System;
using UnityEngine;

namespace Billiyard
{
    public class BilliyardPocket : MonoBehaviour
    {
        public Action<BilliyardBall> OnBallCaught;

        void OnTriggerEnter(Collider other) {
            if(other.TryGetComponent(out BilliyardBall ball)){
                OnBallCaught?.Invoke(ball);
            }
        }
    }
}
