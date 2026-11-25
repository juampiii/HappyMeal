# Validación de refinar-historia

**Fecha:** 2026-11-25.

## Comprobaciones realizadas

- `quick_validate.py` de `skill-creator`: resultado `Skill is valid!` tanto para
  `Happymeal-BE/skills/refinar-historia` como para la entrada de Claude Code.
- Enlaces relativos de la entrada y del índice: todos resuelven a archivos existentes.
- Revisión manual de las instrucciones con los escenarios siguientes: se han
  recorrido las decisiones del flujo, sin ejecutar un modelo de Claude Code.
  Esta revisión comprueba cobertura escrita, no demuestra comportamiento en una
  conversación real ni operaciones de guardado realizadas por Claude.

El entorno no dispone del ejecutable `claude`. Los escenarios quedan preparados
para repetirlos en una sesión real. Usar un checkout de prueba y comenzar Claude
Code desde `Happymeal-BE`; los archivos generados deben quedar en `docs/stories/`
desde la raíz Git, no en `Happymeal-BE/docs/stories/`.

## 1. Idea ambigua, confirmación parcial y pausa

Invocación:

```text
/refinar-historia Quiero organizar mejor mis comidas.
```

Comprobar que resume en dos o tres frases y pregunta solo lo necesario, con un
máximo de cinco preguntas. No debe decidir por su cuenta el rol, el beneficio,
el periodo de planificación ni reglas sobre recetas o nutrición.

Responder únicamente al rol y al beneficio:

```text
Lo usaré yo para decidir qué comer sin tener que improvisar cada día.
```

Comprobar que actualiza el borrador, conserva el resto pendiente y no repite lo
resuelto. Si ofrece propuestas explícitas, responder «ok»: solo esas propuestas
pueden pasar a confirmadas; preguntas sin respuesta siguen abiertas.

Mientras quede una decisión de negocio bloqueante, responder:

```text
Suficiente por hoy, termina aunque falte concretar lo demás.
```

Resultado a comprobar: mantiene el archivo `borrador-<nombre>.md` en estado
`Pendiente de refinar`, identifica bloqueos y muestra el contenido completo en un
único bloque Markdown. La orden de terminar no confirma propuestas.

**Revisión de instrucciones:** cubierto por Inicio y reanudación, Rondas de
refinamiento 1–4 y Preparación, pausa y entrega. **Ejecución real:** pendiente.

## 2. Historia demasiado grande

Invocación:

```text
/refinar-historia Como persona que cocina en casa, quiero planificar mis menús,
generar la lista de la compra, comprar en supermercados y compartir la planificación
con mi familia, para ahorrar tiempo al organizar nuestras comidas.
```

Comprobar que utiliza INVEST para detectar el tamaño y las dependencias, propone
entregas con valor independiente y solicita confirmar la división. No debe crear
varias historias automáticamente, inventar integración con supermercados ni
diseñar pagos, endpoints o almacenamiento.

Responder:

```text
Empecemos solo por planificar un menú para mí. Dejamos compra y compartir fuera.
```

Resultado a comprobar: refina esa entrega, conserva el objetivo confirmado y deja
las otras capacidades fuera de alcance. Pide aclarar las reglas que sigan abiertas;
no da por resueltas las dudas del menú por haber confirmado su división.

**Revisión de instrucciones:** cubierto por Fuentes y alcance, Rondas 5–6 y la
condición de preparación. **Ejecución real:** pendiente.

## 3. Retomar un borrador y finalizar sin duplicados

Crear en el checkout de prueba `docs/stories/borrador-consultar-menu.md` con este
contenido aportado como datos del usuario para la prueba:

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
  sin menú. No quedan otras dudas de negocio en este escenario.
```

Invocar:

```text
/refinar-historia Retoma docs/stories/borrador-consultar-menu.md
```

Comprobar que recupera las decisiones sin volver a preguntar rol, periodo o
exclusiones, y presenta las propuestas aún pendientes. Responder «ok».

Resultado a comprobar: incorpora P1 y P2 como reglas y criterios observables,
elimina esas dudas, marca `Lista para planificación técnica` y renombra a
`docs/stories/consultar-menu.md`. Solo debe quedar un documento vigente y su
contenido debe coincidir con el bloque Markdown completo del chat.

Variantes a repetir por separado desde el borrador original:

- **Contradicción:** responder «También quiero ver el menú de la semana que viene».
  Debe aclarar el cambio de alcance antes de sustituir la decisión sobre el periodo.
- **Colisión:** crear antes un `consultar-menu.md` que represente otra historia.
  No debe sobrescribirlo al finalizar ni perder las decisiones del borrador.

**Revisión de instrucciones:** cubierto por Inicio y reanudación 4–5, Rondas 3–4,
Rondas 6 y las reglas de renombrado y entrega. **Ejecución real:** pendiente.

## Comprobaciones comunes durante la sesión real

Revisar las herramientas utilizadas: limitar las lecturas al documento seleccionado
y a las instrucciones de la skill; no consultar código ni documentación de negocio
para completar requisitos. Verificar el guardado tras cada ronda, el español en
preguntas y documentos, y que no haya publicación externa ni diseño técnico.
