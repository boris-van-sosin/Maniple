using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Maniple;

public class HUD2 : MonoBehaviour
{

	// Use this for initialization
	void Start ()
    {
        HumanPlayer.PlayerHUD2 = this;

        Transform resourceTextTransform = transform.Find("TopPanel/ResourceText");
        _resourceText = resourceTextTransform.GetComponent<Text>();

        Transform statusTextTransform = transform.Find("BottomPanel/StatusText");
        _statusText = statusTextTransform.GetComponent<Text>();

        _forcesPanel = transform.Find("SidePanel/UnitsScrollView/Viewport/Content");
        _forcesPanelScrollBox = transform.Find("SidePanel/UnitsScrollView");
        _commandsPanel = transform.Find("BottomPanel/CommandsScrollView/Viewport/CommandsBox");
    }

    void OnGUI()
    {
        if (HumanPlayer != null && HumanPlayer.Human)
        {
            DrawResourceBar();
            DrawOrdersBar();
        }
    }

    private void DrawResourceBar()
    {
        _resourceText.text = string.Format("{0}/{1}",
                                           HumanPlayer.GetResource(Maniple.GameResources.ResourceType.Money),
                                           HumanPlayer.GetMaxResource(Maniple.GameResources.ResourceType.Money));
    }

    private void DrawOrdersBar()
    {
        if (HumanPlayer.SelectedObject != null)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string line in HumanPlayer.SelectedObject.StatusLines())
            {
                sb.AppendLine(line);
            }
            _statusText.text = sb.ToString();
        }
        else
        {
            _statusText.text = string.Empty;
        }
    }

    public void UpdateForces(List<Formation> formations)
    {
        _formationsIdxs = formations;
        Button[] existingButtons = _forcesPanel.GetComponentsInChildren<Button>();
        List<Button> newButtons = existingButtons.ToList();
        while (newButtons.Count > _formationsIdxs.Count)
        {
            newButtons[newButtons.Count - 1].gameObject.SetActive(false);
            newButtons.RemoveAt(newButtons.Count - 1);
        }
        for (int i = 0; i < newButtons.Count; ++i)
        {
            newButtons[i].image.sprite = ResourceManager.Production.GetCardSprite(_formationsIdxs[i].ProductionName);
            newButtons[i].gameObject.SetActive(true);
        }
        for (int i = newButtons.Count; i < _formationsIdxs.Count; ++i)
        {
            Button b = Instantiate<Button>(ButtonTemplate);
            b.image.sprite = ResourceManager.Production.GetCardSprite(_formationsIdxs[i].ProductionName);
            RectTransform rt = b.GetComponent<RectTransform>();
            rt.SetParent(_forcesPanel);
        }
    }

    public void UpdateSelectionCommands(WorldObject selected)
    {
        Button[] existingButtons = _commandsPanel.GetComponentsInChildren<Button>();
        if (selected != null && selected.Owner == HumanPlayer)
        {
            List<Button> newButtons = existingButtons.ToList();
            string[] actions = selected.GetActions();
            while (newButtons.Count > selected.GetActions().Length)
            {
                newButtons[newButtons.Count - 1].gameObject.SetActive(false);
                newButtons.RemoveAt(newButtons.Count - 1);
            }
            for (int i = 0; i < newButtons.Count; ++i)
            {
                string currAction = actions[i];
                newButtons[i].image.sprite = ResourceManager.Production.GetCardSprite(currAction);
                newButtons[i].gameObject.SetActive(true);
                newButtons[i].onClick.RemoveAllListeners();
                newButtons[i].onClick.AddListener(new UnityEngine.Events.UnityAction(delegate ()
                {
                    Debug.Log(string.Format("Order {0}: {1}", selected, currAction));
                    selected.PerformAction(currAction);
                }));
            }
            for (int i = newButtons.Count; i < actions.Length; ++i)
            {
                Button b = Instantiate<Button>(ButtonTemplate);
                string currAction = actions[i];
                b.image.sprite = ResourceManager.Production.GetCardSprite(currAction);
                b.onClick.AddListener(new UnityEngine.Events.UnityAction(delegate() 
                    {
                        Debug.Log(string.Format("Order {0}: {1}", selected, currAction));
                        selected.PerformAction(currAction);
                    }));
                RectTransform rt = b.GetComponent<RectTransform>();
                rt.SetParent(_commandsPanel);
            }
        }
        else
        {
            foreach (Button b in existingButtons)
            {
                b.gameObject.SetActive(false);
            }
            {

            }
        }
    }

    public void ToggleUnitsPanel()
    {
        _forcesPanelActive = !_forcesPanelActive;
        _forcesPanelScrollBox.gameObject.SetActive(_forcesPanelActive);
    }

    public void UICommand()
    {

    }

    private Text _resourceText;
    private Text _statusText;
    private Transform _forcesPanel;
    private Transform _forcesPanelScrollBox;
    private Transform _commandsPanel;
    public Player HumanPlayer;
    private List<Formation> _formationsIdxs = new List<Formation>();
    public Button ButtonTemplate;
    private bool _forcesPanelActive = false;
}
