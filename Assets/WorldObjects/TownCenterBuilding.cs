using System.Collections;
using System.Collections.Generic;
using Maniple;
using UnityEngine;

public class TownCenterBuilding : Building
{
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnGUI()
    {
        base.OnGUI();
    }

    protected override void Start()
    {
        base.Start();
        Transform spawnPt = transform.Find("SpawnPt");
        Transform rallyPt = transform.Find("RallyPt");
        _controlCircleCenter = transform.Find("Village_control");
        _spawnPoint = rallyPt.position;
        float yRot = transform.rotation.eulerAngles.y;
        _prod = new ProductionAspect(this, spawnPt.position, Quaternion.Euler(0.0f, yRot, 0.0f), rallyPt.position);

        _prod.OnFormationCreate += f => { if (OnFormationCreate != null) { OnFormationCreate(this, f); } };
        _prod.OnFormationFinalize += f => { if (OnFormationFinalize != null) { OnFormationFinalize(this, f); } };

        _actions = new string[] { "Pike Company", "Musketeer Company" };
        ActionsDict.Add(_actions[0], new ProductionAspect.ProductionItem()
        {
            ItemType = ProductionAspect.ProductionItemType.ProductionItemCompany,
            ProductionTime = ResourceManager.Production.DefaultProdTime,
            Cost = 200,
            ProductionKey = _actions[0]
        });
        ActionsDict.Add("MusketeersReinforce", new ProductionAspect.ProductionItem()
        {
            ItemType = ProductionAspect.ProductionItemType.ProductionItemUnit,
            ProductionTime = ResourceManager.Production.DefaultProdTime,
            Cost = 200,
            ProductionKey = "Musketeer"
        });
        ActionsDict.Add(_actions[1], new ProductionAspect.ProductionItem()
        {
            ItemType = ProductionAspect.ProductionItemType.ProductionItemCompany,
            ProductionTime = ResourceManager.Production.DefaultProdTime,
            Cost = 200,
            ProductionKey = _actions[1]
        });
        ActionsDict.Add("PikemenReinforce", new ProductionAspect.ProductionItem()
        {
            ItemType = ProductionAspect.ProductionItemType.ProductionItemUnit,
            ProductionTime = ResourceManager.Production.DefaultProdTime,
            Cost = 200,
            ProductionKey = "Pikeman"
        });
        ActionsDict.Add("InfantryOfficer", new ProductionAspect.ProductionItem()
        {
            ItemType = ProductionAspect.ProductionItemType.ProductionItemUnit,
            ProductionTime = ResourceManager.Production.DefaultProdTime,
            Cost = 200,
            ProductionKey = "Officer"
        });
        _controlCircleRenderer = _controlCircleCenter.GetComponent<LineRenderer>();
        DrawControlCircle();
        StartCoroutine(DoChecks());
    }

    protected override void Update()
    {
        base.Update();
    }

    private IEnumerator DoChecks()
    {
        int t = 0;
        while (true)
        {
            CheckOwnership();
            AdvanceProduction();
            if (_owner != null)
            {
                if (t++ >= (_ticksToResource - 1))
                {
                    _owner.AddResource(GameResources.ResourceType.Money, ResourcesPerTick);
                    t = 0;
                }
            }
            yield return new WaitForSeconds(0.1f);

        }
    }

    public override void DeSelectThis()
    {
        base.DeSelectThis();
    }

    public override void MouseClick(ClickHitObject hitObject, Player controller)
    {
        base.MouseClick(hitObject, controller);
    }

    public override void SelectThis()
    {
        base.SelectThis();
    }

    public override void PerformAction(string action)
    {
        base.PerformAction(action);
        if (!_contested)
        {
            Debug.Log(string.Format("Training {0}", action));
            ProductionAspect.ProductionItem prodItem = ActionsDict[action];
            prodItem.TargetFormation = null;
            _prod.EnqueueProduction(prodItem);
        }
        else
        {
            Debug.Log(string.Format("Under attack. Can't produce"));
        }
    }

    public virtual void ReinforceFormation(Formation f)
    {
        if (f != null && !f.Forming)
        {
            ProductionAspect.ProductionItem prodItem = ActionsDict[f.ReinforceAction];
            prodItem.TargetFormation = f;
            f.Forming = true;
            if (f.CommanderAlive)
            {
                for (int i = 0; i < f.MaxUnits - f.NumUnits; ++i)
                {
                    _prod.EnqueueProduction(prodItem);
                }
            }
            else
            {
                ProductionAspect.ProductionItem officerProdItem = ActionsDict["InfantryOfficer"];
                officerProdItem.TargetFormation = f;
                _prod.EnqueueProduction(officerProdItem);
                for (int i = 0; i < f.MaxUnits - f.NumUnits - 1; ++i)
                {
                    _prod.EnqueueProduction(prodItem);
                }
            }
            _prod.EnqueueProduction(ProductionAspect.ProductionItem.FinishForming(f));
        }
    }

    private void CheckOwnership()
    {
        Collider[] unitsInRange = Physics.OverlapSphere(_controlCircleCenter.position, CaptureRange, _unitsLayerMask);
        if (unitsInRange.Length > 0)
        {
            Player p = null;
            bool controlled = false;
            foreach (Collider c in unitsInRange)
            {
                Unit currUnit = c.GetComponent<Unit>();
                if (currUnit != null && currUnit.Owner != null)
                {
                    if (p == null)
                    {
                        p = currUnit.Owner;
                        controlled = true;
                    }
                    else if (currUnit.Owner != p)
                    {
                        controlled = false;
                        if (p.IsHostile(currUnit.Owner))
                        {
                            _contested = true; // if hostile units are present, the town center is contested
                        }
                        break;
                    }
                }
            }
            if (controlled)
            {
                if (_owner != p && p != null)
                {
                    if (_owner != null)
                    {
                        _owner.RemoveTownCenter(this);
                    }
                    p.AddTownCenter(this);

                    _controlCircleRenderer.endColor = p.TeamColor;
                    _controlCircleRenderer.startColor = p.TeamColor;
                }
                _owner = p;
                _contested = false;
            }
        }
    }

    private void DrawControlCircle()
    {
        float x;
        float y = 0.0f;
        float z;

        float angle = 0.0f;
        _controlCircleRenderer.positionCount = _controlCircleSegemets + 1;
        for (int i = 0; i < (_controlCircleSegemets + 1); i++)
        {
            x = Mathf.Sin(angle) * CaptureRange;
            z = Mathf.Cos(angle) * CaptureRange;

            _controlCircleRenderer.SetPosition(i, new Vector3(x, y, z));

            angle += (Mathf.PI * 2.0f / _controlCircleSegemets);
        }
    }

    private void AdvanceProduction()
    {
        _prod.BuildQueueStep();
    }

    private IEnumerator DelayedSendUnitToRallyPoint(WorldObject wo)
    {
        yield return new WaitForSeconds(0.5f);
        wo.IssueOrder(new ClickHitObject() { HitLocation = _spawnPoint, HitObject = null }, _owner);
        yield return null;
    }

    public void SendUnitToRallyPoint(WorldObject wo)
    {
        StartCoroutine(DelayedSendUnitToRallyPoint(wo));
    }

    public bool IsProducing { get { return _prod.IsProducing; } }

    public Transform ControlCircleCenter { get { return _controlCircleCenter; } }

    private ProductionAspect _prod;
    private Vector3 _spawnPoint;

    private Transform _controlCircleCenter;
    private LineRenderer _controlCircleRenderer;
    private static readonly int _controlCircleSegemets = 32;
    public int CaptureRange;
    private readonly int _ticksToResource = 10;
    public int ResourcesPerTick = 5;
    public readonly Dictionary<string, ProductionAspect.ProductionItem> ActionsDict = new Dictionary<string, ProductionAspect.ProductionItem>();

    public delegate void TownFormationDlg(TownCenterBuilding t, Formation f);
    public event TownFormationDlg OnFormationCreate;
    public event TownFormationDlg OnFormationFinalize;
    private bool _contested = false;
}
