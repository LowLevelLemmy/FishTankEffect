using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FishTank : MonoBehaviour, IShootable
{
    [SerializeField] GameObject bulletHoleSprite;
    [SerializeField] GameObject waterParticle;
    [SerializeField] Vector2 remapRange;

    Material mat;
    float gunshotPoint = 999999999;
    float timeItTakesForTheEntireTankToEmpty = 10;

    // Start is called before the first frame update
    void Start()
    {
        mat = transform.GetChild(0).GetComponent<Renderer>().material;
    }

    public void OnShot(RaycastHit hit)
    {
        // spawn bullet hole decal
        var spawnedDecal = Instantiate(bulletHoleSprite, hit.transform);
        spawnedDecal.transform.position = hit.point;
        spawnedDecal.transform.rotation = Quaternion.LookRotation(hit.normal);

        print(hit.collider.transform.InverseTransformPoint(hit.point).y);

        float factor = CalcGunshotFactor(hit);
        if (factor < gunshotPoint)
        {
            gunshotPoint = factor;
            AnimateWaterToGunshotPoint(hit);
        }
    }

    float CalcGunshotFactor(RaycastHit hit)
    {
        float abba = hit.collider.transform.InverseTransformPoint(hit.point).y;
        return abba.Remap(remapRange.x, remapRange.y, 0, 1) - 0.04f;   // subtracted just to make sure it's BELOW the bullet hole
    }

    void AnimateWaterToGunshotPoint(RaycastHit hit)
    {
        //mat.DOComplete();   // Clear previous tweens... hopefully?    // it breaks the effect womp womp
        float previousValue = mat.GetFloat("GradientMultiplier");
        float distance = previousValue - gunshotPoint;
        float duration = distance * timeItTakesForTheEntireTankToEmpty;

        mat.DOFloat(gunshotPoint, "GradientMultiplier", duration).SetEase(Ease.OutSine);  // tween the water down

        // spawn particles
        var spawnedParticles = Instantiate(waterParticle, hit.transform);
        spawnedParticles.transform.position = hit.point;
        spawnedParticles.transform.rotation = Quaternion.LookRotation(hit.normal);
        
        // set particle duration
        var particleSys = spawnedParticles.GetComponent<ParticleSystem>();
        var main = particleSys.main;
        main.duration = duration;
        particleSys.Play();
    }
}
