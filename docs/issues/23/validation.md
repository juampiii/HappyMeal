# Validation of refine-feature

**Date:** 2026-11-25.

## Status

- `quick_validate.py` from `skill-creator` returned `Skill is valid!` for the shared skill
  and for the Claude Code entry.
- The links in the index and in the entry resolve to existing files.
- The scenarios below were run in real Claude Code sessions on 2026-11-25. The results are
  in the [issue README](README.md).

## How to run them

Use a test checkout and open Claude Code from `Happymeal-BE`. The files must end up in
`docs/stories/` from the Git root, not in `Happymeal-BE/docs/stories/`.

The scenario messages are in Spanish on purpose. They are what the user types in the
chat, and they check that the conversation follows the user's language while the
document is written in English.

During each session, also check that it:

- reads only the selected document and the skill itself, without looking at code or
  documentation to complete requirements;
- saves the document after each round, in its own folder with an English name and one
  file per language (`docs/stories/<name>/<name>.en.md`);
- asks questions in Spanish and writes the story in English by default. The template
  headings and the status values come out in English (`Needs refinement`,
  `Ready for technical planning`). When it resumes a story that is in Spanish, it keeps
  Spanish;
- does not publish anything outside the repository and does not propose a technical
  design.

## 1. Ambiguous idea, partial confirmation and pause

```text
/refine-feature Quiero organizar mejor mis comidas.
```

It must summarize in two or three sentences and ask five questions at most. It must not
decide on its own the role, the benefit, the period or rules about recipes or nutrition.

Reply only about the role and the benefit:

```text
Lo usaré yo para decidir qué comer sin tener que improvisar cada día.
```

It must update the document, keep the rest pending and not repeat what is resolved. If it
offers explicit proposals, reply "ok": only those become confirmed.

With a blocking decision still open, reply:

```text
Suficiente por hoy, termina aunque falte concretar lo demás.
```

**Expected result:** the file `docs/stories/<name>/<name>.en.md` stays in status
`Needs refinement`, with the blockers identified, and the full document is shown in a
single Markdown block. The order to finish does not confirm proposals.

## 2. Story that is too big

```text
/refine-feature Como persona que cocina en casa, quiero planificar mis menús,
generar la lista de la compra, comprar en supermercados y compartir la planificación
con mi familia, para ahorrar tiempo al organizar nuestras comidas.
```

It must use INVEST to detect the size, propose deliveries with independent value and ask
the user to confirm the split. It must not create stories that are not confirmed, invent a
supermarket integration or design payments, endpoints or storage.

Reply:

```text
Empecemos solo por planificar un menú para mí. Dejamos compra y compartir fuera.
```

**Expected result:** it refines only that delivery and leaves the other capabilities out of
scope. It keeps asking the menu rules that are still open.

## 3. Resume and finish a story

Create in the test checkout `docs/stories/view-menu/view-menu.es.md`. The story is in
Spanish on purpose, to check that the skill keeps the language of a story it resumes:

```markdown
# Consultar mi menú semanal

**Estado:** Pendiente de refinar

## Historia de usuario

Como persona que cocina en casa, quiero consultar mi menú de la semana actual,
para saber qué comida tengo prevista cada día.

## Contexto y problema que resuelve

El menú ya está planificado. Necesito consultarlo para no improvisar.

## Reglas de negocio

- Consultar solo mi menú de la semana actual, de lunes a domingo.
- Mostrar la comida prevista para cada día, en orden de lunes a domingo.

## Criterios de aceptación

### Camino feliz

- Al consultar una semana con menú completo veo la comida prevista para cada día
  de lunes a domingo.

## Fuera de alcance

- Crear o editar el menú, consultar otras semanas y compartirlo.

## Decisiones confirmadas

- El rol, el beneficio, el periodo, el orden y las exclusiones anteriores.

## Propuestas pendientes

- P1 (ronda actual): si un día no tiene comida prevista, mostrar «Sin planificar».
- P2 (ronda actual): si no hay menú para la semana, mostrar «Semana sin planificar».

## Dudas abiertas

- Bloqueante: confirmar el comportamiento de los días sin comida y de una semana
  sin menú.
```

```text
/refine-feature Retoma docs/stories/view-menu/view-menu.es.md
```

It must recover the decisions without asking again the role, the period or the
exclusions, and present the pending proposals. Reply "ok".

**Expected result:** P1 and P2 become rules and observable criteria (in errors or edge
cases), those doubts disappear and the status changes to ready for technical planning in
the same file, in Spanish. No other document is created and the content matches the
Markdown block in the chat. With this example draft, a new and legitimate question can
appear, for example whether each day has one meal or several. Answer it so the story can
be closed.

Variants, each repeated separately from the original document:

- **Contradiction:** reply "También quiero ver el menú de la semana que viene". It must
  clarify the change of scope before it replaces the decision about the period.
- **Name already used:** ask for a new story whose natural name is `view-menu`. It must not
  overwrite the existing folder and must use another name.
- **Stories related by name:** with another story in `docs/stories/` that has a similar
  name, ask for a new idea. It must judge only by the folder names, not open that story,
  and ask whether to resume it or start a new one.
- **Translation and out-of-date warning:** ask for the English version of a ready story
  and then change the Spanish one. It must create `<name>.en.md` with the same status,
  leave the original untouched, and warn that the English file is out of date without
  translating it by itself.
