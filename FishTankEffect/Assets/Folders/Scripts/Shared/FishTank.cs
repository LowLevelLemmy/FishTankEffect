using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FishTank : MonoBehaviour, IShootable
{
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
        // TODO: Generate gunshot decal
        // TODO: spawn water leaking particle effect for the duration of the thingy

        float factor = CalcGunshotFactor(hit);
        if (factor < gunshotPoint)
        {
            gunshotPoint = factor;
            LerpToGunshotPoint();
        }
    }

    float CalcGunshotFactor(RaycastHit hit)
    {
        float abba = hit.collider.transform.InverseTransformPoint(hit.point).y;
        return abba.Remap(-0.5f, 0.5f, 0, 1);
    }

    void LerpToGunshotPoint()
    {
        mat.DOComplete();   // Clear previous tweens... hopefully?

        float previousValue = mat.GetFloat("GradientMultiplier");
        float distance = previousValue - gunshotPoint;

        float duration = distance * timeItTakesForTheEntireTankToEmpty;

        mat.DOFloat(gunshotPoint, "GradientMultiplier", duration);
    }
}
