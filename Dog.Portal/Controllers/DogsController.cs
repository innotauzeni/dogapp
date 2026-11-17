using Dog.Application.Contracts;
using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace Dog.Portal.Controllers
{
    public class DogsController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly IFileStorageService _storage;

        public DogsController(IApiClient apiClient, IFileStorageService storage)
        {
            _apiClient = apiClient;
            _storage = storage;
        }

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));

        /// Turns a raw (never-swallowed) API response into an honest success/message pair
        /// instead of the generic fallback text that used to mask real failure reasons.
        private static object BuildResult(ApiRawResult raw, string successFallback)
        {
            ApiResponse<object>? parsed = null;
            try { parsed = JsonSerializer.Deserialize<ApiResponse<object>>(raw.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
            catch { /* body wasn't the expected envelope shape — fall through */ }

            if (raw.IsSuccessStatusCode && parsed?.Success == true)
                return new { success = true, message = parsed.Message ?? successFallback };

            if (raw.StatusCode == 401 || raw.StatusCode == 403)
                return new { success = false, message = "You don't have permission to do this." };

            if (parsed?.Message != null)
                return new { success = false, message = parsed.Message };

            var bodySnippet = raw.Body.Length > 200 ? raw.Body[..200] + "…" : raw.Body;
            return new { success = false, message = $"Request failed ({raw.StatusCode}): {bodySnippet}" };
        }

        /// <summary>
        /// Returns the active company ID for data scoping:
        /// head-office with branch selected → branch ID, otherwise own company ID.
        /// </summary>
        private int GetActiveCompanyId(UserSessionModel session)
        {
            if (session.IsHeadOffice && session.CurrentBranchId.HasValue && session.CurrentBranchId.Value > 0)
                return session.CurrentBranchId.Value;
            return Convert.ToInt32(session.CompanyId);
        }

        private UserSessionModel? GetSession()
        {
            var json = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(json)) return null;
            return JsonSerializer.Deserialize<UserSessionModel>(json);
        }
        public async Task<IActionResult> Index(string? search, string? status, string? gender, int page = 1, int pageSize = 15, bool assignedOnly = false)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");
            var vm = await BuildDogsListViewModel(search, status, gender, page, pageSize, assignedOnly);
            return View("Index", vm);
        }

        public IActionResult Assigned(string? search, string? status, string? gender, int page = 1, int pageSize = 15)
            => RedirectToAction("Handlers", new { search, status, gender, page, pageSize });

        public async Task<IActionResult> Handlers(string? search, string? status, string? gender, int page = 1, int pageSize = 15)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");
            var vm = await BuildDogsListViewModel(search, status, gender, page, pageSize, true);
            return View("Handlers", vm);
        }

        private async Task<DogsListViewModel> BuildDogsListViewModel(string? search, string? status, string? gender, int page, int pageSize, bool assignedOnly)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);

            var vm = new DogsListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };
            ViewBag.StatusFilter = status;
            ViewBag.GenderFilter = gender;

            try
            {
                // Use active context (branch user → own company; head-office → selected branch or own)
                var activeCompanyId = userSession!.IsHeadOffice && userSession.CurrentBranchId.HasValue
                    ? userSession.CurrentBranchId.Value
                    : Convert.ToInt32(userSession.CompanyId);

                var route = assignedOnly
                    ? $"api/animal/GetAssignedDogs?handlerUserId={Uri.EscapeDataString(userSession.Id)}"
                    : $"api/animal/GetPets?CompanyId={activeCompanyId}";
                var resp = await _apiClient.GetAsync<ApiResponse<List<AnimalListDto>>>(route);
                var all = resp?.Data ?? new List<AnimalListDto>();

                var filteredDogs = all;

                if (!string.IsNullOrWhiteSpace(search))
                {
                    filteredDogs = filteredDogs.Where(c =>
                        (c.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.Breed?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.Color?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.MicrochipNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (status == "active")
                        filteredDogs = filteredDogs.Where(c => c.IsActive).ToList();
                    else if (status == "inactive")
                        filteredDogs = filteredDogs.Where(c => !c.IsActive).ToList();
                }

                if (!string.IsNullOrWhiteSpace(gender))
                {
                    filteredDogs = filteredDogs.Where(c =>
                        string.Equals(c.Gender, gender, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                vm.TotalCount = filteredDogs.Count;
                vm.Doglist = filteredDogs.OrderBy(c => c.Name)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToList();
            }
            catch
            {
                ViewBag.FetchError = "Unable to load dogs. Please try again.";
            }

            return vm;
        }

        // POST /Dogs/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAnimalRequest req)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (req == null) return Json(new { success = false, message = "Invalid request data." });
            try
            {
                // Decode base64 photo and store it (R2 if configured, else disk)
                if (!string.IsNullOrEmpty(req.PhotoBase64))
                {
                    var base64Data = req.PhotoBase64;
                    string extension = ".jpg";
                    string contentType = "image/jpeg";

                    // Strip the data URI prefix: data:image/png;base64,....
                    if (base64Data.Contains(','))
                    {
                        var header = base64Data[..base64Data.IndexOf(',')];
                        base64Data = base64Data[(base64Data.IndexOf(',') + 1)..];
                        if (header.Contains("png")) { extension = ".png"; contentType = "image/png"; }
                        else if (header.Contains("gif")) { extension = ".gif"; contentType = "image/gif"; }
                        else if (header.Contains("webp")) { extension = ".webp"; contentType = "image/webp"; }
                    }

                    var bytes = Convert.FromBase64String(base64Data);
                    req.PhotoUrl = await SavePhotoAsync(bytes, extension, contentType);
                }

                req.CompanyId = GetActiveCompanyId(userSession!);
                var resp = await _apiClient.PostAsync<ApiResponse<AnimalDto>>("api/animal/CreatePet", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Created." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }


        // POST /Dogs/Update
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] System.Text.Json.JsonElement body)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                // Parse id from the JSON body — handle both number and string
                if (!body.TryGetProperty("Id", out var idProp) && !body.TryGetProperty("id", out idProp))
                    return Json(new { success = false, message = "Dog ID is required." });

                int id;
                if (idProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                    id = idProp.GetInt32();
                else if (idProp.ValueKind == System.Text.Json.JsonValueKind.String && int.TryParse(idProp.GetString(), out var parsedId))
                    id = parsedId;
                else
                    return Json(new { success = false, message = "Invalid Dog ID." });

                // Build a clean dictionary to forward to the API
                var dict = new Dictionary<string, object?>();
                foreach (var prop in body.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.ValueKind switch
                    {
                        System.Text.Json.JsonValueKind.String => prop.Value.GetString(),
                        System.Text.Json.JsonValueKind.True => (object)true,
                        System.Text.Json.JsonValueKind.False => (object)false,
                        System.Text.Json.JsonValueKind.Null => null,
                        System.Text.Json.JsonValueKind.Number =>
                            prop.Value.TryGetInt64(out var lng) ? (object)lng : prop.Value.GetDouble(),
                        _ => prop.Value.GetRawText()
                    };
                }

                // If no PhotoUrl sent (null/empty), fetch existing photo and preserve it
                var photoVal = dict.TryGetValue("PhotoUrl", out var pv) ? pv as string : null;
                if (string.IsNullOrEmpty(photoVal))
                {
                    try
                    {
                        var profileResp = await _apiClient.GetAsync<ApiResponse<DogProfileDto>>($"api/animal/GetDogProfile/{id}");
                        if (profileResp?.Success == true && profileResp.Data?.Dog?.PhotoUrl != null)
                            dict["PhotoUrl"] = profileResp.Data.Dog.PhotoUrl;
                    }
                    catch { /* photo fetch failed — proceed without photo */ }
                }

                // Ensure CompanyId is set from session if missing or zero
                var companyIdVal = dict.TryGetValue("CompanyId", out var cv) ? cv : null;
                var companyIdInt = companyIdVal == null ? 0 : Convert.ToInt32(companyIdVal);
                if (companyIdInt == 0)
                {
                    var s = GetSession();
                    dict["CompanyId"] = s != null ? GetActiveCompanyId(s) : 0;
                }

                var raw = await _apiClient.PutRawAsync($"api/animal/UpdatePet/{id}", dict);
                return Json(BuildResult(raw, "Updated."));
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Dogs/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/animal/DeletePet/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Deleted." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Dogs/UploadPhoto
        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });

            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "No file uploaded." });

            try
            {
                // Validate file type and size
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Json(new { success = false, message = "Only JPG, PNG, and GIF files are allowed." });
                }

                if (file.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    return Json(new { success = false, message = "File size must be less than 5MB." });
                }

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var contentType = string.IsNullOrEmpty(file.ContentType) ? "image/jpeg" : file.ContentType;
                var fileUrl = await SavePhotoAsync(ms.ToArray(), fileExtension, contentType);

                return Json(new { success = true, url = fileUrl, message = "File uploaded successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Upload failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// Stores a dog photo and returns its URL. Uses Cloudflare R2 when
        /// configured; otherwise falls back to the local wwwroot/uploads/dogs
        /// folder so the portal keeps working without R2 set up.
        /// </summary>
        private async Task<string> SavePhotoAsync(byte[] bytes, string extension, string contentType)
        {
            if (_storage.IsEnabled)
            {
                return await _storage.UploadBytesAsync(bytes, $"dog{extension}", contentType, "dogs");
            }

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "dogs");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{extension}";
            await System.IO.File.WriteAllBytesAsync(Path.Combine(uploadsDir, fileName), bytes);
            return $"/uploads/dogs/{fileName}";
        }

        // GET /Dogs/VaccineDue — dogs with vaccinations due/overdue (for the alerts bell).
        [HttpGet]
        public async Task<IActionResult> VaccineDue(int days = 30)
        {
            if (!IsAuthenticated()) return Json(new { success = false, items = Array.Empty<object>() });
            try
            {
                var session = JsonSerializer.Deserialize<UserSessionModel>(HttpContext.Session.GetString("UserSession")!);
                var companyId = GetActiveCompanyId(session!);
                var resp = await _apiClient.GetAsync<ApiResponse<List<VaccineDueDto>>>($"api/animal/vaccine-due?companyId={companyId}&days={days}");
                return Json(new { success = resp?.Success ?? false, items = resp?.Data ?? new List<VaccineDueDto>() });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message, items = Array.Empty<object>() }); }
        }

        // POST /Dogs/ToggleStatus/{id}
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.PostAsync<ApiResponse<bool>>($"api/animal/ToggleStatus/{id}", new { });
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Status toggled." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // GET /Dogs/ActivityReportPdf/{id} — downloadable PDF activity report (company logo + details,
        // dog details, care, vaccinations, breeding). Replaces the old HTML print page.
        [HttpGet]
        public async Task<IActionResult> ActivityReportPdf(int id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<DogProfileDto>>($"api/animal/GetDogProfile/{id}");
                var profile = resp?.Data;
                if (resp?.Success != true || profile?.Dog == null)
                    return BadRequest("Dog not found.");

                CompanyDto? company = null;
                try
                {
                    var compResp = await _apiClient.GetAsync<ApiResponse<CompanyDto>>($"api/companies/{profile.Dog.CompanyId}");
                    company = compResp?.Data;
                }
                catch { /* branding is optional */ }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var logo = await PdfImageLoader.LoadAsync(company?.Logo, baseUrl);
                var photo = await PdfImageLoader.LoadAsync(profile.Dog.PhotoUrl, baseUrl);

                var pdf = DogActivityReportPdfService.Build(profile, company, logo, photo);
                return File(pdf, "application/pdf", $"Dog_Activity_Report_{profile.Dog.Name}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest($"Report generation failed: {ex.Message}");
            }
        }

        // GET /Dogs/GalleryList?dogId= — gallery images for a dog.
        [HttpGet]
        public async Task<IActionResult> GalleryList(int dogId)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<GalleryImageDto>>>($"api/animal/Gallery/{dogId}");
                return Json(new { success = resp?.Success ?? false, data = resp?.Data ?? new List<GalleryImageDto>(), message = resp?.Message });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message, data = Array.Empty<object>() }); }
        }

        // POST /Dogs/GalleryUpload — stores one (already browser-resized) image and attaches it to the dog.
        [HttpPost]
        public async Task<IActionResult> GalleryUpload(IFormFile file, int dogId)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (file == null || file.Length == 0) return Json(new { success = false, message = "No file uploaded." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext))
                return Json(new { success = false, message = "Only JPG, PNG, GIF and WebP images are allowed." });
            if (file.Length > 10 * 1024 * 1024)
                return Json(new { success = false, message = "Image must be 10 MB or smaller." });

            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var contentType = string.IsNullOrEmpty(file.ContentType) ? "image/jpeg" : file.ContentType;
                var url = await SavePhotoAsync(ms.ToArray(), ext, contentType);

                var session = JsonSerializer.Deserialize<UserSessionModel>(HttpContext.Session.GetString("UserSession")!);
                var body = new
                {
                    companyId = GetActiveCompanyId(session!),
                    entityId = dogId,
                    fileName = file.FileName,
                    fileUrl = url,
                    contentType
                };
                var resp = await _apiClient.PostAsync<ApiResponse<object>>("api/animal/GalleryAdd", body);
                if (resp == null)
                    return Json(new { success = false, message = "The server refused the upload. You may not have permission to edit this dog." });
                return Json(new { success = resp.Success, message = resp.Message ?? (resp.Success ? "Photo added." : "Failed to add photo.") });
            }
            catch (Exception ex) { return Json(new { success = false, message = $"Upload failed: {ex.Message}" }); }
        }

        // POST /Dogs/GalleryDelete — removes one gallery image.
        [HttpPost]
        public async Task<IActionResult> GalleryDelete(int dogId, int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.DeleteAsync<ApiResponse<bool>>($"api/animal/GalleryDelete/{dogId}/{id}");
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Failed to delete photo." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Dogs/SetProfilePhoto/{id} — makes a gallery image the dog's profile picture.
        [HttpPost]
        public async Task<IActionResult> SetProfilePhoto(int id, [FromForm] string photoUrl)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (string.IsNullOrWhiteSpace(photoUrl)) return Json(new { success = false, message = "No image selected." });

            try
            {
                var profileResp = await _apiClient.GetAsync<ApiResponse<DogProfileDto>>($"api/animal/GetDogProfile/{id}");
                var current = profileResp?.Data?.Dog;
                if (profileResp?.Success != true || current == null)
                    return Json(new { success = false, message = "Dog not found." });

                // UpdatePet overwrites every field, so round-trip the current values — only the photo changes.
                var updateRequest = new UpdateAnimalRequest
                {
                    Id = id,
                    Name = current.Name,
                    Breed = current.Breed,
                    Color = current.Color,
                    DateOfBirth = current.DateOfBirth,
                    DateOfDeath = current.DateOfDeath,
                    Gender = current.Gender,
                    MicrochipNumber = current.MicrochipNumber,
                    PhotoUrl = photoUrl,
                    SireId = current.SireId,
                    DamId = current.DamId,
                    CompanyId = current.CompanyId,
                    CustomerId = current.CustomerId,
                    IsActive = current.IsActive
                };

                var resp = await _apiClient.PutAsync<ApiResponse<AnimalDto>>($"api/animal/UpdatePet/{id}", updateRequest);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Profile picture updated." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET /Dogs/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            try
            {
                // Get full dog profile with all related data
                var resp = await _apiClient.GetAsync<ApiResponse<DogProfileDto>>($"api/animal/GetDogProfile/{id}");

                if (resp == null)
                {
                    TempData["Error"] = "Unable to connect to API. Please try again.";
                    return RedirectToAction("Index");
                }

                if (!resp.Success)
                {
                    TempData["Error"] = resp.Message ?? "Failed to load dog details.";
                    return RedirectToAction("Index");
                }

                if (resp.Data == null)
                {
                    TempData["Error"] = "Dog not found.";
                    return RedirectToAction("Index");
                }

                var session = JsonSerializer.Deserialize<UserSessionModel>(HttpContext.Session.GetString("UserSession")!);
                if (session != null)
                {
                    // Fetch all users and branches, then filter according to the current user's scope.
                    var usersRespTask = _apiClient.GetAsync<ApiResponse<List<UserDto>>>("api/users");
                    var branchesRespTask = _apiClient.GetAsync<ApiResponse<List<DogBranchOption>>>($"api/companies/{session.CompanyId}/branches");
                    await Task.WhenAll(usersRespTask, branchesRespTask);

                    var allUsers = (await usersRespTask)?.Data ?? new List<UserDto>();
                    var allBranches = (await branchesRespTask)?.Data ?? new List<DogBranchOption>();

                    // Determine which branches the current user can see/assign.
                    List<int> allowedBranchIds = new();
                    if (session.IsHeadOffice)
                    {
                        // Head office can see all branches AND its own head-office handlers.
                        // The branches endpoint only returns child branches, so add the head
                        // office company id explicitly — otherwise handlers registered directly
                        // under head office never appear and dogs can't be assigned to them.
                        allowedBranchIds = allBranches.Select(b => b.Id).ToList();
                        allowedBranchIds.Add(Convert.ToInt32(session.CompanyId));
                    }
                    else
                    {
                        // Non-head users are limited to their current branch (fallback to CompanyId)
                        var myBranchId = session.CurrentBranchId ?? Convert.ToInt32(session.CompanyId);
                        allowedBranchIds.Add(myBranchId);
                    }

                    // Filter branches for the UI
                    var visibleBranches = allBranches.Where(b => allowedBranchIds.Contains(b.Id)).ToList();

                    // Filter users to those belonging to the allowed branches and exclude current user (cannot assign to self)
                    var visibleUsers = allUsers.Where(u => allowedBranchIds.Contains(u.CompanyId) && u.Id != session.Id).ToList();

                    ViewBag.Handlers = visibleUsers;
                    ViewBag.Branches = visibleBranches;
                    ViewBag.DefaultBranchId = session.CurrentBranchId ?? Convert.ToInt32(session.CompanyId);
                }

                // Company branding for the printable care timeline (optional).
                try
                {
                    var dogCompanyId = resp?.Data?.Dog?.CompanyId ?? 0;
                    if (dogCompanyId > 0)
                    {
                        var compResp = await _apiClient.GetAsync<ApiResponse<CompanyDto>>($"api/companies/{dogCompanyId}");
                        ViewBag.Company = compResp?.Data;
                    }
                }
                catch { /* branding is optional */ }

                return View(resp.Data);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Unable to load dog details: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AssignDog([FromBody] AssignDogRequest request)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var response = await _apiClient.PostAsync<ApiResponse<bool>>("api/animal/AssignDog", request);
                return Json(new { success = response?.Success ?? false, message = response?.Message ?? "Assignment saved." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> AddCareRecord([FromBody] AddDogCareRecordRequest request)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var session = JsonSerializer.Deserialize<UserSessionModel>(HttpContext.Session.GetString("UserSession")!);
                request.CompanyId = Convert.ToInt32(session!.CompanyId);
                var response = await _apiClient.PostAsync<ApiResponse<bool>>("api/animal/AddCareRecord", request);
                return Json(new { success = response?.Success ?? false, message = response?.Message ?? "Care record saved." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Dogs/MarkAsDeceased/{id}
        [HttpPost]
        public async Task<IActionResult> MarkAsDeceased(int id, [FromForm] DateTime? dateOfDeath, [FromForm] string? photoUrl)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });

            try
            {
                // First fetch the current dog data so we don't overwrite any fields
                var profileResp = await _apiClient.GetAsync<ApiResponse<DogProfileDto>>($"api/animal/GetDogProfile/{id}");
                if (profileResp?.Success != true || profileResp.Data?.Dog == null)
                    return Json(new { success = false, message = "Dog not found." });

                var current = profileResp.Data.Dog;

                // Build update request preserving ALL existing fields — only change status + date of death
                // (and the photo, if a picture of the deceased dog was uploaded).
                var updateRequest = new UpdateAnimalRequest
                {
                    Id = id,
                    Name = current.Name,
                    Breed = current.Breed,
                    Color = current.Color,
                    DateOfBirth = current.DateOfBirth,
                    DateOfDeath = dateOfDeath ?? current.DateOfDeath,
                    Gender = current.Gender,
                    MicrochipNumber = current.MicrochipNumber,
                    PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? current.PhotoUrl : photoUrl,
                    SireId = current.SireId,
                    DamId = current.DamId,
                    CompanyId = current.CompanyId,
                    CustomerId = current.CustomerId,
                    IsActive = false
                };

                var resp = await _apiClient.PutAsync<ApiResponse<AnimalDto>>(
                    $"api/animal/UpdatePet/{id}", updateRequest);

                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Dog marked as deceased." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET /Dogs/GetDogProfile/{id}
        [HttpGet]
        public async Task<IActionResult> GetDogProfile(int id)
        {
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<DogProfileDto>>($"api/animal/GetDogProfile/{id}");
                return Json(new { success = resp?.Success ?? false, data = resp?.Data, message = resp?.Message });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
        // GET /Dogs/GetAnimalsForDropdown
        [HttpGet]
        public async Task<IActionResult> GetAnimalsForDropdown()
        {
            if (!IsAuthenticated()) return Json(new List<object>());
            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                var userSession = string.IsNullOrEmpty(userSessionJson)
                    ? null
                    : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);

                var resp = await _apiClient.GetAsync<ApiResponse<List<AnimalListDto>>>(
                    $"api/animal/GetPets?CompanyId={GetActiveCompanyId(userSession!)}");

                var animals = resp?.Data ?? new List<AnimalListDto>();
                return Json(animals.Select(a => new { id = a.Id, name = a.Name }));
            }
            catch
            {
                return Json(new List<object>());
            }
        }

        // POST /Dogs/AddVaccination
        [HttpPost]
        public async Task<IActionResult> AddVaccination([FromBody] AddVaccinationRequest req)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (req == null) return Json(new { success = false, message = "Invalid request data." });
            
            try
            {
                req.CompanyId = GetActiveCompanyId(userSession!);
                var resp = await _apiClient.PostAsync<ApiResponse<bool>>("api/animal/AddVaccination", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Vaccination added." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Dogs/AddMatingRecord
        [HttpPost]
        public async Task<IActionResult> AddMatingRecord([FromBody] AddMatingRecordRequest req)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (req == null) return Json(new { success = false, message = "Invalid request data." });
            
            try
            {
                req.CompanyId = GetActiveCompanyId(userSession!);
                var resp = await _apiClient.PostAsync<ApiResponse<bool>>("api/animal/AddMatingRecord", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Mating record added." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // POST /Dogs/AddLitter
        [HttpPost]
        public async Task<IActionResult> AddLitter([FromBody] AddLitterRequest req)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (req == null) return Json(new { success = false, message = "Invalid request data." });
            
            try
            {
                req.CompanyId = GetActiveCompanyId(userSession!);
                var resp = await _apiClient.PostAsync<ApiResponse<bool>>("api/animal/AddLitter", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Litter added." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // GET /Dogs/GetMatingRecords
        [HttpGet]
        public async Task<IActionResult> GetMatingRecords()
        {
            if (!IsAuthenticated()) return Json(new List<object>());
            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                var userSession = string.IsNullOrEmpty(userSessionJson)
                    ? null
                    : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);

                var resp = await _apiClient.GetAsync<ApiResponse<List<MatingRecordDto>>>(
                    $"api/animal/GetMatingRecords?CompanyId={GetActiveCompanyId(userSession!)}");

                var records = resp?.Data ?? new List<MatingRecordDto>();
                return Json(records.Select(r => new { 
                    id = r.Id, 
                    sireName = r.SireName, 
                    damName = r.DamName, 
                    matingDate = r.MatingDate.ToString("yyyy-MM-dd") 
                }));
            }
            catch
            {
                return Json(new List<object>());
            }
        }

        // POST /Dogs/AddPuppy
        [HttpPost]
        public async Task<IActionResult> AddPuppy([FromBody] AddPuppyRequest req)
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);
            if (!IsAuthenticated()) return Json(new { success = false, message = "Session expired." });
            if (req == null) return Json(new { success = false, message = "Invalid request data." });
            
            try
            {
                req.CompanyId = GetActiveCompanyId(userSession!);
                var resp = await _apiClient.PostAsync<ApiResponse<bool>>("api/animal/AddPuppy", req);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Puppy added." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // GET /Dogs/GetLitters
        [HttpGet]
        public async Task<IActionResult> GetLitters()
        {
            if (!IsAuthenticated()) return Json(new List<object>());
            try
            {
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                var userSession = string.IsNullOrEmpty(userSessionJson)
                    ? null
                    : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);

                var resp = await _apiClient.GetAsync<ApiResponse<List<LitterDto>>>(
                    $"api/animal/GetLitters?CompanyId={GetActiveCompanyId(userSession!)}");

                var litters = resp?.Data ?? new List<LitterDto>();
                return Json(litters.Select(l => new { 
                    id = l.Id, 
                    sireName = l.SireName, 
                    damName = l.DamName, 
                    birthDate = l.BirthDate.ToString("yyyy-MM-dd"),
                    totalBorn = l.TotalBorn
                }));
            }
            catch
            {
                return Json(new List<object>());
            }
        }
    }
}

