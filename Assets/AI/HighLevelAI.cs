using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighLevelAI : MonoBehaviour {

    void Awake()
    {
        _wallsLayerMask = LayerMask.GetMask("Walls");
    }

    // Use this for initialization
    void Start ()
    {
        StartCoroutine(Pulse());
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    IEnumerator Pulse()
    {
        while (true)
        {
            // test
            RequisionFormation();
            //

            yield return new WaitForSeconds(0.5f);
        }
    }

    public void AddFormation(Formation f)
    {
        // allocate to group which needs it most
        // find towns which needs defenses:
        TownCenterBuilding townWithMostReqForces = null;
        int missingInTownWithMostReqForces = 0;
        foreach (TownCenterBuilding b in _towns)
        {
            int missingForces = _townDefenseRequiredForces[b] - EstimateForce(_townDefenseForces[b]);
            if (townWithMostReqForces == null || missingForces > missingInTownWithMostReqForces)
            {
                townWithMostReqForces = b;
                missingInTownWithMostReqForces = missingForces;
            }
        }
        if (missingInTownWithMostReqForces > 0)
        {
            _townDefenseForces[townWithMostReqForces].Add(f);
            return;
        }

        // check if reserve requires reinforcement
        int missingReserveForces = _reserveRequiredForce - EstimateForce(_reserveForce);
        if (missingReserveForces > 0)
        {
            _reserveForce.Add(f);
        }

        _attackForce.Add(f);
    }

    public void RequisionFormation()
    {
        // allocate to group which needs it most
        // find towns which needs defenses:
        TownCenterBuilding townWithMostReqForces = null;
        int missingInTownWithMostReqForces = 0;
        foreach (TownCenterBuilding b in _towns)
        {
            if (b.IsProducing)
            {
                continue;
            }
            int missingForces = _townDefenseRequiredForces[b] - EstimateForce(_townDefenseForces[b]);
            if (townWithMostReqForces == null || missingForces > missingInTownWithMostReqForces)
            {
                townWithMostReqForces = b;
                missingInTownWithMostReqForces = missingForces;
            }
        }
        if (missingInTownWithMostReqForces > 0)
        {
            townWithMostReqForces.PerformAction(RequiredFormationType(_townDefenseForces[townWithMostReqForces]));
            _townProducedFormationTargets[townWithMostReqForces].Enqueue(
                new ProducedFormationTarget()
                {
                    TargetForceType = ForceType.TownDefense,
                    TargetTownCenter = townWithMostReqForces
                });
            return;
        }

        // check if reserve requires reinforcement
        int missingReserveForces = _reserveRequiredForce - EstimateForce(_reserveForce);
        if (missingReserveForces > 0)
        {
            //_reserveForce.Add(f);
        }

        //_attackForce.Add(f);
    }

    public void RemoveFormation(Formation f)
    {
        foreach (List<Formation> townDefenses in _townDefenseForces.Values)
        {
            if (townDefenses.Contains(f))
            {
                townDefenses.Remove(f);
                return;
            }
        }
        if (_reserveForce.Contains(f))
        {
            _reserveForce.Remove(f);
            return;
        }
        if (_attackForce.Contains(f))
        {
            _attackForce.Remove(f);
        }
    }

    private string RequiredFormationType(List<Formation> force)
    {
        int musketeers = 0;
        int pikes = 0;
        foreach (Formation f in force)
        {
            if (f.ProductionName == "Pike Company")
            {
                pikes += f.NumUnits;
            }
            else if (f.ProductionName == "Musketeer Company")
            {
                musketeers += f.NumUnits;
            }
        }
        if (pikes <= (musketeers * 2))
        {
            return "Pike Company";
        }
        else
        {
            return "Musketeer Company";
        }
    }

    public void AddTown(TownCenterBuilding b)
    {
        _towns.Add(b);
        _townDefenseForces.Add(b, new List<Formation>());
        _townDefenseRequiredForces.Add(b, _requiredTownDefenseForces);
        _townProducedFormationTargets.Add(b, new Queue<ProducedFormationTarget>());
        b.OnFormationCreate += FormationCreated;
        b.OnFormationFinalize += FormationFinalized;
    }

    public void RemoveTown(TownCenterBuilding b)
    {
        _towns.Remove(b);
        _townDefenseForces.Remove(b);
        _townDefenseRequiredForces.Remove(b);
        _townProducedFormationTargets.Remove(b);
        b.OnFormationCreate -= FormationCreated;
        b.OnFormationFinalize -= FormationFinalized;
    }

    private void FormationCreated(TownCenterBuilding tc, Formation f)
    {
        ProducedFormationTarget target = _townProducedFormationTargets[tc].Dequeue();
        switch (target.TargetForceType)
        {
            case ForceType.TownDefense:
                _townDefenseForces[target.TargetTownCenter].Add(f);
                break;
            case ForceType.Attack:
                _attackForce.Add(f);
                break;
            case ForceType.Reserve:
                _reserveForce.Add(f);
                break;
            default:
                break;
        }
    }

    private void FormationFinalized(TownCenterBuilding tc, Formation f)
    {

    }

    private AIPlayerState _currState;

    private Dictionary<TownCenterBuilding, List<Formation>> _townDefenseForces = new Dictionary<TownCenterBuilding, List<Formation>>();
    private Dictionary<TownCenterBuilding, int> _townDefenseRequiredForces = new Dictionary<TownCenterBuilding, int>();
    private List<Formation> _reserveForce = new List<Formation>();
    private int _reserveRequiredForce;
    private List<Formation> _attackForce = new List<Formation>();
    private int _attackRequiredForce;
    private List<TownCenterBuilding> _towns = new List<TownCenterBuilding>();
    private Dictionary<TownCenterBuilding, Queue<ProducedFormationTarget>> _townProducedFormationTargets = new Dictionary<TownCenterBuilding, Queue<ProducedFormationTarget>>();

    private int EstimateForce(IEnumerable<Formation> forces)
    {
        int forcesSum = 0;

        foreach (Formation f in forces)
        {
            Collider[] walls = Physics.OverlapSphere(f.GetRangedTargetingPosition(), 0.1f, _wallsLayerMask);
            if (walls.Length > 0)
            {
                forcesSum += (f.NumUnits * 2);
            }
            else
            {
                forcesSum += f.NumUnits;
            }
        }

        return forcesSum;
    }

    private int _wallsLayerMask;

    private int _requiredTownDefenseForces = 20 * 3;

    public enum AIPlayerState
    {
        Buildup,
        Attacking,
        AttackingWithReserve,
        AttackSuccess,
        AttackFail,
        ReplenishReserve,
        Defending,
        DefensingWithReserve
    };

    private enum ForceType
    {
        TownDefense,
        Attack,
        Reserve
    }

    private struct ProducedFormationTarget
    {
        public ForceType TargetForceType;
        public TownCenterBuilding TargetTownCenter;
    }
}
