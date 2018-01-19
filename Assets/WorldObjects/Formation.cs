using Maniple;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class Formation : WorldObject
{
    public enum FormationBehavior { MeleeBehavior, MissileBehavior };
    public enum FormationType { Field, WallLeft, WallRight }

    protected void GeneratePositions(FormationType t)
    {
        switch (t)
        {
            case FormationType.Field:
                _relativePositions = FieldFormation().ToArray();
                break;
            case FormationType.WallLeft:
                _relativePositions = WallFormation(true).ToArray();
                break;
            case FormationType.WallRight:
                _relativePositions = WallFormation(false).ToArray();
                break;
            default:
                break;
        }
    }

    private List<Vector3> FieldFormation()
    {
        List<Vector3> positions = new List<Vector3>(21);
        for (int i = 0; i < 3; ++i)
        {
            float xOffset = 0.0f;
            for (int j = 0; j < _formationWidth; ++j)
            {
                float zOffset = -i * _spacing;
                if (i == 2 && j == 0)
                {
                    positions.Insert(0, new Vector3(xOffset, 0, zOffset));
                }
                else
                {
                    positions.Add(new Vector3(xOffset, 0, zOffset));
                }
                if (j % 2 == 0)
                {
                    xOffset = -xOffset + _spacing;
                }
                else
                {
                    xOffset = -xOffset;
                }
            }
        }
        return positions;
    }

    private List<Vector3> WallFormation(bool left)
    {
        List<Vector3> positions = new List<Vector3>(21);
        for (int i = 0; i < 3; ++i)
        {
            float zBase = ((_formationWidth / 2) + 1) * _spacing;
            if (left)
            {
                zBase = -zBase;
            }
            float zOffset = 0.0f;
            for (int j = 0; j < _formationWidth; ++j)
            {
                float xOffset = -i * _spacing;
                if (i == 2 && j == 0)
                {
                    positions.Insert(0, new Vector3(xOffset, 0, zOffset + zBase));
                }
                else
                {
                    positions.Add(new Vector3(xOffset, 0, zOffset + zBase));
                }
                if (j % 2 == 0)
                {
                    zOffset = -zOffset + _spacing;
                }
                else
                {
                    zOffset = -zOffset;
                }
            }
        }
        return positions;
    }


    protected override void Awake()
    {
        base.Awake();
        _actions = new string[] { StopAction, ReinforceAction };
        _currType = FormationType.Field;
        GeneratePositions(_currType);
        _meleeUnits = new List<Unit>(_relativePositions.Length / 2);
        _missileUnits = new List<Unit>(_relativePositions.Length / 2);
        _issuedOrder = false;
        Forming = false;
        _statusLines = new string[] { DisplayName, "", "", "", "" };

        _leaderDriftThreshold = 5.0f * 5.0f;
        _maxDrift = 6.0f * 6.0f;
        _unitDriftThreshold = 1.5f * 1.5f;

        _wallsLayerMask = LayerMask.GetMask("Walls");
    }

    // Use this for initialization
    protected override void Start ()
    {
        base.Start();
        ComputeCenterOfMass();
        _forceFormationTime = Time.unscaledTime;
        _virtualLeader = transform.parent.gameObject;
        _leaderNavAgent = _virtualLeader.GetComponent<NavMeshAgent>();
        _leaderBaseSpeed = _leaderNavAgent.speed;
        _targetMarkerRenderer = GetComponent<LineRenderer>();
        StartCoroutine(PeriodicForcePosition());
        StartCoroutine(PeriodicDetectEnemy());
    }

    // Update is called once per frame
    protected override void Update ()
    {
        base.Update();
        if (!_issuedOrder && _leaderNavAgent.enabled)
        {
            float dist = _leaderNavAgent.remainingDistance;
            if (dist != Mathf.Infinity && _leaderNavAgent.pathStatus == NavMeshPathStatus.PathComplete && _leaderNavAgent.remainingDistance <= _leaderNavAgent.stoppingDistance)
            {
                _leaderNavAgent.enabled = false;
                ResetSpeed();
                if (_rotateAtTarget)
                {
                    _virtualLeader.transform.rotation = _targetRotation;
                }
                if (_attackTargetFormation != null)
                {
                    if (_attackBehavior == FormationBehavior.MissileBehavior)
                    {
                        Vector3 vecToTarget = _attackTargetFormation.GetRangedTargetingPosition() - _virtualLeader.transform.position;
                        _virtualLeader.transform.rotation = Quaternion.LookRotation(vecToTarget);
                        _virtualLeader.transform.position += _virtualLeader.transform.forward.normalized * 2;
                        _attackTargetFormation = null;
                    }
                    else if (_attackBehavior == FormationBehavior.MeleeBehavior)
                    {
                        MeleePushStep(2.0f + NominalMeleeRange);
                        StartCoroutine(MeleePushBehavior());
                    }
                }
            }
        }

        if (_issuedOrder && _leaderNavAgent.enabled && _leaderNavAgent.velocity.sqrMagnitude > 0.001f)
        {
            _issuedOrder = false;
        }

        if (_owner != null)
        {
            _statusLines[1] = string.Format("{0}", _owner.name);
        }
        _statusLines[2] = string.Format("{0}/{1}", _meleeUnits.Count + _missileUnits.Count + ((_commander != null) ? 1 : 0), MaxUnits);
    }

    public int MaxUnits { get { return _relativePositions.Length; } }
    public int NumUnits { get { return _meleeUnits.Count + _missileUnits.Count + ((_commander != null) ? 1 : 0); } }

    private void ComputeCenterOfMass()
    {
        Vector3 sum = Vector3.zero;
        int actualUnits = 0;
        foreach (Unit u in GetUnits())
        {
            sum += u.transform.position;
            ++actualUnits;
        }
        if (actualUnits > 0)
        {
            _centerOfMass = sum / actualUnits;
        }
        else
        {
            _centerOfMass = Vector3.zero;
        }
    }

    private IEnumerable<Unit> GetUnits()
    {
        return GetUnits(false);
    }

    private IEnumerable<Unit> GetUnits(bool nullCommander)
    {
        if (nullCommander || _commander != null)
        {
            yield return _commander;
        }
        IEnumerator<Unit> meleePtr = _meleeUnits.GetEnumerator();
        IEnumerator<Unit> missilePtr = _missileUnits.GetEnumerator();

        while (true)
        {
            // Melee rank:
            for (int i = 0; i < _formationWidth; ++i)
            {
                if (meleePtr.MoveNext())
                {
                    yield return meleePtr.Current;
                }
                else if (missilePtr.MoveNext())
                {
                    yield return missilePtr.Current;
                }
                else
                {
                    yield break;
                }
            }
            // Missile rank:
            for (int i = 0; i < _formationWidth; ++i)
            {
                if (meleePtr.MoveNext())
                {
                    yield return meleePtr.Current;
                }
                else if (missilePtr.MoveNext())
                {
                    yield return missilePtr.Current;
                }
                else
                {
                    yield break;
                }
            }
        }
    }

    private IEnumerable<Tuple<Unit, Vector3>> GetUnitsWithPositions()
    {
        int i = 0;
        foreach (Unit u in GetUnits(true))
        {
            if (u != null)
            {
                yield return new Tuple<Unit, Vector3>(u, _relativePositions[i]);
            }
            ++i;
        }
    }

    private IEnumerable<Tuple<int, Unit, Vector3>> GetUnitsWithIndices()
    {
        int i = 0;
        foreach (Unit u in GetUnits(true))
        {
            if (u != null)
            {
                yield return new Tuple<int, Unit, Vector3>(i, u, _relativePositions[i]);
            }
            ++i;
        }
    }

    private IEnumerator PeriodicForcePosition()
    {
        while (true)
        {
            CheckWallBehavior();
            ComputeCenterOfMass();
            AdjustLeaderSpeed();
            AdjustUnitSpeed();
            ForcePositions();
            yield return new WaitForSeconds(1.0f);
        }
    }

    private void ForcePositions()
    {
        List<Tuple<Unit, Vector3>> PR2 = new List<Tuple<Unit, Vector3>>();
        foreach (Tuple<Unit, Vector3> unit in GetUnitsWithPositions())
        {
            Vector3 targetPos = transform.TransformPoint(unit.Item2);
            targetPos.y = unit.Item1.transform.position.y;
            PR2.Add(new Tuple<Unit, Vector3>(unit.Item1, targetPos));
        }
        foreach (Tuple<Unit, Vector3> unit in PR2)
        {
            _allInPosition = true;
            if ((unit.Item1.transform.position - unit.Item2).sqrMagnitude > 0.01)
            {
                if (!_meleeAttackMove)
                {
                    unit.Item1.StartMove(unit.Item2, Quaternion.LookRotation(transform.forward));
                }
                else
                {
                    unit.Item1.MeleeAttackMoveOrder(unit.Item2);
                }
                _allInPosition = false;
            }
            else
            {
                unit.Item1.Stop();
            }
        }
    }

    private void AdjustLeaderSpeed()
    {
        float sqrDistFromCM = (_virtualLeader.transform.position - _centerOfMass).sqrMagnitude;
        if (sqrDistFromCM > _leaderDriftThreshold)
        {
            _speedModifier = (_maxDrift - (sqrDistFromCM - _leaderDriftThreshold)) / (_maxDrift);
            if (_speedModifier < _minSpeedModifier)
            {
                _speedModifier = _minSpeedModifier;
            }
            _leaderNavAgent.speed = _leaderBaseSpeed * _speedModifier;
        }
        else
        {
            _speedModifier = 1.0f;
            _leaderNavAgent.speed = _leaderBaseSpeed;
        }
    }

    private void AdjustUnitSpeed()
    {
        List<Tuple<Unit, Vector3>> PR2 = new List<Tuple<Unit, Vector3>>();
        foreach (Tuple<Unit, Vector3> unit in GetUnitsWithPositions())
        {
            Vector3 targetPos = transform.TransformPoint(unit.Item2);
            targetPos.y = unit.Item1.transform.position.y;
            PR2.Add(new Tuple<Unit, Vector3>(unit.Item1, targetPos));
        }
        bool allInPos = true;
        foreach (Tuple<Unit, Vector3> unit in PR2)
        {
            Vector3 unitPos = unit.Item1.transform.position;
            Vector3 unitTargetPos = unit.Item2;
            if ((unitPos - unitTargetPos).sqrMagnitude > _unitDriftThreshold)
            {
                allInPos = false;
                break;
            }
        }
        if (!allInPos)
        {
            foreach (Tuple<Unit, Vector3> unit in PR2)
            {
                Vector3 unitPos = unit.Item1.transform.position;
                Vector3 unitTargetPos = unit.Item2;
                NavMeshAgent agent = unit.Item1.GetComponent<NavMeshAgent>();
                if ((unitPos - unitTargetPos).sqrMagnitude < _unitDriftThreshold)
                {
                    agent.speed = _leaderBaseSpeed * _speedModifier;
                }
                else
                {
                    agent.speed = _leaderBaseSpeed;
                }
            }
        }
        else
        {
            foreach (Tuple<Unit, Vector3> unit in PR2)
            {
                NavMeshAgent agent = unit.Item1.GetComponent<NavMeshAgent>();
                agent.speed = _leaderBaseSpeed;
            }
        }
    }

    private void ResetSpeed()
    {
        _speedModifier = 1.0f;
        _leaderNavAgent.speed = _leaderBaseSpeed;
        foreach (Unit unit in GetUnits())
        {
            NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
            agent.speed = _leaderBaseSpeed;
        }
    }

    private void CheckWallBehavior()
    {
        Collider[] walls = Physics.OverlapSphere(_virtualLeader.transform.position, 0.2f, _wallsLayerMask);
        if (walls.Length > 0)
        {
            Transform wallAlignMarker = null;
            float minDist = -1.0f;
            foreach (Collider w in walls)
            {
                float currDist = (w.transform.position - transform.position).sqrMagnitude;
                if (minDist < 0 || currDist < minDist)
                {
                    Transform currMarker = w.transform.Find("AlignMarker");
                    if (currMarker != null)
                    {
                        minDist = currDist;
                        wallAlignMarker = currMarker;
                    }
                }
            }

            if (wallAlignMarker != null)
            {
                FormationType newType = FormationType.WallLeft;
                if (_leaderNavAgent.enabled)
                {
                    if (Vector3.Dot(_leaderNavAgent.velocity, wallAlignMarker.right) > 0.0f)
                    {
                        newType = FormationType.WallLeft;
                    }
                    else
                    {
                        newType = FormationType.WallRight;
                    }
                }
                if (_currType != newType)
                {
                    _currType = newType;
                    GeneratePositions(_currType);
                }
                _currWall = wallAlignMarker;
            }
        }
        else
        {
            if (_currType != FormationType.Field)
            {
                _currType = FormationType.Field;
                GeneratePositions(_currType);
                _currWall = null;
            }
        }

        if (_currWall)
        {
            Vector3 currWallAxis = _currWall.right;

            Vector3 currPos = _virtualLeader.transform.position;
            if (_leaderNavAgent.enabled)
            {
                Vector3 currDest = _leaderNavAgent.destination;
                Vector3 vecToDest = currDest - _currWall.position;
                Vector3 newDest = _currWall.position + Vector3.Project(vecToDest, currWallAxis);
                _issuedOrder = true;
                _rotateAtTarget = true;
                _leaderNavAgent.SetDestination(newDest);
                _targetRotation = Quaternion.LookRotation(currWallAxis);
            }
            else
            {
                Vector3 vecToPos = currPos - _currWall.position;
                Vector3 newPos = _currWall.position + Vector3.Project(vecToPos, currWallAxis);
                _virtualLeader.transform.position = newPos;
                _virtualLeader.transform.rotation = Quaternion.LookRotation(currWallAxis);
            }
        }
    }

    public override void IssueOrder(ClickHitObject target, ClickHitObject rClickStart, Player controller)
    {
        base.IssueOrder(target, rClickStart, controller);
        if (_owner == controller)
        {
            if (target != null)
            {
                Unit targetUnit = target.HitObject.GetComponent<Unit>();
                if (targetUnit == null || !_owner.IsHostile(targetUnit.Owner))
                {
                    // Move order
                    if (rClickStart == null || target.HitLocation == rClickStart.HitLocation)
                    {
                        _leaderNavAgent.enabled = true;
                        _leaderNavAgent.SetDestination(target.HitLocation);
                        _issuedOrder = true;
                        _rotateAtTarget = false;
                    }
                    else
                    {
                        _targetRotation = Quaternion.LookRotation(target.HitLocation - rClickStart.HitLocation);
                        _leaderNavAgent.enabled = true;
                        _leaderNavAgent.SetDestination(rClickStart.HitLocation);
                        _issuedOrder = true;
                        _rotateAtTarget = true;
                    }
                }
                else
                {
                    // attack order
                    if (_attackBehavior == FormationBehavior.MissileBehavior)
                    {
                        Vector3 attackPos = FindNearestAttackPosition(targetUnit);
                        _leaderNavAgent.enabled = true;
                        _leaderNavAgent.SetDestination(attackPos);
                        _issuedOrder = true;
                        _attackTargetFormation = targetUnit.ContainingFormation;
                        StartCoroutine(AdjustAttackFormationPosition());
                    }
                    else if (_attackBehavior == FormationBehavior.MeleeBehavior)
                    {
                        _attackTargetFormation = targetUnit.ContainingFormation;
                        Unit closestEnemy = _attackTargetFormation.ClosestUnitToPoint(transform.position);
                        Vector3 attackPos = FindNearestAttackPosition(closestEnemy);
                        _meleeAttackMove = true;
                        _leaderNavAgent.enabled = true;
                        _leaderNavAgent.SetDestination(attackPos);
                        _issuedOrder = true;
                    }
                }
                //DrawLineToTarget(_leaderNavAgent.destination);
                StartCoroutine(DelayedRemoveTargetLine());
            }
        }
    }

    public void AddUnit(Unit u)
    {
        if (u is Officer)
        {
            if (_commander == null)
            {
                _commander = u as Officer;
            }
        }
        else if (u.IsMissileUnit)
        {
            if (!_missileUnits.Contains(u))
            {
                _missileUnits.Add(u);
            }
        }
        else
        {
            if (!_meleeUnits.Contains(u))
            {
                _meleeUnits.Add(u);
            }
        }
    }
    public void RemoveUnit(Unit u)
    {
        if (u.IsMissileUnit)
        {
            RemoveAndMaintainFormation(u, _missileUnits);
        }
        else
        {
            RemoveAndMaintainFormation(u, _meleeUnits);
        }
        if (_meleeUnits.Count == 0 && _missileUnits.Count ==0)
        {
            DestroyFormation();
        }
    }

    private void RemoveAndMaintainFormation(Unit u, List<Unit> unitList)
    {
        int idx = unitList.IndexOf(u);
        if (idx >= 0)
        {
            if (unitList.Count > idx + _formationWidth)
            {
                unitList[idx] = unitList[idx + _formationWidth];
                unitList.RemoveAt(idx + _formationWidth);
            }
            else
            {
                unitList.RemoveAt(idx);
            }
        }
    }

    public Unit ClosestUnitToPoint(Vector3 pt)
    {
        float dist = Mathf.Infinity;
        Unit res = null;
        foreach (Unit u in GetUnits())
        {
            float currDist = (pt - u.transform.position).sqrMagnitude;
            if (currDist < dist)
            {
                dist = currDist;
                res = u;
            }
        }
        return res;
    }

    private void DestroyFormation()
    {
        transform.parent.parent = null;
        _owner.ForceUpdateForces();
        Destroy(transform.parent.gameObject);
    }

    public override void SelectThis()
    {
        base.SelectThis();
        foreach (Unit u in GetUnits())
        {
            u.DisplaySelectionMarker();
        }
    }
    public override void DeSelectThis()
    {
        base.DeSelectThis();
        foreach (Unit u in GetUnits())
        {
            u.DisplaySelectionMarker();
        }
    }

    private void DetectEmeny()
    {
        Tuple<Unit, Weapon> target = null;
        if (_attackBehavior == FormationBehavior.MissileBehavior)
        {
            Unit u = GetRangedTargetingUnit();
            if (u == null || u.SelectedWeapon == null)
            {
                return;
            }
            target = u.AcquireTargetWithDefaultWeapon();
        }
        if (target != null && target.Item2.AttackWeaponType == WeaponType.Musket)
        {
            Formation targetFormation = target.Item1.ContainingFormation;
            foreach (Unit u in GetUnits())
            {
                if (u.SelectedWeapon.AttackWeaponType == WeaponType.Musket)
                {
                    if (!u.IsAttacking)
                    {
                        u.AttackOrder(ResourceManager.GetRandom(targetFormation.GetUnits()));
                    }
                }
            }
        }

        if (_attackTargetFormation == null)
        {
            _attackTargetFormation = null;
        }
    }

    private IEnumerator PeriodicDetectEnemy()
    {
        while (true)
        {
            if (_leaderNavAgent.enabled == false)
            {
                DetectEmeny();
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    public Unit GetRangedTargetingUnit()
    {
        IEnumerator<Unit> unitsPtr = GetUnits().GetEnumerator();
        if (!unitsPtr.MoveNext())
        {
            return null;
        }
        // commander
        Unit commander = unitsPtr.Current;
        if (unitsPtr.MoveNext())
        {
            // center of first rank
            Unit center = unitsPtr.Current;
            if (center != null)
            {
                return center;
            }
        }
        if (commander != null)
        {
            return commander;
        }
        return null;
    }

    public Vector3 GetRangedTargetingPosition()
    {
        Unit targetUnit = GetRangedTargetingUnit();
        if (targetUnit == null)
        {
            return Vector3.zero;
        }
        else
        {
            return targetUnit.transform.position;
        }
    }

    private Vector3 FindNearestAttackPosition(Unit attackTarget)
    {
        float attackRange = 0.0f;
        if (_attackBehavior == FormationBehavior.MissileBehavior)
        {
            if (_missileUnits.Count > 0)
            {
                attackRange = _missileUnits[0].SelectedWeapon.MaxRange;
            }
        }
        else
        {
            if (_meleeUnits.Count > 0)
            {
                attackRange = _meleeUnits[0].SelectedWeapon.MaxRange;
            }
        }
        Vector3 targetLocation = attackTarget.transform.position;
        Vector3 direction = targetLocation - transform.position;
        float targetDistance = direction.magnitude;
        if (targetDistance < attackRange)
        {
            return transform.position;
        }
        float distanceToTravel = targetDistance - (0.95f * attackRange);
        return Vector3.Lerp(transform.position, targetLocation, distanceToTravel / targetDistance);
    }

    private IEnumerator AdjustAttackFormationPosition()
    {
        while(_attackTargetFormation != null)
        {
            Vector3 attackPos = FindNearestAttackPosition(_attackTargetFormation.GetRangedTargetingUnit());
            if (transform.position != attackPos)
            {
                _leaderNavAgent.enabled = true;
                _leaderNavAgent.SetDestination(attackPos);
                _issuedOrder = true;
            }
            yield return new WaitForSeconds(1.0f);
        }
    }

    private IEnumerator MeleePushBehavior()
    {
        while (_attackTargetFormation != null)
        {
            bool outOfMelee = true;
            foreach (Unit u in GetUnits())
            {
                if (u.IsAttacking)
                {
                    outOfMelee = false;
                    break;
                }
            }
            if (outOfMelee && _allInPosition)
            {
                MeleePushStep(0.5f);
            }
            yield return new WaitForSeconds(0.1f);
        }
        _attackTargetFormation = null;
        _meleeAttackMove = false;
        yield return null;
    }

    private void MeleePushStep(float size)
    {
        Vector3 centerOfEnemyFormation = _attackTargetFormation._centerOfMass;
        Vector3 pushVector = (centerOfEnemyFormation - transform.position).normalized * size;
        _virtualLeader.transform.position += pushVector;
    }

    private void DrawLineToTarget(Vector3 target)
    {
        _targetMarkerRenderer.enabled = true;
        _targetMarkerRenderer.positionCount = _targetMarkerSegments;
        Vector3 currPt = transform.position;
        Vector3 step = (target - currPt) / (_targetMarkerSegments - 1);
        //float heightAtMax = (currPt.y + target.y) / 2 + 4;
        for (int i = 0; i < _targetMarkerSegments; ++i)
        {
            _targetMarkerRenderer.SetPosition(i, currPt);
            currPt.x += step.x;
            currPt.z += step.z;
            if (i < _targetMarkerSegments / 2)
            {
                currPt.y += Mathf.Abs(step.y) * (i * 0.5f) * (i * 0.5f);
            }
            else
            {
                currPt.y -= Mathf.Abs(step.y) * ((_targetMarkerSegments - i) * 0.5f) * ((_targetMarkerSegments - i) * 0.5f);
            }
        }
    }

    private IEnumerator DelayedRemoveTargetLine()
    {
        yield return new WaitForSeconds(2.0f);
        _targetMarkerRenderer.enabled = false;
        yield return null;
    }

    public float NominalMeleeRange
    {
        get
        {
            if (_meleeUnits.Count > 0)
            {
                return _meleeUnits[0].SelectedWeapon.MaxRange;
            }
            else
            {
                return 0.0f;
            }
        }
    }

    public float NominalMissileRange
    {
        get
        {
            if (_missileUnits.Count > 0)
            {
                return _missileUnits[0].SelectedWeapon.MaxRange;
            }
            else
            {
                return 0.0f;
            }
        }
    }

    public override void PerformAction(string action)
    {
        base.PerformAction(action);
        if (action == ReinforceAction)
        {
            TownCenterBuilding[] allTownCenters = FindObjectsOfType<TownCenterBuilding>();
            foreach (TownCenterBuilding t in allTownCenters)
            {
                if (t.Owner == _owner)
                {
                    Vector3 vecToTownCenter = t.ControlCircleCenter.position - transform.position;
                    if (vecToTownCenter.sqrMagnitude < t.CaptureRange * t.CaptureRange)
                    {
                        t.ReinforceFormation(this);
                        break;
                    }
                }
            }
        }
        else if (action == StopAction)
        {
            if (_leaderNavAgent.enabled)
            {
                _leaderNavAgent.destination = _virtualLeader.transform.position;
                _leaderNavAgent.enabled = false;
            }
            ResetSpeed();
            _attackTargetFormation = null;
            foreach (Unit u in GetUnits())
            {
                u.StopMovingAndAttacking();
            }
        }
    }

    public override void DisplayOrderMarker(LineRenderer targetLR, ClickHitObject source, ClickHitObject target)
    {
        base.DisplayOrderMarker(targetLR, source, target);
        Vector3 targetPos = (target.HitLocation != source.HitLocation) ? target.HitLocation : (source.HitLocation + transform.forward);
        targetLR.positionCount = 5;
        Vector3 formarionForwardVec = targetPos - source.HitLocation;
        formarionForwardVec.y = 0;
        Quaternion formationTargetForward = Quaternion.LookRotation(formarionForwardVec);
        //Quaternion formationTargetLeft = formationTargetForward * Quaternion.AngleAxis(90, Vector3.up);
        Vector3 formarionLeftVec = Quaternion.AngleAxis(90, Vector3.up) * formarionForwardVec;
        Vector3 formarionForwardVecN = formarionForwardVec.normalized;
        Vector3 formarionLeftVecN = formarionLeftVec.normalized;
        Vector3 formationTargetOrigin = source.HitLocation;
        formationTargetOrigin.y += 0.1f;
        Vector3 formationForLeftCorner = formationTargetOrigin + (formarionLeftVecN * (_formationWidth * _spacing / 2.0f));
        Vector3 formationForRightCorner = formationForLeftCorner - (formarionLeftVecN * (_formationWidth * _spacing));
        float formationDepth = Mathf.Ceil(((float)NumUnits) / _formationWidth) * _spacing;
        Vector3 formationBackLeftCorner = formationForLeftCorner - (formarionForwardVecN * formationDepth);
        Vector3 formationBackRightCorner = formationForRightCorner - (formarionForwardVecN * formationDepth);
        targetLR.SetPosition(0, formationForLeftCorner);
        targetLR.SetPosition(1, formationForRightCorner);
        targetLR.SetPosition(2, formationBackRightCorner);
        targetLR.SetPosition(3, formationBackLeftCorner);
        targetLR.SetPosition(4, formationForLeftCorner);
    }

    public bool Forming { get; set; }

    public bool CommanderAlive { get { return _commander != null; } }

    //protected Transform[] _unitPositions;
    protected Vector3[] _relativePositions;
    protected Officer _commander;
    protected List<Unit> _meleeUnits;
    protected List<Unit> _missileUnits;
    protected Vector3 _centerOfMass;
    protected bool _forming;
    protected GameObject _virtualLeader;
    private NavMeshAgent _leaderNavAgent;
    protected float _forceFormationTime;
    private bool _issuedOrder, _meleeAttackMove, _allInPosition;
    private Quaternion _targetRotation;
    private bool _rotateAtTarget;
    public FormationBehavior _attackBehavior;
    private FormationType _currType;
    private int _formationWidth = 7;
    private float _spacing = 2.0f;
    private Formation _attackTargetFormation;
    private readonly int _targetMarkerSegments = 32;
    private LineRenderer _targetMarkerRenderer;
    public string ReinforceAction;
    private static readonly string StopAction = "Stop";

    private float _leaderBaseSpeed;
    private float _maxDrift;
    private float _leaderDriftThreshold;
    private float _unitDriftThreshold;
    private float _minSpeedModifier = 0.3f;
    private float _speedModifier;

    private int _wallsLayerMask;
    private Transform _currWall = null;
}
