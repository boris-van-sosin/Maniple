using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : WorldObject
{
    protected override void Awake()
    {
        base.Awake();
        SetBounds();
    }

    protected override void OnGUI()
    {
        base.OnGUI();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected void SetBounds()
    {
        _selectionBounds = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            _selectionBounds.Encapsulate(r.bounds);
        }
    }

    protected Bounds _selectionBounds;
    public Formation ProductionTargetFormation { get; set; }
}
