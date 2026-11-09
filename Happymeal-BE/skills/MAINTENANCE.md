# SkillKit maintenance

## Migration inventory

| Skill | Shared content |
| --- | --- |
| `unit-tests` (pilot) | Unit testing conventions and six relative references covering test structure, style, assertions, domain and application tests, and pitfalls. |
| `architecture` | Architecture rules and five relative references covering the project map, layers, validation, runtime and feature workflow. |
| `integration-tests` | Integration testing guidance and four relative references covering infrastructure, test writing, async effects and pitfalls. |

All three skills use standard `name` and `description` front matter. Their references link within their own skill folders. The inventory found no Claude-only skill metadata, tool calls or bundled scripts. The existing `.claude/docs/` files are historical implementation plans outside the skill catalog.

Claude Code discovers entries in `.claude/skills/` and imports the shared backend instructions from `AGENTS.md` through `CLAUDE.md`. Codex discovers entries in `.agents/skills/` and reads `AGENTS.md` directly. Each entry contains the discovery metadata and a relative Markdown link to the shared instructions. Keep agent-specific instructions in the corresponding entry point.

The entries and their Markdown links work in a Windows checkout with Git's `core.symlinks=false`. Both tools can follow directory symlinks, but Git materializes committed symlinks as text files under that configuration.

## Add or update a skill

1. Edit `skills/<skill-name>/SKILL.md` and its resources. Use a lowercase, hyphenated directory name matching the `name` in front matter and include a `description` that explains when to use the skill.
2. Keep supporting files inside the skill folder and link to them using paths relative to the file that references them. Update [the index](INDEX.md) when adding, renaming or removing a skill.
3. When adding or renaming a skill, give each tool a `<skill-name>/SKILL.md` entry with matching discovery metadata and a link to `../../../skills/<skill-name>/SKILL.md`. Update the entries if the name, description or link target changes. Changes to the shared instructions and resources take effect through the links.

## Verify discovery and references

Open a fresh session from `Happymeal-BE/` in each tool. In Claude Code, invoke `/unit-tests`. In Codex, open `/skills` and select `$unit-tests`. Ask for the test naming pattern and confirm the agent reads the shared `skills/unit-tests/SKILL.md`, then its `references/test-style.md`. For future skills with scripts, verify that both agents can run them from the shared skill folder using the documented interpreter.
