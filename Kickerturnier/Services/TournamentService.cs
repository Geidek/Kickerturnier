using Kickerturnier.Models;
using System.Text.Json;

namespace Kickerturnier.Services;

/// <summary>
/// Service for managing the tournament state and operations
/// Handles team management, match generation, standings calculation, and persistence
/// </summary>
public class TournamentService
{
    private const string StorageKey = "kickerturnier_state";
    
    public List<Team> Teams { get; private set; } = new();
    public List<Match> Matches { get; private set; } = new();
    
    // Event to notify UI of changes
    public event Action? OnChange;

    /// <summary>
    /// Add a new team to the tournament
    /// </summary>
    public void AddTeam(Team team)
    {
        Teams.Add(team);
        NotifyStateChanged();
    }

    /// <summary>
    /// Update an existing team
    /// </summary>
    public void UpdateTeam(Team team)
    {
        var existingTeam = Teams.FirstOrDefault(t => t.Id == team.Id);
        if (existingTeam != null)
        {
            existingTeam.Name = team.Name;
            existingTeam.Player1Name = team.Player1Name;
            existingTeam.Player2Name = team.Player2Name;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Remove a team from the tournament (only if tournament hasn't started)
    /// </summary>
    public void RemoveTeam(Guid teamId)
    {
        if (Matches.Any())
        {
            throw new InvalidOperationException("Cannot remove teams after tournament has started");
        }
        Teams.RemoveAll(t => t.Id == teamId);
        NotifyStateChanged();
    }

    /// <summary>
    /// Generate all round-robin matches for the group stage
    /// Each team plays every other team exactly once
    /// </summary>
    public void GenerateGroupStageMatches()
    {
        if (Teams.Count < 2)
        {
            throw new InvalidOperationException("Need at least 2 teams to start a tournament");
        }

        // Clear existing matches
        Matches.Clear();

        int matchNumber = 1;
        
        // Round-robin algorithm: each team plays every other team once
        for (int i = 0; i < Teams.Count; i++)
        {
            for (int j = i + 1; j < Teams.Count; j++)
            {
                Matches.Add(new Match
                {
                    TeamA = Teams[i],
                    TeamB = Teams[j],
                    Phase = MatchPhase.GroupStage,
                    MatchNumber = matchNumber++
                });
            }
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Update match result
    /// </summary>
    public void UpdateMatchResult(Guid matchId, int goalsTeamA, int goalsTeamB)
    {
        var match = Matches.FirstOrDefault(m => m.Id == matchId);
        if (match != null)
        {
            if (goalsTeamA < 0 || goalsTeamB < 0)
            {
                throw new ArgumentException("Goals cannot be negative");
            }
            
            match.GoalsTeamA = goalsTeamA;
            match.GoalsTeamB = goalsTeamB;
            NotifyStateChanged();
            
            // Check if group stage is complete and generate final matches
            if (IsGroupStageComplete() && !HasFinalMatches())
            {
                GenerateFinalMatches();
            }
        }
    }

    /// <summary>
    /// Calculate current standings based on match results
    /// Sorting: Points > Goal Difference > Goals For > Head-to-Head
    /// </summary>
    public List<Standing> GetStandings()
    {
        var standings = new List<Standing>();

        foreach (var team in Teams)
        {
            var standing = new Standing
            {
                Team = team
            };

            var teamMatches = Matches
                .Where(m => m.Phase == MatchPhase.GroupStage && 
                           m.IsFinished && 
                           (m.TeamA.Id == team.Id || m.TeamB.Id == team.Id))
                .ToList();

            standing.MatchesPlayed = teamMatches.Count;

            foreach (var match in teamMatches)
            {
                bool isTeamA = match.TeamA.Id == team.Id;
                int goalsFor = isTeamA ? match.GoalsTeamA!.Value : match.GoalsTeamB!.Value;
                int goalsAgainst = isTeamA ? match.GoalsTeamB!.Value : match.GoalsTeamA!.Value;

                standing.GoalsFor += goalsFor;
                standing.GoalsAgainst += goalsAgainst;

                if (goalsFor > goalsAgainst)
                {
                    standing.Wins++;
                    standing.Points += 3;
                }
                else if (goalsFor == goalsAgainst)
                {
                    standing.Draws++;
                    standing.Points += 1;
                }
                else
                {
                    standing.Losses++;
                }
            }

            standings.Add(standing);
        }

        // Sort standings: Points desc, Goal Difference desc, Goals For desc
        standings = standings
            .OrderByDescending(s => s.Points)
            .ThenByDescending(s => s.GoalDifference)
            .ThenByDescending(s => s.GoalsFor)
            .ToList();

        // Apply head-to-head tiebreaker for teams with same points, goal difference, and goals for
        standings = ApplyHeadToHeadTiebreaker(standings);

        // Assign positions
        for (int i = 0; i < standings.Count; i++)
        {
            standings[i].Position = i + 1;
        }

        return standings;
    }

    /// <summary>
    /// Apply head-to-head tiebreaker for teams with identical stats
    /// </summary>
    private List<Standing> ApplyHeadToHeadTiebreaker(List<Standing> standings)
    {
        var result = new List<Standing>();
        var i = 0;

        while (i < standings.Count)
        {
            // Find all teams with same points, goal difference, and goals for
            var tiedTeams = standings
                .Skip(i)
                .TakeWhile(s => s.Points == standings[i].Points && 
                               s.GoalDifference == standings[i].GoalDifference && 
                               s.GoalsFor == standings[i].GoalsFor)
                .ToList();

            if (tiedTeams.Count > 1)
            {
                // Apply head-to-head
                tiedTeams = ApplyHeadToHead(tiedTeams);
            }

            result.AddRange(tiedTeams);
            i += tiedTeams.Count;
        }

        return result;
    }

    /// <summary>
    /// Apply head-to-head comparison between tied teams
    /// </summary>
    private List<Standing> ApplyHeadToHead(List<Standing> tiedTeams)
    {
        var headToHeadPoints = new Dictionary<Guid, int>();
        var headToHeadGoalDiff = new Dictionary<Guid, int>();

        foreach (var team in tiedTeams)
        {
            headToHeadPoints[team.Team.Id] = 0;
            headToHeadGoalDiff[team.Team.Id] = 0;
        }

        var tiedTeamIds = tiedTeams.Select(t => t.Team.Id).ToHashSet();

        // Calculate head-to-head results only among tied teams
        var headToHeadMatches = Matches
            .Where(m => m.Phase == MatchPhase.GroupStage && 
                       m.IsFinished &&
                       tiedTeamIds.Contains(m.TeamA.Id) && 
                       tiedTeamIds.Contains(m.TeamB.Id))
            .ToList();

        foreach (var match in headToHeadMatches)
        {
            int goalsA = match.GoalsTeamA!.Value;
            int goalsB = match.GoalsTeamB!.Value;

            headToHeadGoalDiff[match.TeamA.Id] += goalsA - goalsB;
            headToHeadGoalDiff[match.TeamB.Id] += goalsB - goalsA;

            if (goalsA > goalsB)
            {
                headToHeadPoints[match.TeamA.Id] += 3;
            }
            else if (goalsB > goalsA)
            {
                headToHeadPoints[match.TeamB.Id] += 3;
            }
            else
            {
                headToHeadPoints[match.TeamA.Id] += 1;
                headToHeadPoints[match.TeamB.Id] += 1;
            }
        }

        return tiedTeams
            .OrderByDescending(t => headToHeadPoints[t.Team.Id])
            .ThenByDescending(t => headToHeadGoalDiff[t.Team.Id])
            .ToList();
    }

    /// <summary>
    /// Check if all group stage matches are complete
    /// </summary>
    public bool IsGroupStageComplete()
    {
        var groupMatches = Matches.Where(m => m.Phase == MatchPhase.GroupStage).ToList();
        return groupMatches.Any() && groupMatches.All(m => m.IsFinished);
    }

    /// <summary>
    /// Check if final matches exist
    /// </summary>
    private bool HasFinalMatches()
    {
        return Matches.Any(m => m.Phase == MatchPhase.Final || m.Phase == MatchPhase.ThirdPlace);
    }

    /// <summary>
    /// Generate final matches based on group stage standings
    /// Final: 1st vs 2nd
    /// Third Place: 3rd vs 4th
    /// 5th place team (if exists) stays in 5th
    /// </summary>
    public void GenerateFinalMatches()
    {
        if (!IsGroupStageComplete())
        {
            throw new InvalidOperationException("Cannot generate final matches until group stage is complete");
        }

        var standings = GetStandings();

        if (standings.Count < 2)
        {
            return; // Not enough teams for finals
        }

        // Remove existing final matches if regenerating
        Matches.RemoveAll(m => m.Phase == MatchPhase.Final || m.Phase == MatchPhase.ThirdPlace);

        // Final: 1st vs 2nd
        Matches.Add(new Match
        {
            TeamA = standings[0].Team,
            TeamB = standings[1].Team,
            Phase = MatchPhase.Final,
            MatchNumber = 1
        });

        // Third place match: 3rd vs 4th (if enough teams)
        if (standings.Count >= 4)
        {
            Matches.Add(new Match
            {
                TeamA = standings[2].Team,
                TeamB = standings[3].Team,
                Phase = MatchPhase.ThirdPlace,
                MatchNumber = 2
            });
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Get final match
    /// </summary>
    public Match? GetFinalMatch()
    {
        return Matches.FirstOrDefault(m => m.Phase == MatchPhase.Final);
    }

    /// <summary>
    /// Get third place match
    /// </summary>
    public Match? GetThirdPlaceMatch()
    {
        return Matches.FirstOrDefault(m => m.Phase == MatchPhase.ThirdPlace);
    }

    /// <summary>
    /// Get tournament champion (winner of final)
    /// </summary>
    public Team? GetChampion()
    {
        var final = GetFinalMatch();
        if (final?.IsFinished == true)
        {
            return final.GoalsTeamA > final.GoalsTeamB ? final.TeamA : final.TeamB;
        }
        return null;
    }

    /// <summary>
    /// Get tournament runner-up (loser of final)
    /// </summary>
    public Team? GetRunnerUp()
    {
        var final = GetFinalMatch();
        if (final?.IsFinished == true)
        {
            return final.GoalsTeamA < final.GoalsTeamB ? final.TeamA : final.TeamB;
        }
        return null;
    }

    /// <summary>
    /// Get third place team
    /// </summary>
    public Team? GetThirdPlace()
    {
        var thirdPlaceMatch = GetThirdPlaceMatch();
        if (thirdPlaceMatch?.IsFinished == true)
        {
            return thirdPlaceMatch.GoalsTeamA > thirdPlaceMatch.GoalsTeamB 
                ? thirdPlaceMatch.TeamA 
                : thirdPlaceMatch.TeamB;
        }
        return null;
    }

    /// <summary>
    /// Reset tournament (clear all matches, keep teams)
    /// </summary>
    public void ResetTournament()
    {
        Matches.Clear();
        NotifyStateChanged();
    }

    /// <summary>
    /// Clear all data (teams and matches)
    /// </summary>
    public void ClearAll()
    {
        Teams.Clear();
        Matches.Clear();
        NotifyStateChanged();
    }

    /// <summary>
    /// Save tournament state to local storage
    /// </summary>
    public string SerializeState()
    {
        var state = new
        {
            Teams = Teams,
            Matches = Matches.Select(m => new
            {
                m.Id,
                TeamAId = m.TeamA.Id,
                TeamBId = m.TeamB.Id,
                m.GoalsTeamA,
                m.GoalsTeamB,
                m.Phase,
                m.MatchNumber
            }).ToList()
        };

        return JsonSerializer.Serialize(state);
    }

    /// <summary>
    /// Load tournament state from serialized data
    /// </summary>
    public void DeserializeState(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Load teams
            Teams.Clear();
            if (root.TryGetProperty("Teams", out var teamsElement))
            {
                foreach (var teamElement in teamsElement.EnumerateArray())
                {
                    var team = new Team
                    {
                        Id = teamElement.GetProperty("Id").GetGuid(),
                        Name = teamElement.GetProperty("Name").GetString() ?? "",
                        Player1Name = teamElement.TryGetProperty("Player1Name", out var p1) ? p1.GetString() ?? "" : "",
                        Player2Name = teamElement.TryGetProperty("Player2Name", out var p2) ? p2.GetString() ?? "" : ""
                    };
                    Teams.Add(team);
                }
            }

            // Load matches
            Matches.Clear();
            if (root.TryGetProperty("Matches", out var matchesElement))
            {
                foreach (var matchElement in matchesElement.EnumerateArray())
                {
                    var teamAId = matchElement.GetProperty("TeamAId").GetGuid();
                    var teamBId = matchElement.GetProperty("TeamBId").GetGuid();
                    
                    var teamA = Teams.FirstOrDefault(t => t.Id == teamAId);
                    var teamB = Teams.FirstOrDefault(t => t.Id == teamBId);

                    if (teamA != null && teamB != null)
                    {
                        var match = new Match
                        {
                            Id = matchElement.GetProperty("Id").GetGuid(),
                            TeamA = teamA,
                            TeamB = teamB,
                            Phase = (MatchPhase)matchElement.GetProperty("Phase").GetInt32(),
                            MatchNumber = matchElement.GetProperty("MatchNumber").GetInt32()
                        };

                        if (matchElement.TryGetProperty("GoalsTeamA", out var goalsA) && goalsA.ValueKind != JsonValueKind.Null)
                        {
                            match.GoalsTeamA = goalsA.GetInt32();
                        }
                        if (matchElement.TryGetProperty("GoalsTeamB", out var goalsB) && goalsB.ValueKind != JsonValueKind.Null)
                        {
                            match.GoalsTeamB = goalsB.GetInt32();
                        }

                        Matches.Add(match);
                    }
                }
            }

            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deserializing state: {ex.Message}");
        }
    }

    /// <summary>
    /// Initialize with example/demo teams
    /// </summary>
    public void InitializeWithExampleTeams()
    {
        if (Teams.Any())
        {
            return; // Don't overwrite existing teams
        }

        Teams.Add(new Team { Name = "FC Tornado", Player1Name = "Max Mustermann", Player2Name = "Anna Schmidt" });
        Teams.Add(new Team { Name = "Die Kicker", Player1Name = "Tom Müller", Player2Name = "Lisa Weber" });
        Teams.Add(new Team { Name = "Tischmeister", Player1Name = "Jan Becker", Player2Name = "Sarah Klein" });
        Teams.Add(new Team { Name = "Ballmagier", Player1Name = "Lukas Wagner", Player2Name = "Emma Hoffmann" });
        Teams.Add(new Team { Name = "Torjäger", Player1Name = "Felix Schulz", Player2Name = "Nina Fischer" });

        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
