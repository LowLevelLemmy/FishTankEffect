using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

enum FireMode
{
    SEMI,
    AUTO,
}

enum WepState
{
    UP,
    DOWN,
}

public class BaseWeapon : MonoBehaviour
{
    [Header("Settings")]
    public string weaponName; // name of weapon
    [SerializeField] float delayBetweenShots = 0.5f;
    [SerializeField] int projectilesPerShot = 1;
    [SerializeField] float reloadDuration = 1.5f;  // when we grant ammo
    [SerializeField] float entireReloadDuration =2.0f;    // when we're able to shoot again
    [SerializeField] float hipFireSpreadAngle = 10;
    [SerializeField] FireMode fireMode = FireMode.SEMI;
    [SerializeField] LayerMask layerMask;

    [Header("ADS Settings")]
    [SerializeField] float adsSpeadAngle = 5;
    public float adsSpeed;
    public float adsFOV;
    public Vector3 aimingPos;

    [Header("Debug Settings")]
    [SerializeField] bool bottomLessMag = false;

    [Header("Ammo")]
    [SerializeField] int maxAmmoMag = 6;    // ammo in each magazine
    [SerializeField] int maxAmmoHeld = 12;  // ammo you can hold on standby, should be a multiple of manAmmoMag

    int _currentAmmoInMag;
    public int currentAmmoInMag
    {
        get => _currentAmmoInMag;
        set
        {
            _currentAmmoInMag = value;
            owner?.onAmmoInMagChanged?.Invoke(_currentAmmoInMag);
            if (currentAmmoInMag <= 0)   // reload if we just got ammo and we empty
            {
                print("AQQI AQQI");
                TryReload();
            }
        }
    }
    
    int _currentAmmoHeld;   // this is not including ammo in current mag
    public int currentAmmoHeld
    {
        get => _currentAmmoHeld;
        set
        {
            _currentAmmoHeld = value;
            owner?.onAmmoHeldChanged?.Invoke(_currentAmmoHeld);
        }
    }

    [Header("Params")]
    [SerializeField] Animator animator;
    [SerializeField] ParticleSystem muzzleFlash;

    [Header("Audio")]
    [SerializeField] AudioClip fireNoise;
    [SerializeField] AudioClip reloadNoise;
    [SerializeField] AudioClip finishedReloadNoise;
    [SerializeField] AudioSource audSrc;

    [Header("Inputs")]
    bool wasPrimaryFireDownLastFrame;

    public bool reloading{ get; set; }
    public bool isEquiped{ get; private set; }
    bool isSprinting => owner.plrCont.isSprinting;
    
    float lastTimeFired = 0;
    PlayerWeaponManager owner;
    Transform parentSocket => owner?.weaponParentSocket;


    public void SetOwner(PlayerWeaponManager _owner)
    {
        owner = _owner;
    }

    void Awake()
    {
        currentAmmoInMag = maxAmmoMag;
        currentAmmoHeld = maxAmmoHeld;
    }

    void Start()
    {
        muzzleFlash ??= GetComponentInChildren<ParticleSystem>();
        animator ??= GetComponent<Animator>();
    }

    public void Equip()
    {
        parentSocket.transform.localPosition = owner.weaponDownPos.localPosition;
        ShowWeapon(true);
        isEquiped = true;
        owner.onAmmoHeldChanged?.Invoke(currentAmmoHeld);
        owner.onAmmoInMagChanged?.Invoke(currentAmmoInMag);
    }

    public void UnEquip()
    {
        isEquiped = false;
        parentSocket.DOLocalMove(owner.weaponDownPos.localPosition, 0.2f).OnComplete(() => ShowWeapon(false));
    }

    public void SetInputs(bool primaryFire, bool reload)
    {
        if (primaryFire)
        {
            if (fireMode.Equals(FireMode.AUTO))
                TryPrimaryFire();
            else if(fireMode.Equals(FireMode.SEMI) && !wasPrimaryFireDownLastFrame)   // primary fire has been pressed down this frame
                TryPrimaryFire();   // so fire
        }

        if (reload)
        {
            TryReload();
        }

        wasPrimaryFireDownLastFrame = primaryFire;
    }

    void TryPrimaryFire()
    {
        if (reloading) return;
        if (isSprinting) return;
        if (lastTimeFired + delayBetweenShots < Time.time)
        {
            if (currentAmmoInMag > 0)
                PrimaryFire();
        }
    }

    void PrimaryFire()  // actually fire
    {
        // effects:
        audSrc.pitch = Random.Range(.90f, 1.10f);
        if (fireNoise) audSrc.PlayOneShot(fireNoise);
        
        string animName = owner.adsing ? "ADSFire" : "Fire";
        animator.SetTrigger(animName);

        if (muzzleFlash)
            muzzleFlash?.Play();

        //  Logic:

        if (!bottomLessMag)
        {
            --currentAmmoInMag;
            owner.onAmmoInMagChanged?.Invoke(currentAmmoInMag);
        }

        lastTimeFired = Time.time;


        for (int i = 0; i < projectilesPerShot; ++i)
        {
            // actual gun firing logic
            Vector3 bulletDirection = GetShotDirectionWithinSpread(owner.plrCont.playerCam.transform);
            RaycastHit hit;
            if (Physics.Raycast(owner.plrCont.playerCam.transform.position, bulletDirection, out hit, 1000, layerMask, QueryTriggerInteraction.Ignore))
            {
                print("Hit: " + hit.collider.name);
                GameObject decal =  GameObject.CreatePrimitive(PrimitiveType.Sphere);

                decal.transform.position = hit.point;
                decal.transform.localScale = Vector3.one * 0.3f;
                decal.GetComponent<Renderer>().material.color = Color.red;
            }
        }

        if (currentAmmoInMag <= 0)  // reload if we at the end of the mag
        {
            print("abba");
            // DOVirtual.DelayedCall(delayBetweenShots, TryReload);
        }
    }

    void TryReload()
    {   
        if (currentAmmoInMag == maxAmmoMag) return; // if full mag
        if (currentAmmoHeld <= 0) return;
        if (reloading) return;
        if (isSprinting) return;

        StartReloadAnimation();
    }
    
    void StartReloadAnimation()
    {
        reloading = true;
        animator.SetTrigger("Reload");
        if (reloadNoise) audSrc.PlayOneShot(reloadNoise);
        StartCoroutine(ReloadingDelayThingy());
    }

    IEnumerator ReloadingDelayThingy()
    {
        float timeWhenTheReloadIsGranted = Time.time + reloadDuration;
        while (reloading)
        {
            if (Time.time >= timeWhenTheReloadIsGranted)
            {
                Reload();
                yield break;
            }
            yield return null;
        }
    }


    void Reload()   // just adds bullets to mag
    {
        if (!reloading) return; // we inturupted the realoding so don't give bullets
        int ammoNeeded = maxAmmoMag - currentAmmoInMag;
        if (ammoNeeded > currentAmmoHeld)   // partial reload
        {
            currentAmmoInMag += currentAmmoHeld;
            currentAmmoHeld = 0;
        }
        else
        {
            currentAmmoHeld -= ammoNeeded;
            currentAmmoInMag += ammoNeeded;
        }

        if (finishedReloadNoise) audSrc.PlayOneShot(finishedReloadNoise);
        owner.onAmmoInMagChanged?.Invoke(currentAmmoInMag);
        DOVirtual.DelayedCall(entireReloadDuration - reloadDuration, () => reloading = false);
    }

    public Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
    {
        float spreadAngleRatio = owner.adsing ? adsSpeadAngle : hipFireSpreadAngle;
        spreadAngleRatio /= 180;
        Vector3 spreadWorldDirection = Vector3.Slerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);

        return spreadWorldDirection;
    }

    public void SetSprintAnimation(bool setting)
    {
        animator.SetBool("isSprinting", setting);
    }

    public void ShowWeapon(bool show)
    {
        reloading = false;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;  // resets in case we swapped when it was animating previously
        gameObject.SetActive(show);
        isEquiped = show;
    }

    public void GiveMaxAmmo()
    {
        currentAmmoHeld = maxAmmoHeld;
        if (currentAmmoInMag <= 0)
            TryReload();
    }
}
