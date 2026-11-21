#!/bin/bash
# Run only in an expendable Debian container; never on the homelab.
set -euo pipefail
[[ -f /.dockerenv && ${HAPPYMEAL_DEPLOY_TEST:-} == 1 ]] || exit 2
mkdir -p /run/sshd /home/juampi/happymeal /usr/local/bin
ssh-keygen -A >/dev/null
ssh-keygen -q -t ed25519 -N '' -f /tmp/test-key
useradd --system --create-home --home-dir /var/lib/happymeal-deploy --shell /bin/sh --password '*' happymeal-deploy
mkdir -p /var/lib/happymeal-deploy/.ssh
printf 'restrict,command="/usr/local/sbin/happymeal-ssh-dispatch" %s\n' "$(cat /tmp/test-key.pub)" \
  > /var/lib/happymeal-deploy/.ssh/authorized_keys
printf 'POSTGRES_PASSWORD=test-only\nHAPPYMEAL_TAG=old\n' > /home/juampi/happymeal/.env
cat > /usr/local/bin/docker <<'MOCK'
#!/bin/sh
case "$*" in
  'compose version') echo 'Docker Compose test double' ;;
  *'com.docker.compose.project'*) echo happymeal ;;
  *'State.Health.Status'*) echo healthy ;;
  *'config --quiet') exit 0 ;;
  *) echo "Unexpected Docker mutation: $*" >&2; exit 1 ;;
esac
MOCK
cat > /usr/local/bin/systemctl <<'MOCK'
#!/bin/sh
[ "$*" = 'reload ssh' ]
MOCK
chmod 755 /usr/local/bin/{docker,systemctl}
sshd -T -C user=juampi,host=localhost,addr=127.0.0.1 > /tmp/admin-before
bash /source/install.sh >/tmp/install-log
sshd -T -C user=juampi,host=localhost,addr=127.0.0.1 > /tmp/admin-after
cmp /tmp/admin-before /tmp/admin-after
[[ $(stat -c '%U:%a' /etc/happymeal/runtime.env) == root:600 ]]
[[ $(stat -c '%U:%a' /usr/local/sbin/happymeal-deploy) == root:755 ]]
[[ $(stat -c '%U:%a' /etc/sudoers.d/happymeal-deploy) == root:440 ]]
if runuser -u happymeal-deploy -- sudo -n /bin/true 2>/dev/null; then exit 1; fi
echo 'PASS: real sudo/sshd configuration, protected files and unchanged administrator settings'

# A real SSH connection proves account login works but cannot obtain a shell.
/usr/sbin/sshd -p 2222
printf '[127.0.0.1]:2222 %s\n' "$(cat /etc/ssh/ssh_host_ed25519_key.pub)" > /tmp/known-hosts
set +e
ssh -T -p 2222 -i /tmp/test-key -o IdentitiesOnly=yes -o BatchMode=yes \
  -o StrictHostKeyChecking=yes -o UserKnownHostsFile=/tmp/known-hosts \
  happymeal-deploy@127.0.0.1 id >/tmp/ssh-output 2>&1
code=$?
set -e
[[ $code == 2 ]]
grep -q 'Only deploy' /tmp/ssh-output
echo 'PASS: SSH key authenticates and arbitrary command is rejected'

cp /etc/happymeal/runtime.env /tmp/runtime-before
printf 'POSTGRES_PASSWORD=changed-in-old-location\n' > /home/juampi/happymeal/.env
bash /source/install.sh >/tmp/install-log
cmp /tmp/runtime-before /etc/happymeal/runtime.env
echo 'PASS: repeated installation preserves the protected runtime configuration'
