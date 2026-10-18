# Cloud Unity validation: prepared, not executed

**Current direction: cloud runner setup is deferred.** Preserve this historical research and opt-in tooling; do not provision or enable a runner. Source migration continues with portable/reference checks, while S1 stays incomplete. A cloud runner is optional infrastructure; actual Unity validation still needs a licensed Editor somewhere when that validation is resumed. Do not access the user's computer.

S1c adds the standalone, runner-independent command in [../README.md](../README.md) and [S1_CHECKLIST.md](S1_CHECKLIST.md). The original cloud wrapper now delegates to the same orchestration with its Windows/cloud/self-hosted preflight still enforced. It retains `Artifacts/UnityCloud` output locations. The legacy Editor `CloudFoundationValidation.ConfigureAndReport` entry point is retained as a forwarding API to `FoundationValidation.ConfigureAndReport`.

Recorded 2026-10-18. S1 remains **incomplete**. The existing Codex cloud machine has no usable Unity installation/licence and cannot reach Unity installer/registry endpoints. A separate cloud CI machine can have different network access. **No computer belonging to the user is accessed or required by this design.** No VM, subscription, paid runner, GitHub setting or CI run was created by this checkpoint.

The user has **no confirmed existing CI licence**. Do not assume a paid entitlement, transferable Personal licence or permission to purchase anything. Actual Unity validation requires a supported licensed Editor environment. The deferred cloud option below would additionally require supported cloud activation and a runner.

## Runner options investigated

| Option | Setup and suitability | Costs and current limit |
| --- | --- | --- |
| Dedicated Windows CLOUD VM registered as a GitHub self-hosted runner | Recommended prepared path. Install/activate pinned Unity once under the runner account; run configuration, EditMode/PlayMode and optional Windows builds with native Editor CLI. Matches the existing Mono/Windows foundation entry point. | Cloud compute, Windows licensing/image, disks, egress and Unity entitlement may cost money. GitHub currently documents self-hosted execution as free; storage allowance still matters. No VM/provider/region or paid entitlement chosen, so there is no verified VM/licence quote. |
| GitHub-hosted runners plus GameCI | Managed ephemeral runner and Unity container/tooling; requires a supported exact Editor image/version and activation method. GameCI is open-source tooling, not a supplied Unity licence. Linux can cover many Editor tests/build tasks; native Windows/desktop/encoding parity still needs suitable Windows validation. | Standard public-repository hosted execution is free; private use consumes allowance then runner-minute charges. Unity licensing is separate. Exact container availability and activation for 6000.3.26f1 were not verified; this route is investigated, not configured. |
| Unity Build Automation | Unity-managed build service; connect GitHub branch, project root UnityProject, Editor version, Windows target and required tests/artifacts. Could reduce VM administration. | Usage/plan charges and exact Editor availability need confirmation. Official service/pricing endpoints are blocked here; no price or included allowance is asserted. No service was connected or activated. |

GameCI's current activation documentation describes Personal/paid/floating-server methods, including Hub-created licence files. That generic documentation is **not proof of licence eligibility or working automated activation for the pinned Unity version**. This project will not collect a licence from the user's computer, invent credentials or bypass activation. Establish a supported cloud-only activation/entitlement with Unity first; confirm any Pro/Industry/Build Server requirement and quote for this application's use.

## Verified GitHub prices, not a Unity/VM quote

Read from GitHub's official documentation source at commit `dae83f7d28f3b8e10015bd6b6d8b72267cedceca`:

| Standard hosted runner | Published metered price (USD/minute) | Example: 30 billed minutes |
| --- | --- | --- |
| Linux 2-core x64 | $0.006 | $0.18 |
| Windows 2-core x64 | $0.010 | $0.30 |

These examples apply to metered **GitHub-hosted** use after applicable allowance, not the dedicated VM workflow's compute price. Standard hosted execution is free for public repositories; private accounts have plan-specific allowance (GitHub Free currently lists 2,000 minutes and 500 MB artifact storage). Repository visibility, account allowance and billing settings were not inspected/verified here. Do not assume the specific repository owes $0 or that included minutes remain available.

The optional larger Windows GPU runner is separately listed at $0.102/minute ($6.12 for 60 minutes), with availability/plan constraints; it is not selected or provisioned. A GPU is not asserted as necessary for the current foundation tests. Artifact storage, disks, idle cloud-VM time, taxes and Unity subscriptions are additional considerations; actual VM cost is `running hours × provider rate + storage/egress/licence`. Confirm the provider/region quote and an idle-shutdown policy before provisioning.

All money amounts are USD and a dated documentation observation, not a purchase or guaranteed future price. Unity pricing pages could not be retrieved, so a Unity subscription figure would be a guess. **No charges were incurred by starting a Unity/cloud runner in this task because none was started.**

## Prepared repository automation

`../../.github/workflows/unity-cloud-validation.yml` triggers on relevant **pushes to unity-migration only**:

1. `reference-checks`: standard `ubuntu-24.04`, pinned official checkout/Python/.NET/artifact actions, static/CI-gate tests, original restore/build and all baseline tests. Actual TRX must contain at least 270 executed/passed cases with no skips. This job runs on an approved checkpoint push, even with Unity disabled; private-repository metering can apply.
2. `unity-cloud`: **skipped by default**. It requires repository variable `UNITY_CLOUD_CI_ENABLED=true`, success of reference checks, the correct repository/branch and runner labels `[self-hosted, Windows, X64, unity-cloud-6000.3.26f1]`. The job uses environment `unity-cloud-validation`; configure its required reviewers/branch restriction before enabling it.

The workflow has read-only contents permission, immutable action SHAs, no credential-bearing activation steps, no PR/fork trigger, no auto-commit/push and no deployment. It never provisions a runner. Concurrency is serialized and job timeouts bounded. No Unity Library cache or licence state is uploaded. Test/source evidence has two-day retention to limit storage; the optional complete Windows player also has two-day retention.

`workflow_dispatch` was deliberately not used as the sole trigger: GitHub documents a default-branch requirement for that event/UI. Do not change or merge main merely to make a Run workflow button appear. Enable the runner only after setup/cost approval, then use the next separately approved migration checkpoint push. A pushed workflow is not proof a job ran successfully.

## Required cloud setup (not performed)

1. **Obtain approval for a cloud provider/region, VM budget and supported Unity licence/activation.** Prefer a dedicated Windows cloud VM; start with roughly 4 vCPU, 16 GB RAM and 100 GB writable disk, then measure. This is sizing guidance, not a purchased instance or price quote. Do not register or connect the user's personal computer.
2. Allow the cloud VM to reach required GitHub Actions/artifact endpoints and Unity download, package and activation destinations through supported networking. Preserve TLS/signature verification. The Codex proxy denial does not establish the other VM's connectivity; test there.
3. Install Unity **6000.3.26f1** and Windows build support where needed. Establish legitimate activation under the **same Windows account that executes the runner** (or the organisation's supported floating-licence service). No secrets or activation files belong in checkout, Git or chat. The prepared workflow expects a usable installation; it does not activate/buy a licence for you.
4. Register that cloud VM as a dedicated repository self-hosted Actions runner with the four labels above. Use secure GitHub runner registration; never commit its registration token. Set machine/runner environment `UNITY_CLOUD_RUNNER=true` and restart the runner process so it inherits the marker. The script refuses other platforms/unmarked machines; the workflow does not manufacture this marker.
5. Install supported runner prerequisites, including its current agent version and a writable tools cache for the pinned setup-python action. Ensure the runner account has disk/registry permissions needed by the tools. Keep Unity licence/profile state outside the workspace.
6. In GitHub, create/review environment **unity-cloud-validation**, restrict it to unity-migration and add required reviewers where the account plan supports that control. Keep runner execution limited to trusted branch code.
7. Set repository Actions variables only after setup/cost review:
   - `UNITY_CLOUD_CI_ENABLED`: `true` to opt in; missing/false skips Unity execution.
   - `UNITY_CLOUD_EDITOR_PATH`: optional non-secret full path to the pinned Editor. Default: `C:\Program Files\Unity\Hub\Editor\6000.3.26f1\Editor\Unity.exe`.
   - `UNITY_CLOUD_BUILD_WINDOWS`: optional `true` to add the slower/larger development-player build; missing/false runs configuration and tests only.
8. Obtain approval to push the current checkpoint. Check Actions results after it runs. Do not assume a skipped Unity job validates S1. VM shutdown and licence cleanup/return follow the provider/licence administration plan; this workflow does not delete machines or silently revoke entitlements.

No GitHub Unity account passwords or licence-file secrets are required by **this pre-activated-runner workflow**. Activation administration may require secure credentials outside the job; this is not permission to expose them. An unknown licence entitlement is still a real blocker, not a reason to supply dummy values.

## What a real cloud run will do

`Tools/run_unity_cloud.py` is authored but **unexecuted against Unity**. It:

- Requires Windows, a self-hosted runner marker and an installed exact Editor; calls the real Editor, never stubs it.
- Uses a fresh UUID private-results directory for each run, with unique public report paths. It does not accept stale test XML/receipts.
- Calls `CloudFoundationValidation.ConfigureAndReport`: actual Configure twice, retained asset GUIDs, scene validation, URP/Mono/.NET Standard and real package-lock checks, and an actual-Editor receipt. The helper is new and has not compiled/run in Unity here.
- Runs actual EditMode and PlayMode tests. Strict result gates reject zero tests, missing required S1 tests, skipped/failed/inconclusive cases and contradictory counters. No `-quit` is passed to Test Runner prematurely.
- Exports an allowlisted generated-source settings/UPM-lock ZIP for review, rejecting detected nonempty credential-like fields. All artifacts still require human/source review before commit; the scanner is not a guarantee against every possible sensitive field.
- Optionally invokes the existing Windows build entry point and requires its executable output. It does not launch the player or assert desktop/recording correctness.

Published artifacts contain sanitized counts/receipt summaries, the generated-source ZIP, and optionally the complete player folder. Raw Unity/stdout/stderr logs and raw test XML stay on the dedicated cloud runner, not in uploaded artifacts; they can contain private activation details. Restrict their access and retention and clean them with the ephemeral VM/workspace lifecycle. No Library/cache/profile/licence directory is included in source export.

When a job fails, inspect its reported phase and the authorised runner's private logs without copying credential values into chat or source. Even a successful automated run does not validate desktop appearance, full application parity, Windows player operation or native encoding. **No automation marks S1 complete.**

## How to resume after the first actual run

1. Verify the run's branch and checkpoint SHA, current-run summary, actual Editor version, expected test names/counts and optional build result. Distinguish skipped/unrun checks from passed tests.
2. Retrieve the generated-source artifact with authorised GitHub access. Review its allowlisted settings, actual dependency lock and all credential-like data before copying files. Preserve existing scene/script GUIDs and unrelated work.
3. Fix real import/compiler/runtime failures; rerun affected checks and original tests as appropriate. Do not change behavioural intent to make tests pass.
4. Record actual evidence, remaining visual/Windows limitations and exact next steps in MIGRATION_STATUS.md. Complete S1 only when its actual gates pass; otherwise leave it incomplete. Commit locally and ask approval before pushing that new checkpoint.

## Research sources and limits

- [GitHub standard prices](https://github.com/github/docs/blob/dae83f7d28f3b8e10015bd6b6d8b72267cedceca/data/reusables/billing/actions-standard-runner-prices.md), [billing rules](https://github.com/github/docs/blob/dae83f7d28f3b8e10015bd6b6d8b72267cedceca/content/billing/concepts/product-billing/github-actions.md), [included quotas](https://github.com/github/docs/blob/dae83f7d28f3b8e10015bd6b6d8b72267cedceca/data/reusables/billing/actions-included-quotas.md), [larger/GPU runner pricing](https://github.com/github/docs/blob/dae83f7d28f3b8e10015bd6b6d8b72267cedceca/content/billing/reference/actions-runner-pricing.md), [dispatch event requirement](https://github.com/github/docs/blob/dae83f7d28f3b8e10015bd6b6d8b72267cedceca/data/reusables/actions/branch-requirement.md). Official source was read over verified HTTPS; the rendered docs site returned proxy 403.
- [GameCI activation](https://github.com/game-ci/documentation/blob/cc8db84994fe5b4bf6f75b926850387e69926329/docs/03-github/02-activation.mdx): community integration documentation, not a Unity licence grant or assurance for this exact Editor. Its rendered site returned proxy 403.
- [Unity Build Automation](https://unity.com/products/unity-build-automation) and [Unity plans](https://unity.com/products/compare-plans): endpoints returned proxy 403; current pricing/support details remain unverified. Obtain a current provider/Unity quote before incurring costs.

Workflow syntax/action expressions and local CI gate regression tests can be checked in this cloud environment; those checks are not a GitHub-hosted job, cloud VM run or Unity validation.
