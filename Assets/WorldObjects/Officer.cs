using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Officer : Infantry
{
    protected override void Start()
    {
        base.Start();
        Player owner = transform.root.GetComponent<Player>();
        if (owner != null)
        {
            Transform beret = transform.Find(BeretPath);
            Renderer r = beret.GetComponent<Renderer>();
            Material[] mtls = r.materials;
            Material newMtl = new Material(mtls[0]);
            newMtl.color = owner.TeamColor;
            mtls[0] = newMtl;
            beret.GetComponent<Renderer>().materials = mtls;
        }
    }

    private static readonly string BeretPath = "ROOT/Hips/Spine/Spine1/Neck/Head/beret3";
}
