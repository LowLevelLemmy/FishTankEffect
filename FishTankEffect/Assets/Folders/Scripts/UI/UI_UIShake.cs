using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class UI_UIShake : MonoBehaviour
{
    // Start is called before the first frame update
    void OnEnable()
    {
        transform.DOShakeRotation(5, 7, 0, 90, false).SetLoops(-1, LoopType.Incremental);
    }
}
