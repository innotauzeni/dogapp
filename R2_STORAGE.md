# Cloudflare R2 image storage

Dog photos (and any future uploads) are stored in Cloudflare R2 via the
S3-compatible API. The upload happens **server-side** in both apps, so the R2
secret never ships to clients (browser or mobile).

## Where it lives

| Piece | File |
|-------|------|
| Options + config/env loading | `Dog.Application/Common/R2Options.cs` |
| Interface | `Dog.Application/Contracts/IFileStorageService.cs` |
| Implementation (AWS SDK → R2) | `Dog.Application/Services/R2FileStorageService.cs` |
| DI registration | `Dog.Application/FileStorageRegistration.cs` (`AddFileStorage`) |
| API endpoint | `POST /api/animal/UploadPhoto` (multipart `file`) → `{ data: "<url>" }` |
| Portal usage | `Dog.Portal/Controllers/DogsController.cs` (`SavePhotoAsync`) |

Both the API and Portal call `services.AddFileStorage(Configuration)`.
Objects are written under the **`dogs/`** prefix; the returned public URL
(`https://pub-…r2.dev/dogs/<guid>.jpg`) is stored in the dog's `PhotoUrl`.

If R2 is **not** configured, the Portal falls back to local `wwwroot/uploads/dogs`
so it keeps working; the API returns a clear "storage not configured" message.

## Configuration

Non-secret values live in each app's `appsettings.json` under `"R2"`
(`Bucket`, `Endpoint`, `PublicUrl`, `Region`).

**Secrets are provided via environment variables** and must NOT be committed:

```
R2_ACCESS_KEY_ID=<your access key id>
R2_SECRET_ACCESS_KEY=<your secret access key>
```

The following env vars are also honoured and override appsettings if set:
`R2_BUCKET`, `R2_ENDPOINT`, `R2_URL`, `R2_DEFAULT_REGION`.

### Setting env vars on the server (systemd example)

```ini
# /etc/systemd/system/dog-api.service.d/r2.conf
[Service]
Environment=R2_ACCESS_KEY_ID=xxxxxxxx
Environment=R2_SECRET_ACCESS_KEY=xxxxxxxx
```
(Do the same for the portal service, then `systemctl daemon-reload && systemctl restart`.)

For local dev you can use user-secrets instead of a file:
```
dotnet user-secrets set "R2:AccessKeyId" "xxxx" --project Dog.Api
dotnet user-secrets set "R2:SecretAccessKey" "xxxx" --project Dog.Api
```

## ⚠️ Security

- **Rotate the R2 key that was shared in chat** — treat it as compromised and
  generate a new one in the Cloudflare dashboard.
- Never put the secret key in `appsettings.json`, the mobile app, or anything
  that ships to a client. Mobile uploads go through the API endpoint above.
- Consider scoping the R2 API token to this single bucket with only the
  permissions it needs (Object Read & Write).
