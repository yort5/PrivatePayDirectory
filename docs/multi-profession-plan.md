# Plan: Generalize PrivatePayDirectory to Multiple Professions

## Context
Currently the app models a single profession (therapists) as a Cosmos DB document
(`Therapist`), with a hardcoded specialty taxonomy and a flat `UserRole` enum. This
plan generalizes it to support chiropractors, massage therapists, hairstylists, etc.,
while keeping a single generic provider document (no per-profession entities/containers
for now — can split into "Details" sub-documents later if a profession needs
substantially different fields).

Note: "un-approve / hide" already exists (`ITherapistRepository.SetVisibilityAsync`,
wired up in the Admin Therapists page's "Hide" button). No new work needed for that —
it just needs to keep working after the rename below.

Do the phases in order. Each phase should build and the app should still run after it.

---

## Phase 1 — Add `Profession` concept

1. Create `src/PrivatePayDirectory.Core/Enums/Profession.cs`:
   ```csharp
   public enum Profession
   {
       Therapist,
       Chiropractor,
       MassageTherapist,
       Hairstylist
   }
   ```
   (New professions get added here later — that's the extension point.)

2. In `Therapist.cs` (soon to be `Provider.cs`, see Phase 2), add:
   ```csharp
   public Profession Profession { get; set; } = Profession.Therapist;
   ```

3. In `Taxonomy.cs`, replace the flat `Specialties` list with a per-profession lookup:
   ```csharp
   public static readonly IReadOnlyDictionary<Profession, IReadOnlyList<string>> SpecialtiesByProfession =
       new Dictionary<Profession, IReadOnlyList<string>>
       {
           [Profession.Therapist] = [ /* move the existing 24 items here */ ],
           [Profession.Chiropractor] = [ "Back Pain", "Neck Pain", "Sports Injuries", "Sciatica", "Prenatal Care", "Posture Correction", "Headaches / Migraines", "Auto Injury / Whiplash" ],
           [Profession.MassageTherapist] = [ "Deep Tissue", "Swedish", "Sports Massage", "Prenatal Massage", "Trigger Point", "Hot Stone", "Lymphatic Drainage", "Myofascial Release" ],
           [Profession.Hairstylist] = [ "Color", "Cuts", "Extensions", "Bridal", "Curly Hair", "Natural / Textured Hair", "Balayage", "Keratin Treatments" ],
       };
   ```
   Placeholder specialty lists above are a starting point — confirm real values with
   the user before shipping, don't just invent a final taxonomy silently.
   Keep `InsurancePlans`, `Languages`, `UnitedStates` as-is — they're already generic.

4. Add a `ProfessionDisplay` helper (or `[Display(Name=...)]` attributes) for
   human-readable labels ("Massage Therapist" vs `MassageTherapist`), used in dropdowns.

---

## Phase 2 — Rename `Therapist` → `Provider` throughout

This is a mechanical rename. Do it as one pass with find/replace, then fix compile errors.

- `Models/Therapist.cs` → `Models/Provider.cs`: class `Therapist` → `Provider`,
  property `TherapistId` → `ProviderId` (keep `[JsonPropertyName("id")]`).
- `Interfaces/ITherapistRepository.cs` → `IProviderRepository.cs`:
  `ITherapistRepository` → `IProviderRepository`, `TherapistFilter` → `ProviderFilter`
  (add `Profession? Profession` field to the filter).
- `Infrastructure/Repositories/CosmosTherapistRepository.cs` → `CosmosProviderRepository.cs`.
- `CosmosOptions.TherapistsContainer` → `ProvidersContainer`. **Do not rename the
  actual Cosmos container/database** unless you're also writing a data migration —
  simplest path is to keep the physical container name `"Therapists"` and just rename
  the C# config property that points to it, documented with a one-line comment.
- `Pages/Therapist/*` → `Pages/Provider/*` (Register, Edit, Profile). Update route
  references (`asp-page="/Provider/..."`) everywhere they're linked from
  (Directory, Admin pages, layout/nav).
- Update `Program.cs` DI registrations for the renamed interface/class.
- `UserRole.Therapist` → `UserRole.Provider` in `Enums/UserRole.cs`, and everywhere
  it's referenced (`Admin/Therapists.cshtml.cs`, `Admin/Users.cshtml.cs`,
  `Account/Register.cshtml.cs`, `Program.cs` policies).

Run a full solution build after this phase before moving on.

---

## Phase 3 — Sub-scoped admins (per-profession approval rights)

Goal: a "Chiropractor Admin" can only see/approve/hide Chiropractor profiles; a
"Therapist Admin" only Therapist profiles. A full Administrator (no scope) can see
everything.

1. In `AppUser.cs`, add:
   ```csharp
   public List<Profession>? ManagedProfessions { get; set; }
   ```
   `null` or empty list = unscoped (can manage all professions, if `Role == Administrator`).
   Non-empty = restricted to those professions only.

2. Keep `UserRole.Administrator` as the single admin role (don't create one enum
   value per profession — `ManagedProfessions` handles the scoping, and it scales
   to new professions without enum changes).

3. In `Admin/Providers.cshtml.cs` (renamed from `Admin/Therapists.cshtml.cs`):
   - Load the current admin's `AppUser` (via existing user lookup / claims — check
     how `Policies.RequireAdmin` currently resolves the user in `Program.cs`).
   - Filter `Pending`/`Approved` lists to `ManagedProfessions` when it's non-empty:
     ```csharp
     if (currentAdmin.ManagedProfessions is { Count: > 0 } scope)
         all = all.Where(p => scope.Contains(p.Profession)).ToList();
     ```
   - Defense in depth: the `OnPostAsync` approve/hide handler must also re-check
     that the target provider's `Profession` is in the admin's `ManagedProfessions`
     (or the admin is unscoped) before acting — don't rely on the UI alone to
     enforce scope, since a scoped admin could otherwise POST an out-of-scope
     `providerId` directly.

4. In `Admin/Users.cshtml(.cs)`, add UI to assign `ManagedProfessions` to an
   Administrator (multi-select checkboxes over the `Profession` enum). Only show/
   matter when `Role == Administrator`.

5. Update the Admin Providers page UI to group Pending/Approved sections by
   profession (tabs or section headers) so a scoped admin's filtered view still
   reads well, and an unscoped admin isn't looking at one giant undifferentiated list.

---

## Phase 4 — Registration / edit / directory UI

1. `Provider/Register.cshtml(.cs)`: add a `Profession` select as the first field.
   Specialty checkboxes/multiselect should be populated from
   `Taxonomy.SpecialtiesByProfession[selectedProfession]` — this needs to react to
   the profession choice (simplest: reload the page/partial on profession change,
   or ship all profession's specialty lists to the client and filter with JS).

2. `Provider/Edit.cshtml(.cs)`: same specialty-list scoping. Decide whether
   `Profession` is editable after registration (recommend: no, or admin-only —
   changing profession after approval re-triggers admin review implications).

3. `Directory.cshtml.cs`: add a `Profession` filter dropdown (from `Profession`
   enum) alongside the existing Specialty/State/Language filters. When a profession
   is selected, the Specialty dropdown options should come from
   `Taxonomy.SpecialtiesByProfession[profession]` instead of the full flat list.
   Push `Profession` into `ProviderFilter` and apply it in
   `CosmosProviderRepository` — since profession is a simple equality filter, this
   one *can* be pushed into the Cosmos LINQ query (not just in-memory) for cheap
   efficiency, unlike the existing string-list filters.

4. Update any nav/branding copy that says "therapist" to something profession-neutral
   ("provider", "practitioner") — check `_Layout` and marketing copy on the home page.

---

## Phase 5 — Data migration

Existing Cosmos documents in the Therapists container have no `Profession` field.
Since Cosmos is schemaless, deserializing them will just give `Profession` its
default enum value (`Therapist`, if that's index 0) — confirm this is acceptable,
or write a one-time backfill script/console command that reads all existing
documents and writes `Profession = Therapist` explicitly, so it's not relying on
enum-default behavior (which is fragile if the enum's member order ever changes).

---

## Out of scope / explicitly deferred (confirm with user before doing)
- Per-profession custom fields beyond specialties (e.g., license numbers, service
  menus/pricing for hairstylists) — planned to come later as embedded "Details"
  documents per the user's stated direction, not part of this pass.
- Splitting into per-profession Cosmos containers — not needed at current scale.
- Placeholder specialty taxonomies in Phase 1 step 3 are guesses — get them
  confirmed/replaced with real values before launch.
