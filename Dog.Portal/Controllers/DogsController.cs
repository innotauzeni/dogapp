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

        public DogsController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));
        public async Task<IActionResult> Index(string? search, string? status, string? gender, int page = 1, int pageSize = 15)
        {
            // ✅ Get session
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson)
                ? null
                : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);


            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");
            var vm = new DogsListViewModel { SearchTerm = search, Page = page, PageSize = pageSize };

            // Pass filter values to ViewBag for form state
            ViewBag.StatusFilter = status;
            ViewBag.GenderFilter = gender;

            try
            {
                // 🔥 ROLE-BASED FILTERING
                List<AnimalListDto> filteredDogs;

                var resp = await _apiClient.GetAsync<ApiResponse<List<AnimalListDto>>>($"api/animal/GetPets?CompanyId={Convert.ToInt32(userSession.CompanyId)}");
                var all = resp?.Data ?? [];

                // Apply filters
                filteredDogs = all;

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

            return View(vm);
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
                // Decode base64 photo and save to disk
                if (!string.IsNullOrEmpty(req.PhotoBase64))
                {
                    var base64Data = req.PhotoBase64;
                    string extension = ".jpg";

                    // Strip the data URI prefix: data:image/png;base64,....
                    if (base64Data.Contains(','))
                    {
                        var header = base64Data[..base64Data.IndexOf(',')];
                        base64Data = base64Data[(base64Data.IndexOf(',') + 1)..];
                        if (header.Contains("png")) extension = ".png";
                        else if (header.Contains("gif")) extension = ".gif";
                        else if (header.Contains("webp")) extension = ".webp";
                    }

                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "dogs");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsDir, fileName);
                    await System.IO.File.WriteAllBytesAsync(filePath, Convert.FromBase64String(base64Data));
                    req.PhotoUrl = $"/uploads/dogs/{fileName}";
                }

                req.CompanyId = Convert.ToInt32(userSession!.CompanyId);
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
                    var userSessionJson = HttpContext.Session.GetString("UserSession");
                    var userSession = string.IsNullOrEmpty(userSessionJson)
                        ? null
                        : System.Text.Json.JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);
                    dict["CompanyId"] = Convert.ToInt32(userSession?.CompanyId);
                }

                var resp = await _apiClient.PutAsync<ApiResponse<AnimalDto>>(
                    $"api/animal/UpdatePet/{id}", dict);
                return Json(new { success = resp?.Success ?? false, message = resp?.Message ?? "Updated." });
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

                // Save file to wwwroot/uploads/dogs directory
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "dogs");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileUrl = $"/uploads/dogs/{fileName}";
                return Json(new { success = true, url = fileUrl, message = "File uploaded successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Upload failed: {ex.Message}" });
            }
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

        // GET /Dogs/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            try
            {
                // Get full dog profile with all related data
                var resp = await _apiClient.GetAsync<ApiResponse<DogProfileDto>>($"api/animal/GetDogProfile/{id}");
                
                //if (resp?.Success != true || resp.Data == null)
                //{
                //    TempData["Error"] = resp?.Message ?? "Dog not found.";
                //    return RedirectToAction("Index");
                //}

                // Map DogProfileDto to the view model that Details.cshtml expects
                return View(resp.Data);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Unable to load dog details: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST /Dogs/MarkAsDeceased/{id}
        [HttpPost]
        public async Task<IActionResult> MarkAsDeceased(int id, [FromForm] DateTime? dateOfDeath)
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
                    PhotoUrl = current.PhotoUrl,
                    SireId = current.SireId,
                    DamId = current.DamId,
                    CompanyId = current.CompanyId,
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
                    $"api/animal/GetPets?CompanyId={Convert.ToInt32(userSession!.CompanyId)}");

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
                req.CompanyId = Convert.ToInt32(userSession!.CompanyId);
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
                req.CompanyId = Convert.ToInt32(userSession!.CompanyId);
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
                req.CompanyId = Convert.ToInt32(userSession!.CompanyId);
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
                    $"api/animal/GetMatingRecords?CompanyId={Convert.ToInt32(userSession!.CompanyId)}");

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
                req.CompanyId = Convert.ToInt32(userSession!.CompanyId);
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
                    $"api/animal/GetLitters?CompanyId={Convert.ToInt32(userSession!.CompanyId)}");

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

