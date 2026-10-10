namespace PrivatePayDirectory.Core.Enums;

/// <summary>
/// Stored in the database by number, so only ever append new values — inserting or reordering
/// would silently change existing providers' professions. Display order lives in Taxonomy.Professions.
/// </summary>
public enum Profession
{
    Therapist,
    Chiropractor,
    MassageTherapist,
    Hairstylist,
    LifeCoach,
}
