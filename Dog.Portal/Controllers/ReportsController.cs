using Dog.Portal.Models;
using Dog.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using PortalModels = Dog.Portal.Models;

namespace Dog.Portal.Controllers
{
    public class ReportsController : Controller
    {
        private readonly IApiClient _apiClient;

        public ReportsController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private bool IsAuthenticated() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("UserSession"));

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

        /// <summary>Best-effort image fetch for PDFs. Absolute URLs are downloaded; anything else / any failure → null.</summary>
        private Task<byte[]?> TryLoadImageAsync(string? url)
            => PdfImageLoader.LoadAsync(url, $"{Request.Scheme}://{Request.Host}");

        public async Task<IActionResult> DetailReport(string? eventType = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            try
            {
                var dogs = await _apiClient.GetAsync<ApiResponse<List<AnimalDto>>>("api/animals");
                var allDogs = dogs?.Data ?? new List<AnimalDto>();

                // Filter by event type if specified
                if (!string.IsNullOrEmpty(eventType))
                {
                    allDogs = allDogs.Where(d =>
                        d.CareRecords != null &&
                        d.CareRecords.Any(c => c.EventType == eventType)
                    ).ToList();
                }

                ViewBag.EventType = eventType ?? "All Events";
                ViewBag.Count = allDogs.Count;
                return View("DetailReport", allDogs);
            }
            catch (Exception ex)
            {
                ViewBag.FetchError = $"Error loading report: {ex.Message}";
                return View("DetailReport", new List<AnimalDto>());
            }
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var vm = new ReportsViewModel();

            try
            {
                // Get report statistics from API
                var dogsResp = await _apiClient.GetAsync<ApiResponse<object>>("api/Animal?pageSize=1");

                // Parse dogs count
                if (dogsResp?.Success == true && dogsResp.Data != null)
                {
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(dogsResp.Data);
                    var dogsData = System.Text.Json.JsonSerializer.Deserialize<AnimalPageDto>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    vm.TotalDogs = dogsData?.Total ?? 0;
                }
                else
                {
                    vm.TotalDogs = 0;
                }

                // For now, set default values for other statistics since the API endpoints might not exist yet
                vm.VaccinatedDogs = 0;
                vm.TotalLitters = 0;
                vm.TotalMatingRecords = 0;

                // Try to get other statistics, but don't fail if endpoints don't exist
                try
                {
                    var vaccinatedResp = await _apiClient.GetAsync<ApiResponse<object>>("api/Animal/vaccinated-count");
                    if (vaccinatedResp?.Success == true && vaccinatedResp.Data != null)
                    {
                        var jsonString = System.Text.Json.JsonSerializer.Serialize(vaccinatedResp.Data);
                        var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);
                        if (jsonDoc.RootElement.TryGetProperty("count", out var countElement))
                        {
                            vm.VaccinatedDogs = countElement.GetInt32();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Vaccinated dogs count error: {ex.Message}");
                }

                try
                {
                    var littersResp = await _apiClient.GetAsync<ApiResponse<object>>("api/Animal/litters-count");
                    if (littersResp?.Success == true && littersResp.Data != null)
                    {
                        var jsonString = System.Text.Json.JsonSerializer.Serialize(littersResp.Data);
                        var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);
                        if (jsonDoc.RootElement.TryGetProperty("count", out var countElement))
                        {
                            vm.TotalLitters = countElement.GetInt32();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Litters count error: {ex.Message}");
                }

                try
                {
                    var matingResp = await _apiClient.GetAsync<ApiResponse<object>>("api/Animal/mating-count");
                    if (matingResp?.Success == true && matingResp.Data != null)
                    {
                        var jsonString = System.Text.Json.JsonSerializer.Serialize(matingResp.Data);
                        var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);
                        if (jsonDoc.RootElement.TryGetProperty("count", out var countElement))
                        {
                            vm.TotalMatingRecords = countElement.GetInt32();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Mating records count error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                ViewBag.FetchError = $"Unable to load report statistics. Error: {ex.Message}";
                Console.WriteLine($"Reports Error: {ex}");
                
                // Set default values on error
                vm.TotalDogs = 0;
                vm.VaccinatedDogs = 0;
                vm.TotalLitters = 0;
                vm.TotalMatingRecords = 0;
            }

            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson) ? null : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);
            ViewBag.CanGenerateHandlerCert = userSession?.Role == "Admin" || userSession?.Role == "SystemOwner";

            return View(vm);
        }

        // ── Browsable report pages (in addition to the raw CSV/PDF exports) ────────

        public async Task<IActionResult> Vaccinated()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<object>>("api/Animal/vaccinated");
                var data = resp?.Success == true && resp.Data != null
                    ? JsonSerializer.Deserialize<List<VaccinatedDogDto>>(JsonSerializer.Serialize(resp.Data), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    : null;
                return View(data ?? new List<VaccinatedDogDto>());
            }
            catch (Exception ex)
            {
                ViewBag.FetchError = $"Error loading vaccinated dogs: {ex.Message}";
                return View(new List<VaccinatedDogDto>());
            }
        }

        public async Task<IActionResult> Puppies()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<object>>("api/Animal/puppies");
                var data = resp?.Success == true && resp.Data != null
                    ? JsonSerializer.Deserialize<List<PuppyReportDto>>(JsonSerializer.Serialize(resp.Data), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    : null;
                return View(data ?? new List<PuppyReportDto>());
            }
            catch (Exception ex)
            {
                ViewBag.FetchError = $"Error loading puppies: {ex.Message}";
                return View(new List<PuppyReportDto>());
            }
        }

        public async Task<IActionResult> Litters()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<object>>("api/Animal/litters");
                var data = resp?.Success == true && resp.Data != null
                    ? JsonSerializer.Deserialize<List<LitterReportDto>>(JsonSerializer.Serialize(resp.Data), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    : null;
                return View(data ?? new List<LitterReportDto>());
            }
            catch (Exception ex)
            {
                ViewBag.FetchError = $"Error loading litters: {ex.Message}";
                return View(new List<LitterReportDto>());
            }
        }

        public async Task<IActionResult> Matings()
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<object>>("api/Animal/mating-records");
                var data = resp?.Success == true && resp.Data != null
                    ? JsonSerializer.Deserialize<List<MatingRecordReportDto>>(JsonSerializer.Serialize(resp.Data), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    : null;
                return View(data ?? new List<MatingRecordReportDto>());
            }
            catch (Exception ex)
            {
                ViewBag.FetchError = $"Error loading mating records: {ex.Message}";
                return View(new List<MatingRecordReportDto>());
            }
        }

        // Export Puppies Report
        [HttpGet]
        public async Task<IActionResult> ExportPuppies(string format = "csv")
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<object>>("api/Animal/puppies");
                if (resp?.Success == true && resp.Data != null)
                {
                    var data = JsonSerializer.Deserialize<List<PuppyReportDto>>(JsonSerializer.Serialize(resp.Data), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<PuppyReportDto>();
                    return format.ToLower() == "pdf" ? await GeneratePuppiesPdf(data) : GeneratePuppiesCsv(data);
                }
                return BadRequest("Failed to export puppies data");
            }
            catch (Exception ex)
            {
                return BadRequest($"Export failed: {ex.Message}");
            }
        }

        // Export Dogs Report
        [HttpGet]
        public async Task<IActionResult> ExportDogs(string format = "csv", string? status = null, string? gender = null)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={status}");
                if (!string.IsNullOrEmpty(gender)) queryParams.Add($"gender={gender}");
                queryParams.Add("pageSize=10000"); // Get all records for export

                var queryString = string.Join("&", queryParams);
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Animal?{queryString}");

                if (resp?.Success == true && resp.Data != null)
                {
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(resp.Data);
                    var animalsData = System.Text.Json.JsonSerializer.Deserialize<AnimalPageDto>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (format.ToLower() == "pdf")
                    {
                        return await GenerateDogsPdf(animalsData?.Data ?? new List<AnimalItemDto>());
                    }
                    else
                    {
                        return GenerateDogsCsv(animalsData?.Data ?? new List<AnimalItemDto>());
                    }
                }

                return BadRequest("Failed to export dogs data");
            }
            catch (Exception ex)
            {
                return BadRequest($"Export failed: {ex.Message}");
            }
        }

        // Export Vaccinated Dogs Report
        [HttpGet]
        public async Task<IActionResult> ExportVaccinatedDogs(string format = "csv")
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<object>>("api/Animal/vaccinated");

                if (resp?.Success == true && resp.Data != null)
                {
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(resp.Data);
                    var vaccinatedData = System.Text.Json.JsonSerializer.Deserialize<List<VaccinatedDogDto>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (format.ToLower() == "pdf")
                    {
                        return await GenerateVaccinatedDogsPdf(vaccinatedData ?? new List<VaccinatedDogDto>());
                    }
                    else
                    {
                        return GenerateVaccinatedDogsCsv(vaccinatedData ?? new List<VaccinatedDogDto>());
                    }
                }

                return BadRequest("Failed to export vaccinated dogs data");
            }
            catch (Exception ex)
            {
                return BadRequest($"Export failed: {ex.Message}");
            }
        }

        // Export Litters Report
        [HttpGet]
        public async Task<IActionResult> ExportLitters(string format = "csv")
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<object>>("api/Animal/litters");

                if (resp?.Success == true && resp.Data != null)
                {
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(resp.Data);
                    var littersData = System.Text.Json.JsonSerializer.Deserialize<List<LitterReportDto>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (format.ToLower() == "pdf")
                    {
                        return await GenerateLittersPdf(littersData ?? new List<LitterReportDto>());
                    }
                    else
                    {
                        return GenerateLittersCsv(littersData ?? new List<LitterReportDto>());
                    }
                }

                return BadRequest("Failed to export litters data");
            }
            catch (Exception ex)
            {
                return BadRequest($"Export failed: {ex.Message}");
            }
        }

        // Export Mating Records Report
        [HttpGet]
        public async Task<IActionResult> ExportMatingRecords(string format = "csv")
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<object>>("api/Animal/mating-records");

                if (resp?.Success == true && resp.Data != null)
                {
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(resp.Data);
                    var matingData = System.Text.Json.JsonSerializer.Deserialize<List<MatingRecordReportDto>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (format.ToLower() == "pdf")
                    {
                        return await GenerateMatingRecordsPdf(matingData ?? new List<MatingRecordReportDto>());
                    }
                    else
                    {
                        return GenerateMatingRecordsCsv(matingData ?? new List<MatingRecordReportDto>());
                    }
                }

                return BadRequest("Failed to export mating records data");
            }
            catch (Exception ex)
            {
                return BadRequest($"Export failed: {ex.Message}");
            }
        }

        // Generate Dog Certificate
        [HttpGet]
        public async Task<IActionResult> DogCertificate(int id)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<object>>($"api/Animal/GetDogProfile/{id}");

                if (resp?.Success == true && resp.Data != null)
                {
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(resp.Data);
                    var dogProfile = System.Text.Json.JsonSerializer.Deserialize<DogProfileDto>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (dogProfile != null)
                    {
                        // Company branding (optional — never blocks the certificate)
                        CompanyDto? company = null;
                        try
                        {
                            var compResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/companies/{dogProfile.Dog.CompanyId}");
                            if (compResp?.Data != null)
                                company = System.Text.Json.JsonSerializer.Deserialize<CompanyDto>(
                                    System.Text.Json.JsonSerializer.Serialize(compResp.Data), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                        catch { /* branding is optional */ }

                        var logo = await TryLoadImageAsync(company?.Logo);
                        var photo = await TryLoadImageAsync(dogProfile.Dog.PhotoUrl);

                        var pdf = CertificatePdfService.Build(dogProfile, company, logo, photo);
                        return File(pdf, "application/pdf", $"Dog_Certificate_{dogProfile.Dog.Name}_{DateTime.Now:yyyyMMdd}.pdf");
                    }
                }

                return BadRequest("Failed to generate dog certificate");
            }
            catch (Exception ex)
            {
                return BadRequest($"Certificate generation failed: {ex.Message}");
            }
        }

        // GET /Reports/GetHandlersForDropdown — every user in the active company, for
        // the Handler Training Certificate form's "Handler" select.
        [HttpGet]
        public async Task<IActionResult> GetHandlersForDropdown()
        {
            if (!IsAuthenticated()) return Json(new List<object>());
            try
            {
                var resp = await _apiClient.GetAsync<ApiResponse<List<UserDto>>>("api/users");
                var users = resp?.Data ?? new List<UserDto>();
                return Json(users.Select(u => new { id = u.Id, name = u.FullName }));
            }
            catch
            {
                return Json(new List<object>());
            }
        }

        // GET /Reports/HandlerCertificate — K9 handler training certificate. Admin/SystemOwner
        // only (checked here rather than [HasPermission] since Portal controllers use
        // session-role checks, not the API's permission attribute system). Generated
        // on demand from admin-entered values; nothing is persisted, matching how the
        // Dog Registration Certificate above already works.
        [HttpGet]
        public async Task<IActionResult> HandlerCertificate(string handlerName, string? dogName, string trainerName)
        {
            if (!IsAuthenticated()) return RedirectToAction("Login", "Auth");

            var userSessionJson = HttpContext.Session.GetString("UserSession");
            var userSession = string.IsNullOrEmpty(userSessionJson) ? null : JsonSerializer.Deserialize<UserSessionModel>(userSessionJson);
            if (userSession == null || (userSession.Role != "Admin" && userSession.Role != "SystemOwner"))
                return StatusCode(403, "Only an Admin or SystemOwner can generate this certificate.");

            if (string.IsNullOrWhiteSpace(handlerName) || string.IsNullOrWhiteSpace(trainerName))
                return BadRequest("Handler and Trainer name are required.");

            try
            {
                var companyId = Convert.ToInt32(userSession.CompanyId);

                Dog.Application.Features.Companies.DTOs.CompanyDto? company = null;
                try
                {
                    var compResp = await _apiClient.GetAsync<ApiResponse<object>>($"api/companies/{companyId}");
                    if (compResp?.Data != null)
                        company = JsonSerializer.Deserialize<Dog.Application.Features.Companies.DTOs.CompanyDto>(
                            JsonSerializer.Serialize(compResp.Data), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch { /* branding is optional */ }

                var logo = await TryLoadImageAsync(company?.Logo);
                var certificateNumber = DateTime.UtcNow.ToString("yyMMddHHmm");

                var pdf = Dog.Application.Common.Pdf.HandlerCertificatePdfBuilder.Build(
                    handlerName.Trim(),
                    string.IsNullOrWhiteSpace(dogName) ? null : dogName.Trim(),
                    trainerName.Trim(),
                    DateTime.Now,
                    certificateNumber,
                    company,
                    logo);

                return File(pdf, "application/pdf", $"Handler_Certificate_{handlerName.Trim().Replace(' ', '_')}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest($"Certificate generation failed: {ex.Message}");
            }
        }

        // CSV Generation Methods
        private FileResult GenerateDogsCsv(List<AnimalItemDto> dogs)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Name,Breed,Color,Gender,Birth Date,Microchip,Status,Owner");

            foreach (var dog in dogs)
            {
                csv.AppendLine($"{dog.Name},{dog.Breed},{dog.Color},{dog.Gender},{dog.BirthDate:yyyy-MM-dd},{dog.MicrochipNumber},{(dog.IsActive ? "Active" : "Inactive")},{dog.OwnerName}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Dogs_Report_{DateTime.Now:yyyyMMdd}.csv");
        }

        private FileResult GenerateVaccinatedDogsCsv(List<VaccinatedDogDto> dogs)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Dog Name,Breed,Last Vaccination,Vaccine Type,Next Due Date,Status");

            foreach (var dog in dogs)
            {
                csv.AppendLine($"{dog.DogName},{dog.Breed},{dog.LastVaccinationDate:yyyy-MM-dd},{dog.VaccineType},{dog.NextDueDate:yyyy-MM-dd},{dog.VaccinationStatus}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Vaccinated_Dogs_Report_{DateTime.Now:yyyyMMdd}.csv");
        }

        private FileResult GenerateLittersCsv(List<LitterReportDto> litters)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Litter ID,Sire,Dam,Birth Date,Total Puppies,Alive,Deceased");

            foreach (var litter in litters)
            {
                csv.AppendLine($"{litter.Id},{litter.SireName},{litter.DamName},{litter.BirthDate:yyyy-MM-dd},{litter.TotalPuppies},{litter.AlivePuppies},{litter.DeceasedPuppies}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Litters_Report_{DateTime.Now:yyyyMMdd}.csv");
        }

        private FileResult GeneratePuppiesCsv(List<PuppyReportDto> puppies)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Name,Color,Gender,Birth Date,Age,Sire,Dam,Status");

            foreach (var p in puppies)
            {
                csv.AppendLine($"{p.Name},{p.Color},{p.Gender},{p.DateOfBirth:yyyy-MM-dd},{p.AgeDisplay},{p.SireName},{p.DamName},{(p.IsAlive ? "Alive" : "Deceased")}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Puppies_Report_{DateTime.Now:yyyyMMdd}.csv");
        }

        private FileResult GenerateMatingRecordsCsv(List<MatingRecordReportDto> matings)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Mating ID,Sire,Dam,Mating Date,Location,Expected Due Date,Status");

            foreach (var mating in matings)
            {
                csv.AppendLine($"{mating.Id},{mating.SireName},{mating.DamName},{mating.MatingDate:yyyy-MM-dd},{mating.Location},{mating.ExpectedDueDate:yyyy-MM-dd},{mating.Status}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Mating_Records_Report_{DateTime.Now:yyyyMMdd}.csv");
        }

        // PDF Generation Methods (simplified - would use a library like iTextSharp or DinkToPdf)
        private async Task<FileResult> GenerateDogsPdf(List<AnimalItemDto> dogs)
        {
            // This is a placeholder - you would implement actual PDF generation here
            var html = GenerateDogsHtml(dogs);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "text/html", $"Dogs_Report_{DateTime.Now:yyyyMMdd}.html");
        }

        private async Task<FileResult> GenerateVaccinatedDogsPdf(List<VaccinatedDogDto> dogs)
        {
            var html = GenerateVaccinatedDogsHtml(dogs);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "text/html", $"Vaccinated_Dogs_Report_{DateTime.Now:yyyyMMdd}.html");
        }

        private async Task<FileResult> GenerateLittersPdf(List<LitterReportDto> litters)
        {
            var html = GenerateLittersHtml(litters);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "text/html", $"Litters_Report_{DateTime.Now:yyyyMMdd}.html");
        }

        private async Task<FileResult> GeneratePuppiesPdf(List<PuppyReportDto> puppies)
        {
            var html = GeneratePuppiesHtml(puppies);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "text/html", $"Puppies_Report_{DateTime.Now:yyyyMMdd}.html");
        }

        private async Task<FileResult> GenerateMatingRecordsPdf(List<MatingRecordReportDto> matings)
        {
            var html = GenerateMatingRecordsHtml(matings);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "text/html", $"Mating_Records_Report_{DateTime.Now:yyyyMMdd}.html");
        }

        private async Task<FileResult> GenerateDogCertificatePdf(DogProfileDto dog)
        {
            var html = GenerateDogCertificateHtml(dog);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "text/html", $"Dog_Certificate_{dog.Dog.Name}_{DateTime.Now:yyyyMMdd}.html");
        }

        // HTML Generation Methods for PDF conversion
        private string GenerateDogsHtml(List<AnimalItemDto> dogs)
        {
            var html = $@"
                <html>
                <head><title>Dogs Report</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 20px; }}
                    table {{ width: 100%; border-collapse: collapse; }}
                    th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                    th {{ background-color: #f2f2f2; }}
                    h1 {{ color: #333; }}
                </style>
                </head>
                <body>
                <h1>Dogs Report - Generated on {DateTime.Now:yyyy-MM-dd HH:mm}</h1>
                <table>
                <tr><th>Name</th><th>Breed</th><th>Color</th><th>Gender</th><th>Birth Date</th><th>Microchip</th><th>Status</th><th>Owner</th></tr>";

            foreach (var dog in dogs)
            {
                html += $"<tr><td>{dog.Name}</td><td>{dog.Breed}</td><td>{dog.Color}</td><td>{dog.Gender}</td><td>{dog.BirthDate:yyyy-MM-dd}</td><td>{dog.MicrochipNumber}</td><td>{(dog.IsActive ? "Active" : "Inactive")}</td><td>{dog.OwnerName}</td></tr>";
            }

            html += "</table></body></html>";
            return html;
        }

        private string GenerateVaccinatedDogsHtml(List<VaccinatedDogDto> dogs)
        {
            var html = $@"
                <html>
                <head><title>Vaccinated Dogs Report</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 20px; }}
                    table {{ width: 100%; border-collapse: collapse; }}
                    th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                    th {{ background-color: #f2f2f2; }}
                    h1 {{ color: #333; }}
                </style>
                </head>
                <body>
                <h1>Vaccinated Dogs Report - Generated on {DateTime.Now:yyyy-MM-dd HH:mm}</h1>
                <table>
                <tr><th>Dog Name</th><th>Breed</th><th>Last Vaccination</th><th>Vaccine Type</th><th>Next Due Date</th><th>Status</th></tr>";

            foreach (var dog in dogs)
            {
                html += $"<tr><td>{dog.DogName}</td><td>{dog.Breed}</td><td>{dog.LastVaccinationDate:yyyy-MM-dd}</td><td>{dog.VaccineType}</td><td>{dog.NextDueDate:yyyy-MM-dd}</td><td>{dog.VaccinationStatus}</td></tr>";
            }

            html += "</table></body></html>";
            return html;
        }

        private string GenerateLittersHtml(List<LitterReportDto> litters)
        {
            var html = $@"
                <html>
                <head><title>Litters Report</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 20px; }}
                    table {{ width: 100%; border-collapse: collapse; }}
                    th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                    th {{ background-color: #f2f2f2; }}
                    h1 {{ color: #333; }}
                </style>
                </head>
                <body>
                <h1>Litters Report - Generated on {DateTime.Now:yyyy-MM-dd HH:mm}</h1>
                <table>
                <tr><th>Litter ID</th><th>Sire</th><th>Dam</th><th>Birth Date</th><th>Total Puppies</th><th>Alive</th><th>Deceased</th></tr>";

            foreach (var litter in litters)
            {
                html += $"<tr><td>{litter.Id}</td><td>{litter.SireName}</td><td>{litter.DamName}</td><td>{litter.BirthDate:yyyy-MM-dd}</td><td>{litter.TotalPuppies}</td><td>{litter.AlivePuppies}</td><td>{litter.DeceasedPuppies}</td></tr>";
            }

            html += "</table></body></html>";
            return html;
        }

        private string GeneratePuppiesHtml(List<PuppyReportDto> puppies)
        {
            var html = $@"
                <html>
                <head><title>Puppies Report</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 20px; }}
                    table {{ width: 100%; border-collapse: collapse; }}
                    th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                    th {{ background-color: #f2f2f2; }}
                    h1 {{ color: #333; }}
                </style>
                </head>
                <body>
                <h1>Puppies Report - Generated on {DateTime.Now:yyyy-MM-dd HH:mm}</h1>
                <table>
                <tr><th>Name</th><th>Color</th><th>Gender</th><th>Birth Date</th><th>Age</th><th>Sire</th><th>Dam</th><th>Status</th></tr>";

            foreach (var p in puppies)
            {
                html += $"<tr><td>{p.Name}</td><td>{p.Color}</td><td>{p.Gender}</td><td>{p.DateOfBirth:yyyy-MM-dd}</td><td>{p.AgeDisplay}</td><td>{p.SireName}</td><td>{p.DamName}</td><td>{(p.IsAlive ? "Alive" : "Deceased")}</td></tr>";
            }

            html += "</table></body></html>";
            return html;
        }

        private string GenerateMatingRecordsHtml(List<MatingRecordReportDto> matings)
        {
            var html = $@"
                <html>
                <head><title>Mating Records Report</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 20px; }}
                    table {{ width: 100%; border-collapse: collapse; }}
                    th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                    th {{ background-color: #f2f2f2; }}
                    h1 {{ color: #333; }}
                </style>
                </head>
                <body>
                <h1>Mating Records Report - Generated on {DateTime.Now:yyyy-MM-dd HH:mm}</h1>
                <table>
                <tr><th>Mating ID</th><th>Sire</th><th>Dam</th><th>Mating Date</th><th>Location</th><th>Expected Due Date</th><th>Status</th></tr>";

            foreach (var mating in matings)
            {
                html += $"<tr><td>{mating.Id}</td><td>{mating.SireName}</td><td>{mating.DamName}</td><td>{mating.MatingDate:yyyy-MM-dd}</td><td>{mating.Location}</td><td>{mating.ExpectedDueDate:yyyy-MM-dd}</td><td>{mating.Status}</td></tr>";
            }

            html += "</table></body></html>";
            return html;
        }

        private string GenerateDogCertificateHtml(DogProfileDto dog)
        {
            return $@"
                <html>
                <head><title>Dog Certificate - {dog.Dog.Name}</title>
                <style>
                    body {{ font-family: 'Times New Roman', serif; margin: 40px; background: #f9f9f9; }}
                    .certificate {{ background: white; padding: 60px; border: 10px solid #8B4513; border-radius: 20px; text-align: center; }}
                    .header {{ font-size: 36px; font-weight: bold; color: #8B4513; margin-bottom: 20px; }}
                    .subheader {{ font-size: 24px; color: #666; margin-bottom: 40px; }}
                    .dog-info {{ font-size: 18px; line-height: 2; text-align: left; margin: 40px 0; }}
                    .signature {{ margin-top: 60px; }}
                    .date {{ position: absolute; bottom: 20px; right: 20px; }}
                </style>
                </head>
                <body>
                <div class='certificate'>
                    <div class='header'>CERTIFICATE OF REGISTRATION</div>
                    <div class='subheader'>Canine Registration Authority</div>
                    
                    <div class='dog-info'>
                        <strong>Dog Name:</strong> {dog.Dog.Name}<br>
                        <strong>Breed:</strong> {dog.Dog.Breed}<br>
                        <strong>Color:</strong> {dog.Dog.Color}<br>
                        <strong>Gender:</strong> {dog.Dog.Gender}<br>
                        <strong>Birth Date:</strong> {dog.Dog.DateOfBirth:MMMM dd, yyyy}<br>
                        <strong>Microchip Number:</strong> {dog.Dog.MicrochipNumber}<br>
                        <strong>Registration Number:</strong> REG-{dog.Dog.Id:D6}<br>
                        <strong>Owner:</strong> {dog.Dog.CompanyName}
                    </div>
                    
                    <div class='signature'>
                        <p>This certifies that the above-named dog is registered with our authority.</p>
                        <br><br>
                        <div style='border-top: 2px solid #333; width: 300px; margin: 0 auto;'></div>
                        <p><strong>Registrar Signature</strong></p>
                    </div>
                    
                    <div class='date'>
                        <strong>Issue Date:</strong> {DateTime.Now:MMMM dd, yyyy}
                    </div>
                </div>
                </body>
                </html>";
        }
    }
}