public sealed class EnemyAISettings
{
    public float ChaseSpeedMultiplier { get; set; } = 1f;
    public float AggressionRadius { get; set; } = 120f;
    public float AvoidanceStrength { get; set; } = 0.4f;
    public float ReactionDelay { get; set; } = 0.15f;
}