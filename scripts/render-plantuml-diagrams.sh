#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DIAGRAM_DIR="${ROOT_DIR}/docs/diagram"
OUT_DIR="${DIAGRAM_DIR}/rendered"

FORMAT="${FORMAT:-png}" # png | svg
VERSION_FILTER="${VERSION_FILTER:-}" # e.g. v1, v2; empty = all

if [[ "${FORMAT}" != "png" && "${FORMAT}" != "svg" ]]; then
  echo "FORMAT must be 'png' or 'svg' (got '${FORMAT}')" >&2
  exit 2
fi

mkdir -p "${OUT_DIR}"

shopt -s nullglob

declare -a inputs=()
declare -a input_basenames=()
for f in "${DIAGRAM_DIR}"/*-v*.puml; do
  base="$(basename "${f}")"
  if [[ -n "${VERSION_FILTER}" && "${base}" != *"-${VERSION_FILTER}."* ]]; then
    continue
  fi
  inputs+=("${f}")
  input_basenames+=("${base}")
done

if [[ ${#inputs[@]} -eq 0 ]]; then
  echo "No inputs found in '${DIAGRAM_DIR}' matching '*-v*.puml'${VERSION_FILTER:+ with version '${VERSION_FILTER}' }." >&2
  exit 1
fi

echo "Rendering ${#inputs[@]} diagram(s) to '${OUT_DIR}' as ${FORMAT}..."

if command -v plantuml >/dev/null 2>&1; then
  # PlantUML CLI (-o is relative to the input directory).
  pushd "${DIAGRAM_DIR}" >/dev/null
  if [[ "${FORMAT}" == "png" ]]; then
    plantuml -tpng -charset UTF-8 -o rendered "${input_basenames[@]}"
  else
    plantuml -tsvg -charset UTF-8 -o rendered "${input_basenames[@]}"
  fi
  popd >/dev/null
  echo "Done (plantuml)."
  exit 0
fi

if command -v docker >/dev/null 2>&1; then
  # Docker fallback (works even when PlantUML isn't installed).
  # We mount the diagram directory and render into /work/rendered.
  user_args=()
  if [[ "$(id -u)" != "0" ]]; then
    user_args=(--user "$(id -u):$(id -g)")
  fi
  docker run --rm \
    "${user_args[@]}" \
    -v "${DIAGRAM_DIR}:/work" \
    -w /work \
    plantuml/plantuml:latest \
    "-t${FORMAT}" \
    -charset UTF-8 \
    -o rendered \
    "${input_basenames[@]}"
  echo "Done (docker)."
  exit 0
fi

echo "Neither 'plantuml' nor 'docker' was found. Install one of them and retry." >&2
exit 1
