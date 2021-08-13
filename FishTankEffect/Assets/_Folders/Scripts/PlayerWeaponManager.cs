using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using EasyButtons;


public class PlayerWeaponManager : MonoBehaviour
{
    //events:
    public UnityAction<int> onAmmoHeldChanged;
    public UnityAction<int> onAmmoInMagChanged;

    [Header("Params")]
    public Transform weaponParentSocket;
    [SerializeField] Transform weaponDefaultPos;
    public Transform weaponDownPos;
    [SerializeField] Transform weaponADSPos;
    [SerializeField] BaseWeapon[] startingWeapons;
    public PlayerController plrCont;

    public bool adsing;

    // private members
    BaseWeapon[] weaponSlots = new BaseWeapon[2];
    int equipedWeaponIndex = -1;
    PlayerInputHandler inputHandler;

    void Start()
    {
        foreach (var wep in startingWeapons)
            AddWeapon(wep);

        weaponParentSocket.localPosition = weaponDefaultPos.localPosition;

        inputHandler ??= GetComponent<PlayerInputHandler>();
        plrCont ??= GetComponent<PlayerController>();
        plrCont.onSprint += OnSprint;
    }

    public void AddWeapon(BaseWeapon newWeapon)
    {
        if (HasWeapon(newWeapon, out var weaponWeHave))
        {
            weaponWeHave.GiveMaxAmmo();
            return;
        }

        for (int i = 0; i < weaponSlots.Length; ++i)
        {
            if (weaponSlots[i] == null)
            {
                GameObject wep = Instantiate(newWeapon.gameObject, weaponParentSocket);
                wep.transform.localPosition = Vector3.zero;
                wep.transform.localRotation = Quaternion.identity;
                weaponSlots[i] = wep.GetComponent<BaseWeapon>();
                weaponSlots[i].SetOwner(this);
                weaponSlots[i].ShowWeapon(false);

                SwapToWeaponAtIndex(i);
                return;
            }
        }
        print("ADD weapon FAILED");
    }

    void SwapToWeaponAtIndex(int i)
    {
        if (weaponSlots[i] == null)
            return;

        if (GetActiveWeapon() == weaponSlots[i])
            return;

        GetActiveWeapon()?.UnEquip();
        equipedWeaponIndex = i;
        
        weaponSlots[i].Equip();
    }

    void UpdateWeaponPos()
    {
        BaseWeapon wep = GetActiveWeapon();
        if (!wep) return;
        if (!wep.isEquiped) return;
        if (adsing && !plrCont.isSprinting)
        {
            plrCont.playerCam.fieldOfView = Mathf.Lerp(plrCont.playerCam.fieldOfView, wep.adsFOV, wep.adsSpeed * Time.deltaTime);
            weaponParentSocket.localPosition = Vector3.Lerp(weaponParentSocket.localPosition, wep.aimingPos, wep.adsSpeed * Time.deltaTime);
        }
        else
        {
            plrCont.playerCam.fieldOfView = Mathf.Lerp(plrCont.playerCam.fieldOfView, 90, wep.adsSpeed * Time.deltaTime);
            weaponParentSocket.localPosition = Vector3.Lerp(weaponParentSocket.localPosition, weaponDefaultPos.localPosition, wep.adsSpeed * Time.deltaTime);
        }
    }

    public BaseWeapon GetActiveWeapon()
    {
        return GetWeaponAtIndex(equipedWeaponIndex);
    }

    public BaseWeapon GetWeaponAtIndex(int i)
    {
        if (i >= 0 && i < weaponSlots.Length)
            return weaponSlots[i];
        else return null;
    }


    void Update()
    {
        adsing = inputHandler.GetAimInputHeld();
        GetActiveWeapon()?.SetInputs(inputHandler.GetFireInputHeld(), inputHandler.GetReloadButtonDown());

        int wepToSwapTo = inputHandler.GetSelectWeaponInput();
        if (wepToSwapTo != -1)
            SwapToWeaponAtIndex(wepToSwapTo-1);

        int scrollDir = inputHandler.GetSwitchWeaponInput();
        SwapWeaponViaDirection(scrollDir);

    }

    void SwapWeaponViaDirection(int i)
    {
        
        if (GetWeaponAtIndex(equipedWeaponIndex + i) != null)
            SwapToWeaponAtIndex(equipedWeaponIndex + i);
            
        else if (GetWeaponAtIndex(equipedWeaponIndex - i) != null)
            SwapToWeaponAtIndex(equipedWeaponIndex - i);
    }

    void LateUpdate()
    {
        UpdateWeaponPos();
    }

    public void SetWeaponSprintAnimation(bool setting)
    {
        BaseWeapon wep = GetActiveWeapon();

        if (wep)
            wep.SetSprintAnimation(setting);
    }

    void OnSprint()
    {
        if (GetActiveWeapon())
            GetActiveWeapon().reloading = false;
    }

    public bool HasWeapon(BaseWeapon wep, out BaseWeapon weaponWeHave)
    {
        weaponWeHave = null;

        if (weaponSlots == null || weaponSlots.Length == 0) return false;

        for (int i = 0; i < weaponSlots.Length; ++i)
        {
            if (weaponSlots[i]?.weaponName == wep.weaponName)  // does this mean they're the same instance or just the same type??
            {
                weaponWeHave = weaponSlots[i];
                return true;
            }
        }
        return false;
    }

    public bool HasWeapon(BaseWeapon wep)
    {
        if (weaponSlots == null || weaponSlots.Length == 0) return false;

        for (int i = 0; i < weaponSlots.Length; ++i)
        {
            if (weaponSlots[i]?.weaponName == wep.weaponName)  // does this mean they're the same instance or just the same type??
            {
                return true;
            }
        }
        return false;
    }
}
