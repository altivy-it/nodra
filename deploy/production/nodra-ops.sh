#!/usr/bin/env bash
set -Eeuo pipefail

usage() {
  cat <<'EOF'
Nodra VPS operations

Usage:
  nodra-ops.sh deploy-app <frontend|backend|all> <vX.Y.Z> <release-dir> <registry-username>
  nodra-ops.sh rollback-app <registry-username>
  nodra-ops.sh deploy-nginx <release-dir>
  nodra-ops.sh rollback-nginx
  nodra-ops.sh deliver-postgres <release-dir>
  nodra-ops.sh --help

Deploy operations expect Docker Compose v2, the external configuration files
under /userapps/configs/altivynotes, and the shared networks/services to exist.
deploy-app reads a short-lived GHCR read token from stdin; it never accepts the
token as an argument. PostgreSQL delivery only copies the reviewed bootstrap
SQL and never executes SQL or restarts PostgreSQL.

Examples:
  nodra-ops.sh deploy-app all v1.2.3 /userapps/altivynotes/releases/v1.2.3
  nodra-ops.sh deploy-nginx /userapps/altivynotes/releases/v1.2.3

See docs/runbooks/deploy-producao-vps.md for host setup, permissions, secrets,
network requirements, rollback limits, and recovery procedures.
EOF
}

fail() {
  printf 'Erro: %s\n' "$1" >&2
  exit 1
}

if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
  usage
  exit 0
fi

operation="${1:-}"
app_root=/userapps/altivynotes
config_root=/userapps/configs/altivynotes
nginx_root=/userapps/configs/nginx/altivynotes
rollback_root="$app_root/.rollback"

require_command() {
  command -v "$1" >/dev/null 2>&1 || fail "Comando obrigatório ausente: $1"
}

read_config_value() {
  local name="$1"
  local file="$2"
  local line

  [[ -f "$file" ]] || fail "Arquivo de configuração ausente: $file"
  while IFS= read -r line || [[ -n "$line" ]]; do
    case "$line" in
      "$name"=*)
        printf '%s' "${line#*=}"
        return 0
        ;;
    esac
  done < "$file"
  fail "Variável $name ausente em $file"
}

check_config_files() {
  local file mode keycloak_proxy_ip backup_retention_days octet
  local -a ip_octets
  [[ -f "$config_root/compose.env" ]] || fail "Crie $config_root/compose.env conforme o runbook."
  for file in bff.env api.env worker.env migrator.env; do
    [[ -f "$config_root/$file" ]] || fail "Arquivo externo obrigatório ausente: $config_root/$file"
    mode="$(stat -c '%a' "$config_root/$file")"
    if (( (8#$mode & 007) != 0 )); then
      fail "$config_root/$file permite acesso a outros usuários; restrinja as permissões."
    fi
  done
  [[ -f "$config_root/altivy-data-protection.pfx" ]] ||
    fail "Certificado de Data Protection ausente: $config_root/altivy-data-protection.pfx"

  [[ "$(read_config_value ALTIVY_DATA_ROOT "$config_root/compose.env")" == /userapps/var/altivynotes ]] ||
    fail 'ALTIVY_DATA_ROOT deve ser /userapps/var/altivynotes.'
  backup_retention_days="$(read_config_value ALTIVY_BACKUP_RETENTION_DAYS "$config_root/compose.env")"
  if [[ ! "$backup_retention_days" =~ ^[1-9][0-9]{0,3}$ ]] || (( backup_retention_days > 3650 )); then
    fail 'ALTIVY_BACKUP_RETENTION_DAYS deve estar entre 1 e 3650.'
  fi
  [[ "$(read_config_value ALTIVY_CONFIG_ROOT "$config_root/compose.env")" == "$config_root" ]] ||
    fail "ALTIVY_CONFIG_ROOT deve ser $config_root."
  keycloak_proxy_ip="$(read_config_value ALTIVY_KEYCLOAK_PROXY_IP "$config_root/compose.env")"
  [[ "$keycloak_proxy_ip" =~ ^([0-9]{1,3}\.){3}[0-9]{1,3}$ ]] ||
    fail 'ALTIVY_KEYCLOAK_PROXY_IP deve ser o IPv4 interno do NGINX compartilhado.'
  IFS='.' read -r -a ip_octets <<< "$keycloak_proxy_ip"
  [[ "${#ip_octets[@]}" -eq 4 ]] || fail 'ALTIVY_KEYCLOAK_PROXY_IP não é um IPv4 válido.'
  for octet in "${ip_octets[@]}"; do
    (( 10#$octet <= 255 )) || fail 'ALTIVY_KEYCLOAK_PROXY_IP não é um IPv4 válido.'
  done
}

compose() {
  docker compose \
    --project-name altivynotes \
    --env-file "$config_root/compose.env" \
    --env-file "$app_root/release.env" \
    --file "$app_root/compose.yaml" \
    "$@"
}

validate_release_dir() {
  local release_dir="$1"
  local resolved
  resolved="$(realpath -e "$release_dir")" || fail "Diretório da release ausente: $release_dir"
  [[ "$resolved" == "$app_root"/releases/* ]] || fail 'Diretório de artefatos fora de /userapps/altivynotes/releases.'
  printf '%s' "$resolved"
}

validate_image_reference() {
  local name="$1"
  local value="$2"
  local expected_image
  case "$name" in
    ALTIVY_WEB_IMAGE) expected_image=nodra-web ;;
    ALTIVY_API_IMAGE) expected_image=nodra-api ;;
    ALTIVY_BFF_IMAGE) expected_image=nodra-bff ;;
    ALTIVY_WORKER_IMAGE) expected_image=nodra-worker ;;
    ALTIVY_MIGRATIONS_IMAGE) expected_image=nodra-migrations ;;
    *) fail "Nome de imagem não permitido: $name" ;;
  esac
  [[ "$value" =~ ^ghcr\.io/lzocateli/$expected_image@sha256:[a-f0-9]{64}$ ]] ||
    fail "Referência de imagem inválida para $name no manifesto de release."
}

manifest_image() {
  local name="$1"
  local manifest="$2"
  local value
  value="$(read_config_value "$name" "$manifest")"
  validate_image_reference "$name" "$value"
  printf '%s' "$value"
}

set_manifest_value() {
  local file="$1"
  local name="$2"
  local value="$3"
  local temp
  temp="$(mktemp "$app_root/.release.env.XXXXXX")"
  if [[ -f "$file" ]]; then
    awk -v name="$name" -v value="$value" '
      BEGIN { found = 0 }
      index($0, name "=") == 1 { print name "=" value; found = 1; next }
      { print }
      END { if (!found) print name "=" value }
    ' "$file" > "$temp"
  else
    printf '%s=%s\n' "$name" "$value" > "$temp"
  fi
  chmod 600 "$temp"
  mv -f "$temp" "$file"
}

prepare_app_release() {
  local release_tag="$1"
  local release_dir="$2"
  local name value
  [[ "$release_tag" =~ ^v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$ ]] ||
    fail 'A versão deve ser uma tag SemVer iniciada por v.'
  [[ -d "$release_dir" ]] || fail "Diretório da release ausente: $release_dir"
  [[ "$(validate_release_dir "$release_dir")" == "$app_root/releases/$release_tag" ]] ||
    fail 'O diretório da release não corresponde à tag SemVer informada.'
  [[ -f "$release_dir/compose.yaml" ]] || fail "compose.yaml ausente em $release_dir"
  [[ -f "$release_dir/images.env" ]] || fail "images.env ausente em $release_dir"
  for name in ALTIVY_WEB_IMAGE ALTIVY_API_IMAGE ALTIVY_BFF_IMAGE ALTIVY_WORKER_IMAGE ALTIVY_MIGRATIONS_IMAGE; do
    manifest_image "$name" "$release_dir/images.env" >/dev/null
  done
  if [[ ! -f "$app_root/release.env" && "$component" != all ]]; then
    fail 'O primeiro deploy precisa selecionar all para inicializar todos os digests de serviço.'
  fi

  mkdir -p "$rollback_root"
  if [[ -f "$app_root/release.env" && -f "$app_root/compose.yaml" ]]; then
    cp -p "$app_root/release.env" "$rollback_root/release.env"
    cp -p "$app_root/compose.yaml" "$rollback_root/compose.yaml"
    : > "$rollback_root/available"
  else
    rm -f "$rollback_root/available"
  fi

  install -m 0644 "$release_dir/compose.yaml" "$app_root/compose.yaml.new"
  mv -f "$app_root/compose.yaml.new" "$app_root/compose.yaml"
  if [[ ! -f "$app_root/release.env" ]]; then
    install -m 0600 "$release_dir/images.env" "$app_root/release.env"
  fi

  case "$component" in
    frontend)
      name=ALTIVY_WEB_IMAGE
      value="$(manifest_image "$name" "$release_dir/images.env")"
      set_manifest_value "$app_root/release.env" "$name" "$value"
      ;;
    backend)
      for name in ALTIVY_API_IMAGE ALTIVY_BFF_IMAGE ALTIVY_WORKER_IMAGE ALTIVY_MIGRATIONS_IMAGE; do
        value="$(manifest_image "$name" "$release_dir/images.env")"
        set_manifest_value "$app_root/release.env" "$name" "$value"
      done
      ;;
    all)
      for name in ALTIVY_WEB_IMAGE ALTIVY_API_IMAGE ALTIVY_BFF_IMAGE ALTIVY_WORKER_IMAGE ALTIVY_MIGRATIONS_IMAGE; do
        value="$(manifest_image "$name" "$release_dir/images.env")"
        set_manifest_value "$app_root/release.env" "$name" "$value"
      done
      ;;
  esac
  chmod 0644 "$app_root/compose.yaml"
}

restore_app_state() {
  local component="${1:-all}"
  local -a services
  case "$component" in
    frontend) services=(web) ;;
    backend) services=(api bff worker) ;;
    all) services=(web api bff worker) ;;
    *) fail "Componente inválido para rollback: $component" ;;
  esac

  if [[ -f "$rollback_root/available" ]]; then
    cp -p "$rollback_root/release.env" "$app_root/release.env"
    cp -p "$rollback_root/compose.yaml" "$app_root/compose.yaml"
    compose pull "${services[@]}" ||
      printf 'Aviso: pull da release anterior falhou; tentando usar as imagens locais.\n' >&2
    compose up --detach --wait --wait-timeout 180 "${services[@]}" ||
      printf 'Aviso: a restauração automática dos containers falhou. Consulte o runbook.\n' >&2
  else
    printf 'Nenhuma release anterior disponível; a implantação inicial não pode ser revertida automaticamente.\n' >&2
    compose stop "${services[@]}" || true
  fi
}

backup_before_backend_release() {
  local postgres_container backup_root data_root timestamp database_temp vault_temp retention_days
  local backup_scope=''
  postgres_container="$(read_config_value POSTGRES_CONTAINER "$config_root/compose.env")"
  data_root="$(read_config_value ALTIVY_DATA_ROOT "$config_root/compose.env")"
  retention_days="$(read_config_value ALTIVY_BACKUP_RETENTION_DAYS "$config_root/compose.env")"
  backup_root="$data_root/backups"
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  mkdir -p "$backup_root"
  chmod 0700 "$backup_root"

  compose stop bff api worker
  database_temp="$backup_root/.altivy-db-$timestamp.dump.tmp"
  if ! docker exec "$postgres_container" sh -ec 'exec pg_dump -U "$POSTGRES_USER" -d altivy -Fc' > "$database_temp"; then
    rm -f "$database_temp"
    fail 'Backup do database altivy falhou; migrations e promoção foram interrompidas.'
  fi
  chmod 0600 "$database_temp"
  mv -f "$database_temp" "$backup_root/altivy-db-$timestamp.dump"

  vault_temp="$backup_root/.altivy-vault-$timestamp.tar.gz.tmp"
  if ! tar -C "$data_root/vaults" -czf "$vault_temp" .; then
    rm -f "$vault_temp"
    fail 'Backup do vault falhou; migrations e promoção foram interrompidas.'
  fi
  chmod 0600 "$vault_temp"
  mv -f "$vault_temp" "$backup_root/altivy-vault-$timestamp.tar.gz"
  backup_scope=' e vault'
  find "$backup_root" -mindepth 1 -maxdepth 1 -type f \
    \( -name 'altivy-db-*.dump' -o -name 'altivy-vault-*.tar.gz' \) \
    -mtime "+$retention_days" -delete
  printf 'Backup PostgreSQL%s concluído em %s.\n' "$backup_scope" "$backup_root"
}

smoke_test() {
  local response
  response="$(curl --fail --silent --show-error --max-time 20 https://notes.altivy.it/bff/session)" || return 1
  [[ "$response" == *'"authenticated":false'* ]]
}

deploy_app() {
  local component="$2"
  local release_tag="$3"
  local release_dir="$4"
  local registry_username="$5"
  local registry_token docker_config edge_network postgres_network keycloak_proxy_ip
  local release_prepared=false

  cleanup_deploy() {
    local status=$?
    if (( status != 0 )) && [[ "$release_prepared" == true ]]; then
      restore_app_state "$component"
    fi
    docker logout ghcr.io >/dev/null 2>&1 || true
    rm -rf "$docker_config"
    return "$status"
  }

  [[ "$component" == frontend || "$component" == backend || "$component" == all ]] ||
    fail 'O componente deve ser frontend, backend ou all.'
  [[ "$registry_username" =~ ^[A-Za-z0-9_.-]+$ ]] || fail 'Usuário GHCR inválido.'
  check_config_files
  require_command docker
  require_command curl
  require_command tar
  require_command find
  registry_token=''
  IFS= read -r registry_token || true
  [[ -n "$registry_token" ]] || fail 'Forneça o token GHCR de leitura por stdin.'

  docker_config="$(mktemp -d "$app_root/.docker-config.XXXXXX")"
  chmod 700 "$docker_config"
  export DOCKER_CONFIG="$docker_config"
  if ! printf '%s' "$registry_token" | docker login ghcr.io --username "$registry_username" --password-stdin >/dev/null; then
    unset registry_token
    rm -rf "$docker_config"
    fail 'Login temporário no GHCR falhou.'
  fi
  unset registry_token
  trap cleanup_deploy EXIT

  prepare_app_release "$release_tag" "$release_dir"
  release_prepared=true
  edge_network="$(read_config_value ALTIVY_EDGE_NETWORK "$config_root/compose.env")"
  postgres_network="$(read_config_value ALTIVY_POSTGRES_NETWORK "$config_root/compose.env")"
  keycloak_proxy_ip="$(read_config_value ALTIVY_KEYCLOAK_PROXY_IP "$config_root/compose.env")"
  [[ "$edge_network" =~ ^[A-Za-z0-9_.-]+$ && "$postgres_network" =~ ^[A-Za-z0-9_.-]+$ ]] ||
    fail 'Os nomes das redes Docker são inválidos.'
  docker network inspect "$edge_network" "$postgres_network" >/dev/null ||
    fail 'A rede Docker compartilhada do NGINX ou PostgreSQL não existe.'
  if ! docker network inspect "$edge_network" \
      --format '{{range .Containers}}{{.IPv4Address}}{{"\n"}}{{end}}' |
      grep -Eq "^${keycloak_proxy_ip}/"; then
    fail 'ALTIVY_KEYCLOAK_PROXY_IP não pertence à rede edge configurada.'
  fi
  compose config --quiet

  case "$component" in
    frontend)
      compose pull web
      if ! compose up --detach --wait --wait-timeout 180 --no-deps web || ! smoke_test; then
        restore_app_state frontend
        release_prepared=false
        fail 'Deploy frontend falhou; rollback da release anterior foi solicitado.'
      fi
      ;;
    backend|all)
      compose pull api bff worker migrator
      backup_before_backend_release
      if ! compose --profile operations run --rm migrator; then
        restore_app_state "$component"
        release_prepared=false
        fail 'Migration falhou; serviços da release anterior foram preservados/restaurados.'
      fi
      if [[ "$component" == all ]]; then
        compose pull web
      fi
      if ! compose up --detach --wait --wait-timeout 180 api bff worker; then
        restore_app_state "$component"
        release_prepared=false
        fail 'Deploy backend falhou; rollback da release anterior foi solicitado.'
      fi
      if [[ "$component" == all ]] && ! compose up --detach --wait --wait-timeout 180 --no-deps web; then
        restore_app_state "$component"
        release_prepared=false
        fail 'Deploy frontend falhou; rollback da release anterior foi solicitado.'
      fi
      if ! smoke_test; then
        restore_app_state "$component"
        release_prepared=false
        fail 'Smoke test externo falhou; rollback da release anterior foi solicitado.'
      fi
      ;;
  esac

  release_prepared=false
  printf 'Deploy %s %s concluído; imagem(ns) identificadas por digest.\n' "$component" "$release_tag"
}

deploy_nginx() {
  local release_dir="$2"
  local nginx_container dest backup output
  release_dir="$(validate_release_dir "$release_dir")"
  [[ -f "$release_dir/notes.altivy.it.conf" ]] || fail 'Arquivo NGINX da release ausente.'
  nginx_container="$(read_config_value NGINX_CONTAINER "$config_root/compose.env")"
  [[ "$nginx_container" =~ ^[A-Za-z0-9_.-]+$ ]] || fail 'NGINX_CONTAINER inválido.'
  docker inspect "$nginx_container" >/dev/null 2>&1 || fail 'Container NGINX não encontrado.'
  docker exec "$nginx_container" test -r /userapps/ssl/altivy.it.pem || fail 'Certificado TLS não está montado no NGINX.'
  docker exec "$nginx_container" test -r /userapps/ssl/altivy.it.key || fail 'Chave TLS não está montada no NGINX.'
  docker exec "$nginx_container" openssl x509 -in /userapps/ssl/altivy.it.pem -noout -checkhost notes.altivy.it >/dev/null ||
    fail 'O certificado TLS não cobre notes.altivy.it.'

  mkdir -p "$nginx_root" "$rollback_root"
  dest="$nginx_root/notes.altivy.it.conf"
  backup="$rollback_root/notes.altivy.it.conf"
  if [[ -f "$dest" ]]; then
    cp -p "$dest" "$backup"
    : > "$rollback_root/nginx-available"
  else
    rm -f "$rollback_root/nginx-available"
  fi
  install -m 0644 "$release_dir/notes.altivy.it.conf" "$dest.new"
  mv -f "$dest.new" "$dest"

  if ! docker exec "$nginx_container" nginx -t >/dev/null 2>&1; then
    if [[ -f "$rollback_root/nginx-available" ]]; then cp -p "$backup" "$dest"; else rm -f "$dest"; fi
    fail 'nginx -t falhou; configuração anterior restaurada e NGINX não recarregado.'
  fi
  output="$(docker exec "$nginx_container" nginx -T 2>&1)" || {
    if [[ -f "$rollback_root/nginx-available" ]]; then cp -p "$backup" "$dest"; else rm -f "$dest"; fi
    fail 'Não foi possível inspecionar a configuração carregada do NGINX.'
  }
  if [[ "$output" != *'server_name notes.altivy.it;'* ]]; then
    if [[ -f "$rollback_root/nginx-available" ]]; then cp -p "$backup" "$dest"; else rm -f "$dest"; fi
    fail 'O include global não carrega a pasta altivynotes; configuração restaurada.'
  fi
  if ! docker exec "$nginx_container" nginx -s reload >/dev/null 2>&1; then
    if [[ -f "$rollback_root/nginx-available" ]]; then cp -p "$backup" "$dest"; else rm -f "$dest"; fi
    docker exec "$nginx_container" nginx -s reload >/dev/null 2>&1 || true
    fail 'Reload do NGINX falhou; configuração anterior restaurada.'
  fi
  printf 'Virtual host notes.altivy.it instalado e NGINX recarregado sem reiniciar o container.\n'
}

rollback_nginx() {
  local nginx_container dest
  [[ -f "$rollback_root/nginx-available" && -f "$rollback_root/notes.altivy.it.conf" ]] ||
    fail 'Não há configuração NGINX anterior para rollback.'
  nginx_container="$(read_config_value NGINX_CONTAINER "$config_root/compose.env")"
  dest="$nginx_root/notes.altivy.it.conf"
  cp -p "$rollback_root/notes.altivy.it.conf" "$dest"
  docker exec "$nginx_container" nginx -t >/dev/null 2>&1 || fail 'Configuração anterior também falhou em nginx -t.'
  docker exec "$nginx_container" nginx -s reload >/dev/null 2>&1 || fail 'Reload do NGINX falhou.'
  printf 'Configuração NGINX anterior restaurada.\n'
}

deliver_postgres() {
  local release_dir="$2"
  local postgres_container source dest
  release_dir="$(validate_release_dir "$release_dir")"
  source="$release_dir/001-bootstrap-altivy-roles.sql"
  [[ -f "$source" ]] || fail 'SQL de bootstrap PostgreSQL ausente na release.'
  postgres_container="$(read_config_value POSTGRES_CONTAINER "$config_root/compose.env")"
  [[ "$postgres_container" =~ ^[A-Za-z0-9_.-]+$ ]] || fail 'POSTGRES_CONTAINER inválido.'
  docker exec "$postgres_container" psql --version >/dev/null || fail 'Container PostgreSQL não está acessível.'
  dest="$app_root/postgres/001-bootstrap-altivy-roles.sql"
  mkdir -p "$(dirname "$dest")"
  install -m 0640 "$source" "$dest.new"
  mv -f "$dest.new" "$dest"
  printf 'Bootstrap SQL entregue em %s; nenhum SQL foi executado e PostgreSQL não foi reiniciado.\n' "$dest"
}

rollback_app() {
  local registry_username="$2"
  local registry_token docker_config
  [[ -f "$rollback_root/available" ]] || fail 'Não há release de aplicação anterior para rollback.'
  [[ "$registry_username" =~ ^[A-Za-z0-9_.-]+$ ]] || fail 'Usuário GHCR inválido.'
  registry_token=''
  IFS= read -r registry_token || true
  [[ -n "$registry_token" ]] || fail 'Forneça o token GHCR de leitura por stdin.'
  docker_config="$(mktemp -d "$app_root/.docker-config.XXXXXX")"
  chmod 700 "$docker_config"
  export DOCKER_CONFIG="$docker_config"
  if ! printf '%s' "$registry_token" | docker login ghcr.io --username "$registry_username" --password-stdin >/dev/null; then
    unset registry_token
    rm -rf "$docker_config"
    fail 'Login temporário no GHCR falhou.'
  fi
  unset registry_token
  trap 'docker logout ghcr.io >/dev/null 2>&1 || true; rm -rf "$docker_config"' EXIT
  cp -p "$rollback_root/release.env" "$app_root/release.env"
  cp -p "$rollback_root/compose.yaml" "$app_root/compose.yaml"
  compose pull web api bff worker
  compose up --detach --wait --wait-timeout 180 web api bff worker
  smoke_test || fail 'Rollback executado, mas smoke test externo continua falhando.'
  printf 'Release anterior restaurada. O schema PostgreSQL não foi revertido.\n'
}

case "$operation" in
  deploy-app)
    [[ $# -eq 5 ]] || { usage >&2; exit 2; }
    deploy_app "$@"
    ;;
  rollback-app)
    [[ $# -eq 2 ]] || { usage >&2; exit 2; }
    require_command docker
    require_command curl
    rollback_app "$@"
    ;;
  deploy-nginx)
    [[ $# -eq 2 ]] || { usage >&2; exit 2; }
    require_command docker
    deploy_nginx "$@"
    ;;
  rollback-nginx)
    [[ $# -eq 1 ]] || { usage >&2; exit 2; }
    require_command docker
    rollback_nginx
    ;;
  deliver-postgres)
    [[ $# -eq 2 ]] || { usage >&2; exit 2; }
    require_command docker
    deliver_postgres "$@"
    ;;
  *)
    usage >&2
    exit 2
    ;;
esac