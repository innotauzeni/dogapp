using Dog.Application.Authorization;
using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Animals.Commands;
using Dog.Application.Features.Animals.DTOs;
using Dog.Application.Features.Animals.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnimalController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IFileStorageService _storage;

        public AnimalController(IMediator mediator, IFileStorageService storage)
        {
            _mediator = mediator;
            _storage = storage;
        }

        private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
        private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

        /// <summary>Upload a dog photo to Cloudflare R2; returns the public URL.</summary>
        [HttpPost("UploadPhoto")]
        public async Task<IActionResult> UploadPhoto(IFormFile file, CancellationToken ct)
        {
            // Allowed for anyone who can add or edit pets.
            var canUpload = User.Claims.Any(c => c.Type == "permission" && (c.Value == "pet.create" || c.Value == "pet.edit"));
            if (!canUpload)
                return Ok(ApiResponse<string>.Fail("You don't have permission to upload pet photos."));

            if (!_storage.IsEnabled)
                return Ok(ApiResponse<string>.Fail("Image storage is not configured on the server."));
            if (file == null || file.Length == 0)
                return Ok(ApiResponse<string>.Fail("No file uploaded."));
            if (file.Length > MaxImageBytes)
                return Ok(ApiResponse<string>.Fail("Image must be 5 MB or smaller."));
            if (!AllowedImageTypes.Contains(file.ContentType))
                return Ok(ApiResponse<string>.Fail("Only JPG, PNG, GIF and WebP images are allowed."));

            await using var stream = file.OpenReadStream();
            var url = await _storage.UploadAsync(stream, file.FileName, file.ContentType, "dogs", ct);
            return Ok(ApiResponse<string>.Ok(url, "Uploaded."));
        }

        [HttpGet("GetPets")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetPets(int CompanyId, CancellationToken ct)
           => Ok(await _mediator.Send(new GetAnimalsByCompanyQuery(CompanyId), ct));

        [HttpGet("GetAssignedDogs")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetAssignedDogs(string handlerUserId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetAssignedDogsQuery(handlerUserId), ct));


        [HttpPost("CreatePet")]
        [HasPermission("pet.create")]
        public async Task<IActionResult> CreatePet([FromBody] CreateAnimalDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateAnimalCommand(dto), ct));


        [HttpPut("UpdatePet/{id:int}")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> UpdatePet(int id, [FromBody] UpdateAnimalDto dto, CancellationToken ct)
        {
            dto.Id = id;
            return Ok(await _mediator.Send(new UpdateAnimalCommand(dto), ct));
        }

        [HttpDelete("DeletePet/{id:int}")]
        [HasPermission("pet.delete")]
        public async Task<IActionResult> DeletePet(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteAnimalCommand(id), ct));

        [HttpPost("ToggleStatus/{id:int}")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new ToggleAnimalCommand(id), ct));

        /// <summary>Assign a dog to a customer (or unassign when CustomerId is null).</summary>
        [HttpPost("AssignCustomer")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> AssignCustomer([FromBody] AssignCustomerDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new AssignAnimalCustomerCommand(dto.DogId, dto.CustomerId), ct));

        [HttpPost("AssignDog")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> AssignDog([FromBody] AssignDogDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new AssignDogCommand(dto), ct));

        [HttpPost("AddCareRecord")]
        // Accessible to any authenticated user — authorization is handled by checking active assignments in the handler layer
        public async Task<IActionResult> AddCareRecord([FromBody] AddDogCareRecordDto dto, CancellationToken ct)
        {
            dto.RecordedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Ok(await _mediator.Send(new AddDogCareRecordCommand(dto), ct));
        }

        // ── Dog gallery ────────────────────────────────────────────────────────
        // Stored as document attachments (EntityType "DogGallery") but guarded by the dog's own
        // permissions (pet.view / pet.edit) so users don't also need the separate document.* ones.
        private const string GalleryType = "DogGallery";

        [HttpGet("Gallery/{dogId:int}")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetGallery(int dogId, CancellationToken ct)
            => Ok(await _mediator.Send(new Dog.Application.Features.Documents.Queries.GetDocumentsByEntityQuery(GalleryType, dogId), ct));

        [HttpPost("GalleryAdd")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> AddGalleryImage([FromBody] Dog.Application.Features.Documents.DTOs.CreateDocumentAttachmentDto dto, CancellationToken ct)
        {
            dto.EntityType = GalleryType; // never let a caller attach to another entity type through here
            dto.UploadedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Ok(await _mediator.Send(new Dog.Application.Features.Documents.Commands.CreateDocumentAttachmentCommand(dto), ct));
        }

        [HttpDelete("GalleryDelete/{dogId:int}/{id:int}")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> DeleteGalleryImage(int dogId, int id, CancellationToken ct)
        {
            // Only allow deleting an image that really belongs to this dog's gallery.
            var list = await _mediator.Send(new Dog.Application.Features.Documents.Queries.GetDocumentsByEntityQuery(GalleryType, dogId), ct);
            if (list?.Data == null || !list.Data.Any(d => d.Id == id))
                return Ok(ApiResponse<bool>.Fail("Photo not found in this dog's gallery."));
            return Ok(await _mediator.Send(new Dog.Application.Features.Documents.Commands.DeleteDocumentAttachmentCommand(id), ct));
        }

        [HttpGet("GetDogProfile/{id:int}")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetDogProfile(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new GetDogProfileQuery(id), ct));

        /// <summary>Dogs with vaccinations due soon or overdue (for alerts).</summary>
        [HttpGet("vaccine-due")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetVaccineDue(int companyId, int days = 30, CancellationToken ct = default)
            => Ok(await _mediator.Send(new GetVaccineDueQuery(companyId, days), ct));

        /// <summary>Care activities recorded by the current handler, newest first.</summary>
        [HttpGet("GetHandlerActivities")]
        public async Task<IActionResult> GetHandlerActivities(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Ok(await _mediator.Send(new GetHandlerActivitiesQuery(userId ?? string.Empty), ct));
        }

        /// <summary>Edit a care record. The API enforces that only the recording handler can update it.</summary>
        [HttpPut("UpdateCareRecord/{id:int}")]
        public async Task<IActionResult> UpdateCareRecord(int id, [FromBody] UpdateDogCareRecordDto dto, CancellationToken ct)
        {
            dto.Id = id;
            dto.RecordedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Ok(await _mediator.Send(new UpdateDogCareRecordCommand(dto), ct));
        }

        [HttpPost("AddVaccination")]
        // Accessible to any authenticated user — the mobile app also lets Dog
        // Handlers record vaccinations (e.g. for puppies), and they don't hold
        // the "pet.edit" permission, matching AddCareRecord's access model.
        public async Task<IActionResult> AddVaccination([FromBody] AddVaccinationDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new AddVaccinationCommand(dto), ct));

        [HttpGet("GetPuppyVaccinations/{puppyId:int}")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetPuppyVaccinations(int puppyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetPuppyVaccinationsQuery(puppyId), ct));

        [HttpPost("AddMatingRecord")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> AddMatingRecord([FromBody] AddMatingRecordDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new AddMatingRecordCommand(dto), ct));

        /// <summary>Add a mating record; accessible to authenticated handlers without explicit permission check.</summary>
        [HttpPost("handler/AddMatingRecord")]
        public async Task<IActionResult> HandlerAddMatingRecord([FromBody] AddMatingRecordDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new AddMatingRecordCommand(dto), ct));

        [HttpPost("AddLitter")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> AddLitter([FromBody] AddLitterDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new AddLitterCommand(dto), ct));

        /// <summary>Add a litter; accessible to authenticated handlers without explicit permission check.</summary>
        [HttpPost("handler/AddLitter")]
        public async Task<IActionResult> HandlerAddLitter([FromBody] AddLitterDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new AddLitterCommand(dto), ct));

        [HttpGet("GetMatingRecords")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetMatingRecords(int CompanyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetMatingRecordsByCompanyQuery(CompanyId), ct));

        [HttpPost("AddPuppy")]
        [HasPermission("pet.edit")]
        public async Task<IActionResult> AddPuppy([FromBody] AddPuppyDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new AddPuppyCommand(dto), ct));

        /// <summary>Add a puppy; accessible to authenticated handlers without explicit permission check.</summary>
        [HttpPost("handler/AddPuppy")]
        public async Task<IActionResult> HandlerAddPuppy([FromBody] AddPuppyDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new AddPuppyCommand(dto), ct));

        [HttpGet("GetLitters")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetLitters(int CompanyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetLittersByCompanyQuery(CompanyId), ct));

        // Reports endpoints
        [HttpGet]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetAnimals([FromQuery] int page = 1, [FromQuery] int pageSize = 15, [FromQuery] string? search = null, [FromQuery] string? status = null, [FromQuery] string? gender = null, CancellationToken ct = default)
        {
            var query = new GetAnimalsQuery 
            { 
                Page = page, 
                PageSize = pageSize, 
                Search = search,
                Status = status,
                Gender = gender
            };
            return Ok(await _mediator.Send(query, ct));
        }

        [HttpGet("vaccinated-count")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetVaccinatedDogsCount([FromQuery] int? companyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetVaccinatedDogsCountQuery(companyId), ct));

        [HttpGet("vaccinated")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetVaccinatedDogs([FromQuery] int? companyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetVaccinatedDogsQuery(companyId), ct));

        [HttpGet("litters-count")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetLittersCount([FromQuery] int? companyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetLittersCountQuery(companyId), ct));

        [HttpGet("litters")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetLittersForReport(CancellationToken ct)
            => Ok(await _mediator.Send(new GetLittersForReportQuery(), ct));

        [HttpGet("puppies")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetPuppiesForReport(CancellationToken ct)
            => Ok(await _mediator.Send(new GetPuppiesForReportQuery(), ct));

        [HttpGet("mating-count")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetMatingRecordsCount([FromQuery] int? companyId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetMatingRecordsCountQuery(companyId), ct));

        [HttpGet("mating-records")]
        [HasPermission("pet.view")]
        public async Task<IActionResult> GetMatingRecordsForReport(CancellationToken ct)
            => Ok(await _mediator.Send(new GetMatingRecordsForReportQuery(), ct));

    }








}
