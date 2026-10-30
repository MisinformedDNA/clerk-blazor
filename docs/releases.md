# Publishing the first preview

The prepared release is **Clerk.Blazor 0.1.0-preview.1**, targeting .NET 10 Blazor WebAssembly. Preparing a package or committing the workflow does not publish it.

The release workflow pins packaging commit `d2edd0c97d18c35f3596242b3b40ae5dbdeb12f7`. That commit adds package metadata and README installation instructions to the Sonopiq-tested library revision `30f77be4a800076b976714033a29d338491f889f`; library implementation, assets and dependencies are unchanged. Sonopiq source and configuration are not included. Do not move the pinned commit without revalidating the candidate.

## One-time owner setup

1. Sign in to the intended owner account at [NuGet.org](https://www.nuget.org). Confirm the account is authorized to publish `Clerk.Blazor`, including permission to create the new ID. An absent public listing does not establish ownership or rule out a reserved prefix.
2. Select your username, then **Trusted Publishing**, and add a GitHub policy using these exact values:

| Policy field | Value |
|---|---|
| Repository Owner | `MisinformedDNA` |
| Repository | `clerk-blazor` |
| Workflow File | `nuget-publish.yml` (filename only) |
| Environment | `nuget.org` |
| Package glob | `Clerk.Blazor` (no wildcard) |
| Scopes | New package publication and new versions |
| Policy owner | The intended NuGet package owner, not inferred from the GitHub username |

3. In the GitHub repository, create the **nuget.org** environment. Add a required reviewer and restrict deployment to the `main` branch. Confirm the approval gate is enforced for this repository's plan and settings; do not dispatch without it.
4. Add an environment secret named **NUGET_USER** containing the publishing account's NuGet profile name, **not its email address**. This is the identity passed to `NuGet/login`; no long-lived API key is needed. Never send credentials in chat.
5. Put the reviewed workflow `.github/workflows/nuget-publish.yml` on `main`, and ensure the pinned packaging commit is available on GitHub. Preserve the commit when integrating this branch, or push the branch so the object remains fetchable. A squash-only integration that discards the original commit is not sufficient by itself.

NuGet policy setup is an account-side action; repository secrets and public API reads cannot prove it was completed.

## Release

Run **Publish Clerk.Blazor preview** manually from the Actions page, selecting **main**. The workflow does not run on pushes, pull requests or tags.

The credential-free build job checks out the pinned source, runs the unit tests, packs Release, inspects identity, metadata, dependencies and byte-identical documentation/static assets, and uploads the exact candidate. Review its output and hash before approving the environment-gated publishing job.

The publishing job alone receives `id-token: write`. It checks version availability, obtains a short-lived credential with the pinned official `NuGet/login` action and pushes the one explicit package file. Duplicate versions fail; the workflow never uses `--skip-duplicate` or chooses another version.

After push, the workflow waits up to 30 minutes for NuGet indexing and restores `[0.1.0-preview.1]` into an isolated cache using only NuGet.org. If indexing verification times out after a successful push, investigate the existing upload; do not blindly rerun publication.

Expected URL after verified publication: <https://www.nuget.org/packages/Clerk.Blazor/0.1.0-preview.1>. This is not a claim that publication has occurred.

The initial preview pins its release source deliberately. Future versions require a separately reviewed version/source update and fresh validation.

Reference: [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing).
