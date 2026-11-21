#!/bin/bash
# One-time/admin installation. Does not deploy, migrate, stop or recreate services.
set -euo pipefail
export PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
umask 077
[[ $EUID == 0 ]] || { echo 'Run with sudo bash install.sh' >&2; exit 1; }
source_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
for tool in docker curl flock sudo visudo sshd systemctl; do command -v "$tool" >/dev/null; done
id happymeal-deploy >/dev/null
[[ -s /var/lib/happymeal-deploy/.ssh/authorized_keys ]]
[[ -f /home/juampi/happymeal/.env ]]
[[ -d /etc/ssh/sshd_config.d ]]
sshd -t
docker compose version
[[ $(docker inspect -f '{{ index .Config.Labels "com.docker.compose.project" }}' happymeal-postgres) == happymeal ]]
[[ $(docker inspect -f '{{.State.Health.Status}}' happymeal-postgres) == healthy ]]
# Match blocks in this drop-in are limited to the deployment account.
temporary=$(mktemp -d)
ssh_config=/etc/ssh/sshd_config.d/00-happymeal-deploy.conf
had_config=false
committed=false
[[ ! -f $ssh_config ]] || { cp -- "$ssh_config" "$temporary/previous"; had_config=true; }
cleanup() {
  if [[ $committed == false ]]; then
    if [[ $had_config == true ]]; then cp -- "$temporary/previous" "$ssh_config";
    else rm -f -- "$ssh_config"; fi
  fi
  rm -rf -- "$temporary"
}
trap cleanup EXIT
visudo -cf "$source_dir/happymeal-deploy.sudoers"
install -d -o root -g root -m 700 /etc/happymeal
if [[ ! -e /etc/happymeal/runtime.env ]]; then
  install -o root -g root -m 600 /home/juampi/happymeal/.env /etc/happymeal/runtime.env
fi
chown root:root /etc/happymeal/runtime.env
chmod 600 /etc/happymeal/runtime.env
# Validate the candidate Compose before replacing any installed deployment script.
env -i PATH="$PATH" HOME=/root HAPPYMEAL_TAG=installation-check docker compose \
  --env-file /etc/happymeal/runtime.env -p happymeal -f "$source_dir/compose.testing.yml" config --quiet
install -o root -g root -m 600 "$source_dir/compose.testing.yml" /etc/happymeal/compose.testing.yml
install -o root -g root -m 755 "$source_dir/happymeal-deploy" /usr/local/sbin/happymeal-deploy
install -o root -g root -m 755 "$source_dir/happymeal-ssh-dispatch" /usr/local/sbin/happymeal-ssh-dispatch
install -o root -g root -m 440 "$source_dir/happymeal-deploy.sudoers" /etc/sudoers.d/happymeal-deploy
visudo -c
chown root:root /var/lib/happymeal-deploy /var/lib/happymeal-deploy/.ssh /var/lib/happymeal-deploy/.ssh/authorized_keys
chmod 755 /var/lib/happymeal-deploy /var/lib/happymeal-deploy/.ssh
chmod 644 /var/lib/happymeal-deploy/.ssh/authorized_keys
cat > "$ssh_config" <<'CONFIG'
Match User happymeal-deploy
    AuthenticationMethods publickey
    PasswordAuthentication no
    KbdInteractiveAuthentication no
    ForceCommand /usr/local/sbin/happymeal-ssh-dispatch
    DisableForwarding yes
    PermitTTY no
Match All
CONFIG
chmod 600 "$ssh_config"
sshd -t
effective=$(sshd -T -C user=happymeal-deploy,host=localhost,addr=127.0.0.1)
for required in 'authenticationmethods publickey' 'passwordauthentication no' \
  'kbdinteractiveauthentication no' 'forcecommand /usr/local/sbin/happymeal-ssh-dispatch' \
  'disableforwarding yes' 'permittty no'; do
  grep -Fxq "$required" <<< "$effective" || { echo "SSH setting not effective: $required" >&2; exit 1; }
done
systemctl reload ssh
committed=true
echo 'Installed. Existing API/database unchanged; no deployment has been run.'
echo 'Server ED25519 host key for Actions (public, not a private credential):'
cat /etc/ssh/ssh_host_ed25519_key.pub
