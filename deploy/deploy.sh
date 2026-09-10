#!/bin/bash
set -e

APP_NAME="profileservice"
REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"
API_DIR="/opt/$APP_NAME"
SERVICE_FILE="/etc/systemd/system/$APP_NAME.service"
NGINX_CONF="/etc/nginx/conf.d/apiweb-profilesystem.conf"
PUBLISH_DIR="$REPO_DIR/publish"
PORT_FILE="/etc/profileservice.port"

# Service listens on 5027 (user requirement). HTTPS domain terminates at nginx:443.
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
  echo "  Verifying listeners..."
  ss -tln | grep -E ":${KESTREL_PORT}" || echo "  WARNING: port ${KESTREL_PORT} not listening yet."
  sleep 2
  if curl -sf "http://127.0.0.1:${KESTREL_PORT}/health" >/dev/null; then
    echo "  Health OK on http://127.0.0.1:${KESTREL_PORT}/health"
  else
    echo "  WARNING: health failed — check: journalctl -u ${APP_NAME} -n 50"
  fi
}

echo "============================================"
echo "   Deploying $APP_NAME (offline-ready)"
echo "============================================"
echo "  Kestrel port: ${KESTREL_PORT}"
echo "  Domain:       https://apiweb-profilesystem.sabzevar.ir/"
echo "${PUBLIC_PORT} ${KESTREL_PORT}" | sudo tee "$PORT_FILE" >/dev/null

if [ -d "$PUBLISH_DIR" ] && [ -f "$PUBLISH_DIR/ProfileService.Api.dll" ]; then
  echo "[1/5] Copying pre-built publish/ (offline)..."
  sudo mkdir -p "$API_DIR"
  if command -v rsync >/dev/null 2>&1; then
    sudo rsync -a --delete "$PUBLISH_DIR/" "$API_DIR/"
  else
    sudo rm -rf "$API_DIR"/*
    sudo cp -a "$PUBLISH_DIR"/. "$API_DIR/"
  fi
else
  echo "[1/5] publish/ missing — trying dotnet publish..."
  if ! command -v dotnet >/dev/null 2>&1; then
    echo "  ERROR: No publish/ folder and no dotnet SDK (expected on offline AlmaLinux)."
    exit 1
  fi
  sudo mkdir -p "$API_DIR"
  dotnet publish "$REPO_DIR/src/ProfileService.Api/ProfileService.Api.csproj" \
    -c Release \
    -o "$API_DIR" \
    --self-contained false
fi

echo "[2/5] Database migrations run automatically on first start."

echo "[3/5] nginx (HTTPS domain -> :5027)..."
if command -v nginx >/dev/null 2>&1; then
  sudo cp "$REPO_DIR/deploy/nginx.conf.template" "$NGINX_CONF"
  echo "  Wrote ${NGINX_CONF} — enable SSL cert lines before production use."
  if sudo nginx -t; then
    sudo systemctl reload nginx || true
  else
    echo "  WARNING: nginx -t failed (often missing SSL certs). Fix certs then reload."
  fi
  open_firewall_port
  allow_selinux_http_port
else
  echo "  nginx not installed — app still serves HTTP on :${KESTREL_PORT}"
  open_firewall_port
fi

echo "[4/5] systemd + env..."
render_template "$REPO_DIR/deploy/profileservice.service.template" "$SERVICE_FILE"
sudo useradd -r -s /usr/sbin/nologin profileservice 2>/dev/null || true
sudo mkdir -p /var/log/profileservice
sudo chown -R profileservice:profileservice /var/log/profileservice "$API_DIR"
if [ -f /etc/profileservice.env ]; then
  sudo chown root:profileservice /etc/profileservice.env
  sudo chmod 640 /etc/profileservice.env
else
  echo "  WARNING: copy deploy/profileservice.env.example -> /etc/profileservice.env and edit secrets"
fi

echo "[5/5] start..."
sudo systemctl daemon-reload
sudo systemctl enable "$APP_NAME"
sudo systemctl restart "$APP_NAME"
sudo systemctl status "$APP_NAME" --no-pager || true
verify_deploy

echo ""
echo "============================================"
echo "   Deploy complete!"
echo "   Health:  curl http://127.0.0.1:${KESTREL_PORT}/health"
echo "   Public:  curl http://SERVER_IP:${PUBLIC_PORT}/health"
echo "   Domain:  https://apiweb-profilesystem.sabzevar.ir/"
echo "============================================"
echo "Logs: sudo journalctl -u $APP_NAME -f"
