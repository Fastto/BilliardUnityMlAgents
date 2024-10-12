using Unity.MLAgentsExamples;
using UnityEngine;

public class ComponentRemover : MonoBehaviour
{
    [SerializeField]
    private bool DeleteRigidBody_;

    [SerializeField]
    private bool DeleteGroundContact_;

    void Awake()
    {
        if (DeleteRigidBody_)
        {
            DeleteComponent<Rigidbody>();
        }

        if (DeleteGroundContact_)
        {
            DeleteComponent<GroundContact>();
        }
    }

    private void DeleteComponent<T>() where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        foreach (T component in components)
        {
            Destroy(component);
        }
    }
}