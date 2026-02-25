# Claude Code Guidelines

## Commit Message Conventions

This repo uses [Conventional Commits](https://www.conventionalcommits.org/) with [semantic-release](https://semantic-release.gitbook.io/semantic-release/) to automate versioning and releases on merge to `main`.

### Prefixes that trigger a release

| Prefix | Version bump | When to use |
|---|---|---|
| `fix:` | patch (1.0.x) | Bug fixes |
| `feat:` | minor (1.x.0) | New features |
| `BREAKING CHANGE:` (footer) | major (x.0.0) | Breaking API changes |

### Prefixes that do NOT trigger a release

| Prefix | When to use |
|---|---|
| `docs:` | Documentation-only changes |
| `chore:` | Maintenance, dependency updates |
| `refactor:` | Code restructuring with no behavior change |
| `test:` | Adding or updating tests |
| `ci:` | CI/CD configuration changes |
| `style:` | Formatting, whitespace |

### Examples

```
feat: add BMI calculation endpoint
fix: correct calorie formula for metric units
docs: update README with usage examples
chore: upgrade dependencies
```
