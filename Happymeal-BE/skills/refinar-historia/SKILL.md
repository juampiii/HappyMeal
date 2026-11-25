---
name: refinar-historia
description: Crear o retomar historias de usuario desde una idea o un borrador mediante preguntas de negocio en español. Guarda el refinamiento y entrega Markdown para una issue manual; no diseña ni implementa la solución técnica.
---

# Refinar una historia de usuario

Transforma la información del usuario en una historia clara y verificable desde
negocio, preparada para su posterior planificación técnica. Trabaja íntegramente
en español, incluidos preguntas, propuestas y archivos.

## Fuentes y alcance de la v1

- Usa solo la información aportada por el usuario y el documento de refinamiento
  que se esté retomando. No deduzcas reglas de negocio de código, documentación
  del repositorio, otras historias, issues, una wiki ni fuentes externas.
- Puedes localizar el directorio de guardado, listar sus archivos y leer el
  borrador o historia seleccionados. Esta operación no autoriza explorar el
  repositorio para completar contexto de negocio.
- Define comportamiento observable: roles, permisos, resultados, límites y qué
  ocurre ante errores cuando afecten al negocio. No diseñes endpoints, entidades,
  bases de datos, arquitectura ni código, ni estimes esfuerzo de desarrollo.
- Entrega Markdown para copiar manualmente. No crees issues ni publiques en
  GitHub Projects, Azure DevOps u otros servicios. La consulta de una wiki queda
  fuera de esta versión.

## Inicio y reanudación

1. Resuelve `docs/stories/` desde la raíz Git del proyecto, aunque la sesión se
   inicie en `Happymeal-BE`. Si no puedes determinar la raíz, pregunta por el
   directorio del proyecto antes de guardar. Crea `docs/stories/` cuando haga falta.
2. Si el usuario indica un borrador o historia, lee ese documento. En otro caso,
   lista los nombres existentes y comprueba si hay uno relacionado antes de crear
   otro. Si hay varias coincidencias o no está clara su identidad, pregunta cuál
   retomar o si se trata de una historia nueva; no leas todas las historias.
3. Para una historia nueva, elige un nombre breve en minúsculas, sin tildes y con
   guiones, basado en la idea: `borrador-<nombre>.md`. No inventes números de issue.
   Antes de escribir, comprueba que no existe; si el nombre ya está ocupado por
   otra historia, usa un nombre distinto sin sobrescribirla.
4. Resume lo entendido en dos o tres frases. Al retomar, recupera decisiones
   confirmadas, propuestas pendientes y dudas abiertas. No vuelvas a preguntar
   decisiones resueltas salvo contradicción o cambio explícito del usuario.
5. Si una respuesta nueva contradice una decisión previa y no expresa claramente
   un cambio, pregunta cuál debe prevalecer. Hasta resolverlo, conserva el conflicto
   como duda bloqueante; no sustituyas silenciosamente la decisión.

## Rondas de refinamiento

1. Identifica qué falta para concretar rol, capacidad, beneficio, alcance y reglas
   de negocio. Pregunta hasta cinco cuestiones por ronda, ordenadas por impacto;
   solo las necesarias. Incluye camino feliz, errores y casos límite pertinentes,
   sin añadir escenarios artificiales.
2. Cuando el contexto lo permita, ofrece respuestas propuestas y márcalas como
   **pendientes de confirmación**. No presentes ejemplos ni propuestas como reglas.
   Si no hay base suficiente para proponer una respuesta, pregunta sin inventarla.
3. Explica que un «ok» confirma las propuestas explícitas de la ronda actual. No
   lo uses para resolver preguntas sin respuesta ni para aprobar propuestas de
   rondas anteriores que no hayas vuelto a presentar. Una respuesta parcial solo
   confirma lo contestado; conserva el resto pendiente. Una orden de parar no es
   una confirmación.
4. Guarda el borrador inicial y actualízalo tras cada ronda, antes de esperar la
   siguiente respuesta. Distingue decisiones confirmadas, propuestas pendientes y
   dudas, señalando cuáles bloquean la preparación. Identifica en el documento las
   propuestas de la ronda actual para poder interpretar un «ok» al reanudar.
   Registra lo que falta como pendiente, sin rellenarlo con supuestos.
5. Revisa INVEST como guía: independencia, posibilidad de negociar alcance, valor,
   información suficiente para que el equipo valore la estimabilidad, tamaño
   acotado y criterios comprobables. No garantices estimabilidad ni asignes puntos
   o tiempos. Si la historia es demasiado grande, propone entregas con valor
   independiente y pide elegir o confirmar la división antes de crear historias
   separadas. Conserva confirmaciones comunes y dudas específicas de cada entrega.
6. Continúa con las dudas pendientes hasta que la historia esté preparada o el
   usuario quiera parar. Al actualizar, comprueba que las reglas y los criterios
   de aceptación reflejan las mismas decisiones y retira propuestas descartadas.

## Preparación, pausa y entrega

Una historia está **Lista para planificación técnica** cuando identifica usuario,
capacidad y valor, tiene alcance y reglas de negocio suficientemente concretos,
criterios de aceptación observables y ninguna decisión de negocio bloqueante sin
confirmar. Son bloqueantes las dudas o propuestas que puedan cambiar quién puede
hacer qué, el beneficio, el alcance, las reglas o el resultado aceptable. Una duda
puramente técnica se reserva para planificación y no obliga a diseñar aquí la
implementación. No inventes requisitos para completar la plantilla.

- Si el usuario para y quedan bloqueos, entrega **Pendiente de refinar**, conserva
  `borrador-<nombre>.md` y destaca qué decisiones impiden cerrar. Si pide terminar
  igualmente, no marques la historia como lista mientras sigan esos bloqueos.
- Al quedar lista, actualiza el contenido y renombra el borrador a `<nombre>.md`
  dentro de `docs/stories/`, dejando un solo documento vigente. Comprueba antes que
  el destino está libre; si existe, resuelve con el usuario si se trata de la misma
  historia o elige otro nombre para una historia distinta. No sobrescribas el
  archivo existente ni borres un borrador sin verificar que su contenido se conserva.
- Si se reabre una historia final y aparecen bloqueos de negocio, conviértela de
  nuevo en borrador con las mismas comprobaciones de destino. Si no hay bloqueos,
  actualiza el mismo archivo final.
- No afirmes que has guardado si falla la escritura. Conserva el contenido en el
  chat y explica qué falta para persistirlo.
- En la entrega final o al pausar, indica ruta y estado y muestra el documento
  completo en un único bloque de código `markdown`, listo para copiar. Durante las
  rondas basta con el resumen, preguntas y ubicación del borrador actualizado.

## Plantilla del documento

Adapta el contenido sin omitir los apartados siguientes. En un borrador, marca los
datos desconocidos como pendientes; en una historia lista no dejes marcadores sin
resolver. Incluye dependencias solo cuando correspondan; distingue las confirmadas
de las que requieren aclaración. Las notas de planificación técnica pueden quedar
en dudas abiertas, identificadas como no bloqueantes para el refinamiento de negocio.

```markdown
# <Título>

**Estado:** Pendiente de refinar / Lista para planificación técnica

## Historia de usuario

Como <rol>, quiero <capacidad>, para <beneficio>.

## Contexto y problema que resuelve

<Situación actual y resultado esperado, aportados por el usuario.>

## Reglas de negocio

<Reglas confirmadas. No incluir propuestas sin confirmar como requisitos.>

## Criterios de aceptación

- <Resultado observable y verificable. Usar Dado/Cuando/Entonces si aporta claridad.>

## Fuera de alcance

<Exclusiones confirmadas. Si se proponen nuevas, registrarlas como propuestas.>

## Decisiones confirmadas

- <Decisión y motivo cuando se conozca.>

## Propuestas pendientes

<Propuestas numeradas e identificación de las presentadas en la ronda actual,
o «Ninguna». No confundirlas con decisiones confirmadas.>

## Dudas abiertas

<Preguntas sin resolver, indicando si bloquean el cierre y por qué, o «Ninguna».>

## Dependencias

<Solo cuando corresponda: dependencia y su efecto en la historia.>
```
