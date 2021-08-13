using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UI_AmmoDisplayWidget : MonoBehaviour
{
    [SerializeField] PlayerWeaponManager weaponManger;
    
    [SerializeField] TextMeshProUGUI ammoHeldTxt;
    [SerializeField] TextMeshProUGUI ammoInMagTxt;
    
    void Start()
    {
        weaponManger ??= FindObjectOfType<PlayerWeaponManager>();
        weaponManger.onAmmoHeldChanged += UpdateAmmoHeld;
        weaponManger.onAmmoInMagChanged += UpdateAmmoInMag;
    }

    void UpdateAmmoHeld(int newVal)
    {
        ammoHeldTxt.text = newVal.ToString();
    }

    void UpdateAmmoInMag(int newVal)
    {
        ammoInMagTxt.text = newVal.ToString();
    }
}
