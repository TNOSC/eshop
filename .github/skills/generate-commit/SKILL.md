---
name: generate-commit
description: Generate a Conventional Commits v1.0 message from the staged git changes. Use when asked to write or suggest a commit message for staged changes.
---

# Generate Commit Message

Create a commit message from the changes staged in the current repository.

## Steps

1. Run `git diff --cached` and inspect the complete staged diff. Do not use unstaged or untracked changes to determine the message.
2. Choose the commit type that best describes the staged changes:
   - `feat`: add a user-facing or system capability.
   - `fix`: correct a bug.
   - `docs`: change documentation only.
   - `style`: make formatting-only changes without changing behavior.
   - `refactor`: restructure code without changing behavior.
   - `perf`: improve performance.
   - `test`: add or update tests without changing production behavior.
   - `build`: change the build system or dependencies.
   - `ci`: change CI/CD configuration or workflows.
   - `chore`: perform other maintenance.
3. Add a scope in parentheses only when the staged changes clearly concern one component or area.
4. Write an imperative, concise subject in the form `<type>(optional-scope): short description`. Keep the entire subject line under 72 characters and omit terminal punctuation.
5. Add an optional body after a blank line only when it usefully explains what changed or why. Keep it factual and specific to the staged diff.
6. If the type is unclear, use `chore`.

## Output

Return only the final commit message: no Markdown, code fences, labels, explanation, or surrounding commentary. Do not run `git commit` or modify the index.
