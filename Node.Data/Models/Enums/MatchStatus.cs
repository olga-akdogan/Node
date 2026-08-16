namespace Node.Data.Models.Enums;

/// <summary>
/// The state of a match between two users.
/// </summary>
public enum MatchStatus
{
    Active,    // Active match: both users can chat
    Unmatched  // One of the two ended the match
}
