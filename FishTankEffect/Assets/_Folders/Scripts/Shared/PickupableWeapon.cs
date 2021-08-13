using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupableWeapon : MonoBehaviour, IInteractable
{
    [SerializeField] BaseWeapon pickupableWeapon;
    string pickUpForTheFirstTimeTxt => "Hold F to pickup " + pickupableWeapon.weaponName;

    public string interactTxt { get => pickUpForTheFirstTimeTxt; }

    public void OnInteractedWith(PlayerInteractioner plrInteractor)
    {
        plrInteractor.plrCon.weaponManager.AddWeapon(pickupableWeapon);
    }

    public void OnLookedAt(PlayerInteractioner plrInteractor)
    {

    }
}
