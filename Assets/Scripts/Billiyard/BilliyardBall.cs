using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BilliyardBall : MonoBehaviour
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
            BilliyardBall ball = other.gameObject.GetComponent<BilliyardBall>();
            FirstHitTargetNum = ball.Number;
        }
    }
}
