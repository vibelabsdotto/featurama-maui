# Featurama .NET MAUI SDK

.NET client and native MAUI pages for [Featurama](https://featurama.app).

## Supported build

The package contains `net10.0`, `net10.0-android36.0`, `net10.0-ios26.5` and `net10.0-maccatalyst26.5` assemblies. The plain .NET target includes the HTTP client, models and dependency injection. Native targets also include the MAUI UI. Supported minimum OS versions are Android 21, iOS 15 and Mac Catalyst 15. The interactive tester requires Android 26.

This release requires .NET 10. MAUI 9 reached end of support on May 12, 2026. The repository pins SDK and workload set `10.0.401` in `global.json`, MAUI Controls `10.0.110`, and Microsoft.Extensions packages `10.0.12`. The Apple workload is `26.5.10318`, which officially requires **Xcode 26.6**, not Xcode 26.5 despite the workload version prefix. Do not disable Xcode validation.

On macOS, install Xcode 26.6 with its command-line tools, the Android SDK and a supported JDK. The following installs .NET and its workloads only under this repository's ignored `.hermes/` directory, without sudo, PATH edits or changes to system-wide .NET workloads:

```sh
bash scripts/setup-toolchain.sh
bash scripts/dotnet-local.sh restore src/Featurama.Maui/Featurama.Maui.csproj --artifacts-path .hermes/build
bash scripts/dotnet-local.sh build src/Featurama.Maui/Featurama.Maui.csproj -c Release --no-restore --artifacts-path .hermes/build
bash scripts/dotnet-local.sh pack src/Featurama.Maui/Featurama.Maui.csproj -c Release --no-build --artifacts-path .hermes/build

bash scripts/dotnet-local.sh build test-app/FeaturamaTester/FeaturamaTester.csproj \
  -p:TargetFrameworks=net10.0-android --artifacts-path .hermes/tester-android
bash scripts/dotnet-local.sh build test-app/FeaturamaTester/FeaturamaTester.csproj \
  -p:TargetFrameworks=net10.0-ios26.5 -p:RuntimeIdentifier=iossimulator-arm64 \
  -p:CodesignKey=- --artifacts-path .hermes/tester-ios
bash scripts/dotnet-local.sh build test-app/FeaturamaTester/FeaturamaTester.csproj \
  -p:TargetFrameworks=net10.0-maccatalyst26.5 -p:RuntimeIdentifier=maccatalyst-arm64 \
  -p:EnableCodeSigning=false --artifacts-path .hermes/tester-mac
```

The iOS simulator command uses local ad-hoc signing. The unsigned Mac command is for local testing. Neither is Store signing. No build command above publishes a package or uploads an app. The repository has an interactive tester, not an automated test project.

Official references:
- [.NET MAUI support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/maui)
- [.NET Apple 26.5.10318 and Xcode 26.6](https://github.com/dotnet/macios/releases/tag/dotnet-10.0.1xx-xcode26.5-10318)
- [MAUI Controls 10.0.110](https://github.com/dotnet/maui/releases/tag/10.0.110)

## Add the package to an app

A MAUI app must reference Controls explicitly. Set its native targets to `net10.0-android`, `net10.0-ios26.5` and `net10.0-maccatalyst26.5`, and add:

```xml
<ItemGroup>
  <PackageReference Include="Featurama.Maui" Version="1.0.0" />
  <PackageReference Include="Microsoft.Maui.Controls" Version="10.0.110" />
</ItemGroup>
```

The pinned workload's template can default to an older Controls package. Do not suppress NU1605 downgrade errors. Match the app's Controls reference to `10.0.110` or a compatible newer version. Portable `net10.0` consumers only need the Featurama package.

## Configure and migrate

The default API is now `https://newapi.featurama.app`. Use a project SDK key created by the matching backend, never a dashboard session or owner credential.

```csharp
using Featurama.Maui;
using SDK = Featurama.Maui.Featurama;

SDK.Init(options => options
    .ApiKey(projectApiKey)
    .Timeout(TimeSpan.FromSeconds(30)));
```

Legacy users must explicitly select the legacy origin and its matching key:

```csharp
SDK.Init(options => options
    .ApiKey(legacyProjectApiKey)
    .BaseUrl(FeaturamaOptions.LegacyBaseUrl)); // https://api.featurama.app
```

There is no origin fallback or automatic retry against another backend. The static facade and dependency-injection transport reject redirects rather than forwarding `X-Api-Key`. When constructing `FeaturamaClient` with your own `HttpClient`, you own its transport: set `HttpClientHandler.AllowAutoRedirect = false` and do not attach unrelated default credentials. `FeaturamaClient` cannot inspect or reconfigure an injected handler.

Local development can use `http://localhost:3000` on the Mac or Apple simulator. Android emulators can use `http://10.0.2.2:3000`, or loopback with `adb reverse`. Configure platform network policy only in the development application. Do not ship broad cleartext exceptions.

The tester stores an explicit origin and key together in SecureStorage. It does not migrate a saved key to the new origin or contain an embedded usable key. Changing settings applies to newly opened pages; an already-open page retains its original client and identity so drafts cannot move between projects.

Dependency injection:

```csharp
using Featurama.Maui.DependencyInjection;

builder.Services.AddFeaturama(options => options.ApiKey(projectApiKey));
```

Inject `FeaturamaClient` for custom screens. Initialize `SDK.Init` before presenting the built-in UI.

## Native page

```csharp
using Featurama.Maui.UI;

await Navigation.PushModalAsync(new NavigationPage(new FeaturamaPage(
    new FeaturamaPageOptions
    {
        SubmitterIdentifier = installationId,
        OnCloseAsync = async () => { await Navigation.PopModalAsync(); }
    })));
```

Keep the installation identity stable across launches. If omitted, the page persists a generated identity in Preferences. The page supports filters, pagination, pending submissions, request votes, comments, comment votes, server branding and optional/required email collection. Failed submissions retain their drafts. Pending requests cannot receive request votes.

Only requests with a nonempty submitter identifier exactly matching the page identity show Edit. The prefilled form validates title and description, disables controls during saving, and keeps failed or cancelled-save drafts. A successful save keeps the current filter/page and reads server state back. List refreshes cannot erase an open draft. Cancel explicitly discards that draft. Server-side ownership checks remain authoritative; an installation identifier is not an authenticated account.

### Permission-free metadata

Native UI submissions and native `FeaturamaClient` create calls automatically collect platform, OS version, device model/manufacturer/type and app version/build using MAUI Essentials. No permissions are requested. The SDK does not automatically collect device name, advertising/vendor IDs, locale, display size or location. Unsupported getters are omitted and do not block feedback submission.

`CreateFeatureRequestInput.DeviceInfo` overrides the complete automatic object. For the built-in page, use `FeaturamaPageOptions.DeviceInfo`. An explicit empty `new Featurama.Maui.Models.DeviceInfo()` disables collection. Null selects automatic collection on native targets. Portable `net10.0` callers provide metadata themselves. Override objects are copied when opening a page or sending a native request.

## HTTP client

```csharp
using Featurama.Maui.Models;

var config = await SDK.GetProjectConfigAsync();
var requests = await SDK.GetFeatureRequestsAsyncForUser(
    installationId, page: 1, pageSize: 20, filter: "new");

var created = await SDK.CreateFeatureRequestAsync(new CreateFeatureRequestInput
{
    Title = "Export my requests",
    Description = "Download a CSV of the requests I submitted.",
    SubmitterIdentifier = installationId,
    Email = contactEmail,
    DeviceInfo = null // Automatic on native targets; optional on the portable target.
});

await SDK.UpdateFeatureRequestAsync(created.Id, "Export my requests", "Updated description", installationId);
var comments = await SDK.GetCommentsAsync(created.Id);
await SDK.AddCommentAsync(created.Id, "Another detail", installationId, "Display name");
```

The current backend requires nonempty title, description and submitter identity. The original convenience overload retains optional parameters for source compatibility. Email is required when the project's email policy says so. New requests start pending approval and include the submitter's initial vote. Use the identity-aware list method to see your own pending requests and server `HasVoted` state. The original anonymous list overload remains available.

Filters are `new`, `planned`, `in_progress` and `done`. Public responses may omit contact and device data. Those model fields are nullable. `IsApproved` defaults to true when omitted by the legacy API.

After approval, use `VoteAsync`, `RemoveVoteAsync` or `ToggleVoteAsync`. Comments have matching `VoteCommentAsync`, `RemoveCommentVoteAsync` and `ToggleCommentVoteAsync` methods. Toggle catches only HTTP 409 and then deletes the existing vote. Comment responses do not contain per-user vote state.

All methods accept cancellation tokens. HTTP 401, 403, 404 and 409 have typed API exceptions. Other failures retain their numeric `StatusCode` and raw `ResponseBody` in `FeaturamaApiException`. Do not log raw response bodies where they might contain personal data. Transport failures and timeouts raise `FeaturamaNetworkException`; caller cancellation stays an `OperationCanceledException`.

## Release preparation

`dotnet pack` produces a local `Featurama.Maui.1.0.0.nupkg` with four assemblies, dependency groups, repository metadata, this README and the MIT license. The package declares the SPDX expression `MIT`; see [LICENSE](LICENSE). Version 1.0.0 is a local, unpublished release candidate. These scripts do not publish to NuGet or upload to a Store. Public distribution requires a separately authorized release.

Local verification evidence lives in `.hermes/reports`. Native Mac Catalyst verification exercises attached UIKit controls and the real local API. Synthetic transport checks, unsigned simulator builds and actual native runtime checks are reported separately. A library build alone does not prove native startup, Store signing or physical-device behavior.
