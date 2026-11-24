#!/bin/sh
# Levanta la DEMO de Dentissa con un comando (docs/DEMO.md). Solo datos ficticios; todo en 127.0.0.1.
#   sh demo/demo.sh
# Requiere Docker. Para empezar de cero: docker compose -f docker-compose.demo.yml --env-file .env.demo down -v
set -e
cd "$(dirname "$0")/.."

docker info >/dev/null 2>&1 || { echo "Docker no está corriendo. Ábrelo y vuelve a intentarlo."; exit 1; }

# .env.demo con valores al azar (no se versiona)
if [ ! -f .env.demo ]; then
  azar() { head -c "$1" /dev/urandom | base64 | tr -d '+/=\n'; }
  sed -e "s|^POSTGRES_PASSWORD=.*$|POSTGRES_PASSWORD=$(azar 24)|" \
      -e "s|^JWT_SECRET=.*$|JWT_SECRET=$(azar 48)|" .env.demo.example > .env.demo
  echo "Se creó .env.demo con valores al azar."
fi

compose="docker compose -f docker-compose.demo.yml --env-file .env.demo"
$compose up -d --build --wait

set +e
$compose run --rm sembrar
estado=$?
set -e
if [ "$estado" -eq 3 ]; then echo "La base ya tenía datos: se conservan (las cuentas se mostraron al sembrar la primera vez)."
elif [ "$estado" -ne 0 ]; then echo "Falló el sembrado."; exit 1; fi

panel=$(sed -n 's/^PANEL_PUERTO=//p' .env.demo)
correo=$(sed -n 's/^MAILPIT_PUERTO=//p' .env.demo)
echo ""
echo "Panel:   http://127.0.0.1:${panel:-8080}"
echo "Correos: http://127.0.0.1:${correo:-8026}"
