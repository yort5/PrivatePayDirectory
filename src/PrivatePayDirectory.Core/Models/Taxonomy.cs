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
            [Profession.LifeCoach] =
            [
                "Career Transitions",
                "Confidence & Mindset",
                "Life Purpose & Direction",
                "Relationships",
                "Productivity & Habits",
                "Leadership",
                "Health & Wellness Goals",
                "Work-Life Balance",
                "Parenting",
                "Life After Retirement",
            ],
        };

    /// <summary>
    /// How a provider makes private pay easier. Every provider here is private pay, so that isn't an option;
    /// the site deliberately doesn't mention insurance outside the About page FAQ.
    /// </summary>
    public static readonly IReadOnlyList<string> PaymentOptions =
    [
        "Sliding Scale",
        "Free Initial Consultation",
        "HSA/FSA Accepted",
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

    /// <summary>
    /// Every profession, in display order (Therapist leads — it's the site's primary audience).
    /// Each gets its own directory page at /{Slug}. Adding a profession: add the enum value,
    /// an entry here, and its specialties above.
    /// </summary>
    public static readonly IReadOnlyList<ProfessionInfo> Professions =
    [
        new(Profession.Therapist, "therapists", "Therapist", "Therapists",
            "Find a Private-Pay Therapist",
            "Independent therapists offering virtual and in-person care. You pay them directly, so your care stays between the two of you.",
            "psychology"),
        new(Profession.LifeCoach, "life-coaches", "Life Coach", "Life Coaches",
            "Find a Life Coach",
            "Independent life coaches who help you set goals, make changes, and follow through.",
            "self_improvement"),
        new(Profession.Chiropractor, "chiropractors", "Chiropractor", "Chiropractors",
            "Find a Private-Pay Chiropractor",
            "Independent chiropractors with clear, upfront pricing. Book and pay directly.",
            "accessibility_new"),
        new(Profession.MassageTherapist, "massage-therapists", "Massage Therapist", "Massage Therapists",
            "Find a Massage Therapist",
            "Independent massage therapists offering private-pay sessions near you.",
            "spa"),
        new(Profession.Hairstylist, "hairstylists", "Hairstylist", "Hairstylists",
            "Find a Hairstylist",
            "Independent stylists you book directly — no salon middleman.",
            "content_cut"),
    ];

    public static ProfessionInfo Info(Profession profession) =>
        Professions.First(p => p.Profession == profession);

    public static ProfessionInfo? FindBySlug(string? slug) =>
        Professions.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));

    public static readonly IReadOnlyDictionary<Profession, string> ProfessionDisplay =
        Professions.ToDictionary(p => p.Profession, p => p.DisplayName);
}
