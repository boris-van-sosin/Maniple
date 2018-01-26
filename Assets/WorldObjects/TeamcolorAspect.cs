using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamcolorAspect : MonoBehaviour
{
	// Use this for initialization
	void Start ()
    {
		
	}

    public void SetTeamColor()
    {
        Player owner = transform.root.GetComponent<Player>();
        Material newMtl;
        if (!owner.TryGetTeamColorMaterial(TeamcolorMaskMaterial.name, out newMtl))
        {
            newMtl = new Material(TeamcolorMaskMaterial);
            newMtl.color = owner.TeamColor;
            owner.CacheTeamColorMaterial(newMtl.name, newMtl);
        }
        Renderer r = transform.GetComponent<Renderer>();
        Material[] mtls = r.materials;
        bool foundMaterial = false;
        for (int i = 0; i < r.materials.Length; ++i)
        {
            if (mtls[i].name.StartsWith(newMtl.name))
            {
                foundMaterial = true;
                mtls[i] = newMtl;
                break;
            }
        }
        if (!foundMaterial)
        {
            Material[] newMtlsArray = new Material[r.materials.Length + 1];
            r.materials.CopyTo(newMtlsArray, 0);
            newMtlsArray[r.materials.Length] = newMtl;
            r.materials = newMtlsArray;
        }
        else
        {
            r.materials = mtls;
        }
    }

    public Material TeamcolorMaskMaterial;
}
