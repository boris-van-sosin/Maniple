﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;
using Maniple;

public class HighLevelAI : MonoBehaviour {

    void Awake()
    {
        _wallsLayerMask = LayerMask.GetMask("Walls", "WallsInvisible");
        _unitsLayerMask = LayerMask.GetMask("Units");
        _player = GetComponent<Player>();
    }

    // Use this for initialization
    void Start ()
    {
        _allTowns = FindObjectsOfType<TownCenterBuilding>().ToList();
        StartCoroutine(ComputeTownsGraph());
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
            if (!_addedInitialFormation)
            {
                AddFreeFormations();
            }
            
            switch (_currState)
            {
                case AIPlayerState.Buildup:
                    _currState = HandleBuildup();
                    break;
                case AIPlayerState.Attacking:
                    _currState = HandleAttacking();
                    break;
                case AIPlayerState.AttackingWithReserve:
                    _currState = HandleAttackingWithReserve();
                    break;
                case AIPlayerState.AttackSuccess:
                    _currState = HandleAttackSuccess();
                    break;
                case AIPlayerState.AttackFail:
                    _currState = HandleAttackFail();
                    break;
                case AIPlayerState.ReplenishReserve:
                    _currState = HandleReplenishReserve();
                    break;
                case AIPlayerState.Defending:
                    _currState = HandleDefending();
                    break;
                case AIPlayerState.DefendingWithReserve:
                    _currState = HandleDefendingWithReserve();
                    break;
                default:
                    break;
            }
            
            if (_adjustForcesLocationCounter > 0)
            {
                --_adjustForcesLocationCounter;
            }
            else
            {
                StartCoroutine(AdjustForcesLocations());
                _adjustForcesLocationCounter = _adjustForcesLocationPulses;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator ComputeTownsGraph()
    {
        _townsGraph = new Dictionary<TownCenterBuilding, Dictionary<TownCenterBuilding, NavMeshPath>>();
        foreach (TownCenterBuilding tc1 in _allTowns)
        {
            Dictionary<TownCenterBuilding, NavMeshPath> currDict = new Dictionary<TownCenterBuilding, NavMeshPath>();
            foreach (TownCenterBuilding tc2 in _allTowns)
            {
                if (tc1 == tc2)
                {
                    continue;
                }
                NavMeshPath currPath = new NavMeshPath();
                if (NavMesh.CalculatePath(tc1.ControlCircleCenter.position, tc2.ControlCircleCenter.position, NavMesh.AllAreas, currPath))
                {
                    currDict[tc2] = currPath;
                }
                yield return new WaitForEndOfFrame();
            }
            _townsGraph[tc1] = currDict;
        }
        yield return new WaitForEndOfFrame(); ;
        AdjustTownDefenseLocations();
        yield return null;
    }

    public void AddFormation(Formation f)
    {
        // allocate to group which needs it most
        // find towns which needs defenses:
        TownCenterBuilding townWithMostReqForces = null;
        int missingInTownWithMostReqForces = 0;
        foreach (TownCenterBuilding b in _ownedTowns)
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

        _forcesLocations.Add(f, Vector3.zero);
    }

    private void RequisionFormation()
    {
        // allocate to group which needs it most
        // find towns which needs defenses:
        TownCenterBuilding townWithMostReqForces = null;
        int missingInTownWithMostReqForces = 0;
        foreach (TownCenterBuilding tc in _ownedTowns)
        {
            if (tc.IsProducing)
            {
                continue;
            }
            int missingForces = _townDefenseRequiredForces[tc] - EstimateForce(_townDefenseForces[tc]);
            if (townWithMostReqForces == null || missingForces > missingInTownWithMostReqForces)
            {
                townWithMostReqForces = tc;
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
            TownCenterBuilding reserveTC = ClosestTownCenterToPoint(_ownedTowns, _reserveForceLocation);
            if (reserveTC != null && !reserveTC.IsProducing)
            {
                reserveTC.PerformAction(RequiredFormationType(_reserveForce));
                _townProducedFormationTargets[reserveTC].Enqueue(
                    new ProducedFormationTarget()
                    {
                        TargetForceType = ForceType.Reserve
                    });
                return;
            }
        }

        // check if reserve requires reinforcement
        int missingAttackForces = _attackRequiredForce - EstimateForce(_attackForce);
        if (missingAttackForces > 0)
        {
            TownCenterBuilding attackTC = ClosestTownCenterToPoint(_ownedTowns, _attackForceLocation);
            if (attackTC != null && !attackTC.IsProducing)
            {
                attackTC.PerformAction(RequiredFormationType(_reserveForce));
                _townProducedFormationTargets[attackTC].Enqueue(
                    new ProducedFormationTarget()
                    {
                        TargetForceType = ForceType.Attack
                    });
                return;
            }
        }
    }

    public void RemoveFormation(Formation f)
    {
        _forcesLocations.Remove(f);
        bool removed = false;
        foreach (List<Formation> townDefenses in _townDefenseForces.Values)
        {
            if (townDefenses.Contains(f))
            {
                townDefenses.Remove(f);
                removed = true;
                break;
            }
        }
        if (!removed && _reserveForce.Contains(f))
        {
            _reserveForce.Remove(f);
            removed = true;
        }
        if (!removed && _attackForce.Contains(f))
        {
            _attackForce.Remove(f);
            SendForceToTarget(_attackForce, _attackTarget.ControlCircleCenter.position, _attackForceAxis, MoveType.Enqueue);
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

    public void AddTown(TownCenterBuilding tc)
    {
        _ownedTowns.Add(tc);
        if (_ownedTowns.Count > 1)
        {
            _firstTown = false;
        }
        _townDefenseForces.Add(tc, new List<Formation>());
        if (_firstTown)
        {
            _townDefenseRequiredForces[tc] = _requiredTownDefenseForcesInitial;
        }
        else
        {
            _townDefenseRequiredForces[tc] = _requiredTownDefenseForces;
        }
        _townProducedFormationTargets.Add(tc, new Queue<ProducedFormationTarget>());
        tc.OnFormationCreate += FormationCreated;
        tc.OnFormationFinalize += FormationFinalized;
        AdjustTownDefenseLocations();
        if (!_firstTown)
        {
            foreach (TownCenterBuilding tc2 in FriendlyFrontTowns())
            {
                _townDefenseRequiredForces[tc2] = _requiredTownDefenseForces;
            }
        }
    }

    public void RemoveTown(TownCenterBuilding tc)
    {
        _ownedTowns.Remove(tc);
        _townDefenseForces.Remove(tc);
        _townDefenseRequiredForces.Remove(tc);
        _townProducedFormationTargets.Remove(tc);
        _townDefenseLocations.Remove(tc);
        tc.OnFormationCreate -= FormationCreated;
        tc.OnFormationFinalize -= FormationFinalized;
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
        _forcesLocations.Add(f, Vector3.zero);
    }

    private void FormationFinalized(TownCenterBuilding tc, Formation f)
    {
        
    }

    private void SendForceToTarget(IEnumerable<Formation> force, Vector3 targetPos, Vector3 forceAxis, MoveType m)
    {
        float offset = 0.0f;
        bool first = true;
        foreach (Formation f in force)
        {
            ClickHitObject target = new ClickHitObject()
            {
                HitLocation = _attackTarget.ControlCircleCenter.position,
                HitObject = null
            };
            if (!first)
            {
                target.HitLocation = ClosestPtOnNavMesh(target.HitLocation + offset * forceAxis);
            }
            ClickHitObject dir = new ClickHitObject()
            {
                HitLocation = target.HitLocation + Quaternion.AngleAxis(90, Vector3.up) * forceAxis,
                HitObject = null
            };
            switch (m)
            {
                case MoveType.Immediate:
                    f.IssueOrder(dir, target, f.Owner);
                    break;
                case MoveType.Enqueue:
                    f.EnqueueOrder(WorldObject.Order.MoveOrder(dir, target));
                    break;
                case MoveType.Push:
                    f.PushInterruptiveOrder(WorldObject.Order.MoveOrder(dir, target));
                    break;
                default:
                    break;
            }
            _forcesLocations[f] = target.HitLocation;
            first = false;
        }
    }

    private static Vector3 MoveOnPath(NavMeshPath p, float amount)
    {
        float amountAdvanced = 0.0f;
        for (int i = 0; i < p.corners.Length - 1; i++)
        {
            Vector3 currSeg = p.corners[i + 1] - p.corners[i];
            float segLength = currSeg.magnitude;
            if (amountAdvanced + segLength >= amount)
            {
                return p.corners[i] + currSeg.normalized * (amount - amountAdvanced);
            }
            amountAdvanced += segLength;
        }
        return p.corners.Last();
    }

    private Tuple<Vector3, Vector3> TownDefenseLocation(TownCenterBuilding tc)
    {
        if (!_townsGraph.ContainsKey(tc))
        {
            return Tuple<Vector3, Vector3>.Create(tc.ControlCircleCenter.position, tc.ControlCircleCenter.right.normalized);
        }

        Vector3 res = Vector3.zero;
        int destNum = 0;
        foreach (KeyValuePair<TownCenterBuilding, NavMeshPath> dest in _townsGraph[tc])
        {
            if (_ownedTowns.Contains(dest.Key))
            {
                continue;
            }
            res += MoveOnPath(dest.Value, _defenseDistance);
            ++destNum;
        }
        Vector3 defensePt = res / destNum;
        Vector3 defenseDir = Quaternion.AngleAxis(90, Vector3.up) * (defensePt - tc.ControlCircleCenter.position).normalized;
        return Tuple<Vector3, Vector3>.Create(defensePt, defenseDir);
    }

    private void AdjustTownDefenseLocations()
    {
        foreach (TownCenterBuilding tc in _ownedTowns)
        {
            _townDefenseLocations[tc] = TownDefenseLocation(tc);
        }
    }

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

    private void ReinforceFormations()
    {
        foreach (TownCenterBuilding tc in _ownedTowns)
        {
            foreach (Formation f in _townDefenseForces[tc])
            {
                ReinforceFormation(f, tc);
            }
        }

        foreach (Formation f in _reserveForce.Concat(_attackForce))
        {
            TownCenterBuilding closestTC = ClosestTownCenterToPoint(_ownedTowns, f.transform.position);
            if (closestTC != null)
            {
                ReinforceFormation(f, closestTC);
            }
        }
    }

    private void ReinforceFormation(Formation f, TownCenterBuilding tc)
    {
        if ((!f.Forming) && !f.HasQueuedOrders && f.NumUnits < f.MaxUnits)
        {
            ClickHitObject currPos = new ClickHitObject()
            {
                HitLocation = f.transform.position,
                HitObject = null
            };
            ClickHitObject target = new ClickHitObject()
            {
                HitLocation = tc.ControlCircleCenter.position,
                HitObject = null
            };
            f.EnqueueOrder(WorldObject.Order.MoveOrder(target, null));
            f.EnqueueOrder(WorldObject.Order.ReinforceOrder());
            f.EnqueueOrder(WorldObject.Order.MoveOrder(currPos, null));
        }
    }

    private IEnumerable<TownCenterBuilding> FriendlyFrontTowns()
    {
        HashSet<TownCenterBuilding> res = new HashSet<TownCenterBuilding>();
        foreach (TownCenterBuilding tc1 in _allTowns.Where(x => _player.IsHostile(x.Owner)))
        {
            TownCenterBuilding tc2 = ClosestTownCenterToPoint(_ownedTowns, tc1.ControlCircleCenter.position);
            if (tc2 != null)
            {
                res.Add(tc2);
            }
        }
        return res;
    }

    private IEnumerable<TownCenterBuilding> EnemyFrontTowns()
    {
        return EnemyFrontTowns(false);
    }

    private IEnumerable<TownCenterBuilding> EnemyFrontTowns(bool includeNeutral)
    {
        HashSet<TownCenterBuilding> res = new HashSet<TownCenterBuilding>();
        foreach (TownCenterBuilding tc1 in _ownedTowns)
        {
            TownCenterBuilding tc2 =
                ClosestTownCenterToPoint(_allTowns.Where(
                                            x => (includeNeutral && x.Owner == null) ||
                                            _player.IsHostile(x.Owner)),
                                         tc1.ControlCircleCenter.position);
            if (tc2 != null)
            {
                res.Add(tc2);
            }
        }
        return res;
    }

    private static TownCenterBuilding ClosestTownCenterToPoint(IEnumerable<TownCenterBuilding> tcs, Vector3 pt)
    {
        float closestTCDist = 0.0f;
        TownCenterBuilding closestTC = null;
        foreach (TownCenterBuilding tc in tcs)
        {
            float sqrDist = (tc.ControlCircleCenter.position - pt).sqrMagnitude;
            if (closestTC == null || sqrDist < closestTCDist)
            {
                closestTC = tc;
                closestTCDist = sqrDist;
            }
        }
        return closestTC;
    }

    private IEnumerator AdjustForcesLocations()
    {
        float offset = 0.0f;
        // Town defense forces
        foreach (TownCenterBuilding tc in _ownedTowns)
        {
            foreach (Formation f in _townDefenseForces[tc])
            {
                Vector3 defenseLineAxis = _townDefenseLocations[tc].Item2;
                Vector3 defemseLineDepth = Quaternion.AngleAxis(-90, Vector3.up) * defenseLineAxis;
                ClickHitObject target = new ClickHitObject()
                {
                    HitLocation = ClosestPtOnNavMesh(_townDefenseLocations[tc].Item1 + defenseLineAxis * offset),
                    HitObject = null
                };
                ClickHitObject dir = new ClickHitObject()
                {
                    HitLocation = _townDefenseLocations[tc].Item1 + defenseLineAxis * offset + defemseLineDepth,
                    HitObject = null
                };
                if (!f.HasQueuedOrders && !f.Forming && (f.transform.position - target.HitLocation).sqrMagnitude > 0.1f * 0.1f)
                {
                    _forcesLocations[f] = target.HitLocation;
                    f.EnqueueOrder(WorldObject.Order.MoveOrder(dir, target));
                }
                offset += _formationWidth + _formation_padding;
                yield return new WaitForEndOfFrame();
            }
        }

        // Reserve
        Vector3 reserveLocation = Vector3.zero;
        int numFrontTowns = 0;
        foreach (TownCenterBuilding tc in FriendlyFrontTowns())
        {
            reserveLocation += tc.ControlCircleCenter.position;
            ++numFrontTowns;
        }
        reserveLocation = reserveLocation / numFrontTowns;
        yield return new WaitForEndOfFrame();
        Vector3 frontLocation = Vector3.zero;
        numFrontTowns = 0;
        foreach (TownCenterBuilding tc in EnemyFrontTowns())
        {
            frontLocation += tc.ControlCircleCenter.position;
            ++numFrontTowns;
        }
        frontLocation = frontLocation / numFrontTowns;
        Vector3 frontDirection = (frontLocation - reserveLocation).normalized;
        _reserveForceAxis = Quaternion.AngleAxis(90, Vector3.up) * frontDirection;
        yield return new WaitForEndOfFrame();

        _reserveForceLocation = reserveLocation;
        offset = 0.0f;
        foreach (Formation f in _reserveForce)
        {
            ClickHitObject target = new ClickHitObject()
            {
                HitLocation = ClosestPtOnNavMesh(reserveLocation + _reserveForceAxis * offset),
                HitObject = null
            };
            if (!f.HasQueuedOrders && !f.Forming && (f.transform.position - target.HitLocation).sqrMagnitude > 0.1f * 0.1f)
            {
                _forcesLocations[f] = target.HitLocation;
                f.EnqueueOrder(WorldObject.Order.MoveOrder(target, null));
            }
            offset += _formationWidth + _formation_padding;
            yield return new WaitForEndOfFrame();
        }

        _attackForceLocation = reserveLocation;
        _attackForceAxis = _reserveForceAxis;
        offset = 0.0f;
        foreach (Formation f in _attackForce)
        {
            ClickHitObject target = new ClickHitObject()
            {
                HitLocation = ClosestPtOnNavMesh(reserveLocation + _attackForceAxis * offset + frontDirection * _formationDepth),
                HitObject = null
            };
            if (!f.HasQueuedOrders && !f.Forming && (f.transform.position - target.HitLocation).sqrMagnitude > 0.1f * 0.1f)
            {
                _forcesLocations[f] = target.HitLocation;
                f.EnqueueOrder(WorldObject.Order.MoveOrder(target, null));
            }
            offset += _formationWidth + _formation_padding;
            yield return new WaitForEndOfFrame();
        }
    }

    private Vector3 ClosestPtOnNavMesh(Vector3 pt)
    {
        NavMeshHit hit;
        for (int i = 1; i < 10; ++i)
        {
            if (NavMesh.SamplePosition(pt, out hit, _formationWidth * i, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return pt;
    }

    private bool DecideStartAttack()
    {
        return _attackTarget != null && _attackRequiredForce <= EstimateForce(_attackForce) && _attackForce.All(x => !x.Forming);
    }

    private void StartAttack()
    {
        SendForceToTarget(_attackForce, _attackTarget.ControlCircleCenter.position, _attackForceAxis, MoveType.Immediate);
    }

    private IEnumerator SetAttackTarget()
    {
        TownCenterBuilding leastDefendedTown = null;
        int leastDefenses = 0;
        foreach (TownCenterBuilding tc in EnemyFrontTowns(true))
        {
            HashSet<Formation> enemyDefenses = new HashSet<Formation>();
            Collider[] unitsAtTarget = Physics.OverlapSphere(tc.ControlCircleCenter.position, _defenseDistance, _unitsLayerMask);
            foreach (Collider c in unitsAtTarget)
            {
                Formation f = c.GetComponent<Formation>();
                if (f != null && _player.IsHostile(f.Owner))
                {
                    enemyDefenses.Add(f);
                }
            }
            int defensesAtTC = EstimateForce(enemyDefenses);
            if (leastDefendedTown == null || defensesAtTC < leastDefenses)
            {
                leastDefendedTown = tc;
                leastDefenses = defensesAtTC;
            }
            yield return new WaitForEndOfFrame();
        }
        _attackTarget = leastDefendedTown;
        yield return null;
    }

    private Formation AggroLocation(Vector3 location, float aggroRange)
    {
        Collider[] detectedUnits = Physics.OverlapSphere(location, aggroRange, _unitsLayerMask);
        foreach (Collider c in detectedUnits)
        {
            Unit u = c.GetComponent<Unit>();
            if (u == null)
            {
                continue;
            }
            Formation fDetected = u.ContainingFormation;
            if (fDetected == null || !_player.IsHostile(fDetected.Owner))
            {
                continue;
            }

            // Enemy!
            return fDetected;
        }
        return null;
    }

    private Formation AggroFormation(Formation f, float aggroRange)
    {
        return AggroLocation(f.transform.position, aggroRange);
    }

    private Formation AggroForce(IEnumerable<Formation> force, float aggroRange)
    {
        foreach (Formation f in force)
        {
            Formation fDetected = AggroFormation(f, aggroRange);
            if (fDetected != null)
            {
                return fDetected;
            }
        }
        return null;
    }

    private Formation AggroTown(TownCenterBuilding tc, float aggroRange)
    {
        return AggroLocation(tc.ControlCircleCenter.position, aggroRange);
    }

    private AIPlayerState HandleBuildup()
    {
        if (_reinforceCounter > 0)
        {
            --_reinforceCounter;
        }
        else
        {
            ReinforceFormations();
            _reinforceCounter = _reinforcePulses;
        }
        RequisionFormation();
        StartCoroutine(SetAttackTarget());

        if (DecideStartAttack())
        {
            StartAttack();
            return AIPlayerState.Attacking;
        }
        return AIPlayerState.Buildup;
    }

    private AIPlayerState HandleAttacking()
    {
        if (_attackForce.Count == 0)
        {
            _attackTarget = null;
            return AIPlayerState.AttackFail;
        }
        else if (_attackTarget.Owner == _player)
        {
            _attackTarget = null;
            return AIPlayerState.AttackSuccess;
        }
        foreach (Formation f in _attackForce)
        {
            if (f.NumQueuedOrders <= 1)
            {
                Formation fEnemy = AggroFormation(f, _attackForceAggroRange);
                if (fEnemy != null)
                {
                    ClickHitObject target = new ClickHitObject()
                    {
                        HitLocation = fEnemy.transform.position,
                        HitObject = fEnemy.GetRangedTargetingUnit().gameObject
                    };
                    f.PushInterruptiveOrder(WorldObject.Order.MoveOrder(target, null));
                }
            }
        }
        return AIPlayerState.Attacking;
    }

    private AIPlayerState HandleAttackingWithReserve()
    {
        return AIPlayerState.AttackingWithReserve;
    }

    private AIPlayerState HandleAttackSuccess()
    {
        return AIPlayerState.Buildup; // temporary
        return AIPlayerState.AttackSuccess;
    }

    private AIPlayerState HandleAttackFail()
    {
        return AIPlayerState.Buildup; // temporary
        return AIPlayerState.AttackFail;
    }

    private AIPlayerState HandleReplenishReserve()
    {
        return AIPlayerState.ReplenishReserve;
    }

    private AIPlayerState HandleDefending()
    {
        return AIPlayerState.Defending;
    }

    private AIPlayerState HandleDefendingWithReserve()
    {
        return AIPlayerState.DefendingWithReserve;
    }

    private void AddFreeFormations()
    {
        Formation[] formations = GetComponentsInChildren<Formation>();
        foreach (Formation f in formations)
        {
            if (_reserveForce.Contains(f) ||
                _attackForce.Contains(f))
            {
                continue;
            }
            bool isFree = true;
            foreach (List<Formation> defForce in _townDefenseForces.Values)
            {
                if (defForce.Contains(f))
                {
                    isFree = false;
                    break;
                }
            }
            if (isFree)
            {
                AddFormation(f);
            }
        }
        _addedInitialFormation = true;
    }

    private AIPlayerState _currState = AIPlayerState.Buildup;

    private Player _player;

    private Dictionary<TownCenterBuilding, List<Formation>> _townDefenseForces = new Dictionary<TownCenterBuilding, List<Formation>>();
    private Dictionary<TownCenterBuilding, int> _townDefenseRequiredForces = new Dictionary<TownCenterBuilding, int>();
    private Dictionary<TownCenterBuilding, Tuple<Vector3, Vector3>> _townDefenseLocations = new Dictionary<TownCenterBuilding, Tuple<Vector3, Vector3>>();

    private List<Formation> _reserveForce = new List<Formation>();
    private int _reserveRequiredForce = 0;
    private Vector3 _reserveForceLocation;
    private Vector3 _reserveForceAxis;

    private List<Formation> _attackForce = new List<Formation>();
    private int _attackRequiredForce = 20;
    private Vector3 _attackForceLocation;
    private Vector3 _attackForceAxis;

    private List<TownCenterBuilding> _ownedTowns = new List<TownCenterBuilding>();
    private List<TownCenterBuilding> _allTowns;
    private Dictionary<TownCenterBuilding, Queue<ProducedFormationTarget>> _townProducedFormationTargets = new Dictionary<TownCenterBuilding, Queue<ProducedFormationTarget>>();
    private bool _addedInitialFormation = false;

    private Dictionary<Formation, Vector3> _forcesLocations = new Dictionary<Formation, Vector3>();

    private Dictionary<TownCenterBuilding, Dictionary<TownCenterBuilding, NavMeshPath>> _townsGraph;

    private TownCenterBuilding _attackTarget = null;
    private TownCenterBuilding _defenseTarget = null;

    private static readonly float _formationWidth = 7.0f * 2.0f;
    private static readonly float _formationDepth = 3.0f * 2.0f;
    private static readonly float _formation_padding = 2.0f;
    private static readonly float _defenseDistance = 60.0f;
    private static readonly float _attackForceAggroRange = 40.0f;
    private static readonly float _TownAggroRange = _defenseDistance + 30.0f;

    private static readonly int _reinforcePulses = 100;
    private int _reinforceCounter = _reinforcePulses;
    private static readonly int _adjustForcesLocationPulses = 10;
    private int _adjustForcesLocationCounter = _adjustForcesLocationPulses;

    private int _wallsLayerMask;
    private int _unitsLayerMask;

    private int _requiredTownDefenseForces = 20 * 3;
    private int _requiredTownDefenseForcesInitial = 20 * 1;
    private int _requiredTownDefenseForcesFront = 20 * 4;
    private bool _firstTown = true;

    public enum AIPlayerState
    {
        Buildup,
        Attacking,
        AttackingWithReserve,
        AttackSuccess,
        AttackFail,
        ReplenishReserve,
        Defending,
        DefendingWithReserve
    };

    private enum MoveType
    {
        Immediate,
        Enqueue,
        Push
    }

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
