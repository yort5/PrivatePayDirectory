namespace PrivatePayDirectory.Core.Models;

/// <summary>Fixed taxonomy values used in dropdowns throughout the application.</summary>
public static class Taxonomy
{
    public static readonly IReadOnlyList<string> Specialties =
    [
        "Anxiety",
        "Depression",
        "Trauma / PTSD",
        "Grief & Loss",
        "Relationship Issues",
        "Family Conflict",
        "ADHD",
        "OCD",
        "Eating Disorders",
        "Substance Use",
        "Life Transitions",
        "Self-Esteem",
        "Stress Management",
        "LGBTQ+ Issues",
        "Anger Management",
        "Bipolar Disorder",
        "Personality Disorders",
        "Chronic Illness",
        "Women's Issues",
        "Men's Issues",
        "Child & Adolescent",
        "Couples / Marriage",
        "Parenting",
        "Career Counseling",
        "Sleep Issues",
    ];

    public static readonly IReadOnlyList<string> InsurancePlans =
    [
        "Self-Pay Only",
        "Aetna",
        "Anthem / Blue Cross Blue Shield",
        "Cigna",
        "Humana",
        "Kaiser Permanente",
        "Medicaid",
        "Medicare",
        "Oscar Health",
        "United Healthcare",
        "Optum",
        "Tricare",
    ];

    public static readonly IReadOnlyList<string> Languages =
    [
        "English",
        "Spanish",
        "French",
        "Mandarin",
        "Cantonese",
        "Korean",
        "Vietnamese",
        "Arabic",
        "Portuguese",
        "Tagalog",
        "Russian",
        "ASL",
    ];

    public static readonly IReadOnlyList<string> UnitedStates =
    [
        "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA",
        "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD",
        "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ",
        "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC",
        "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY",
        "DC",
    ];
}
