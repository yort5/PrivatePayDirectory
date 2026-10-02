using PrivatePayDirectory.Core.Enums;

namespace PrivatePayDirectory.Core.Models;

/// <summary>Fixed taxonomy values used in dropdowns throughout the application.</summary>
public static class Taxonomy
{
    public static readonly IReadOnlyDictionary<Profession, IReadOnlyList<string>> SpecialtiesByProfession =
        new Dictionary<Profession, IReadOnlyList<string>>
        {
            [Profession.Therapist] =
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
            ],
            [Profession.Chiropractor] =
            [
                "Back Pain",
                "Neck Pain",
                "Sports Injuries",
                "Sciatica",
                "Prenatal Care",
                "Posture Correction",
                "Headaches / Migraines",
                "Auto Injury / Whiplash",
            ],
            [Profession.MassageTherapist] =
            [
                "Deep Tissue",
                "Swedish",
                "Sports Massage",
                "Prenatal Massage",
                "Trigger Point",
                "Hot Stone",
                "Lymphatic Drainage",
                "Myofascial Release",
            ],
            [Profession.Hairstylist] =
            [
                "Color",
                "Cuts",
                "Extensions",
                "Bridal",
                "Curly Hair",
                "Natural / Textured Hair",
                "Balayage",
                "Keratin Treatments",
            ],
        };

    /// <summary>All specialties across all professions, deduplicated and sorted, for use when no profession filter is active.</summary>
    public static IReadOnlyList<string> AllSpecialties { get; } =
        SpecialtiesByProfession.Values
            .SelectMany(s => s)
            .Distinct()
            .OrderBy(s => s)
            .ToList()
            .AsReadOnly();

    public static readonly IReadOnlyList<string> InsurancePlans =
    [
        "Private Pay",
        "Sliding Scale",
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

    public static readonly IReadOnlyDictionary<Profession, string> ProfessionDisplay =
        new Dictionary<Profession, string>
        {
            [Profession.Therapist] = "Therapist",
            [Profession.Chiropractor] = "Chiropractor",
            [Profession.MassageTherapist] = "Massage Therapist",
            [Profession.Hairstylist] = "Hairstylist",
        };
}
