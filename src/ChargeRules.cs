namespace TwentyMinutes.Modernization;
public enum Evolution { None, OverloadShell, ArmorPierce, DualBarrage }
public readonly struct ShotPlan
{
    public readonly int Tier, ProjectileFactor;
    public readonly float DamageMultiplier, SizeMultiplier;
    public readonly bool PierceAll;
    public ShotPlan(int tier,float damage,float size,int projectiles,bool pierce)
    { Tier=tier;DamageMultiplier=damage;SizeMultiplier=size;ProjectileFactor=projectiles;PierceAll=pierce; }
}
public static class ChargeRules
{
    public static ShotPlan Plan(float seconds,Evolution evolution)
    {
        int tier=seconds>=1.5f?2:seconds>=.8f?1:0;
        float damage=tier==2?4f:tier==1?2.2f:1f;
        if(evolution==Evolution.ArmorPierce && tier==2) damage*=1.15f;
        // Dual Barrage's count/spread and -20% stat change belong to GunData/GunEvolution,
        // as in Android. Applying them here would multiply existing projectile bonuses twice.
        return new ShotPlan(tier,damage,evolution==Evolution.OverloadShell && tier==2?3f:1f,1,tier==2 || evolution==Evolution.ArmorPierce);
    }
}
