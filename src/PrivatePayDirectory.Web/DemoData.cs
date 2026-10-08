using PrivatePayDirectory.Core.Enums;
using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Web;

/// <summary>
/// Fictional example providers (15–20 per profession) so alpha testers see a populated site.
/// Controlled by the DemoData:Enabled setting, applied at startup:
///   true  → every example profile is (re)written, so edits here show up on the next deploy;
///   false → all example profiles are deleted. Real profiles are never touched.
/// Generation is deterministic (fixed IDs and random seed), so restarts don't create duplicates.
/// </summary>
public static class DemoData
{
    /// <summary>UserId marking a profile as demo data — no real account owns it.</summary>
    public const string DemoUserId = "demo";

    private static readonly Dictionary<Profession, int> CountPerProfession = new()
    {
        [Profession.Therapist] = 20,
        [Profession.Chiropractor] = 16,
        [Profession.MassageTherapist] = 18,
        [Profession.Hairstylist] = 15,
    };

    public static async Task SyncAsync(IProviderRepository repo, bool enabled, ILogger logger)
    {
        var existing = (await repo.GetAllAsync()).Where(p => p.UserId == DemoUserId).ToList();
        var wanted = enabled ? Generate() : [];
        var wantedIds = wanted.Select(p => p.ProviderId).ToHashSet();

        foreach (var stale in existing.Where(p => !wantedIds.Contains(p.ProviderId)))
            await repo.DeleteAsync(stale.ProviderId);
        foreach (var provider in wanted)
            await repo.SaveAsync(provider);

        logger.LogInformation(
            "Demo data {State}: {Written} example profiles written, {Removed} removed.",
            enabled ? "enabled" : "disabled", wanted.Count, existing.Count(p => !wantedIds.Contains(p.ProviderId)));
    }

    /// <summary>Photos of the owners' corgis in wwwroot/images/demo (pet-01.jpg … pet-51.jpg).</summary>
    private const int PhotoCount = 51;

    public static List<Provider> Generate()
    {
        var rng = new Random(20261003);
        var providers = new List<Provider>();
        var usedNames = new HashSet<string>();

        foreach (var info in Taxonomy.Professions)
        {
            for (var i = 1; i <= CountPerProfession[info.Profession]; i++)
            {
                string first, last;
                do
                {
                    first = Pick(rng, FirstNames);
                    last = Pick(rng, LastNames);
                } while (!usedNames.Add(first + " " + last));

                var id = $"demo-{info.Slug}-{i:D2}";
                var specialties = PickSome(rng, Taxonomy.SpecialtiesByProfession[info.Profession], 2, 4);
                var city = Pick(rng, Cities);
                var (virtualStates, offices) = Locations(rng, info.Profession, city, id);

                providers.Add(new Provider
                {
                    ProviderId = id,
                    // Cycle through the photos across all professions; with 20 or fewer per profession,
                    // no dog appears twice on the same directory page
                    ProfilePhotoKey = $"{StaticPhotos.Prefix}pet-{providers.Count % PhotoCount + 1:D2}.jpg",
                    UserId = DemoUserId,
                    IsVisible = true,
                    Profession = info.Profession,
                    FirstName = first,
                    LastName = $"{last} (Example)",
                    Title = Pick(rng, Titles[info.Profession]),
                    Bio = Bio(rng, info.Profession, specialties, city.City),
                    Specialties = specialties,
                    InsuranceAccepted = rng.NextDouble() < 0.35 ? ["Private Pay", "Sliding Scale"] : ["Private Pay"],
                    Languages = rng.NextDouble() < 0.3 ? ["English", Pick(rng, Taxonomy.Languages.Skip(1).ToList())] : ["English"],
                    LicensedVirtualStates = virtualStates,
                    Offices = offices,
                    AcceptingNewClients = rng.NextDouble() < 0.8,
                    Phone = $"{city.AreaCode}-555-01{rng.Next(0, 100):D2}",
                    Email = $"{first}.{last}@example.com".ToLowerInvariant().Replace("'", ""),
                    WebsiteUrl = rng.NextDouble() < 0.5 ? $"https://www.example.com/{first}-{last}".ToLowerInvariant().Replace("'", "") : null,
                    CreatedAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(rng.Next(0, 24 * 30)),
                });
            }
        }

        return providers;
    }

    private static (List<string> VirtualStates, List<OfficeLocation> Offices) Locations(
        Random rng, Profession profession, Place city, string providerId)
    {
        var office = new OfficeLocation
        {
            OfficeId = $"{providerId}-office",
            Label = $"{city.City} {OfficeNoun[profession]}",
            Street = $"{rng.Next(100, 9900)} {Pick(rng, Streets)}",
            City = city.City,
            State = city.State,
            Zip = city.Zip,
        };

        // Only therapists offer virtual sessions; a mix of virtual-only, in-person-only, and both
        if (profession != Profession.Therapist)
            return ([], [office]);

        var states = new List<string> { city.State };
        states.AddRange(PickSome(rng, Taxonomy.UnitedStates.Where(s => s != city.State).ToList(), 0, 2));

        return rng.Next(3) switch
        {
            0 => (states, []),
            1 => ([], [office]),
            _ => (states, [office]),
        };
    }

    private static string Bio(Random rng, Profession profession, List<string> specialties, string city)
    {
        var focus = string.Join(" and ", specialties.Take(2));
        var years = rng.Next(3, 26);
        return profession switch
        {
            Profession.Therapist =>
                $"{years} years helping adults work through {focus}. My approach is collaborative and practical — " +
                "we'll set goals together and adjust as you go. This is an example profile for testing.",
            Profession.Chiropractor =>
                $"Chiropractor in {city} with {years} years of experience, focusing on {focus}. " +
                "Clear pricing, no insurance paperwork. This is an example profile for testing.",
            Profession.MassageTherapist =>
                $"Licensed massage therapist in {city} specializing in {focus}. {years} years in practice; " +
                "sessions tailored to what your body needs that day. This is an example profile for testing.",
            _ =>
                $"Independent stylist in {city} with {years} years behind the chair. Known for {focus}. " +
                "Book directly with me. This is an example profile for testing.",
        };
    }

    private static T Pick<T>(Random rng, IReadOnlyList<T> items) => items[rng.Next(items.Count)];

    private static List<T> PickSome<T>(Random rng, IReadOnlyList<T> items, int min, int max) =>
        items.OrderBy(_ => rng.Next()).Take(rng.Next(min, max + 1)).ToList();

    private sealed record Place(string City, string State, string Zip, string AreaCode);

    private static readonly List<Place> Cities =
    [
        new("Austin", "TX", "78701", "512"), new("Houston", "TX", "77002", "713"),
        new("Dallas", "TX", "75201", "214"), new("Denver", "CO", "80202", "303"),
        new("Seattle", "WA", "98101", "206"), new("Portland", "OR", "97204", "503"),
        new("Phoenix", "AZ", "85004", "602"), new("Chicago", "IL", "60601", "312"),
        new("Minneapolis", "MN", "55401", "612"), new("Atlanta", "GA", "30303", "404"),
        new("Nashville", "TN", "37203", "615"), new("Raleigh", "NC", "27601", "919"),
        new("Boston", "MA", "02108", "617"), new("New York", "NY", "10005", "212"),
        new("Philadelphia", "PA", "19107", "215"), new("San Diego", "CA", "92101", "619"),
        new("Los Angeles", "CA", "90010", "310"), new("San Francisco", "CA", "94105", "415"),
        new("Salt Lake City", "UT", "84111", "801"), new("Miami", "FL", "33131", "305"),
    ];

    private static readonly List<string> Streets =
    [
        "Main St", "Oak Ave", "Congress Ave", "Pine St", "Elm St", "Market St", "Broadway",
        "Maple Ave", "Cedar Ln", "Park Blvd", "Lake St", "Washington Ave", "2nd St", "Grand Ave",
    ];

    private static readonly Dictionary<Profession, string> OfficeNoun = new()
    {
        [Profession.Therapist] = "Office",
        [Profession.Chiropractor] = "Clinic",
        [Profession.MassageTherapist] = "Studio",
        [Profession.Hairstylist] = "Salon",
    };

    private static readonly Dictionary<Profession, List<string>> Titles = new()
    {
        [Profession.Therapist] = ["LCSW", "LPC", "LMFT", "LMHC", "PhD", "PsyD"],
        [Profession.Chiropractor] = ["DC"],
        [Profession.MassageTherapist] = ["LMT", "LMT", "CMT"],
        [Profession.Hairstylist] = ["", "Master Stylist", "Colorist", "Curl Specialist"],
    };

    private static readonly List<string> FirstNames =
    [
        "Alex", "Jordan", "Taylor", "Morgan", "Casey", "Riley", "Jamie", "Avery", "Sam", "Quinn",
        "Maria", "Priya", "Aisha", "Elena", "Grace", "Hannah", "Leah", "Nora", "Sofia", "Yuki",
        "David", "Marcus", "Andre", "Daniel", "Ethan", "Hiro", "Luis", "Omar", "Ryan", "Thomas",
        "Chloe", "Fatima", "Isabel", "Mei", "Olivia", "Rosa", "Zoe", "Ben", "Kofi", "Noah",
    ];

    private static readonly List<string> LastNames =
    [
        "Anderson", "Bennett", "Carter", "Diaz", "Edwards", "Foster", "Garcia", "Hughes", "Ibrahim",
        "Jensen", "Kim", "Lopez", "Mitchell", "Nguyen", "O'Brien", "Patel", "Quinn", "Reyes",
        "Singh", "Tanaka", "Underwood", "Vargas", "Walker", "Xu", "Young", "Zimmerman", "Brooks",
        "Chen", "Delgado", "Ellis", "Fischer", "Greene", "Hayes", "Moreno", "Okafor", "Russo",
    ];
}
