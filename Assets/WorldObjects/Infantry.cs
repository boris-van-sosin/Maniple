using System.Collections;
using System.Collections.Generic;
using Maniple;
using UnityEngine;
using UnityEngine.AI;

public class Infantry : Unit
{
    public Animator animator;

    protected override void Awake()
    {
        base.Awake();
        _readyForDeathAnim = false;
        _muzzle = transform.Find("ROOT/Hips/Spine/Spine1/R Clavicle/R UpperArm/R Forearm/R Hand/R Weapon/Musket/Muzzle");
    }

    protected override void OnGUI()
    {
        base.OnGUI();
    }

    protected override void Start()
    {
        base.Start();
        switch (InfType)
        {
            case InfantryType.Pikeman:
                animator.SetInteger("WeaponState", 7);
                _weapons = new Weapon[] { Weapon.BasicPike(), Weapon.BasicSword() };
                Defenses = new Defense(0.45f, 0.35f, 0.20f, 0.50f);
                break;
            case InfantryType.Musketeer:
                animator.SetInteger("WeaponState", 6);
                _weapons = new Weapon[] { Weapon.BasicMusket(), Weapon.BasicSword() };
                Defenses = new Defense(0.30f, 0.20f, 0.05f, 0.05f);
                break;
            case InfantryType.Officer:
                animator.SetInteger("WeaponState", 1);
                _weapons = new Weapon[] { Weapon.BasicSword() };
                Defenses = new Defense(0.5f, 0.45f, 0.1f, 0.2f);
                break;
            default:
                animator.SetInteger("WeaponState", 0);
                break;
        }

        _selectedWeapon = _weapons[0];
        Transform muzzleFlashTransform = transform.Find("ROOT/Hips/Spine/Spine1/R Clavicle/R UpperArm/R Forearm/R Hand/R Weapon/Musket/MuzzleFlash");
        if (muzzleFlashTransform != null)
        {
            _muzzleFlash = muzzleFlashTransform.GetComponent<MuzzleFlashController>();
        }

        Transform musketTransform = transform.Find("ROOT/Hips/Spine/Spine1/R Clavicle/R UpperArm/R Forearm/R Hand/R Weapon/Musket");
        Transform swordTransform = transform.Find("ROOT/Hips/Spine/Spine1/R Clavicle/R UpperArm/R Forearm/R Hand/R Weapon/Sword");
        if (musketTransform != null && swordTransform != null)
        {
            _musketMR = musketTransform.GetComponent<MeshRenderer>();
            _swordMR = swordTransform.GetComponent<MeshRenderer>();
        }

        animator.SetBool("Idling", true);
        _prevMove = false;
    }

    protected override void Update()
    {
        base.Update();
        if (_readyForDeathAnim)
        {
            CompleteDeath();
        }
        if (_dying)
        {
            return;
        }
        if (_rotating || _moving || _turningToTarget)
        {
            animator.SetBool("NonCombat", true);
            animator.SetBool("Idling", false);
            
        }
        else
        {
            animator.SetBool("Idling", true);
            animator.SetBool("NonCombat", !_inCombat);
        }
    }

    public override void MouseClick(ClickHitObject hitObject, Player controller)
    {
        base.MouseClick(hitObject, controller);
        //animator.SetTrigger("Use");
    }

    public override void IssueOrder(ClickHitObject target, ClickHitObject rClickStart, Player controller)
    {
        base.IssueOrder(target, controller);
    }

    public IEnumerator DelayedDeath(float delay)
    {
        Debug.Log("In DeathAnim");
        yield return new WaitForSeconds(delay);
        _readyForDeathAnim = true;
        yield return null;
    }

    public override void Kill(float delay)
    {
        base.Kill(delay);
        Debug.Log(string.Format("Unit {0} killed!", this));
        StartCoroutine(DelayedDeath(delay));
    }

    private void CompleteDeath()
    {
        int deathAnim = Random.Range(1, 4);
        Debug.Log(string.Format("Doing death anim {0}.", deathAnim));
        animator.SetInteger("Death", deathAnim);
        Destroy(_navAgent);
        _navAgent = null;
        Destroy(this);
    }

    protected override void UseWeaponAnim()
    {
        base.UseWeaponAnim();
        animator.SetTrigger("Use");
        if (_selectedWeapon.AttackWeaponType == WeaponType.Musket)
        {
            if (_muzzle != null)
            {
                GameObject projectileBase = ResourceManager.Production.GetOtherObject("MusketRound");
                Quaternion rotation = Quaternion.LookRotation(_attackTarget.transform.position - _muzzle.position);
                GameObject projectile = Instantiate<GameObject>(projectileBase, _muzzle.position, rotation);
                MusketRoundBehavior musketRound = projectile.GetComponent<MusketRoundBehavior>();
                musketRound.SetColor(Color.black);
                musketRound.SetTarget(_attackTarget.transform.position + Vector3.up * _missileTargetingBaseHeight);
            }
            if (_muzzleFlash != null)
            {
                _muzzleFlash.Play();
            }
        }
    }

    protected override void SwitchWeapon(int idx)
    {
        Weapon prevWeapon = _selectedWeapon;
        base.SwitchWeapon(idx);
        if (prevWeapon == _selectedWeapon)
        {
            return;
        }
        switch (InfType)
        {
            case InfantryType.Pikeman:
                break;
            case InfantryType.Musketeer:
                if (_selectedWeapon.AttackWeaponType == WeaponType.Musket)
                {
                    animator.SetInteger("WeaponState", 6);
                    if (_swordMR != null && _musketMR)
                    {
                        _musketMR.enabled = true;
                        _swordMR.enabled = false;
                    }
                }
                else if (_selectedWeapon.AttackWeaponType == WeaponType.Sword)
                {
                    animator.SetInteger("WeaponState", 1);
                    if (_swordMR != null && _musketMR)
                    {
                        _musketMR.enabled = false;
                        _swordMR.enabled = true;
                    }
                }
                break;
            default:
                break;
        }
    }

    public enum InfantryType { Pikeman, Musketeer, Officer };

    public InfantryType InfType;
    private bool _readyForDeathAnim;
    private Transform _muzzle;
    private MuzzleFlashController _muzzleFlash;
    private MeshRenderer _musketMR;
    private MeshRenderer _swordMR;
}
