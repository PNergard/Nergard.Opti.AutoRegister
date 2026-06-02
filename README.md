# Nergard.Opti.AutoRegister

Auto-wire newly created Optimizely CMS content into a settings property — no manual settings editing.

You often create content types (a search page, a news archive, …) that the rest of the solution must
reference from a single source of truth, usually a settings page. Normally an editor has to open the
settings page and point a (often required) `ContentReference` property at the new page. This library does
it for you: decorate the content type, and on create/publish the new page is written into the matching,
currently‑empty reference on the resolved settings instance.

Built for **Optimizely CMS 13** (`net10.0`). 

## Use it in your own solution

This is shared as source. To use it in another solution, add the project to your solution and reference it
from your CMS web project:

```bash
# from your solution folder
dotnet sln add path/to/Nergard.Opti.AutoRegister/Nergard.Opti.AutoRegister.csproj
dotnet add YourSite.csproj reference path/to/Nergard.Opti.AutoRegister/Nergard.Opti.AutoRegister.csproj
```

That's it — an initialization module registers the services and subscribes to content events. There is
nothing to add to `Startup`. (Copying the project into your repo as a submodule or a local folder works
just as well; there is no NuGet package to install.)

## Usage

Decorate the content type that should register itself, naming the settings **type**:

```csharp
// Auto-match: the settings property whose [AllowedTypes] most specifically accepts ContactPage is used.
[RegisterIn(typeof(StartPage))]
public class ContactPage : SitePageData { }

// Explicit property: recommended when the settings property allows a broad base type (e.g. PageData).
[RegisterIn(typeof(StartPage), nameof(StartPage.ContactsPageLink))]
public class ContactPage : SitePageData { }

// React on creation instead of publish.
[RegisterIn(typeof(StartPage), Trigger = RegisterTrigger.Created)]
public class ContactPage : SitePageData { }
```

The attribute can be applied multiple times to register into several settings targets.

### Behaviour

- **Trigger** — `Published` by default; switch to `Created` to wire up before publish.
- **Target instance** — resolved from the content tree, scoped to the source content's site (the site's
  start page, or the single instance of the settings type beneath it). If zero or several instances are
  found, the registration is skipped and a warning is logged.
- **Property match** — explicit `PropertyName` wins; otherwise the `ContentReference` property whose
  `[AllowedTypes]` *most specifically* accepts the created type is used (a type-specific slot beats a
  generic `[AllowedTypes(typeof(PageData))]` slot). Only an equally-specific tie is skipped + logged
  (add a `PropertyName`).
- **Empty‑only** — an existing (non‑empty) reference is never overwritten.
- **Persist** — the settings instance is saved and published so the change is immediately live.
- **Scope (v1)** — pages only; single `ContentReference` properties only (no `ContentArea`/lists).

## Customizing target resolution

The default resolver finds the settings instance in the content tree. You can override the resolver if needed.

```csharp
services.Replace(ServiceDescriptor.Transient<ISettingsTargetResolver, MySettingsServiceResolver>());
```

Your implementation returns the settings instance(s) for a given settings type and source content.

## License

MIT
