using Maniple;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Player : MonoBehaviour
{
    private void Awake()
    {
        _playerResourceLimits = GameResources.MaxResources();
        _playerResources = GameResources.StartResources();
        AI = GetComponent<HighLevelAI>();
    }

    // Use this for initialization
    void Start ()
    {
        StartCoroutine(UpdateForcesOnceDelayed());
	}
	
	// Update is called once per frame
	void Update ()
    {
        //Debug.Log("Player.Update");
    }

    //public HUD PlayerHUD { get; private set; }
    public HUD2 PlayerHUD2 { get; set; }

    public WorldObject SelectedObject { get; set; }

    public void DeselectCurrent()
    {
        if (SelectedObject != null)
        {
            SelectedObject.DeSelectThis();
            SelectedObject = null;
            PlayerHUD2.UpdateSelectionCommands(SelectedObject);
        }
    }

    public void ChangeSelection(WorldObject obj)
    {
        if (SelectedObject != null)
        {
            SelectedObject.DeSelectThis();
        }
        if (obj != null)
        {
            obj.SelectThis();
        }
        SelectedObject = obj;
        PlayerHUD2.UpdateSelectionCommands(SelectedObject);
    }

    public int GetResource(GameResources.ResourceType res)
    {
        return _playerResources[res];
    }

    public int GetMaxResource(GameResources.ResourceType res)
    {
        return _playerResourceLimits[res];
    }

    public void AddResource(GameResources.ResourceType res, int amount)
    {
        _playerResources[res] += amount;
    }

    public void IncMaxResource(GameResources.ResourceType res, int amount)
    {
        _playerResourceLimits[res] += amount;
    }

    public Unit AddUnit(string unitName, Vector3 spawnPoint, Quaternion rotation)
    {
        Transform unitsList = transform.Find("UnitsList");
        IEnumerable<GameObject> possibleUnits = ResourceManager.Production.GetUnit(unitName);
        GameObject newUnit = Instantiate<GameObject>(ResourceManager.GetRandom(possibleUnits), spawnPoint, rotation);
        newUnit.transform.parent = unitsList;
        return newUnit.GetComponent<Unit>();
    }
    public CompanyObject AddCompany(string unitName, Vector3 spawnPoint, Quaternion rotation)
    {
        Transform unitsList = transform.Find("UnitsList");
        IEnumerable<GameObject> possibleWOs = ResourceManager.Production.GetWorldObject(unitName);
        GameObject newCompany = Instantiate<GameObject>(possibleWOs.First(), spawnPoint, rotation);
        newCompany.transform.parent = unitsList;

        UpdateForces(unitsList);

        return newCompany.GetComponent<CompanyObject>();
    }

    private void UpdateForces(Transform unitsList)
    {
        if (Human)
        {
            List<Formation> allForces = new List<Formation>();
            CompanyObject[] allComps = unitsList.GetComponentsInChildren<CompanyObject>();
            foreach (CompanyObject c in allComps)
            {
                Formation f = c.GetComponentInChildren<Formation>();
                if (f != null)
                {
                    allForces.Add(f);
                }
            }
            PlayerHUD2.UpdateForces(allForces);
        }
    }

    public void ForceUpdateForces()
    {
        if (Human)
        {
            Transform unitsList = transform.Find("UnitsList");
            UpdateForces(unitsList);
        }
    }

    private IEnumerator UpdateForcesOnceDelayed()
    {
        yield return new WaitForSeconds(0.1f);
        Transform unitsList = transform.Find("UnitsList");
        UpdateForces(unitsList);
        yield return null;
    }

    public bool IsHostile(Player other)
    {
        if (other == null)
        {
            return false;
        }
        return other != this; // until allied forces are implemented
    }

    public bool _human;

    public string Name { get; set; }
    public bool Human
    {
        get
        {
            return _human;
        }
        set
        {
            _human = value;
        }
    }

    public void AddTownCenter(TownCenterBuilding tc)
    {
        if (AI != null)
        {
            AI.AddTown(tc);
        }
    }

    public void RemoveTownCenter(TownCenterBuilding tc)
    {
        if (AI != null)
        {
            AI.RemoveTown(tc);
        }
    }

    private Dictionary<GameResources.ResourceType, int> _playerResources, _playerResourceLimits;

    public Color TeamColor;
    private HighLevelAI AI = null;
}
