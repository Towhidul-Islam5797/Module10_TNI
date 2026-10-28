using UnityEngine;
using UnityEngine.Events;

public class AttackEventRelay : MonoBehaviour
{
    [SerializeField] private UnityEvent onHitFrame;

    public void HitFrame()
    {
        onHitFrame.Invoke();
    }
}
