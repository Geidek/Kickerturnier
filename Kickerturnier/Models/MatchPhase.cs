namespace Kickerturnier.Models;

/// <summary>
/// Represents the phase of a match in the tournament
/// </summary>
public enum MatchPhase
{
    GroupStage,    // Vorrunde (Round-Robin)
    Final,         // Finale (1st vs 2nd)
    ThirdPlace     // Spiel um Platz 3 (3rd vs 4th)
}
