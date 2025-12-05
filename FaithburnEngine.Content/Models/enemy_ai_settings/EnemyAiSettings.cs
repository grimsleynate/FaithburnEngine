public sealed class EnemyAISettings
{
    //Used for controlling difficulty. Higher values = Quicker enemies.
    public float ChaseSpeedMultiplier { get; set; } = 1f;
    //Used to control difficulty. Higher values = Higher radius of detection.
    public float AggressionRadius { get; set; } = 120f;
    //How strongly the enemy avoids obstacles (0 = no avoidance, 1 = full avoidance)
    public float AvoidanceStrength { get; set; } = 0.4f;
    //Delay before the enemy reacts to player presence (in seconds)
    public float ReactionDelay { get; set; } = 0.15f;
}