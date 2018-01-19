using Maniple;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Prototypes : MonoBehaviour
{

	// Use this for initialization
	void Start ()
    {
		
	}

    void Awake()
    {
        if (!_created)
        {
            DontDestroyOnLoad(transform.gameObject);
            ResourceManager.SetPrototypeList(this);
            _created = true;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    // Update is called once per frame
    void Update ()
    {
		
	}

    public IEnumerable<GameObject> GetBuilding(string name)
    {
        List<GameObject> res;
        if (_buildingsDict.TryGetValue(name, out res))
        {
            return res;
        }

        res = new List<GameObject>();
        for (int i = 0; i < Buildings.Length; i++)
        {
            Building building = Buildings[i].GetComponent<Building>();
            if (building != null && building.ProductionName == name)
            {
                res.Add(Buildings[i]);
            }
        }
        if (res.Count > 0)
        {
            _buildingsDict[name] = res;
            return res;
        }
        return null;
    }

    public IEnumerable<GameObject> GetUnit(string name)
    {
        List<GameObject> res;
        if (_unitsDict.TryGetValue(name, out res))
        {
            return res;
        }

        res = new List<GameObject>();
        for (int i = 0; i < Units.Length; i++)
        {
            Unit unit = Units[i].GetComponent<Unit>();
            if (unit != null && unit.ProductionName == name)
            {
                res.Add(Units[i]);
            }
        }
        if (res.Count > 0)
        {
            _unitsDict[name] = res;
            return res;

        }
        else
        {
            return null;
        }
    }

    public IEnumerable<GameObject> GetWorldObject(string name)
    {
        List<GameObject> res;
        if (_worldObjsDict.TryGetValue(name, out res))
        {
            return res;
        }

        res = new List<GameObject>();
        for (int i = 0; i < WorldObjects.Length; i++)
        {
            WorldObject wo = WorldObjects[i].GetComponent<WorldObject>();
            if (wo != null && wo.ProductionName == name)
            {
                res.Add(WorldObjects[i]);
            }
        }
        if (res.Count > 0)
        {
            _worldObjsDict[name] = res;
            return res;
        }
        else
        {
            return null;
        }
    }

    public GameObject GetOtherObject(string name)
    {
        GameObject res;
        if (_otherObjsDict.TryGetValue(name, out res))
        {
            return res;
        }

        for (int i = 0; i < OtherObjects.Length; i++)
        {
            GameObject obj = OtherObjects[i];
            if (obj != null && obj.name == name)
            {
                _otherObjsDict[name] = obj;
                return obj;
            }
        }
        return null;
    }

    public GameObject GetPlayerObject()
    {
        return Player;
    }

    public Texture2D GetCard(string name)
    {
        Texture2D res;
        if (_cards.TryGetValue(name, out res))
        {
            return res;
        }

        for (int i = 0; i < Buildings.Length; i++)
        {
            Building building = Buildings[i].GetComponent<Building>();
            if (building != null && building.ProductionName == name)
            {
                GetBuilding(name);
                _cards[name] = building.CardImage;
                return building.CardImage;
            }
        }
        for (int i = 0; i < Units.Length; i++)
        {
            Unit unit = Units[i].GetComponent<Unit>();
            if (unit != null && unit.ProductionName == name)
            {
                GetUnit(name);
                _cards[name] = unit.CardImage;
                return unit.CardImage;
            }
        }
        for (int i = 0; i < WorldObjects.Length; i++)
        {
            WorldObject wo = WorldObjects[i].GetComponent<WorldObject>();
            if (wo != null && wo.ProductionName == name)
            {
                GetWorldObject(name);
                _cards[name] = wo.CardImage;
                return wo.CardImage;
            }
        }
        int numNamedTextures = Math.Min(NamedTextureNames.Length, NamedTextures.Length);
        for (int i = 0; i < numNamedTextures; ++i)
        {
            if (NamedTextureNames[i] == name)
            {
                if (!_cards.ContainsKey(NamedTextureNames[i]))
                {
                    _cards[NamedTextureNames[i]] = NamedTextures[i];
                    return NamedTextures[i];
                }
            }
        }
        return null;
    }

    public Sprite GetCardSprite(string name)
    {
        Sprite res;
        if (_sprites.TryGetValue(name, out res))
        {
            return res;
        }
        Texture2D texture = GetCard(name);
        if (texture != null)
        {
            res = Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(64, 64));
            _sprites[name] = res;
            return res;
        }
        return null;
    }

    public GameObject[] Buildings;
    public GameObject[] Units;
    public GameObject[] WorldObjects;
    public GameObject[] OtherObjects;
    public string[] NamedTextureNames;
    public Texture2D[] NamedTextures;
    public GameObject Player;

    private Dictionary<string, List<GameObject>> _buildingsDict = new Dictionary<string, List<GameObject>>();
    private Dictionary<string, List<GameObject>> _unitsDict = new Dictionary<string, List<GameObject>>();
    private Dictionary<string, List<GameObject>> _worldObjsDict = new Dictionary<string, List<GameObject>>();
    private Dictionary<string, GameObject> _otherObjsDict = new Dictionary<string, GameObject>();
    private Dictionary<string, Texture2D> _cards = new Dictionary<string, Texture2D>();
    private Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

    private static bool _created = false;
}
