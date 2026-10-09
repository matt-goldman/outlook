# Zuemail: Practical Fluent Email Plan

## Direction

Build on Zuemail, not a replacement architecture. Keep `EmailMessage`, `EmailContact`, `EmailMessageBuilder`, `EmailMessageHandler`, and the existing sender contracts. Preserve simple dictionary headers, plain-text/HTML body properties, and familiar fluent methods. Add reusable composition, independent snapshots/templates, safer attachment handling, and reliable provider execution. There is no AST, visitor, rewriter, immutable-envelope redesign, or mandatory new dependency framework in this plan.

Configuration and construction must not connect or send. `Compose(...)`, `Body(...)`, `Attach(...)`, `SaveAsTemplate()`, and `Build()` prepare messages; only explicit `SendAsync()`/`TrySendAsync()` execute delivery. Keep the builder transport-free and the handler responsible for validation, sending, and resetting from saved defaults.

## Existing Project Boundaries

| Project | Role | Rule |
| --- | --- | --- |
| `Zuemail.Core` | Message/contact models, neutral options, builder/handler contracts, sender interfaces, templates, validation, and fluent composition. | No MailKit/MimeKit types. Retain useful existing Microsoft.Extensions integrations; do not chase a zero-package rewrite. |
| `Zuemail.MailKit` | MIME mapping, SMTP/IMAP adapters, TLS/authentication, capabilities, protocol logging, and client ownership. | References Core; Core never references this provider. |
| Console/app | Provider selection, configuration, application policy, and hosting. | Use local project references where appropriate; do not move app requirements into Core. |

Do not introduce another Core/Hosting/Provider project merely to reorganize names. Extend the existing boundaries and add an assembly only for an independently useful provider/package.

## Phase 1: Shared Build Configuration

- [x] Adapt MailKitSimplified's source build props as `Source/Directory.Build.props`, with Zuemail URLs and shared authors/style settings.
- [x] Share `netstandard2.0;net10.0` across Core and MailKit; remove their duplicate framework/URL declarations. Keep console/app target selection separate.
- [x] Remove the unused Gateway project and its solution, console, and shared-build references.
- [x] Set C# 14 for library builds so existing global usings/file-scoped namespaces also compile for `netstandard2.0`; keep runtime targeting separate from language version.
- [x] Pin the installed .NET 10 SDK (`10.0.401`) in `global.json` with `latestPatch` roll-forward and previews disabled.
- [x] Add a small xUnit executable test project on `net10.0` using Microsoft.Testing.Platform v2 and direct `dotnet test --project <project>` execution; avoid legacy adapters.
- [ ] Check package resource paths independently; decide the MAUI framework/workload upgrade separately from the library work. Do not silently change the app's platform targets.

## Phase 2: Independent Messages and Reusable Composition

- [x] Replace message/builder shallow copying with independent headers, recipient lists, contacts, attachment dictionaries, and byte arrays. Preserve documented caller ownership of legacy streams.
- [x] Stop new builders from sharing mutable `EmailMessage.Default` state. Clone defaults and prove callers cannot mutate each other's messages. Option factories remain a phase 4 task.
- [x] Add `Compose(Action<IEmailMessageBuilder>)` to the fluent contract and implementation; invoke it immediately and return the same builder. Reuse actions for signatures, campaigns, recipients, and standard headers.
- [x] Add `Body(string plainText, string html = null)` as a convenience over the existing body properties. No content tree or multipart domain abstraction is needed.
- [x] Add `Build()` returning an independent message snapshot without file access or transport I/O; keep existing `Email` and `Copy()` behavior documented.
- [x] Make `SaveAsTemplate()` capture independent state, initialize builders from independent configured defaults, and ensure composing/copying a template does not mutate the original.

## Phase 3: Send Handler and Validation

- [x] Keep `EmailMessageHandler` as the execution boundary over `EmailMessageBuilder` and the sender interface; do not add a client or send methods to the plain builder.
- [x] Supply a default validation strategy when none is configured; allow an explicit alternate strategy without null dereferences.
- [x] Respect a failed validation result instead of sending anyway. Validate From and the combined To/Cc/Bcc set once, reject header injection, and never manufacture addresses or mutate messages in validation.
- [x] Propagate cancellation through handler validation and transport. Existing `IValidationStrategy<T>` implementations remain supported; optional `ICancellableValidationStrategy<T>` receives the token. Legacy strategies are checked before/after execution but cannot be interrupted by the handler.
- [x] Reset from an independent template after confirmed success. Retain the composed message after failure, cancellation, or `TrySendAsync(false)`; document explicit reset behavior.
- [x] Test repeated sends, template reset, cancellation, validation failure, and message retention with a fake sender before connecting to a real server.
- [x] Use a simple composition-then-send pattern in the console instead of calling terminal handler methods on `IEmailMessageBuilder`. A broader handler-interface redesign is not required for this sample.

## Phase 4: Options, Attachments and MailKit Execution

- [ ] Fix `EmailOptions.Create()` to copy defaults, and `SetHost()` to set host-only inputs as well as host/port inputs. Support bracketed IPv6 deliberately; reject ambiguous/invalid port strings rather than ad hoc splitting.
- [ ] Keep host/port/credentials/optional `TimeSpan` timeout in Core options; keep `SecureSocketOptions`, authentication mechanisms/capabilities, protocol logging, and MailKit delegates in provider options.
- [ ] Make option copies independent for mutable collections and protocol-log settings; redact credentials and validate timeout ranges/conversion before applying them to clients.
- [ ] Replace client-producing `Lazy` properties with an explicit provider factory/owner. Do not create a new `Lazy<SmtpClient>` on each access; never store connected clients in neutral options.
- [ ] Adapt authentication delegates with typed wrappers, not casts between `Func<ISmtpClient, Task>` and `Func<IMailService, Task>`.
- [ ] Add a small typed attachment descriptor supporting copied bytes or a fresh-stream factory, with filename/content type/optional content ID. Keep legacy `Attach(string, object)` as a compatibility path.
- [ ] Add fluent typed `Attach(...)`; open files/streams only during cancellable MIME materialization. Define consumer-owned factory streams versus borrowed legacy streams and dispose owned resources on success/failure.
- [ ] Use MimeKit `BodyBuilder` for text/HTML, attachments, and linked resources. Reuse a focused mapping helper; do not write a MIME engine, AST, or visitor.
- [ ] Ensure the provider connects/authenticates before sending, preserves Bcc envelope delivery without exposing Bcc on the wire, and disposes or reuses the correct owned client/message resources.

## Phase 5: Configuration, Verification and Documentation

- [ ] Extend existing `ConfigureEmail()`/`ConfigureDefaultEmail()` registrations with action/configuration overloads and safe named account/template support only where needed.
- [ ] Add a simple cancellation-aware null/fake sender for dry runs and unit tests; do not count simulated sends as delivered mail.
- [ ] Test composition, independent snapshots/defaults, templates, reset policies, host parsing, attachment ownership/replay, MIME alternatives/linked resources, and Core/provider dependency boundaries.
- [ ] Add local SMTP integration tests covering connection/authentication, cancellation, Bcc, and repeated sending; do not depend on real account credentials in tests.
- [ ] Verify both library TFMs and update the console sample/README with concise fluent usage, DI registration, ownership, and migration notes.
- [x] Replace the console's nonexistent receiver wrapper with native MailKit IMAP APIs for reading and IDLE monitoring; build the sample against Core and MailKit only.

## Later, Only When Needed

- Add retries/timeouts/telemetry behind the sender interface after defining transient versus ambiguous delivery. Do not blindly retry SMTP after possible acceptance; do not require Scrutor or HTTP resilience packages for simple SMTP usage.
- Add bounded Channels/background workers only for a real queued workload, documenting backpressure and lack of crash durability.
- Add connection pooling only after measuring concurrent-send/handshake costs; maintain exclusive client ownership and bounded capacity.
- Add a cloud provider only after selecting a concrete requirement/provider. Do not make it a gate for finishing the first reliable MailKit workflow.

## First Implementation Sequence

1. Verify the shared build props; add focused Core tests without upgrading the MAUI app.
2. Fix independent message/default/template copies before exposing more reuse.
3. Add `Compose()`, `Body()`, and detached `Build()`; demonstrate reusable defaults in tests.
4. Fix default validation and send/reset behavior, with fake-sender tests.
5. Fix host/options parsing and client ownership; add typed attachments using MimeKit `BodyBuilder`.
6. Finish configuration/sample usage and local SMTP verification. Leave pooling, queueing, cloud adapters, and elaborate policy frameworks for demonstrated needs.

## Fluent Shape

The builder methods below are implemented. Sending still uses an explicitly configured provider.

```csharp
Action<IEmailMessageBuilder> welcome = email => email
    .From("sender@example.com")
    .Header("X-Campaign", "welcome")
    .Body("Welcome", "<p>Welcome</p>");

var message = new EmailMessageBuilder()
    .Compose(welcome)
    .To("recipient@example.com")
    .Subject("Hello")
    .Build();

await sender.SendAsync(message, cancellationToken);
```

## Completion Criteria

- [x] Independently composed messages/defaults/templates isolate mutable message collections, contacts, and byte arrays; legacy stream/object attachments remain explicitly borrowed.
- [ ] Fluent construction performs no network/file I/O; explicit sending respects validation and cancellation.
- [ ] Core remains unaware of MailKit, while provider client/MIME resource ownership is explicit and tested.
- [ ] The simple MailKit workflow passes focused tests and both library TFMs before optional throughput features are added.

## Verified Setup

- Shared library framework and repository metadata are inherited from `Source/Directory.Build.props`; the URL is `https://github.com/danzuep/Zuemail`.
- Core and MailKit compile for `netstandard2.0` and `net10.0`. Library validation uses `-p:GeneratePackageOnBuild=false`; package resource/path validation remains outstanding.
- MSBuild evaluation confirms the console has `TargetFramework=net10.0` and no inherited `TargetFrameworks`. MAUI platform targets are unchanged; its workloads/build were not validated.
- Gateway has been removed; no replacement project is introduced.
- The console now builds using `Microsoft.NET.Sdk` without warnings or errors. Configuration/attachment paths resolve from the executable directory; SMTP options are passed directly to the provider, and IMAP reads/IDLE use the existing MailKit client APIs.
- The initial setup did not execute the console against a live server or introduce automated tests. Subsequent verified composition/handler/test work is recorded below; provider lifecycle and live-server verification remain outstanding.

## Verified Composition and Handler Increment

- `dotnet test --project tests/Zuemail.Core.Tests/Zuemail.Core.Tests.csproj`: 30 passed, 0 failed, 0 skipped using the pinned SDK and MTP runner.
- `dotnet build Source/Zuemail.slnx -p:GeneratePackageOnBuild=false`: passes for Core/MailKit on both library targets, the console, and Core tests. Tests are included in the solution.
- `Compose()`, `Body()`, detached `Build()`, default/configured-template cloning, and independent `Copy()`/`SaveAsTemplate()` are implemented. Streams and arbitrary non-byte attachment objects remain borrowed; file paths are not opened during copying/building.
- Copying does not access the lazily computed body preview or mutate the source text. Header/attachment dictionary comparers are retained when their source is a `Dictionary`.
- The handler uses a nonmutating default validator, respects custom validation results, forwards cancellation, and resets only after a successful send. `SendAsync()` throws on invalid content; `TrySendAsync()` returns false. Sender exceptions/cancellation retain the message and are not swallowed by the handler.
- The default validator uses BCL address parsing, accepts To/Cc/Bcc recipients and legitimate self-addressed mail, rejects header injection, and does not invent debug-only sender/reply addresses.
- The console now demonstrates reusable composition and independent per-recipient messages instead of sharing live builder state. It was compiled, not executed against a server.
- Next: phase 4 option factories/host parsing and MailKit client ownership, followed by typed attachments. MailKit's direct sender paths still need their provider-specific validation/lifecycle/cancellation work; Core fake-sender tests do not claim those paths are fixed.