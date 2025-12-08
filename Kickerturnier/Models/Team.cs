namespace Kickerturnier.Models;

/// <summary>
/// Represents a team in the tournament (2 players)
/// </summary>
public class Team
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Player1Name { get; set; } = string.Empty;
    public string Player2Name { get; set; } = string.Empty;
}
