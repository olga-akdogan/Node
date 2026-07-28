namespace Node.Data.Models.Enums;

/// <summary>
/// De toestand van een match tussen twee gebruikers.
/// </summary>
public enum MatchStatus
{
    Active,    // Actieve match: beide gebruikers kunnen chatten
    Unmatched  // Eén van beide gebruikers heeft de match beëindigd
}
