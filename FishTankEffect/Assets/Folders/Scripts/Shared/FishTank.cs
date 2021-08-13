using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FishTank : MonoBehaviour, IShootable
{
    Material mat;

    // Start is called before the first frame update
    void Start()
    {
        mat = transform.GetChild(0).GetComponent<Renderer>().material;
    }

    public void OnShot(RaycastHit hit)
    {
        print("SHOTTT!");
        float abba = hit.collider.transform.InverseTransformPoint(hit.point).y;
        float factor = abba.Remap(-0.5f, 0.5f, 0, 1);
        mat.SetFloat("GradientMultiplier", factor);
    }
}
