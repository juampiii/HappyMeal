#!/bin/bash
set -euo pipefail
[[ $# == 1 && $1 =~ ^[0-9a-f]{40}$ ]] || exit 2
sha=$1
context=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
error_file=$(mktemp)
trap 'rm -f -- "$error_file"' EXIT
for component in api migrate; do
  image="ghcr.io/juampiii/happymeal-$component:sha-$sha"
  if docker manifest inspect "$image" >/dev/null 2>"$error_file"; then
    echo "Already published, preserving $image"
    continue
  fi
  # Fail closed on authentication, network and registry failures; only absence
  # permits a build. A partial publication can be retried without replacing API.
  if ! grep -Eiq 'manifest unknown|no such manifest|not found' "$error_file"; then
    echo "Cannot establish whether $image exists; refusing to overwrite" >&2
    cat "$error_file" >&2
    exit 1
  fi
  target=final
  [[ $component != migrate ]] || target=migrate
  docker buildx build --platform linux/amd64 --target "$target" \
    --label "org.opencontainers.image.source=https://github.com/juampiii/HappyMeal" \
    --label "org.opencontainers.image.revision=$sha" \
    --tag "$image" --push "$context"
done
