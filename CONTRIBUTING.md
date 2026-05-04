# Contributing to WysiMd.Blazor

Thank you for taking the time to contribute!

---

## Getting started

1. Fork the repository and clone your fork.
2. Open `WysiMd.Blazor.sln` in Visual Studio 2022 / Rider / VS Code.
3. Build with `dotnet build`.
4. Run tests (when present) with `dotnet test`.

---

## Branching and commits

| Branch | Purpose |
|---|---|
| `main` | Stable, released code. Only PRs from `dev` or hot-fix branches merge here. |
| `dev` | Active development. Base your feature branches off here. |
| `feature/<name>` | New features. |
| `fix/<name>` | Bug fixes. |

Commit messages should follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add paste-as-markdown support
fix: correct undo stack trimming logic
docs: update toolbar reference table
chore: bump Markdig to x.y.z
```

---

## Submitting a pull request

1. Branch off `dev`.
2. Make focused, single-purpose changes.
3. Update `CHANGELOG.md` under `[Unreleased]`.
4. Open a PR against `dev` with a clear description of what changed and why.
5. Link any related issues.

---

## Coding conventions

- **C#**: follow the existing style (file-scoped namespaces, primary constructors where idiomatic, expression bodies for simple members).
- **Razor**: keep `@code` blocks tidy; HTML structure above, code below.
- **JavaScript**: functions stay inside `window.WysiMdBlazor`. No external dependencies.
- **No comments for obvious code** — only add a comment when the *why* is non-obvious.
- All public types and members should have XML doc comments.

---

## Reporting bugs

Open a [GitHub Issue](../../issues) with:
- .NET version and Blazor hosting model (WASM / Server / Hybrid).
- Minimal reproduction steps.
- Expected vs actual behaviour.

---

## License

By contributing you agree that your contributions will be licensed under the [MIT License](LICENSE).
