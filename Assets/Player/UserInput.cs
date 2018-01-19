using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Maniple;
using UnityEngine.EventSystems;

public class UserInput : MonoBehaviour
{
    private void Awake()
    {
        UILayerBitmask = LayerMask.GetMask("UI");
    }

    // Use this for initialization
    void Start ()
    {
        _player = transform.root.GetComponent<Player>();
        _orderLR = transform.GetComponentInChildren<LineRenderer>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        //Debug.Log("UserInput.Update");
		if (_player.Human)
        {
            MoveCamera();
            RotateCamers();
            MouseActivity();
            //Debug.Log("Moving camera...");
        }
	}

    private void MoveCamera()
    {
        float xPos = Input.mousePosition.x;
        float yPos = Input.mousePosition.y;
        Vector3 movement = new Vector3(0, 0, 0);

        bool keyboardScroll = false;
        if (Input.GetKey(KeyCode.A))
        {
            movement.x = -ResourceManager.Controls.KeyboardScrollSpeed;
            keyboardScroll = true;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            movement.x = ResourceManager.Controls.KeyboardScrollSpeed;
            keyboardScroll = true;
        }
        if (Input.GetKey(KeyCode.S))
        {
            movement.z = -ResourceManager.Controls.KeyboardScrollSpeed;
            keyboardScroll = true;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            movement.z = ResourceManager.Controls.KeyboardScrollSpeed;
            keyboardScroll = true;
        }

        if (!keyboardScroll)
        {
            if (0 <= xPos && xPos < ResourceManager.Controls.ScrollMargin)
            {
                movement.x = -ResourceManager.Controls.MouseScrollSpeed;
            }
            else if ((Screen.width - ResourceManager.Controls.ScrollMargin) < xPos && xPos <= Screen.width)
            {
                movement.x = ResourceManager.Controls.MouseScrollSpeed;
            }

            if (0 <= yPos && yPos < ResourceManager.Controls.ScrollMargin)
            {
                movement.z = -ResourceManager.Controls.MouseScrollSpeed;
            }
            else if ((Screen.height - ResourceManager.Controls.ScrollMargin) < yPos && yPos <= Screen.height)
            {
                movement.z = ResourceManager.Controls.MouseScrollSpeed;
            }
        }

        movement = Camera.main.transform.TransformDirection(movement);
        movement.y = - ResourceManager.Controls.MouseWheelVScrollSpeed * Input.GetAxis("Mouse ScrollWheel");
        Vector3 dest = Camera.main.transform.position + movement;

        dest.y = Math.Min(dest.y, ResourceManager.Controls.MaxCameraHeight);
        dest.y = Math.Max(dest.y, ResourceManager.Controls.MinCameraHeight);

        Vector3 origin = Camera.main.transform.position;
        if (dest != origin)
        {
            Camera.main.transform.position = Vector3.MoveTowards(origin, dest, Time.deltaTime * ResourceManager.Controls.GlobalScrollCoefficient);
        }
    }

    private void RotateCamers()
    {
        Vector3 origin = Camera.main.transform.eulerAngles;
        Vector3 dest = origin;

        bool keyRotate = false;
        if (Input.GetKey(KeyCode.Q))
        {
            dest.y -= ResourceManager.Controls.KeyboardRotateSpeed;
            keyRotate = true;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            dest.y += ResourceManager.Controls.KeyboardRotateSpeed;
            keyRotate = true;
        }

        if (!keyRotate)
        {
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetMouseButton(1))
            {
                dest.x -= Input.GetAxis("Mouse Y") * ResourceManager.Controls.MouseRotateSpeed;
                dest.y += Input.GetAxis("Mouse X") * ResourceManager.Controls.MouseRotateSpeed;
            }
        }

        if (dest!= origin)
        {
            Camera.main.transform.eulerAngles = Vector3.MoveTowards(origin, dest, Time.deltaTime * ResourceManager.Controls.GlobalRotateCoefficient);
        }
    }

    private void MouseActivity()
    {
        if (Input.GetMouseButtonDown(0))
        {
            MouseLeftClick();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            MouseRightClickStart();
        }
        else if (Input.GetMouseButton(1))
        {
            MouseRightClickUpdate();
        }
        else if (Input.GetMouseButtonUp(1))
        {
            MouseRightClickComplete();
        }
    }

    private void MouseLeftClick()
    {
        if (MouseInBounds())
        {
            ClickHitObject hitObj = FindHitObject();
            if (hitObj != null && hitObj.HitObject.name != "Ground")
            {
                WorldObject worldObject = hitObj.HitObject.transform.GetComponent<WorldObject>();
                if (worldObject != null)
                {
                    _player.ChangeSelection(worldObject.GetSelectionObject());
                    worldObject.MouseClick(hitObj, _player);
                }
            }
            if (hitObj == null || hitObj.HitObject.name == "Ground")
            {
                _player.DeselectCurrent();
            }
        }
    }

    private void MouseRightClickStart()
    {
        if (MouseInBounds())
        {
            ClickHitObject hitObj = FindHitObject();
            WorldObject SelectedObj = _player.SelectedObject;
            if (SelectedObj != null && hitObj != null)
            {
                if (hitObj.HitObject.transform.GetComponent<Unit>() != null)
                {
                    _startRClickHit = null;
                    SelectedObj.IssueOrder(hitObj, _player);
                }
                else
                {
                    _startRClickHit = hitObj;
                    _startRClickTime = Time.time;
                }
            }
        }
    }

    private void MouseRightClickComplete()
    {
        if (MouseInBounds())
        {
            ClickHitObject hitObj = FindHitObject();
            WorldObject SelectedObj = _player.SelectedObject;
            if (SelectedObj != null && hitObj != null)
            {
                if (Time.time - _startRClickTime < StartRClickThreshold ||
                    (_startRClickHit == null || hitObj.HitLocation == _startRClickHit.HitLocation))
                {
                    SelectedObj.IssueOrder(hitObj, _player);
                }
                else if (_startRClickHit != null || hitObj.HitLocation != _startRClickHit.HitLocation)
                {
                    SelectedObj.IssueOrder(hitObj, _startRClickHit, _player);
                }
            }
        }
        _startRClickHit = null;
        _orderLR.enabled = false;
    }

    private void MouseRightClickUpdate()
    {
        if (MouseInBounds())
        {
            ClickHitObject hitObj = FindHitObject();
            WorldObject SelectedObj = _player.SelectedObject;
            if (SelectedObj != null && hitObj != null && _startRClickHit != null && _orderLR != null)
            {
                SelectedObj.DisplayOrderMarker(_orderLR, _startRClickHit, hitObj);
                _orderLR.enabled = true;
            }
        }
    }

    private ClickHitObject FindHitObject()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return new ClickHitObject() { HitObject = hit.collider.gameObject, HitLocation = hit.point };
        }
        else {
            return null;
        }
    }

    private bool MouseInBounds()
    {
        return !EventSystem.current.IsPointerOverGameObject();
    }

    private Player _player;

    private ClickHitObject _startRClickHit;
    private float _startRClickTime;
    private static readonly float StartRClickThreshold = 0.5f;
    private LineRenderer _orderLR;
    private int UILayerBitmask;
}
