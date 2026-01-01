using UnityEngine;
using DG.Tweening;

public class TestDOTween : MonoBehaviour
{
    void Start()
    {
        // Fai scalare il pulsante avanti-indietro
        transform.DOScale(1.2f, 1f).SetLoops(-1, LoopType.Yoyo);
    }
}
