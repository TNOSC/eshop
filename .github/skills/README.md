# Copilot CLI skills

A skill is a workflow stored in `.github/skills/<name>/SKILL.md`. Copilot CLI discovers skills
beneath this directory and invokes one when the request matches its description or when asked for by
name. Supporting templates and references live beside each skill.

| Skill | Does |
|---|---|
| [`add-blazor-component`](add-blazor-component/SKILL.md) | Scaffolds a Blazor client page or feature component with its ViewModel and service. |
| [`add-context`](add-context/SKILL.md) | Bootstraps a bounded context across the projects, including the first slice and migration. |
| [`add-entity`](add-entity/SKILL.md) | Adds a domain aggregate end to end, with persistence, read model and migration. |
| [`add-feature`](add-feature/SKILL.md) | Scaffolds a complete command/query vertical slice and its tests. |
| [`add-tests`](add-tests/SKILL.md) | Finds coverage gaps and adds tests to the correct suite. |
| [`aspire`](aspire/SKILL.md) | Routes Aspire work to the appropriate task-specific workflow. |
| [`aspire-deployment`](aspire-deployment/SKILL.md) | Publishes or deploys Aspire apps to supported targets. |
| [`aspire-init`](aspire-init/SKILL.md) | Initializes Aspire in a new or existing repository. |
| [`aspire-monitoring`](aspire-monitoring/SKILL.md) | Inspects Aspire logs, traces, metrics and resource health. |
| [`aspire-orchestration`](aspire-orchestration/SKILL.md) | Manages the Aspire AppHost lifecycle and resource operations. |
| [`aspireify`](aspireify/SKILL.md) | Wires resources, service defaults and telemetry into an existing AppHost. |
| [`ca-review`](ca-review/SKILL.md) | Reviews pending changes against the repository's Clean Architecture conventions. |
| [`fix-build`](fix-build/SKILL.md) | Triages warnings-as-errors build failures and fixes their causes. |
| [`fluentui-blazor-usage`](fluentui-blazor-usage/SKILL.md) | Provides coding patterns for Microsoft Fluent UI Blazor v5. |
| [`generate-commit`](generate-commit/SKILL.md) | Generates a Conventional Commit message from staged changes. |
| [`migration`](migration/SKILL.md) | Adds and reviews an EF Core migration with the required context selection. |
| [`playwright-cli`](playwright-cli/SKILL.md) | Automates browser interaction and Playwright-based testing. |
| [`verify`](verify/SKILL.md) | Runs the definition-of-done build and test gate, reporting skipped suites honestly. |

`verify`, `migration` and `fix-build` are also exposed to VS Code Copilot Chat as `/verify`,
`/migration` and `/fix-build` via [`../prompts/`](../prompts), which points back to each skill so
each workflow has one authoritative body.

## Adding a skill

Keep each skill focused on a workflow: provide `name` and `description` frontmatter, numbered steps,
and a report section that says what to output. Link to a relevant
`../instructions/*.instructions.md` file rather than restating policy. State what the skill must not
do, especially where a helpful agent might overstep.
