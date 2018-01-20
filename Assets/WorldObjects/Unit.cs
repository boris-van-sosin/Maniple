using System;
using System.Collections;
using System.Collections.Generic;
using Maniple;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class Unit : WorldObject
{
    protected override void Awake()
    {
        base.Awake();
        _missileTargetingLayerMask = _unitsLayerMask | _LOSObstacleLayerMask;
        ResetStatus();
    }

    protected override void OnGUI()
    {
        base.OnGUI();
    }

    protected override void Start()
    {
        base.Start();
        TeamcolorAspect teamcolor = transform.GetComponentInChildren<TeamcolorAspect>();
        if (teamcolor != null)
        {
            teamcolor.SetTeamColor();
        }
        _attackTime = Time.unscaledTime;
        DrawControlCircle();
        _navAgent = GetComponent<NavMeshAgent>();
        _navAgent.destination = transform.position;
        if (ContainingFormation != null)
        {
            ContainingFormation.AddUnit(this);
        }
        DisplaySelectionMarker();
        if (!IsMissileUnit)
        {
            StartCoroutine(MeleeAcquireTarget());
        }
    }

    protected override void Update()
    {
        base.Update();
        if (_dying)
        {
            return;
        }

        _moving = (!_navAgent.isStopped) && _navAgent.hasPath;
        _rotating = false;
        if (_moving)
        {
            if (_navAgent.velocity != Vector3.zero)
            {
                Quaternion q = Quaternion.LookRotation(_navAgent.velocity);
                _rotating = Quaternion.Angle(q, transform.rotation) < 5.0f;
            }
        }
        _prevMove = _rotating || _moving || _turningToTarget;
        Attack();
        TurnToRequiredFacing();

        if (!_dying && _navAgent.remainingDistance < _navAgent.stoppingDistance)
        {
            _attackMove = false;
        }
    }

    protected virtual void Attack()
    {
        if (!_moving && _attacking && _movingToAttack && CanAttack())
        {
            _movingToAttack = false;
            _turningToTarget = true;
            _facingTarget = false;
        }
        if (_attacking)
        {
            if (_attackTarget == null || _attackTarget.Dying)
            {
                ResetStatus();
                return;
            }
            if (_turningToTarget)
            {
                TurnToTarget();
            }
            if (FacingTarget)
            {
                if (CanAttack())
                {
                    UseWeapon();
                    _inCombat = true;
                }
                else
                {
                    ResetStatus();
                }
            }
            else
            {
                _turningToTarget = true;
                TurnToTarget();
            }
        }
    }

    public override void MouseClick(ClickHitObject hitObject, Player controller)
    {
        if (_dying)
        {
            return;
        }
        base.MouseClick(hitObject, controller);
    }

    public override void IssueOrder(ClickHitObject target, ClickHitObject rClickStart, Player controller)
    {
        if (_dying)
        {
            return;
        }
        if (_owner != null && _owner.Human && _selected && target != null)
        {
            Unit targetWO = target.HitObject.GetComponent<Unit>();
            if (_weapons != null && _weapons.Length > 0 && targetWO != null && _owner.IsHostile(targetWO._owner))
            {
                // Target is hostile. Attack.
                AttackOrder(targetWO);
            }
            else
            {
                ResetStatus();
                StartMove(new Vector3(target.HitLocation.x, transform.position.y, target.HitLocation.z));
            }
        }
    }

    public void AttackOrder(Unit targetWO)
    {
        SwitchWeapon(0);
        AttackOrderWithSelectedWeapon(targetWO);
    }

    public void AttackOrderWithSelectedWeapon(Unit targetWO)
    {
        _attackTarget = targetWO;
        _attacking = true;
        if (TargetInRange())
        {
            _turningToTarget = true;
            _movingToAttack = false;
        }
        else
        {
            _movingToAttack = true;
            StartMove(FindNearestAttackPosition(_selectedWeapon.MaxRange));
        }
    }

    public virtual void MeleeAttackMoveOrder(Vector3 dest)
    {
        _attackMove = true;
        StartMove(dest);
    }

    public void StartMove(Vector3 dest)
    {
        if (_dying)
        {
            return;
        }
        if (_navAgent != null)
        {
            _requiredFacing = null;
            _destLocation = dest;
            _navAgent.SetDestination(dest);
            //Debug.Log(string.Format("Moving. Distance: {0}", (dest - transform.position).magnitude));
            _navAgent.isStopped = false;
            //
            //GameObject p0 = transform.root.Find("TargetMarker").gameObject;
            //GameObject p1 = Instantiate<GameObject>(p0, dest, Quaternion.LookRotation(Vector3.up));
            //Destroy(p1, 10.0f);
            //
        }
    }

    public void StartMove(Vector3 dest, Quaternion requiredFacing)
    {
        StartMove(dest);
        _requiredFacing = requiredFacing;
    }

    public void Stop()
    {
        if (!_dying)
        {
            _destLocation = transform.position;
            _navAgent.SetDestination(_destLocation);
            _navAgent.isStopped = true;
            _navAgent.isStopped = false;
        }
    }

    public void StopMovingAndAttacking()
    {
        Stop();
        ResetStatus();
        _inCombat = false;
        _inMelee = false;
    }

    protected void TurnToRequiredFacing()
    {
        if (_moving || _attacking || _dying || _requiredFacing == null)
        {
            return;
        }
        if (_prevMove)
        {
            _turningInFormation = true;
        }
        if (_turningInFormation)
        {
            if (Quaternion.Angle(transform.rotation, _requiredFacing.Value) < 1.0f)
            {
                _turningInFormation = false;
                _requiredFacing = null;
            }
            else
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, _requiredFacing.Value, RotateSpeed);
            }
        }
    }

    protected void TurnToTarget()
    {
        if (_moving || _movingToAttack || _dying)
        {
            return;
        }
        ValueTuple<Quaternion, float> angleToTarget = AngleToTarget;
        if (angleToTarget.Item2 < 5.0f)
        {
            _turningToTarget = false;
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, angleToTarget.Item1, RotateSpeed);
        }
    }

    protected ValueTuple<Quaternion, float> AngleToTarget
    {
        get
        {
            Vector3 posFlat = new Vector3(transform.position.x, 0.0f, transform.position.z);
            Vector3 targetFlat = new Vector3(_attackTarget.transform.position.x, 0.0f, _attackTarget.transform.position.z);
            Quaternion targetRotation = Quaternion.LookRotation(targetFlat - posFlat);
            return new ValueTuple<Quaternion, float>(targetRotation, Quaternion.Angle(transform.rotation, targetRotation));
        }
    }

    protected bool FacingTarget
    {
        get
        {
            return AngleToTarget.Item2 < 5.0f;
        }
    }

    protected virtual void UseWeapon()
    {
        if (_dying)
        {
            return;
        }
        if (Time.unscaledTime - _attackTime > _selectedWeapon.AttackDelay)
        {
            UseWeaponAnim();
            float range;
            if (_selectedWeapon.AttackWeaponType == WeaponType.Musket)
            {
                range = Combat.ComputeMissileRange(GetTargetingPosition(_selectedWeapon),
                                                   GetTargetingEnemyPosition(_attackTarget, _selectedWeapon), false);
            }
            else
            {
                range = 0.0f;
            }
            if (Combat.Hit(_selectedWeapon, _attackTarget.Defenses, range))
            {
                switch (_selectedWeapon.AttackWeaponType)
                {
                    case WeaponType.Pike:
                        _attackTarget.Kill(0.5f * 0.65f);
                        break;
                    case WeaponType.Sword:
                        _attackTarget.Kill(0.7f * 0.8f);
                        break;
                    case WeaponType.Musket:
                        _attackTarget.Kill(0.0f);
                        break;
                    default:
                        _attackTarget.Kill(0.0f);
                        break;
                }

                ResetStatus();
                if (_attackMove)
                {
                    _navAgent.isStopped = false;
                }
            }
            else
            {
                _attackTarget.Attacked(this, _selectedWeapon);
            }
            _attackTime = Time.unscaledTime;
        }
    }

    protected virtual void UseWeaponAnim()
    { }

    public virtual void Kill(float delay)
    {
        _dying = true;
        _selectionMarkerRenderer.enabled = false;
        if (ContainingFormation != null)
        {
            ContainingFormation.RemoveUnit(this);
        }
        ResetStatus();
        _navAgent.enabled = false;
    }

    protected virtual Vector3 FindNearestAttackPosition(float weaponRange)
    {
        Vector3 targetLocation = _attackTarget.transform.position;
        Vector3 direction = targetLocation - transform.position;
        float targetDistance = direction.magnitude;
        float distanceToTravel = targetDistance - (0.95f * weaponRange);
        return Vector3.Lerp(transform.position, targetLocation, distanceToTravel / targetDistance);
    }

    protected bool TargetInRange()
    {
        return TargetInRangeWithWeapon(_selectedWeapon);
    }

    protected bool TargetInRangeWithWeapon(Weapon w)
    {
        return w.InRange(GetTargetingPosition(w), GetTargetingEnemyPosition(_attackTarget, w));
    }

    protected bool TargetInMinimumRangeWithWeapon(Weapon w, Unit target)
    {
        return w.InMinimumRange(GetTargetingPosition(w), GetTargetingEnemyPosition(target, w));
    }

    protected virtual bool MissileTargetInLOS()
    {
        return MissileTargetInLOS(_selectedWeapon, _attackTarget);
    }

    protected virtual bool MissileTargetInLOS(Weapon w, Unit u)
    {
        Vector3 attackerPos = GetTargetingPosition(w);
        Vector3 targetPos = GetTargetingEnemyPosition(u, w);
        attackerPos.y += _missileTargetingBaseHeight;
        targetPos.y += _missileTargetingBaseHeight;
        Vector3 attackDirection = targetPos - attackerPos;
        RaycastHit hit;
        if (Physics.Raycast(attackerPos, attackDirection, out hit, attackDirection.magnitude - 0.5f, _LOSObstacleLayerMask))
        {
            return false;
        }
        return true;
    }

    protected bool CanAttackMissile()
    {
        return TargetInRange() && MissileTargetInLOS();
    }

    protected bool CanAttack()
    {
        if (IsMissileUnit)
        {
            return CanAttackMissile();
        }
        else
        {
            return TargetInRange();
        }
    }

    protected virtual void ResetStatus()
    {
        _attackTarget = null;
        _moving = _rotating = _turningInFormation = _attacking = _movingToAttack = _turningToTarget = _facingTarget = false;
    }

    public bool Dying
    {
        get
        {
            return _dying;
        }
    }

    public override void SelectThis()
    {
        Formation f = transform.GetComponentInParent<Formation>();
        if (f != null)
        {
            f.SelectThis();
        }
    }
    public override WorldObject GetSelectionObject()
    {
        if (ContainingFormation != null)
        {
            return ContainingFormation;
        }
        return null;
    }

    public override void DisplaySelectionMarker()
    {
        if (_selectionMarkerRenderer != null)
        {
            if (ContainingFormation != null)
            {
                _selectionMarkerRenderer.enabled = ContainingFormation.IsSelected;
            }
        }
    }

    public Unit AcquireTarget(Weapon weapon)
    {
        Collider[] unitsInRange = Physics.OverlapSphere(GetTargetingPosition(weapon), weapon.MaxRange, _unitsLayerMask);
        if (unitsInRange.Length > 0)
        {
            foreach (Collider c in unitsInRange)
            {
                Unit u = c.GetComponent<Unit>();
                if (_owner != null && u != null && !u.Dying &&
                    _owner.IsHostile(u.Owner) &&
                    weapon.InRange(GetTargetingPosition(weapon), GetTargetingEnemyPosition(u, weapon)))
                {
                    if (IsMissileUnit && !MissileTargetInLOS(weapon, u))
                    {
                        continue;
                    }
                    return u;
                }
            }
        }
        _inCombat = false;
        if (_selectedWeapon != _weapons[0])
        {
            SwitchWeapon(0);
        }
        return null;
    }

    public Tuple<Unit, Weapon> AcquireTargetWithAnyWeapon(IEnumerable<Weapon> weapons)
    {
        float maxRange = 0.0f;
        foreach (Weapon w in weapons)
        {
            if (w.MaxRange > maxRange)
            {
                maxRange = w.MaxRange;
            }
        }
        Collider[] unitsInRange = Physics.OverlapSphere(transform.position, maxRange, _unitsLayerMask);
        if (unitsInRange.Length > 0)
        {
            foreach (Weapon w in weapons)
            {
                foreach (Collider c in unitsInRange)
                {
                    Unit u = c.GetComponent<Unit>();
                    if (_owner != null && u != null && !u.Dying &&
                        _owner.IsHostile(u.Owner))
                    {
                        if (w.InRange(GetTargetingPosition(w), GetTargetingEnemyPosition(u, w)))
                        {
                            if (IsMissileUnit && !MissileTargetInLOS(w, u))
                            {
                                continue;
                            }
                            return new Tuple<Unit, Weapon>(u, w);
                        }
                    }
                }
            }
        }
        _inCombat = false;
        if (_selectedWeapon != _weapons[0])
        {
            SwitchWeapon(0);
        }
        return null;
    }

    public Tuple<Unit, Weapon> AcquireTargetWithDefaultWeapon()
    {
        if (_weapons == null || _weapons.Length == 0 || _weapons[0] == null)
        {
            return null;
        }
        Unit target = AcquireTarget(_weapons[0]);
        if (target != null)
        {
            return new Tuple<Unit, Weapon>(target, _weapons[0]);
        }
        else
        {
            return null;
        }
    }

    public Tuple<Unit, Weapon> AcquireTargetWithSelectedWeapon()
    {
        if (_selectedWeapon == null)
        {
            return null;
        }
        Unit target = AcquireTarget(_selectedWeapon);
        if (target != null)
        {
            return new Tuple<Unit, Weapon>(target, _selectedWeapon);
        }
        else
        {
            return null;
        }
    }

    private Vector3 GetTargetingPosition(Weapon w)
    {
        if (w.AttackWeaponType == WeaponType.Musket)
        {
            if (ContainingFormation != null)
            {
                return ContainingFormation.GetRangedTargetingPosition();
            }
        }
        return transform.position;
    }
    private Vector3 GetTargetingEnemyPosition(Unit targetUnit, Weapon w)
    {
        if (w.AttackWeaponType == WeaponType.Musket)
        {
            if (targetUnit.ContainingFormation != null)
            {
                return targetUnit.ContainingFormation.GetRangedTargetingPosition();
            }
        }
        if (targetUnit == null)
        {
            Debug.LogWarning("Blah!");
        }
        return targetUnit.transform.position;
    }

    private IEnumerator MeleeAcquireTarget()
    {
        while (true)
        {
            if (_weapons != null)
            {
                if (_dying)
                {
                    yield break;
                }
                if (!IsAttacking)
                {
                    Tuple<Unit, Weapon> newTarget = AcquireTargetWithAnyWeapon(WeaponsReversed);
                    if (newTarget != null)
                    {
                        if (_attackMove)
                        {
                            _navAgent.isStopped = true;
                        }
                        AttackOrder(newTarget.Item1);
                    }
                    else
                    {
                        if (_attackMove)
                        {
                            _navAgent.isStopped = false;
                        }
                    }
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    protected virtual bool JustStopped()
    {
        return _prevMove && !_moving;
    }

    protected virtual void SwitchWeapon(int idx)
    {
        _selectedWeapon = _weapons[idx];
    }

    public virtual void Attacked(Unit attacker, Weapon w)
    {
        if (attacker.Owner == _owner)
        {
            // Friendly fire. Ignore...
            return;
        }

        if (w.IsMelee)
        {
            Weapon prevWeapon = SelectedWeapon;
            for (int i = 0; i < _weapons.Length; ++i)
            {
                if (_weapons[i].IsMelee && !TargetInMinimumRangeWithWeapon(_weapons[i], attacker))
                {
                    SwitchWeapon(i);
                    break;
                }
            }
            if ((!prevWeapon.IsMelee) || _attackTarget == null)
            {
                AttackOrderWithSelectedWeapon(attacker);
            }
        }
        _inCombat = true;
    }

    private void DrawControlCircle()
    {
        float x;
        float y = 0.0f;
        float z;

        float angle = 0.0f;

        _selectionMarkerRenderer.positionCount = _selectionMarkerSegments + 1;
        for (int i = 0; i < (_selectionMarkerSegments + 1); i++)
        {
            x = Mathf.Sin(angle) * SelectionMarkerRadius;
            z = Mathf.Cos(angle) * SelectionMarkerRadius;

            _selectionMarkerRenderer.SetPosition(i, new Vector3(x, y, z));

            angle += (Mathf.PI * 2.0f / _selectionMarkerSegments);
        }
    }

    public Weapon SelectedWeapon { get { return _selectedWeapon; } }
    public bool IsAttacking { get { return _attacking && _attackTarget != null && !_attackTarget.Dying; } }


    protected Weapon[] WeaponsReversed { get { return _weapons.Reverse().ToArray(); } }

    protected bool _moving, _prevMove, _rotating, _run, _dying, _turningInFormation, _attacking, _movingToAttack, _turningToTarget, _facingTarget, _inCombat, _inMelee, _attackMove;
    protected Vector3 _destLocation;
    public float RotateSpeed, MoveSpeed, RunSpeed;
    protected Weapon[] _weapons;
    protected Weapon _selectedWeapon;
    public Defense Defenses;
    protected Unit _attackTarget;
    protected NavMeshAgent _navAgent;
    private float _attackTime;
    protected Quaternion? _requiredFacing;
    public Formation ContainingFormation;
    public bool IsMissileUnit;
    public float SelectionMarkerRadius;
    protected static readonly int _selectionMarkerSegments = 32;
    protected int _missileTargetingLayerMask;
    protected float _missileTargetingBaseHeight = 1.2f;
}
