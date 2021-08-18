using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerInteractioner : MonoBehaviour
{
    [SerializeField] LayerMask layerMsk;
    
    public UnityAction<IInteractable> OnInteractTxtShouldComeUpNow;
    public UnityAction OnLostSightOfInteraction;

    public PlayerController plrCon;

    Transform camTran => plrCon.playerCam.transform;

    void Awake()
    {
        plrCon ??= GetComponent<PlayerController>();
    }

    void Update()
    {
        Collider[] hitCols = Physics.OverlapSphere(camTran.position + camTran.forward * 0.5f, 0.75f, layerMsk, QueryTriggerInteraction.Collide);
        if (hitCols.IsNullOrEmpty())
        {
            OnLostSightOfInteraction?.Invoke();
            return;
        }

        if (hitCols[0].TryGetComponent<IInteractable>(out var interactable))
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                interactable.OnInteractedWith(this);
            }

            interactable.OnLookedAt(this);
            OnInteractTxtShouldComeUpNow?.Invoke(interactable);
        }
    }
}
