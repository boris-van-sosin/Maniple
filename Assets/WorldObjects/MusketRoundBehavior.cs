using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusketRoundBehavior : MonoBehaviour
{
	// Use this for initialization
	void Start ()
    {
        _vecVelocity = (_target - transform.position).normalized * Velocity;
        _startPos = transform.position;
    }
	
	// Update is called once per frame
	void FixedUpdate ()
    {
        if ((transform.position - _startPos).sqrMagnitude > _sqrDistToTravel)
        {
            Destroy(gameObject);
        }
        transform.position += _vecVelocity;
	}

    public void SetColor(Color c)
    {
        LineRenderer r = transform.GetComponent<LineRenderer>();
        if (r != null)
        {
            r.startColor = Color.white;
            r.endColor = c;
        }
    }

    public void SetTarget(Vector3 target)
    {
        _target = target;
        _sqrDistToTravel = (_target - _startPos).sqrMagnitude;
    }

    private Vector3 _target, _startPos;
    float _sqrDistToTravel;

    public float Velocity;
    private Vector3 _vecVelocity;
}
