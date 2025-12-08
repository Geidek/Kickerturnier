namespace Kickerturnier.Models;

/// <summary>
/// Represents a match between two teams
/// </summary>
public class Match
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Team TeamA { get; set; } = null!;
    public Team TeamB { get; set; } = null!;
    public int? GoalsTeamA { get; set; }
    public int? GoalsTeamB { get; set; }
    public bool IsFinished => GoalsTeamA.HasValue && GoalsTeamB.HasValue;
    public MatchPhase Phase { get; set; } = MatchPhase.GroupStage;
    public int MatchNumber { get; set; } // For display order
}
