#!/usr/bin/env bash
# ──────────────────────────────────────────────────────────────────────────────
# uStock — one-shot production deployment on Ubuntu 24.04
# Run as root on the VPS:
#   bash <(curl -fsSL https://raw.githubusercontent.com/golamhabibpalash/WHInventory/master/deploy.sh)
# ──────────────────────────────────────────────────────────────────────────────
set -euo pipefail

REPO="https://github.com/golamhabibpalash/WHInventory.git"
APP_DIR="/opt/ustock"
DOMAIN="ustock.unitymicrofund.com"
EMAIL="golamhabibpalash@gmail.com"
COMPOSE_FILE="docker-compose.synology.yml"

GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'
step() { echo -e "\n${GREEN}▶ $*${NC}"; }
warn() { echo -e "${YELLOW}⚠  $*${NC}"; }
die()  { echo -e "${RED}✗  $*${NC}" >&2; exit 1; }

[[ $EUID -eq 0 ]] || die "Run as root: sudo bash deploy.sh"

# ── 1. System update ──────────────────────────────────────────────────────────
step "[1/8] Updating system packages"
apt-get update -qq
DEBIAN_FRONTEND=noninteractive apt-get upgrade -y -qq

# ── 2. Docker ─────────────────────────────────────────────────────────────────
step "[2/8] Installing Docker"
if command -v docker &>/dev/null; then
    echo "  Already installed: $(docker --version)"
else
    apt-get install -y -qq ca-certificates curl gnupg lsb-release
    install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
        | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
    chmod a+r /etc/apt/keyrings/docker.gpg
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
        | tee /etc/apt/sources.list.d/docker.list >/dev/null
    apt-get update -qq
    apt-get install -y -qq docker-ce docker-ce-cli containerd.io \
        docker-buildx-plugin docker-compose-plugin
    systemctl enable --now docker
    echo "  Installed: $(docker --version)"
fi

# ── 3. Nginx + Certbot ────────────────────────────────────────────────────────
step "[3/8] Installing nginx & certbot"
apt-get install -y -qq nginx certbot python3-certbot-nginx

# ── 4. Firewall ───────────────────────────────────────────────────────────────
step "[4/8] Configuring UFW firewall"
ufw allow OpenSSH
ufw allow 'Nginx Full'
ufw --force enable

# ── 5. Clone / update repo ────────────────────────────────────────────────────
step "[5/8] Fetching application code"
if [[ -d "$APP_DIR/.git" ]]; then
    echo "  Repo exists — pulling latest…"
    git -C "$APP_DIR" pull
else
    git clone "$REPO" "$APP_DIR"
fi

# ── 6. .env file ──────────────────────────────────────────────────────────────
step "[6/8] Checking .env file"
ENV_FILE="$APP_DIR/.env"
if [[ ! -f "$ENV_FILE" ]]; then
    warn ".env not found — creating from template."
    cp "$APP_DIR/.env.example" "$ENV_FILE"
    echo ""
    echo "  Open a new terminal and fill in all CHANGE_ME values:"
    echo "    nano $ENV_FILE"
    echo ""
    read -rp "  Press ENTER once you have saved .env… "
fi

# Sanity check — abort if template values are still present
if grep -q "CHANGE_ME" "$ENV_FILE"; then
    die ".env still contains CHANGE_ME placeholders. Edit $ENV_FILE and re-run."
fi
echo "  .env OK."

# ── 7. Build & start containers ───────────────────────────────────────────────
step "[7/8] Building Docker image and starting services (this takes a few minutes)"
cd "$APP_DIR"
docker compose -f "$COMPOSE_FILE" --env-file .env up -d --build

echo "  Waiting for app to respond on :8080…"
timeout 180 bash -c \
    'until curl -s --max-time 5 http://localhost:8080 >/dev/null 2>&1; do sleep 4; done' \
    && echo "  App is up." \
    || warn "App did not respond within 3 min — check: docker compose -f $APP_DIR/$COMPOSE_FILE logs app"

# ── 8. Nginx + SSL ────────────────────────────────────────────────────────────
step "[8/8] Configuring nginx reverse proxy & SSL"
NGINX_CONF="/etc/nginx/sites-available/ustock"

cat > "$NGINX_CONF" <<NGINX
server {
    listen 80;
    server_name ${DOMAIN};

    location / {
        proxy_pass         http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade \$http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host \$host;
        proxy_set_header   X-Real-IP \$remote_addr;
        proxy_set_header   X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        client_max_body_size 100M;
        proxy_read_timeout 300s;
    }
}
NGINX

ln -sf "$NGINX_CONF" /etc/nginx/sites-enabled/ustock
rm -f /etc/nginx/sites-enabled/default
nginx -t
systemctl reload nginx

echo "  Obtaining Let's Encrypt certificate for ${DOMAIN}…"
certbot --nginx \
    --non-interactive \
    --agree-tos \
    --redirect \
    -m "$EMAIL" \
    -d "$DOMAIN"

systemctl reload nginx

# ── Done ──────────────────────────────────────────────────────────────────────
echo ""
echo -e "${GREEN}╔═══════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║   uStock deployment complete!                        ║${NC}"
echo -e "${GREEN}║   https://${DOMAIN}  ║${NC}"
echo -e "${GREEN}╚═══════════════════════════════════════════════════════╝${NC}"
echo ""
echo "  Admin login : check ADMIN_EMAIL / ADMIN_PASSWORD in $ENV_FILE"
echo "  View logs   : docker compose -f $APP_DIR/$COMPOSE_FILE logs -f"
echo "  Update app  : bash $APP_DIR/update.sh"
