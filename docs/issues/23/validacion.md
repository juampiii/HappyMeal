# Validación de refine-feature

**Fecha:** 2026-11-25.

## Estado

- `quick_validate.py` de `skill-creator` dio `Skill is valid!` para la skill compartida
  y para la entrada de Claude Code.
- Los enlaces del índice y de la entrada resuelven a archivos existentes.
- La skill **no se ha ejecutado** en una sesión real de Claude Code. Los escenarios
  siguientes quedan preparados para hacerlo.

## Cómo ejecutarlos

Usar un checkout de prueba y abrir Claude Code desde `Happymeal-BE`. Los archivos deben
quedar en `docs/stories/` desde la raíz Git, no en `Happymeal-BE/docs/stories/`.

Durante cada sesión, comprobar también que:

- solo lee el documento seleccionado y la propia skill, sin consultar código ni
  documentación para completar requisitos;
- guarda el documento tras cada ronda;
- pregunta y escribe en español, porque las peticiones de los escenarios están en
  español. Los títulos y los estados de la plantilla deben salir traducidos
  (`Pendiente de refinar`, `Lista para planificación técnica`);
- no publica nada fuera ni propone diseño técnico.

## 1. Idea ambigua, confirmación parcial y pausa

```text
/refine-feature Quiero organizar mejor mis comidas.
```

Debe resumir en dos o tres frases y hacer un máximo de cinco preguntas. No debe decidir
por su cuenta el rol, el beneficio, el periodo ni reglas sobre recetas o nutrición.

Responder solo al rol y al beneficio:

```text
Lo usaré yo para decidir qué comer sin tener que improvisar cada día.
```

Debe actualizar el documento, conservar el resto pendiente y no repetir lo resuelto. Si
ofrece propuestas explícitas, responder «ok»: solo esas pasan a confirmadas.

Con una decisión bloqueante abierta, responder:

```text
Suficiente por hoy, termina aunque falte concretar lo demás.
```

**Resultado esperado:** el archivo `docs/stories/<nombre>.md` queda en estado
`Pendiente de refinar`, con los bloqueos identificados, y el documento completo se
muestra en un único bloque Markdown. La orden de terminar no confirma propuestas.

## 2. Historia demasiado grande

```text
/refine-feature Como persona que cocina en casa, quiero planificar mis menús,
generar la lista de la compra, comprar en supermercados y compartir la planificación
con mi familia, para ahorrar tiempo al organizar nuestras comidas.
```

Debe usar INVEST para detectar el tamaño, proponer entregas con valor independiente y
pedir que se confirme la división. No debe crear historias que no se hayan confirmado, inventar una
integración con supermercados ni diseñar pagos, endpoints o almacenamiento.

Responder:

```text
Empecemos solo por planificar un menú para mí. Dejamos compra y compartir fuera.
```

**Resultado esperado:** refina solo esa entrega y deja las otras capacidades fuera de
alcance. Sigue preguntando las reglas del menú que estén abiertas.

## 3. Retomar y finalizar una historia

Crear en el checkout de prueba `docs/stories/consultar-menu.md`:

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
/refine-feature Retoma docs/stories/consultar-menu.md
```

Debe recuperar las decisiones sin volver a preguntar rol, periodo ni exclusiones, y
presentar las propuestas pendientes. Responder «ok».

**Resultado esperado:** P1 y P2 pasan a ser reglas y criterios observables (en errores
o casos límite), desaparecen esas dudas y el estado cambia a
`Lista para planificación técnica` en el mismo archivo. No se crea ningún otro
documento y el contenido coincide con el bloque Markdown del chat.

Variantes, repetidas por separado desde el documento original:

- **Contradicción:** responder «También quiero ver el menú de la semana que viene».
  Debe aclarar el cambio de alcance antes de sustituir la decisión sobre el periodo.
- **Nombre ocupado:** pedir una historia nueva cuyo nombre natural sea `consultar-menu`.
  No debe sobrescribir el archivo existente y debe usar otro nombre.
