using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuzzleFlashController : MonoBehaviour
{

	// Use this for initialization
	void Start ()
    {
        _particles = GetComponents<ParticleSystem>();
	}
	
	// Update is called once per frame
	void Update ()
    {
	}

    public void Play()
    {
        foreach (ParticleSystem p in _particles)
        {
            p.Play(true);
        }
    }

    private ParticleSystem[] _particles;
}
