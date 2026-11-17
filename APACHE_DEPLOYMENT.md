# Apache Deployment Guide

## Files Created
- `apache-api.conf` - Configuration for api.blackeaglesurveillance.co.zw
- `apache-portal.conf` - Configuration for portal.blackeaglesurveillance.co.zw

## Deployment Steps on Your Linux Droplet

### 1. Copy configuration files to Apache
```bash
sudo cp apache-api.conf /etc/apache2/sites-available/
sudo cp apache-portal.conf /etc/apache2/sites-available/
```

### 2. Enable required Apache modules
```bash
sudo a2enmod proxy
sudo a2enmod proxy_http
sudo a2enmod rewrite
sudo a2enmod ssl
```

### 3. Enable the new sites
```bash
sudo a2ensite apache-api.conf
sudo a2ensite apache-portal.conf
```

### 4. Test Apache configuration
```bash
sudo apache2ctl configtest
```
(Should return "Syntax OK")

### 5. Restart Apache
```bash
sudo systemctl restart apache2
```

### 6. Set up SSL certificates with Let's Encrypt
```bash
sudo certbot --apache -d api.blackeaglesurveillance.co.zw -d portal.blackeaglesurveillance.co.zw
```

## Important Notes

- **Dog.Api** will run on port `5001`
- **Dog.Portal** will run on port `5002`
- Make sure both applications are configured to listen on these ports
- The configurations redirect HTTP to HTTPS automatically
- SSL certificates are managed by Let's Encrypt (auto-renewal included)

## Application Configuration (in your .NET projects)

In your `appsettings.json` or program startup, configure:

**Dog.Api:**
```json
"Urls": "http://127.0.0.1:5001"
```

**Dog.Portal:**
```json
"Urls": "http://127.0.0.1:5002"
```

## Troubleshooting

- Check Apache logs: `sudo tail -f /var/log/apache2/api-error.log`
- Check application logs on the droplet
- Verify DNS is pointing to your droplet IP
- Make sure ports 5001 and 5002 are not already in use: `sudo netstat -tulpn | grep -E '5001|5002'`
