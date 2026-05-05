# Deployment Guide — DigitalOcean + Apache + GitHub Actions

## Architecture

```
Internet
   │
   ▼
Apache (port 80/443)
   ├── yourdomain.com        → Dog.Portal  (localhost:5200)
   └── api.yourdomain.com   → Dog.Api     (localhost:5100)
                                    │
                                    ▼
                               MySQL (localhost:3306)
```

---

## STEP 1 — Create a DigitalOcean Droplet

1. Go to [digitalocean.com](https://digitalocean.com) → Create Droplet
2. Choose: **Ubuntu 22.04 LTS**
3. Plan: **Basic — $6/mo** (1 vCPU, 1GB RAM) minimum, **$12/mo** recommended
4. Add your **SSH key** during creation
5. Note the droplet's **IP address**

---

## STEP 2 — First-Time Server Setup

SSH into your droplet as root:

```bash
ssh root@YOUR_DROPLET_IP
```

Upload and run the setup script (from your local machine):

```bash
# From your project root on Windows (Git Bash or WSL):
scp deploy/server-setup.sh root@YOUR_DROPLET_IP:/root/
scp -r deploy/ root@YOUR_DROPLET_IP:/root/

# Then on the server:
ssh root@YOUR_DROPLET_IP
bash /root/server-setup.sh
```

---

## STEP 3 — Configure MySQL

On the server:

```bash
sudo mysql_secure_installation
# Follow prompts — set a root password

sudo mysql -u root -p
```

Then run:

```sql
CREATE DATABASE dogsectiondb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE dogsectiondbqueue CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'doguser'@'localhost' IDENTIFIED BY 'YOUR_STRONG_PASSWORD';
GRANT ALL PRIVILEGES ON dogsectiondb.* TO 'doguser'@'localhost';
GRANT ALL PRIVILEGES ON dogsectiondbqueue.* TO 'doguser'@'localhost';
FLUSH PRIVILEGES;
EXIT;
```

---

## STEP 4 — Update Apache Vhost Configs

Edit the domain names on the server:

```bash
sudo nano /etc/apache2/sites-available/dog-api.conf
# Replace: api.yourdomain.com → your actual API domain

sudo nano /etc/apache2/sites-available/dog-portal.conf
# Replace: yourdomain.com → your actual domain

sudo systemctl reload apache2
```

---

## STEP 5 — Point Your Domain DNS

In your domain registrar (or DigitalOcean DNS):

| Type | Name              | Value              |
|------|-------------------|--------------------|
| A    | yourdomain.com    | YOUR_DROPLET_IP    |
| A    | api.yourdomain.com| YOUR_DROPLET_IP    |

Wait for DNS to propagate (up to 30 min).

---

## STEP 6 — Add GitHub Secrets

Go to your GitHub repo → **Settings → Secrets and variables → Actions → New repository secret**

Add these 5 secrets:

| Secret Name        | Value                                                    |
|--------------------|----------------------------------------------------------|
| `DO_HOST`          | Your droplet IP address (e.g. `167.99.1.2`)             |
| `DO_USER`          | `root` (or a deploy user you created)                    |
| `DO_SSH_KEY`       | Your **private** SSH key (the full contents of `~/.ssh/id_rsa`) |
| `API_APPSETTINGS`  | Contents of `deploy/secrets-templates/API_APPSETTINGS.json` (edited with real values) |
| `PORTAL_APPSETTINGS` | Contents of `deploy/secrets-templates/PORTAL_APPSETTINGS.json` |

### How to get your SSH private key (Windows):

```powershell
Get-Content "$env:USERPROFILE\.ssh\id_rsa"
# Copy the entire output including -----BEGIN ... END----- lines
```

### Edit API_APPSETTINGS before adding as secret:
- Replace `YOUR_STRONG_PASSWORD` with your MySQL password
- Replace `CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS` with a real JWT key
- Replace `yourdomain.com` with your actual domain

---

## STEP 7 — Push to GitHub

```bash
git init                          # if not already a git repo
git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPO.git
git add .
git commit -m "Initial deployment setup"
git push -u origin main
```

GitHub Actions will automatically:
1. Build both projects in Release mode
2. Publish the binaries
3. SCP the archives to your droplet
4. Stop the services, deploy new files, restart services
5. Reload Apache

Watch the progress at: `https://github.com/YOUR_USERNAME/YOUR_REPO/actions`

---

## STEP 8 — SSL Certificate (HTTPS) — Optional but Recommended

Once DNS is pointing to your droplet:

```bash
sudo certbot --apache -d yourdomain.com -d api.yourdomain.com
# Follow prompts — choose to redirect HTTP to HTTPS
```

Then update `PORTAL_APPSETTINGS` secret — change `ApiSettings.BaseUrl` to `https://api.yourdomain.com`

---

## Useful Server Commands

```bash
# Check service status
sudo systemctl status dog-api
sudo systemctl status dog-portal

# View live logs
sudo journalctl -u dog-api -f
sudo journalctl -u dog-portal -f

# Restart manually
sudo systemctl restart dog-api
sudo systemctl restart dog-portal

# Check Apache
sudo systemctl status apache2
sudo tail -f /var/log/apache2/dog-api-error.log
sudo tail -f /var/log/apache2/dog-portal-error.log
```

---

## Files Created

```
.github/
  workflows/
    deploy.yml                  ← GitHub Actions pipeline

deploy/
  README.md                     ← This file
  server-setup.sh               ← One-time server setup script
  systemd/
    dog-api.service             ← Systemd service for the API
    dog-portal.service          ← Systemd service for the Portal
  apache/
    dog-api.conf                ← Apache vhost for API
    dog-portal.conf             ← Apache vhost for Portal
  secrets-templates/
    API_APPSETTINGS.json        ← Template for GitHub Secret
    PORTAL_APPSETTINGS.json     ← Template for GitHub Secret
```
