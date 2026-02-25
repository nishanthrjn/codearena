#!/bin/bash
# Placeholder for build-runner-images.sh
#!/usr/bin/env bash
set -euo pipefail

REGISTRY="${REGISTRY:-}"
TAG="${TAG:-latest}"

echo "Building CodeArena runner sandbox images..."

build_image() {
    local name="$1"
    local file="$2"
    local full_name="codearena-runner-${name}:${TAG}"
    echo "→ Building ${full_name} from ${file}"
    docker build \
        --no-cache \
        -t "${full_name}" \
        -f "infra/runner-images/${file}" \
        "infra/runner-images/"
    if [[ -n "$REGISTRY" ]]; then
        docker tag "${full_name}" "${REGISTRY}/${full_name}"
        docker push "${REGISTRY}/${full_name}"
    fi
}

build_image "python"  "Dockerfile.python"
build_image "node"    "Dockerfile.node"
build_image "c-cpp"   "Dockerfile.c_cpp"
build_image "csharp"  "Dockerfile.csharp"

echo "✓ All runner images built"