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
        Material newMtl = new Material(TeamcolorMaskMaterial);
        newMtl.color = owner.TeamColor;
        Renderer r = transform.GetComponent<Renderer>();
        bool foundMaterial = false;
        for (int i = 0; i < r.materials.Length; ++i)
        {
            if (r.materials[i].name == newMtl.name)
            {
                foundMaterial = true;
                r.materials[i] = newMtl;
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
    }

    public Material TeamcolorMaskMaterial;
}
