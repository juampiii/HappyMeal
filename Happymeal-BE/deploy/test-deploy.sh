#!/bin/bash
# Run ONLY inside the disposable container described in README.md.
set -euo pipefail
[[ -f /.dockerenv && ${HAPPYMEAL_DEPLOY_TEST:-} == 1 ]] || exit 2
install -d /etc/happymeal /usr/local/bin /usr/local/sbin
touch /etc/happymeal/runtime.env /etc/happymeal/compose.testing.yml
cp /source/happymeal-deploy /usr/local/sbin/happymeal-deploy
chmod 755 /usr/local/sbin/happymeal-deploy
cat > /usr/local/bin/docker <<'MOCK'
#!/bin/bash
set -eu
printf '%s\n' "$*" >> /calls
scenario=$(cat /scenario)
case "$*" in
  *'inspect -f {{.State.Health.Status}}'*) echo healthy ;;
  *'inspect -f {{.State.Running}}'*) echo true ;;
  *'inspect -f {{.Config.Image}}'*) echo happymeal-api:previous ;;
  *' ps -a -q api') echo previous-container ;;
  *' pull api migrate') [[ $scenario != pull-fail ]] ;;
  *' exec -T postgres pg_dump '*) [[ $scenario != backup-fail ]]; echo dump ;;
  *' exec -T postgres pg_restore --list') cat >/dev/null ;;
  *' run --rm --no-deps migrate') [[ $scenario != migration-fail ]] ;;
esac
MOCK
cat > /usr/local/bin/curl <<'MOCK'
#!/bin/sh
[ "$(cat /scenario)" != health-fail ]
MOCK
printf '#!/bin/sh\nexit 0\n' > /usr/local/bin/sleep
chmod 755 /usr/local/bin/{docker,curl,sleep}
sha=$(printf 'a%.0s' {1..40})
reset_case() {
  rm -rf /var/lib/happymeal-deploy/state /var/backups/happymeal
  : > /calls
  echo "$1" > /scenario
}
expect_fail() {
  if /usr/local/sbin/happymeal-deploy "$@" >/test-output 2>&1; then
    echo 'Expected deployment failure' >&2; exit 1
  fi
}
reset_case success
for run in 1 2 3 4; do /usr/local/sbin/happymeal-deploy "$sha" "$run" >/test-output; done
[[ $(find /var/backups/happymeal -name '*.dump' | wc -l) == 3 ]]
[[ -f /var/lib/happymeal-deploy/state/current ]]
[[ ! -f /var/lib/happymeal-deploy/state/needs-recovery ]]
: > /calls
expect_fail "$sha" 3
[[ ! -s /calls ]]
expect_fail '../invalid' 5
[[ ! -s /calls ]]
echo 'PASS: success, retention, stale runs and input validation'

reset_case pull-fail
expect_fail "$sha" 1
! grep -q ' stop ' /calls
echo 'PASS: failed download does not stop API'

reset_case backup-fail
expect_fail "$sha" 1
grep -q '^start previous-container$' /calls
! grep -q ' run --rm' /calls
[[ ! -f /var/lib/happymeal-deploy/state/needs-recovery ]]
echo 'PASS: failed backup restarts previous API without migrations'

for scenario in migration-fail health-fail; do
  reset_case "$scenario"
  expect_fail "$sha" 1
  [[ -f /var/lib/happymeal-deploy/state/needs-recovery ]]
  ! grep -q '^start ' /calls
  : > /calls
  expect_fail "$sha" 2
  [[ ! -s /calls ]]
  echo "PASS: $scenario requires recovery and blocks subsequent deployments"
done

cat > /usr/local/bin/sudo <<'MOCK'
#!/bin/sh
printf '%s\n' "$*" > /sudo-call
MOCK
# Dispatch uses the absolute system sudo path.
cp /usr/local/bin/sudo /usr/bin/sudo
chmod 755 /usr/bin/sudo
SSH_ORIGINAL_COMMAND="deploy $sha 5" bash /source/happymeal-ssh-dispatch
grep -q -- "-n /usr/local/sbin/happymeal-deploy $sha 5" /sudo-call
rm /sudo-call
for command in "deploy $sha 5; id" "deploy $sha 5 extra" '' 'bash' 'deploy ../../bad 1'; do
  if SSH_ORIGINAL_COMMAND="$command" bash /source/happymeal-ssh-dispatch 2>/dev/null; then exit 1; fi
done
[[ ! -f /sudo-call ]]
echo 'PASS: SSH dispatch accepts only the exact deployment grammar'

cat > /usr/local/bin/docker <<'MOCK'
#!/bin/bash
set -eu
echo "$*" >> /publish-calls
scenario=$(cat /publish-scenario)
if [[ $1 == manifest ]]; then
  case "$scenario" in
    existing) exit 0 ;;
    partial) [[ $* != *happymeal-api:* ]] || exit 0 ;;
    unavailable) echo 'registry connection refused' >&2; exit 1 ;;
  esac
  echo 'no such manifest' >&2
  exit 1
fi
[[ $1 == buildx && $2 == build ]]
MOCK
chmod 755 /usr/local/bin/docker
for scenario in missing partial existing; do
  echo "$scenario" > /publish-scenario
  : > /publish-calls
  bash /source/publish-images.sh "$sha" >/test-output
  count=$(grep -c '^buildx build ' /publish-calls || true)
  case "$scenario" in
    missing) [[ $count == 2 ]] ;;
    partial) [[ $count == 1 ]]; grep '^buildx build ' /publish-calls | grep -q 'happymeal-migrate:' ;;
    existing) [[ $count == 0 ]] ;;
  esac
done
echo unavailable > /publish-scenario
: > /publish-calls
if bash /source/publish-images.sh "$sha" >/test-output 2>&1; then exit 1; fi
! grep -q '^buildx build ' /publish-calls
echo 'PASS: publication preserves commit tags, retries partial uploads and fails closed on registry errors'
