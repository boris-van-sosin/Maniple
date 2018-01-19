using Maniple;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUD : MonoBehaviour
{

	// Use this for initialization
	void Start ()
    {
        _player = transform.root.GetComponent<Player>();
        _resourceIcons = new Dictionary<GameResources.ResourceType, Texture2D>()
        {
            { GameResources.ResourceType.Money, HUDIcons[0] }
        };
    }
	
	// Update is called once per frame
	void Update ()
    {
	}

    private void OnGUI()
    {
        if (_player != null && _player.Human)
        {
            //DrawOrdersBar();
            //DrawResourceBar();
        }
    }

    private void DrawOrdersBar()
    {
        GUI.skin = OrdersSkin;
        GUI.BeginGroup(new Rect(0, Screen.height - ResourceManager.UISettings.OrdersBarSize, Screen.width, ResourceManager.UISettings.OrdersBarSize));
        GUI.Box(new Rect(0, 0, Screen.width, ResourceManager.UISettings.OrdersBarSize), "");
        if (_player.SelectedObject != null && _player.SelectedObject.ProductionName != "")
        {
            GUI.Label(new Rect(ResourceManager.UISettings.SelectedLabelHOffset, 10, Screen.width - ResourceManager.UISettings.SelectedLabelHOffset, ResourceManager.UISettings.SelectedLabelSize), _player.SelectedObject.DisplayName);
            if (_player.SelectedObject.Owner == _player)
            {
                DrawActions(_player.SelectedObject.GetActions());
            }
        }
        GUI.EndGroup();
    }

    private void DrawResourceBar()
    {
        GUI.skin = ResourceSkin;
        GUI.BeginGroup(new Rect(0, 0, Screen.width, ResourceManager.UISettings.ResourceBarSize));
        GUI.Box(new Rect(0, 0, Screen.width, ResourceManager.UISettings.ResourceBarSize), "");
        GUI.Box(ResourceManager.UISettings.ResourceSupplyIconRect, _resourceIcons[GameResources.ResourceType.Money]);
        GUI.Box(ResourceManager.UISettings.ResourceSupplyAmountRect, 
                string.Format("{0}/{1}", _player.GetResource(GameResources.ResourceType.Money),
                                         _player.GetMaxResource(GameResources.ResourceType.Money)));
        GUI.EndGroup();
    }

    public bool MouseInBounds()
    {
        //Screen coordinates start in the lower-left corner of the screen
        //not the top-left of the screen like the drawing coordinates do
        Vector3 mousePos = Input.mousePosition;
        bool insideWidth = (0 <= mousePos.x) && (mousePos.x <= Screen.width);
        bool insideHeight = (ResourceManager.UISettings.OrdersBarSize <= mousePos.y) && (mousePos.y <= Screen.height - ResourceManager.UISettings.ResourceBarSize);
        return insideWidth && insideHeight;
    }

    private void DrawActions(string[] actions)
    {
        if (actions == null)
        {
            return;
        }
        int numActions = actions.Length;
        //define the area to draw the actions inside
        GUI.BeginGroup(new Rect(120, 10, Screen.width, ResourceManager.UISettings.BuildAreaHeight));
        //draw scroll bar for the list of actions if need be
        //if (numActions >= MaxNumRows(buildAreaHeight)) DrawSlider(buildAreaHeight, numActions / 2.0f);
        //display possible actions as buttons and handle the button click for each
        for (int i = 0; i < numActions; i++)
        {
            int column = i % 2;
            int row = i / 2;
            Rect pos = GetButtonPos(row, column);
            Texture2D action = ResourceManager.Production.GetCard(actions[i]);
            if (action != null)
            {
                //create the button and handle the click of that button
                if (GUI.Button(pos, action))
                {
                    if (_player.SelectedObject != null)
                    {
                        _player.SelectedObject.PerformAction(actions[i]);
                    }
                }
            }
        }
        GUI.EndGroup();
    }

    private Rect GetButtonPos(int row, int column)
    {
        return new Rect(column * (ResourceManager.UISettings.CardSize + ResourceManager.UISettings.CardPadding),
                        row * (ResourceManager.UISettings.CardSize + ResourceManager.UISettings.CardPadding),
                        ResourceManager.UISettings.CardSize, ResourceManager.UISettings.CardSize);
    }

    public GUISkin _resourceSkin, _ordersSkin;

    public GUISkin ResourceSkin
    {
        get
        {
            return _resourceSkin;
        }

        set
        {
            _resourceSkin = value;
        }
    }

    public GUISkin OrdersSkin
    {
        get
        {
            return _ordersSkin;
        }

        set
        {
            _ordersSkin = value;
        }
    }

    private Player _player;

    public Texture2D[] HUDIcons;

    private Dictionary<GameResources.ResourceType, Texture2D> _resourceIcons;

    public int MaxCardsInOrdersBar;
}
