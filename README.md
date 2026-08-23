# Node — Astrology Dating App

Examen project for ASP.NET Core MVC (EhB). A dating app where matching is based on
astrological compatibility: every member's natal chart is calculated from their birth
date, time and place, and pairs who like each other get an AI-written compatibility
explanation based on both charts.

## Tech stack

- **.NET 9**, C#, ASP.NET Core MVC with Razor views and Bootstrap
- **Node.Data** — class library: EF Core models, `ApplicationDbContext` (based on
  `IdentityDbContext<ApplicationUser>`), migrations, and startup seeding
- **Node.Web** — the MVC site, plus a set of REST API controllers under `Controllers/Api`
  for a companion app
- **SQL Server** via EF Core
- **ASP.NET Core Identity** for authentication, roles and email confirmation
- **SwissEphNet** for real planetary positions, with **NodaTime** + **GeoTimeZone** to
  convert local birth time to UT using the historical timezone of the birth place
- **Anthropic Claude API** to write natural-language match/chart interpretations
- **GetStream** for real-time chat between matches
- **MailKit** for SMTP email (verification, notifications)
- Full localization (nl / en / fr) for UI, Identity pages and validation messages

## Solution structure

```
Node.sln
├── Node.Data/              # class library
│   ├── Models/              # ApplicationUser, NatalChart, Placement, Match, Swipe,
│   │                        #   PartnerPreference, Report, Notification, CookieConsentLog, enums
│   ├── Data/                 # ApplicationDbContext, DbSeeder, DemoSynastry
│   ├── Services/             # natal chart calculation, LLM interpretation services
│   └── Migrations/
└── Node.Web/                # ASP.NET Core MVC app
    ├── Controllers/           # Account, Manage, Swipe, Match, Chart, Notification,
    │                          #   Report, Moderation, Admin, Culture, Home
    ├── Controllers/Api/       # REST endpoints: Auth, Profile, NatalCharts, Swipes,
    │                          #   Matches, Notifications, Reports, AdminUsers
    ├── Services/               # email, geocoding, JWT, chat, notifications, profile pictures
    ├── Middleware/             # cookie consent middleware
    ├── Resources/              # SharedResource.{nl,en,fr}.resx
    └── Views/
```

## Core domain

- **ApplicationUser** — custom Identity user with display name, bio, profile picture,
  birth date/time/place, geocoded lat/lng, gender and partner preferences.
- **NatalChart** / **Placement** — one chart per user, calculated via Swiss Ephemeris;
  a placement per celestial body (Sun through Pluto, plus Ascendant) with sign, house
  and degree.
- **Swipe** / **Match** — mutual likes become a match, with a compatibility score and
  an AI-written explanation of the synastry between both charts.
- **Report** / moderation queue — members can report each other; moderators review and
  resolve reports, and can block reported users.
- **Notification** — in-app notifications (new match, new message, report handled, ...).

## Roles

Three roles, seeded at startup and auto-assigned at registration:

- **Admin** — user management (assign roles, block/unblock).
- **Moderator** — moderation queue (reports), can block reported users.
- **Lid** (Member) — default role; swiping, matching, chat and profile management.

## Getting started

1. Requires the .NET 9 SDK and a reachable SQL Server instance (LocalDB works for
   local development).
2. Configure secrets via `dotnet user-secrets` (never commit these to `appsettings*.json`):
   - `ConnectionStrings:DefaultConnection`
   - `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`
   - `Stream:ApiKey`, `Stream:ApiSecret`
   - `Anthropic:ApiKey`
   - SMTP settings for `SmtpEmailService`
3. From `Node.Web`, run:
   ```
   dotnet run
   ```
   Pending EF Core migrations are applied automatically on startup, followed by
   extensive demo seeding (roles, ~24 demo members with real public birth data,
   calculated natal charts, matches and sample reports).

Migrations are managed from the solution root with `Node.Data` as the migrations
project and `Node.Web` as the startup project, e.g.:

```
dotnet ef migrations add <Name> --project Node.Data --startup-project Node.Web
```

## Note on demo data

Demo member accounts use real, publicly documented birth data (public figures) so the
Swiss Ephemeris calculation can be checked against independently verifiable charts.
Mutual likes/matches are only seeded for pairs with a real, publicly documented
relationship.

## Next phase: wingman agents

Swiping and chatting through every potential match by hand is time-consuming. The
planned next phase gives each user an AI "wingman" agent that does this legwork for
them:

- **Profile scouting** — the agent chats with its own user (personal questions, on top
  of birth data) to build a richer picture of what they're looking for, then searches
  for matching profiles on their behalf instead of the user swiping through the deck
  themselves.
- **Agent-to-agent negotiation** — each user's agent talks to the agent of a candidate
  match (LLM-to-LLM conversation) to gauge compatibility and interest before either
  user has to spend time chatting directly. Only promising matches get surfaced to the
  actual users.

This maps to the `Agent` / `AgentConversation` / `AgentMessage` not yet implemented.

Another future development would be to automatically screen bio descriptions and chat 
conversations for hate speech and other inappropriate content. 

## (AI) sources used

- Anthropic (Claude code)
- OpenAI (ChatGPT/Codex)
- Chani app
- Co-Star app
- Lesmateriaal
- Several GitHub repos (https://github.com/topics/dating-app)
- API-documentation (SwissEphNet, GetStream, Anthropic API, Nominatim/OpenStreetMap)
- Discussions with Senior Developers (inspiration to use Getstream)