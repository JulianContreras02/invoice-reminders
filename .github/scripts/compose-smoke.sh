#!/usr/bin/env bash
# Prueba de humo del despliegue: reproduce el guion de docs/VERIFICACION.md contra el
# docker compose en ejecución. También sirve en local: docker compose up -d --build --wait
# y luego: bash .github/scripts/compose-smoke.sh  (requiere curl y jq).
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:8080}"
MAILPIT_URL="${MAILPIT_URL:-http://localhost:8025}"

step() { printf '\n==> %s\n' "$1"; }

# Evalúa una expresión jq sobre un JSON; si no se cumple, muestra el JSON y falla.
check() {
  local json="$1" expression="$2" description="$3"
  if jq -e "$expression" >/dev/null <<<"$json"; then
    echo "OK  $description"
  else
    echo "FALLO  $description"
    echo "      esperado: $expression"
    jq . <<<"$json" || echo "$json"
    exit 1
  fi
}

step "Salud: MongoDB y SMTP"
check "$(curl -fsS "$BASE_URL/health/ready")" '.status == "Healthy"' "health/ready es Healthy"

step "Estado inicial: 3 clientes y 10 facturas (4 / 3 / 1 / 2)"
summary="$(curl -fsS "$BASE_URL/api/invoices")"
check "$summary" '.total == 10 and (.customers | length) == 3' "10 facturas de 3 clientes"
check "$summary" '.countsByStatus.primerrecordatorio == 4 and .countsByStatus.segundorecordatorio == 3' "4 en primer y 3 en segundo recordatorio"
check "$summary" '.countsByStatus.desactivado == 1 and .countsByStatus.pagada == 2' "1 desactivada y 2 pagadas"

step "Mailpit vacío antes de empezar"
curl -fsS -X DELETE "$MAILPIT_URL/api/v1/messages" >/dev/null

step "Ejecución 1: 7 correos a 3 destinatarios"
check "$(curl -fsS -X POST "$BASE_URL/api/reminders/run")" '.emailsSent == 7 and .updated == 7 and .failed == 0' "7 enviados y 7 actualizadas"
messages="$(curl -fsS "$MAILPIT_URL/api/v1/messages")"
check "$messages" '.total == 7' "Mailpit recibió 7 correos"
check "$messages" '[.messages[].To[].Address] | unique | length == 3' "a 3 destinatarios distintos"

step "Ejecución 2: 4 correos (los que pasaron a segundo recordatorio)"
check "$(curl -fsS -X POST "$BASE_URL/api/reminders/run")" '.emailsSent == 4 and .updated == 4' "4 enviados y 4 actualizadas"

step "Ejecución 3: nada que hacer"
check "$(curl -fsS -X POST "$BASE_URL/api/reminders/run")" '.candidates == 0' "0 candidatas"
check "$(curl -fsS "$BASE_URL/api/invoices")" '.countsByStatus.desactivado == 8 and .countsByStatus.pagada == 2' "8 desactivadas y 2 pagadas intactas"

step "Reinicio de datos"
check "$(curl -fsS -X POST "$BASE_URL/api/admin/seed/reset")" '.applied == true and .invoices == 10' "semilla restaurada"
check "$(curl -fsS "$BASE_URL/api/invoices")" '.countsByStatus.primerrecordatorio == 4' "vuelve al estado inicial"

step "Rutas"
status="$(curl -s -o /dev/null -w '%{http_code}' "$BASE_URL/api/no-existe")"
[[ "$status" == "404" ]] && echo "OK  /api/no-existe responde 404" || { echo "FALLO  /api/no-existe respondió $status"; exit 1; }
curl -fsS "$BASE_URL/" | grep -q '<div id="root">' && echo "OK  la interfaz React se sirve en /"
curl -fsS "$BASE_URL/openapi/v1.json" | jq -e '.paths["/api/reminders/run"]' >/dev/null && echo "OK  documento OpenAPI publicado"

printf '\nPrueba de humo superada.\n'
