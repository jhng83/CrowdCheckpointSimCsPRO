# Resumable Unity migration

Read the repository-root `AGENTS.md`, then `MIGRATION_STATUS.md` and `MIGRATION_PLAN.md` before doing work here. Those files are the handoff contract; do not depend on chat history.

## Resume procedure

1. Confirm the sanitized origin is `https://github.com/jhng83/CrowdCheckpointSimCsPRO`, inspect `git status`, current branch, HEAD and recent commits. Preserve unrelated changes. Never print credentials.
2. Work on `unity-migration` only. If the branch is absent locally, inspect the remote before creating anything; check out the existing remote migration branch when present. Do not reset, force-push or recreate it from `main`. Do not create another worktree by default.
3. Check the publication/approval state in `MIGRATION_STATUS.md`. A local checkpoint is not available to a fresh GitHub-based task until the user approves a push and that push succeeds. Do not infer approval from elapsed time, an interruption, a stale status entry or approval of a previous checkpoint.
4. Inspect the first unfinished stage, in-progress substep, validation evidence and exact next steps. Reconcile them with the actual files and commit history before proceeding. Check tool/version availability again in a new environment; processes and licences are not implied by saved files.
5. Work on one small stage at a time. Update the in-progress substep and recovery notes before lengthy or failure-prone operations and after meaningful results, so an interruption leaves a useful handoff. Keep source work reviewable even when validation is blocked.

## Stage checkpoint procedure

- Run the stage's actual validation. Keep .NET tests, Unity compilation, EditMode, PlayMode, Windows build, desktop checks and video encoding results distinct. Never substitute source inspection or a .NET build for Unity validation.
- Update `MIGRATION_STATUS.md` with completed/remaining work, known issues, commands and results, unperformed checks, toolchain/dependency versions, decisions, affected files and exact next steps. Use `not started`, `in progress`, `implemented / unvalidated`, `blocked` or `complete`; label every placeholder explicitly.
- A stage is complete only when its documented gates pass. If interrupted or externally blocked, describe partial work honestly; an incomplete checkpoint must remain marked incomplete.
- Inspect the complete staged diff, whitespace errors, generated files and accidental secrets. Never stage with `git add .`; stage only intended source, assets, documentation and settings. Preserve asset `.meta` files when Unity assets exist.
- Commit the checkpoint locally on `unity-migration`. Record its identity in the final response; the status describes its own commit as "the commit containing this revision" to avoid a self-referential SHA. Never amend a published checkpoint just to update that reference.
- **Stop after the checkpoint and ask for explicit approval to push that specific commit to `origin/unity-migration`.** Do not push, open a pull request, merge, or start another implementation stage while waiting. Push approval applies only to the identified checkpoint. If publication fails, report it and preserve the local commit.
- When an approved push succeeds, verify the remote SHA. Record the publication transition in the next status update and report it to the user; do not pretend the previous committed status was a live remote observation.

## Invariants and boundaries

- Keep the complete Unity project under `UnityProject/`. Preserve the root .NET application/tests as the executable reference and keep them buildable. Before adding Unity C# files, exclude `UnityProject/**/*.cs` from the root SDK project's compile glob.
- Keep simulation mathematics/state independent of `UnityEngine`; adapt vectors only at the presentation boundary. Preserve existing SFM, orderly queues, timing, collision/right-of-way, exclusive locks, Purple priorities, wheeled robot and human-only Dasher rules.
- No Unity physics/NavMesh replacement, unapproved behaviour changes, disabled regression assertions or fabricated validation. Investigate code/test/doc disagreements and record uncertain intent before changing behaviour.
- No credentials, Unity activation/licence files, private keys, tokens, generated caches, recordings or user configurations in commits. Secrets belong in secure environment/CI settings, not status files, logs or commands saved here. Retain third-party redistribution notices.
- Unity version/package/backend choices are proposals until pinned and verified. A documentation checkpoint or a collection of scripts is not a complete Unity port.
