#!/bin/bash
# ============================================================
# One-time server setup script for DigitalOcean Ubuntu droplet
# Run as root: bash server-setup.sh
# ============================================================
set -e

echo "==> Updating system packages..."
apt-get update && apt-get upgrade -y

# ── Install .NET 8 Runtime ────────────────────────────────────────────────────
echo "==> Installing .NET 8 Runtime..."
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
apt-get update
apt-get install -y aspnetcore-runtime-8.0

# ── Install Apache ────────────────────────────────────────────────────────────
echo "==> Installing Apache..."
apt-get install -y apache2

# Enable required Apache modules
a2enmod proxy
a2enmod proxy_http
a2enmod headers
a2enmod rewrite
a2enmod ssl

# ── Install MySQL ─────────────────────────────────────────────────────────────
echo "==> Installing MySQL..."
apt-get install -y mysql-server

# Secure MySQL (set root password, remove test db, etc.)
# Run manually: sudo mysql_secure_installation

# ── Install Certbot (Let's Encrypt SSL) ───────────────────────────────────────
echo "==> Installing Certbot..."
apt-get install -y certbot python3-certbot-apache

# ── Create app directories ────────────────────────────────────────────────────
echo "==> Creating app directories..."
mkdir -p /var/www/dog-api
mkdir -p /var/www/dog-portal
chown -R www-data:www-data /var/www/dog-api
chown -R www-data:www-data /var/www/dog-portal

# ── Install systemd service files ─────────────────────────────────────────────
echo "==> Installing systemd services..."
cp deploy/systemd/dog-api.service    /etc/systemd/system/dog-api.service
cp deploy/systemd/dog-portal.service /etc/systemd/system/dog-portal.service
systemctl daemon-reload

# ── Install Apache vhost configs ──────────────────────────────────────────────
echo "==> Installing Apache vhosts..."
cp deploy/apache/dog-api.conf    /etc/apache2/sites-available/dog-api.conf
cp deploy/apache/dog-portal.conf /etc/apache2/sites-available/dog-portal.conf

# Disable default site, enable ours
a2dissite 000-default.conf || true
a2ensite dog-api.conf
a2ensite dog-portal.conf

systemctl restart apache2

# ── Create MySQL database and user ────────────────────────────────────────────
echo "==> Setting up MySQL database..."
echo "  Run the following manually in MySQL:"
echo "  CREATE DATABASE dogsectiondb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
echo "  CREATE DATABASE dogsectiondbqueue CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
echo "  CREATE USER 'doguser'@'localhost' IDENTIFIED BY 'YOUR_STRONG_PASSWORD';"
echo "  GRANT ALL PRIVILEGES ON dogsectiondb.* TO 'doguser'@'localhost';"
echo "  GRANT ALL PRIVILEGES ON dogsectiondbqueue.* TO 'doguser'@'localhost';"
echo "  FLUSH PRIVILEGES;"

echo ""
echo "✅ Server setup complete!"
echo ""
echo "Next steps:"
echo "  1. Edit /etc/apache2/sites-available/dog-api.conf    — replace yourdomain.com"
echo "  2. Edit /etc/apache2/sites-available/dog-portal.conf — replace yourdomain.com"
echo "  3. Run: sudo mysql_secure_installation"
echo "  4. Create MySQL DB and user (see above)"
echo "  5. Add GitHub Secrets (see README)"
echo "  6. Push to main branch to trigger first deploy"
echo "  7. Run: sudo certbot --apache -d yourdomain.com -d api.yourdomain.com"
