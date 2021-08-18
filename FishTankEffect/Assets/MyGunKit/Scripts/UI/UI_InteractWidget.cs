using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UI_InteractWidget : MonoBehaviour
{
    [SerializeField] PlayerInteractioner playerInteracter;
    [SerializeField] TextMeshProUGUI txt;

    void Awake()
    {
        txt ??= GetComponentInChildren<TextMeshProUGUI>();
        playerInteracter ??= FindObjectOfType<PlayerInteractioner>();
        playerInteracter.OnInteractTxtShouldComeUpNow += UpdateTxt;
        playerInteracter.OnLostSightOfInteraction += OnInteractSightLoss;
        
        txt.gameObject.SetActive(false);
    }

    void UpdateTxt(IInteractable interactable)
    {
        txt.gameObject.SetActive(true);
        txt.text = interactable.interactTxt;
    }

    void OnInteractSightLoss()
    {
        txt.gameObject.SetActive(false);
    }
}
