#!/bin/bash
set -e

APP_NAME="profileservice"
REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"
API_DIR="/opt/$APP_NAME"
SERVICE_FILE="/etc/systemd/system/$APP_NAME.service"
NGINX_CONF="/etc/nginx/conf.d/apiweb-profilesystem.conf"
PUBLISH_DIR="$REPO_DIR/publish"
PORT_FILE="/etc/profileservice.port"

KESTREL_PORT=5027
PUBLIC_PORT=5027

render_template() {
  local src="$1"
  local dest="$2"
  sed \
    -e "s/__PUBLIC_PORT__/${PUBLIC_PORT}/g" \
    -e "s/__KESTREL_PORT__/${KESTREL_PORT}/g" \
    "$src" | sudo tee "$dest" >/dev/null
}

open_firewall_port() {
  if command -v firewall-cmd >/dev/null 2>&1 && systemctl is-active firewalld >/dev/null 2>&1; then
    echo "  Opening firewalld port ${PUBLIC_PORT}/tcp ..."
    sudo firewall-cmd --permanent --add-port="${PUBLIC_PORT}/tcp" || true
    sudo firewall-cmd --reload || true
  fi
}

allow_selinux_http_port() {
  if command -v semanage >/dev/null 2>&1; then
    echo "  Allowing SELinux http_port_t on ${PUBLIC_PORT}/tcp ..."
    sudo semanage port -a -t http_port_t -p tcp "${PUBLIC_PORT}" 2>/dev/null \
      || sudo semanage port -m -t http_port_t -p tcp "${PUBLIC_PORT}" 2>/dev/null \
      || true
  fi
}

verify_deploy() {
  echo "  Waiting for listen on :${KESTREL_PORT} ..."
  local i
  for i in 1 2 3 4 5 6 7 8 9 10; do
    if ss -tln 2>/dev/null | grep -qE ":${KESTREL_PORT}\\b"; then
      break
    fi
    sleep 1
  done
  ss -tln | grep -E ":${KESTREL_PORT}" || echo "  WARNING: port ${KESTREL_PORT} not listening."

  if curl -sf "http://127.0.0.1:${KESTREL_PORT}/health" >/dev/null; then
    echo "  Health OK: http://127.0.0.1:${KESTREL_PORT}/health"
  else
    echo "  WARNING: health failed — last logs:"
    sudo journalctl -u "${APP_NAME}" -n 40 --no-pager || true
  fi
}

echo "============================================"
echo "   Deploying $APP_NAME (offline-ready)"
echo "============================================"
echo "  Kestrel port: ${KESTREL_PORT}"
echo "${PUBLIC_PORT} ${KESTREL_PORT}" | sudo tee "$PORT_FILE" >/dev/null

if [ -d "$PUBLISH_DIR" ] && [ -f "$PUBLISH_DIR/ProfileService.Api.dll" ]; then
  echo "[1/5] Copying pre-built publish/ (offline)..."
  sudo mkdir -p "$API_DIR"
  if command -v rsync >/dev/null 2>&1; then
    sudo rsync -a --delete "$PUBLISH_DIR/" "$API_DIR/"
  else
    sudo rm -rf "${API_DIR:?}/"*
    sudo cp -a "$PUBLISH_DIR"/. "$API_DIR/"
  fi
else
  echo "[1/5] ERROR: publish/ missing."
  exit 1
fi

sudo mkdir -p "$API_DIR/logs" /var/log/profileservice

echo "[2/5] Database note: migrations run on start if SQL is reachable."
echo "      If DB missing, run deploy/create-database.sql in SSMS once."

echo "[3/5] nginx..."
# Remove previous conflicting SSL vhost we may have written; domain already exists in apis.conf
if [ -f "$NGINX_CONF" ]; then
  echo "  Removing conflicting ${NGINX_CONF} (use existing SSL vhost -> 127.0.0.1:5027)"
  sudo rm -f "$NGINX_CONF"
fi
sudo cp "$REPO_DIR/deploy/nginx.conf.template" /opt/profileservice/nginx-notes.conf
if command -v nginx >/dev/null 2>&1; then
  sudo nginx -t && sudo systemctl reload nginx || true
fi
open_firewall_port
allow_selinux_http_port

echo "[4/5] systemd + env..."
render_template "$REPO_DIR/deploy/profileservice.service.template" "$SERVICE_FILE"
sudo useradd -r -s /usr/sbin/nologin profileservice 2>/dev/null || true
# Always refresh env from example (no ConnectionString with $ — safer)
sudo cp "$REPO_DIR/deploy/profileservice.env.example" /etc/profileservice.env
# Strip any old ConnectionStrings line that breaks $ password
sudo sed -i '/^ConnectionStrings__Profile=/d' /etc/profileservice.env
sudo chown root:profileservice /etc/profileservice.env
sudo chmod 640 /etc/profileservice.env
sudo chown -R profileservice:profileservice /var/log/profileservice "$API_DIR"

echo "[5/5] start..."
sudo systemctl daemon-reload
sudo systemctl enable "$APP_NAME"
sudo systemctl restart "$APP_NAME"
sleep 2
sudo systemctl status "$APP_NAME" --no-pager || true
verify_deploy

echo ""
echo "============================================"
echo "   Deploy complete!"
echo "   curl http://127.0.0.1:${KESTREL_PORT}/health"
echo "   curl http://127.0.0.1:${KESTREL_PORT}/api/health"
echo "   journalctl -u ${APP_NAME} -n 80 --no-pager"
echo "============================================"
