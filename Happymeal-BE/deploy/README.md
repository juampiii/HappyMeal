# Despliegue de HappyMeal en el homelab de pruebas

El objetivo es que una PR fusionada en `develop` actualice automáticamente el
entorno `testing` del homelab. Se admite una interrupción breve de la API.
Guardar estos archivos en el repositorio no los instala ni activa el despliegue.

## Configuración realizada y estado actual

Inventario confirmado el **22 de noviembre de 2026**. La instalación del servidor
está completada; **el primer despliegue automático sigue pendiente**. El entorno
de GitHub y sus secretos ya están preparados. Falta habilitar el interruptor de
repositorio y verificar la ejecución completa. Las dos imágenes publicadas para
el commit `04cc9bf4db0314af74b2e445e56a89aa05305139` son **privadas**: la descarga
anónima devuelve `unauthorized`. Antes de activar, hacer públicos ambos paquetes
según lo acordado, o configurar credenciales de lectura en el servidor.

| Dónde | Configuración realizada | Para qué sirve |
|---|---|---|
| GitHub → Settings → Environments → `testing` | Selected branches and tags: solo la rama `develop`, sin etiquetas; sin revisores obligatorios ni espera | El job `deploy` solo puede usar este entorno desde `develop`, sin intervención manual en cada despliegue. |
| Router | Reserva DHCP de `192.168.1.142` y TCP `2222` externo → `192.168.1.142:22` | Mantiene estable el destino local y lleva las conexiones externas al SSH del homelab. |
| Duck DNS | `homepilab-juampi.duckdns.org`; actualización con `homepilab-ddns.timer` | Mantiene el nombre apuntando a la IPv4 pública cuando cambia. El token permanece en el servidor. |
| Homelab | Compose y `runtime.env` en `/etc/happymeal`, propiedad de root | Configuración del despliegue y contraseña de PostgreSQL fuera de GitHub y del repositorio. |
| Homelab | Scripts en `/usr/local/sbin`, permiso sudo limitado y cuenta `happymeal-deploy` restringida | Actions puede solicitar un despliegue, pero no obtener consola ni modificar scripts o Compose. |
| Homelab → systemd | `/etc/systemd/system/homepilab-firewall.service.d/20-public-ssh.conf` | Permite conexiones IPv4 TCP nuevas al puerto **22 interno**, en `wlp4s0` y `enp5s0f1`. El router ya transforma el puerto externo 2222 en 22. |
| Homelab → copias | `/var/backups/happymeal`, retención de tres copias completadas | Permite recuperar datos anteriores a una migración; las copias se generan durante los despliegues. |

Para reaplicar la configuración persistente del firewall, usar como administrador
`systemctl restart homepilab-firewall.service`. No ejecutar directamente el
archivo de reglas original: se omitiría el ajuste del drop-in de systemd. Este
servicio y su ajuste son configuración del homelab, no los instala `install.sh`.

Las secciones siguientes explican cómo reproducir y mantener la instalación.

## Qué hace cada pieza

| Pieza | Dónde se usa | Para qué sirve |
|---|---|---|
| [Workflow de Actions](../../.github/workflows/testing.yml) | GitHub | Prueba las PR y, tras el merge, publica imágenes y solicita el despliegue. |
| `publish-images.sh` | GitHub Actions | Publica API y migraciones en GHCR, sin sobrescribir las etiquetas de commit existentes. |
| `happymeal-deploy` y `compose.testing.yml` | Homelab | Descargan imágenes, guardan una copia, aplican migraciones y actualizan la API. |
| `happymeal-ssh-dispatch` y `happymeal-deploy.sudoers` | Homelab | Limitan la clave de Actions a una orden de despliegue concreta. |
| `install.sh` | Sesión de administrador del homelab | Instala la configuración una vez y permite actualizarla tras revisarla. |
| `test-deploy.sh` y `test-install.sh` | Contenedores desechables | Comprueban fallos del despliegue y la instalación de permisos. |

El servidor ejecuta una copia de los scripts y del Compose instalada por `root`.
Actions no puede sustituirlos ni ejecutar un Compose arbitrario como `root`.
Los cambios de la aplicación solo necesitan imágenes nuevas; los cambios de
estos scripts o de Compose requieren revisión y reinstalación por un administrador.

Las contraseñas, datos, tres copias y estado del despliegue permanecen en el
homelab. Duck DNS mantiene actualizado el nombre público del servidor.

## Cómo se identifica y ejecuta un despliegue

La clave restringida admite únicamente esta orden:

```text
deploy <SHA completo de 40 caracteres> <número de ejecución del workflow>
```

Antes deben existir estas dos imágenes públicas para `linux/amd64`:

- `ghcr.io/juampiii/happymeal-api:sha-<SHA>`
- `ghcr.io/juampiii/happymeal-migrate:sha-<SHA>`

Ambas corresponden al mismo commit. El workflow conserva una etiqueta ya
publicada y, si una publicación quedó a medias, publica solo la imagen que falta.
Hay que proteger `develop` y los permisos de publicación de GHCR: las imágenes
son código que el servidor acepta ejecutar.

El servidor procesa un despliegue cada vez y rechaza números de ejecución
anteriores al último admitido. Hay que usar el contador de un único workflow;
cambiar de workflow requiere que `root` revise ese estado. Una reejecución de
Actions conserva su número original y puede reintentar la misma versión.

El orden del despliegue es:

1. Descargar las dos imágenes antes de interrumpir el servicio.
2. Comprobar el PostgreSQL existente y detener la API, incluido el outbox.
3. Crear una copia completa de la base de HappyMeal en formato personalizado de
   PostgreSQL, comprobar el índice del archivo y conservar las últimas tres
   copias completadas correctamente.
4. Aplicar las migraciones y arrancar la nueva API.
5. Exigir una respuesta HTTP satisfactoria en `/health` y `/api/recipes`.

El script no recrea ni actualiza PostgreSQL y no gestiona otros proyectos.
La preparación inicial de una base vacía es manual.

## 1. Instalar en el servidor

Este paso se hace desde una cuenta de administrador, no desde la clave de Actions.
Antes de instalar, revisar los archivos y confirmar:

- Proyecto Compose existente: `happymeal`.
- Contenedor de PostgreSQL: `happymeal-postgres`, saludable, con su volumen actual.
- Carpeta manual existente: `/home/juampi/happymeal`, con su `.env` del servidor.
- Usuario `happymeal-deploy` y su clave pública restringida ya creados.
- Herramientas: Bash, Docker Compose, curl, flock, sudo y GNU find/coreutils.

Conservar el proyecto y el volumen actuales. No usar `down -v`, cambiar el nombre
del proyecto ni inicializar una base de reemplazo para instalar la automatización.

Desde **PowerShell en el ordenador**, empaquetar y transferir los archivos:

```powershell
tar -czf "$env:TEMP\happymeal-deploy-setup.tgz" -C C:\Repos\HappyMeal\Happymeal-BE deploy
scp "$env:TEMP\happymeal-deploy-setup.tgz" juampi@192.168.1.142:/home/juampi/
```

Desde la **sesión SSH del servidor como root**:

```bash
install -d -m 700 /root/happymeal-deploy-setup
tar -xzf /home/juampi/happymeal-deploy-setup.tgz -C /root/happymeal-deploy-setup
bash /root/happymeal-deploy-setup/deploy/install.sh
```

El instalador no detiene la API, no ejecuta migraciones y no abre puertos del
router. Instala los siguientes archivos protegidos:

| Ruta del servidor | Propietario y modo |
|---|---|
| `/etc/happymeal/` | `root`, `700` |
| `/etc/happymeal/compose.testing.yml` | `root`, `600` |
| `/etc/happymeal/runtime.env` | `root`, `600`; copia del `.env` existente |
| `/usr/local/sbin/happymeal-deploy` y `happymeal-ssh-dispatch` | `root`, `755` |
| `/etc/sudoers.d/happymeal-deploy` | `root`, `440` |

El usuario de despliegue no debe poder modificar esos archivos ni sus directorios.
No necesita pertenecer al grupo Docker. Su entrada de clave pública conserva
`restrict,command="/usr/local/sbin/happymeal-ssh-dispatch"`. Además, el instalador
restringe esa cuenta en SSH a autenticación por clave, comando forzado y sin
reenvíos ni terminal. Valida sudo y la configuración efectiva de SSH antes de
recargar SSH; la configuración se aplica solo a `happymeal-deploy` y conserva
el acceso del administrador.

Si falla una comprobación, revisar el error antes de continuar. La instalación
puede haber copiado algunos archivos antes del fallo. El instalador se puede
repetir y conserva un `runtime.env` ya instalado. Si cambia la contraseña de la
aplicación, actualizar ese archivo como administrador. No copiar su contenido
ni el de una clave privada al chat o al repositorio.

Comprobar la conexión desde **PowerShell**:

```powershell
ssh -T -o BatchMode=yes -o IdentitiesOnly=yes -i "$env:USERPROFILE\.ssh\happymeal-actions-testing" happymeal-deploy@192.168.1.142
```

Debe rechazar la conexión sin orden con `Only deploy ... is accepted` y código
2: el acceso funciona, pero no ofrece una consola. Probar una orden de despliegue
local con un commit publicado antes de abrir el acceso externo.

## 2. Configurar GitHub y publicar las primeras imágenes

Crear el entorno **testing**, restringido a `develop`, sin aprobación manual por
despliegue si queremos automatización completa. Proteger `develop` para exigir PR
y el job `test` del workflow antes del merge.

Dejar la variable de repositorio `HOMELAB_DEPLOY_ENABLED` ausente o en `false`.
Al fusionar el workflow en `develop`, ejecutará las pruebas y publicará las
imágenes, pero omitirá el despliegue. La ejecución manual con `workflow_dispatch`
solo estará disponible si este workflow existe en la rama predeterminada. La
rama predeterminada actual es `main`; no se cambia como parte de esta instalación.

En GitHub > perfil > Packages, cambiar a **Public** los paquetes `happymeal-api`
y `happymeal-migrate`. Los paquetes nuevos de GHCR pueden ser privados aunque el
repositorio sea público. Comprobar su descarga anónima antes de continuar.

## 3. Configurar acceso externo y secretos

Reservar en DHCP la dirección `192.168.1.142` para el homelab. Después de probar
la clave en la LAN, configurar una redirección **TCP 2222 externo → 22 del
homelab**. Es el puerto propuesto; si ya se usa, elegir otro. Esta acción hace
accesible SSH desde Internet y se configura explícitamente en el router.
No abrir PostgreSQL ni la administración del router.

Las **variables** son configuración no confidencial; los **secretos** contienen
credenciales o datos de confianza que GitHub guarda cifrados. Se configuran en
GitHub → Settings → Environments → `testing`. Los cuatro valores siguientes ya
están guardados en ese entorno y los consume el job `deploy` de `testing.yml`:

| Tipo | Nombre | Valor o contenido | Cómo lo utiliza el workflow |
|---|---|---|---|
| Variable del entorno | `HOMELAB_HOST` | `homepilab-juampi.duckdns.org` | Destino de la conexión SSH. |
| Variable del entorno | `HOMELAB_SSH_PORT` | `2222` | Puerto externo de la conexión SSH. |
| Secreto del entorno | `HOMELAB_SSH_KEY` | Clave privada exclusiva de Actions, guardada por el usuario; no se documenta su valor | Autentica al **cliente**, GitHub Actions, como `happymeal-deploy`. |
| Secreto del entorno | `HOMELAB_KNOWN_HOSTS` | Clave pública ED25519 del servidor asociada al nombre y puerto | Verifica la identidad del **servidor**, evitando conectar con otro equipo. |

La huella pública del servidor verificada en la configuración es
`SHA256:zLv3eEf39Fg/w3e5LnnXDKSsr4VHSIXLBScTqnc4izc`. La identidad pública no es
una contraseña. Si se reinstala el servidor y cambia su clave, verificar la nueva
huella desde el propio servidor antes de actualizar `HOMELAB_KNOWN_HOSTS`.

El instalador muestra la clave **pública** ED25519 del servidor. Usarla para
construir una línea como:

```text
[homepilab-juampi.duckdns.org]:2222 ssh-ed25519 AAAA...clave-publica-real...
```

No usar `StrictHostKeyChecking=no` ni confiar en un `ssh-keyscan` sin comparar con
la clave del servidor. La clave privada de Actions está en el ordenador, en
`C:\Users\jprod\.ssh\happymeal-actions-testing`. Introducirla directamente en el
secreto de GitHub; no copiarla al repositorio ni al chat.

La clave personal `homepilab-juampi-personal` autentica al administrador `juampi`
desde su ordenador y **no se usa en Actions**. Su frase de paso tampoco se guarda
en los secretos del workflow. No confundir la clave privada del cliente con la
clave pública del servidor: cumplen funciones distintas.

El `.env` original de `/home/juampi/happymeal` y su copia protegida
`/etc/happymeal/runtime.env` pertenecen al servidor. Compose lee esta última para
configurar PostgreSQL y la API; el workflow no necesita conocer esa contraseña.
El instalador conserva `runtime.env` al reinstalar, por lo que los cambios de
contraseña deben actualizarse allí expresamente, además de aplicarse en la base.

`GITHUB_TOKEN` es otro secreto, generado automáticamente por GitHub para cada
ejecución. El job `publish` lo usa para autenticarse en GHCR con permiso
`packages: write`; no hay que crear un token personal con ese nombre.

## 4. Primera prueba y activación

Probar la conexión desde fuera de la LAN y la descarga de ambas imágenes: Duck DNS
no demuestra por sí solo que la redirección del router funcione. El interruptor
**todavía pendiente de configurar** es:

| Dónde | Tipo y nombre | Valores | Quién lo consume |
|---|---|---|---|
| GitHub → Settings → Secrets and variables → Actions → Variables | Variable de **repositorio** `HOMELAB_DEPLOY_ENABLED` | `true` activa; `false` o ausente omite el despliegue | Condición del job `deploy`, antes de acceder al entorno `testing`. |

No crear este interruptor como variable de entorno: su ámbito de repositorio
permite al workflow leerlo antes de entrar en `testing`. Una vez hechas las
comprobaciones previas, establecer su valor en `true`.

Para probar el commit ya publicado, después de hacer públicos ambos paquetes y
activar el interruptor, abrir la [ejecución del push a develop](https://github.com/juampiii/HappyMeal/actions/runs/44934419370)
y elegir **Re-run all jobs**. Repite las pruebas, conserva las imágenes existentes
y solicita el despliegue del commit `04cc9bf4db0314af74b2e445e56a89aa05305139`.
No necesita cambiar la rama predeterminada. Otra opción es fusionar una nueva PR
a `develop` para publicar y desplegar una versión más reciente.

Si más adelante está disponible Run workflow, seleccionar `develop` y marcar
`deploy`: esa casilla solicita el despliegue manual, pero también necesita el
interruptor en `true`. El resultado esperado es publicación de las dos imágenes y
`Deployment healthy: <commit>`. Desde entonces, cada merge a `develop` ejecuta
pruebas, publicación y despliegue automático.

Tras la primera ejecución, comprobar desde el servidor:

```bash
curl --fail http://127.0.0.1:8091/health
curl --fail http://127.0.0.1:8091/api/recipes
sudo cat /var/lib/happymeal-deploy/state/current
sudo find /var/backups/happymeal -maxdepth 1 -type f -name '*.dump' -printf '%f\n'
```

Los endpoints deben responder correctamente, `current` debe identificar el commit
desplegado y debe existir la copia previa. La retención dejará como máximo tres
copias completadas cuando se hayan realizado suficientes despliegues. Ninguna de
estas comprobaciones equivale a haber ensayado una restauración.

Para suspender nuevos despliegues, poner la variable en `false`. No cancelar una
migración en curso. Las pruebas y publicaciones pueden seguir funcionando.
Cambiar el interruptor no detiene un despliegue ya iniciado ni revierte una
versión: la recuperación se trata en la sección siguiente.

## Estado, copias y recuperación

| Ruta del servidor | Contenido |
|---|---|
| `/var/lib/happymeal-deploy/state/current` | Último commit verificado, imagen anterior y copia previa a migrar. |
| `/var/lib/happymeal-deploy/state/latest-run` | Última ejecución admitida y SHA, incluidos intentos fallidos. |
| `/var/lib/happymeal-deploy/state/needs-recovery` | Bloqueo de nuevos despliegues cuando una migración o verificación falla. |
| `/var/lib/happymeal-deploy/state/migration.log` | Salida de la última migración, legible solo por root y no impresa en CI. |
| `/var/backups/happymeal/*.dump` | Las últimas tres copias completas verificadas de HappyMeal. |

Antes de empezar a migrar, un fallo intenta arrancar el contenedor anterior si
estaba en ejecución. Una vez empezadas las migraciones, cualquier fallo requiere
revisión manual; no se vuelve automáticamente a una imagen ni se restaura la base.

El administrador debe revisar el log de migración, el esquema y los logs de la
API. Antes de restaurar datos, detener una nueva API que haya quedado arrancada.
Elegir una imagen anterior compatible o restaurar la copia previa con
`pg_restore` mediante un procedimiento ensayado. Repetir las dos comprobaciones
HTTP. Eliminar `needs-recovery` solo cuando se conozca el estado de esquema y
aplicación y sea seguro reintentar; no borrarlo automáticamente desde CI.

Volver a una imagen anterior no deshace migraciones. El administrador puede hacer
una recuperación manual deliberada; el script rechaza números de ejecución
antiguos. Conservar ambas imágenes de cada versión en GHCR.

Las copias se guardan en el mismo servidor: no protegen frente a pérdida de disco
o del equipo. Comprobar el índice del archivo no demuestra una restauración
correcta; ensayarla contra una base separada antes de confiar en ella.

## Pruebas y límites de la validación

El workflow ejecuta las pruebas unitarias y de integración antes de publicar
imágenes. Las de integración crean su propio PostgreSQL temporal.

`test-deploy.sh` simula Docker y HTTP, pero ejecuta los scripts reales y comprueba
retención, fallos, orden de ejecuciones, publicación y gramática de órdenes SSH.
No monta el socket de Docker ni se conecta al homelab. Puede ejecutarse desde
`Happymeal-BE`, con Docker activo:

```powershell
docker run --rm --entrypoint sh -e HAPPYMEAL_DEPLOY_TEST=1 --mount "type=bind,source=$((Get-Location).Path)/deploy,target=/source,readonly" postgres:18-alpine -c 'apk add --no-cache bash util-linux coreutils findutils >/dev/null && bash /source/test-deploy.sh'
```

`test-install.sh`, también llamado por el workflow, usa un Debian desechable con
SSH y sudo reales, y simula Docker y la recarga del servicio. Comprueba permisos,
rechazo de órdenes arbitrarias y conservación de la configuración al reinstalar.
Las herramientas de prueba se instalan solo dentro de los contenedores temporales.

Estas comprobaciones no sustituyen la primera ejecución en Actions, las descargas
reales de GHCR, la instalación en el Debian del homelab, la conexión externa ni una
restauración ensayada. El despliegue sigue unido a la sesión SSH y al timeout de
Actions: una interrupción puede requerir recuperación manual.
