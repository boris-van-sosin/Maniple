using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType { Pike, Sword, Musket };

public class Weapon
{
    public WeaponType AttackWeaponType { get; set; }
    public float Skill { get; set; }
    public float MinRange { get; set; }
    public float MaxRange { get; set; }
    public float AttackDelay { get; set; }
    public float AttacksPerMinute { get { return 60.0f / AttackDelay; } }

    public bool InRange(Vector3 attacker, Vector3 target)
    {
        Vector3 direction = target - attacker;
        float range = (AttackWeaponType == WeaponType.Musket) ? Combat.ComputeMissileRange(attacker, target) : direction.sqrMagnitude;
        if (range <= MaxRange * MaxRange &&
            range >= MinRange * MinRange)
        {
            return true;
        }
        return false;
    }

    public bool InMinimumRange(Vector3 attacker, Vector3 target)
    {
        Vector3 direction = target - attacker;
        float range = (AttackWeaponType == WeaponType.Musket) ? Combat.ComputeMissileRange(attacker, target) : direction.sqrMagnitude;
        return range < MinRange * MinRange;
    }

    public bool IsMelee { get { return AttackWeaponType != WeaponType.Musket; } }

    public static Weapon BasicMusket()
    {
        return new Weapon()
        {
            AttackWeaponType = WeaponType.Musket,
            Skill = 0.05f,
            MinRange = 4.0f,
            MaxRange = 30.0f,
            AttackDelay = 10.0f
        };
    }

    public static Weapon BasicPike()
    {
        return new Weapon()
        {
            AttackWeaponType = WeaponType.Pike,
            Skill = 0.3f,
            MinRange = .0f,//1.5f,
            MaxRange = 3.5f,
            AttackDelay = 1.5f
        };
    }

    public static Weapon BasicSword()
    {
        return new Weapon()
        {
            AttackWeaponType = WeaponType.Sword,
            Skill = 0.15f,
            MinRange = 0.0f,
            MaxRange = 1.5f,
            AttackDelay = 1.5f
        };
    }

    public static Weapon OfficerSword()
    {
        return new Weapon()
        {
            AttackWeaponType = WeaponType.Sword,
            Skill = 0.4f,
            MinRange = 0.0f,
            MaxRange = 1.5f,
            AttackDelay = 1.5f
        };
    }
}

public class Defense
{
    public Defense()
    {
    }

    public Defense(float pike, float sword, float musket, float cavCharge)
    {
        Defenses[WeaponType.Pike] = pike;
        Defenses[WeaponType.Sword] = sword;
        Defenses[WeaponType.Musket] = musket;
    }

    public readonly Dictionary<WeaponType, float> Defenses = new Dictionary<WeaponType, float>()
    {
        { WeaponType.Pike, 0 },
        { WeaponType.Sword, 0 },
        { WeaponType.Musket, 0 }
    };
}

public static class Combat
{
    public static bool Hit(Weapon attackingWeapon, Defense defense, float range)
    {
        float defenseValue;
        if (defense == null || !defense.Defenses.TryGetValue(attackingWeapon.AttackWeaponType, out defenseValue))
        {
            defenseValue = 0;
        }
        if (attackingWeapon.AttackWeaponType == WeaponType.Musket)
        {
            float t = (range - attackingWeapon.MinRange) / (attackingWeapon.MaxRange - attackingWeapon.MinRange);
            // Bezier curve for hit chance. Starts at 0.95 at point blank, ends at 0.05 at max range, and the middle
            // control point is determined by the weapon skill.
            float hitChance = MaxMissileHitChance * (1 - t) * (1 - t) + attackingWeapon.Skill * 2 * t * (1 - t) + MinMissileHitChance * t * t;
            float hitRoll = Random.value;
            if (hitRoll > hitChance)
            {
                Debug.Log(string.Format("Musket attack at distance {0}. Chance to hit: {1}. Roll: {2}. Miss.", range, hitChance, hitRoll));
                return false;
            }
            float resistRoll = Random.value;
            if (defenseValue > MusketResistClamp)
            {
                defenseValue = MusketResistClamp;
            }
            Debug.Log(string.Format("Musket attack at distance {0}. Chance to hit: {1}. Roll: {2}. Hit. {3}", range, hitChance, hitRoll, (resistRoll > defenseValue) ? "" : " Resisted."));
            return resistRoll > defenseValue;
        }
        else
        {
            float attackerDifference = attackingWeapon.Skill - defenseValue;
            // Generalized logistic 
            float hitChance = MinMeleeHitChance + (MaxMeleeHitChance - MinMeleeHitChance) / (1 + Mathf.Exp(-LogisticSteepness*attackerDifference));
            float hitRoll = Random.value;
            Debug.Log(string.Format("Meele attack at distance {0}. Attacker: {1}. Defender: {2}. Chance to hit: {3}. Roll: {4}.", range, attackingWeapon.Skill, defenseValue, hitChance, hitRoll));
            return hitRoll < hitChance;
        }
    }

    public static float ComputeMissileRange(Vector3 attackerPos, Vector3 targetPos, bool sqr)
    {
        Vector3 direction = targetPos - attackerPos;
        if (direction.y < 0)
        {
            // if firing down, ignore vertical distance
            direction.y = 0;
        }
        if (sqr)
        {
            return direction.sqrMagnitude;
        }
        else
        {
            return direction.magnitude;
        }
    }

    public static float ComputeMissileRange(Vector3 attackerPos, Vector3 targetPos)
    {
        return ComputeMissileRange(attackerPos, targetPos, true);
    }


    private static readonly float MinMeleeHitChance = 0.05f;
    private static readonly float MaxMeleeHitChance = 0.95f;
    private static readonly float MinMissileHitChance = 0.05f;
    private static readonly float MaxMissileHitChance = 0.95f;
    private static readonly float MusketResistClamp = 0.95f;
    private static readonly float LogisticSteepness = 10.0f;
}
