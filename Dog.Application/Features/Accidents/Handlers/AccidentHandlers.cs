using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Accidents.Commands;
using Dog.Application.Features.Accidents.DTOs;
using Dog.Application.Features.Accidents.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.Accidents.Handlers;

public class CreateAccidentHandler : IRequestHandler<CreateAccidentCommand, ApiResponse<AccidentDto>>
{
    private readonly IUnitOfWork _uow;

    public CreateAccidentHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<AccidentDto>> Handle(CreateAccidentCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        var dog = await _uow.Repository<Dogtbl>()
            .TableNoTracking
            .FirstOrDefaultAsync(d => d.Id == dto.DogId && d.CompanyId == dto.CompanyId, ct);

        if (dog == null)
            return ApiResponse<AccidentDto>.Fail("Dog not found.");

        var accident = new Accident
        {
            DogId = dto.DogId,
            CompanyId = dto.CompanyId,
            AccidentDate = dto.AccidentDate,
            AccidentType = (AccidentType)dto.AccidentType,
            Severity = (AccidentSeverity)dto.Severity,
            Description = dto.Description,
            Location = dto.Location,
            Resolution = dto.Resolution,
            IsResolved = dto.IsResolved,
            PersonName = dto.PersonName,
            PersonPhoneNumber = dto.PersonPhoneNumber,
            PersonEmail = dto.PersonEmail,
            MedicalTreatmentProvided = dto.MedicalTreatmentProvided,
            HospitalVisitRequired = dto.HospitalVisitRequired,
            Symptoms = dto.Symptoms,
            Treatment = dto.Treatment,
            VeterinarianName = dto.VeterinarianName,
            RecoveryDate = dto.RecoveryDate,
            EstimatedDamageCost = dto.EstimatedDamageCost,
            DamageDescription = dto.DamageDescription,
            InsuranceClaimFiled = dto.InsuranceClaimFiled,
            InsuranceClaimNumber = dto.InsuranceClaimNumber
        };

        await _uow.Repository<Accident>().AddAsync(accident, ct);
        return ApiResponse<AccidentDto>.Ok(AccidentMapper.MapAccident(accident, dog.Name), "Accident recorded successfully.");
    }
}

public class UpdateAccidentHandler : IRequestHandler<UpdateAccidentCommand, ApiResponse<AccidentDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateAccidentHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<AccidentDto>> Handle(UpdateAccidentCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Accident>();
        var accident = await repo.GetByIdAsync(request.Dto.Id, ct);

        if (accident == null)
            return ApiResponse<AccidentDto>.Fail("Accident not found.");

        var dog = await _uow.Repository<Dogtbl>()
            .TableNoTracking
            .FirstOrDefaultAsync(d => d.Id == accident.DogId, ct);

        accident.AccidentDate = request.Dto.AccidentDate;
        accident.AccidentType = (AccidentType)request.Dto.AccidentType;
        accident.Severity = (AccidentSeverity)request.Dto.Severity;
        accident.Description = request.Dto.Description;
        accident.Location = request.Dto.Location;
        accident.Resolution = request.Dto.Resolution;
        accident.IsResolved = request.Dto.IsResolved;
        accident.PersonName = request.Dto.PersonName;
        accident.PersonPhoneNumber = request.Dto.PersonPhoneNumber;
        accident.PersonEmail = request.Dto.PersonEmail;
        accident.MedicalTreatmentProvided = request.Dto.MedicalTreatmentProvided;
        accident.HospitalVisitRequired = request.Dto.HospitalVisitRequired;
        accident.Symptoms = request.Dto.Symptoms;
        accident.Treatment = request.Dto.Treatment;
        accident.VeterinarianName = request.Dto.VeterinarianName;
        accident.RecoveryDate = request.Dto.RecoveryDate;
        accident.EstimatedDamageCost = request.Dto.EstimatedDamageCost;
        accident.DamageDescription = request.Dto.DamageDescription;
        accident.InsuranceClaimFiled = request.Dto.InsuranceClaimFiled;
        accident.InsuranceClaimNumber = request.Dto.InsuranceClaimNumber;

        await repo.UpdateAsync(accident, ct);
        return ApiResponse<AccidentDto>.Ok(AccidentMapper.MapAccident(accident, dog?.Name ?? "Unknown"), "Accident updated successfully.");
    }
}

public class DeleteAccidentHandler : IRequestHandler<DeleteAccidentCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _uow;

    public DeleteAccidentHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(DeleteAccidentCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Accident>();
        var accident = await repo.GetByIdAsync(request.Id, ct);

        if (accident == null)
            return ApiResponse<bool>.Fail("Accident not found.");

        await repo.DeleteAsync(accident, ct);
        return ApiResponse<bool>.Ok(true, "Accident deleted successfully.");
    }
}

public class MarkAccidentResolvedHandler : IRequestHandler<MarkAccidentResolvedCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _uow;

    public MarkAccidentResolvedHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(MarkAccidentResolvedCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Accident>();
        var accident = await repo.GetByIdAsync(request.Id, ct);

        if (accident == null)
            return ApiResponse<bool>.Fail("Accident not found.");

        accident.IsResolved = true;
        accident.Resolution = request.Resolution;
        await repo.UpdateAsync(accident, ct);

        return ApiResponse<bool>.Ok(true, "Accident marked as resolved.");
    }
}

public class GetAccidentByIdHandler : IRequestHandler<GetAccidentByIdQuery, ApiResponse<AccidentDto>>
{
    private readonly IUnitOfWork _uow;

    public GetAccidentByIdHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<AccidentDto>> Handle(GetAccidentByIdQuery request, CancellationToken ct)
    {
        var accident = await _uow.Repository<Accident>()
            .TableNoTracking
            .Include(a => a.Dog)
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct);

        if (accident == null)
            return ApiResponse<AccidentDto>.Fail("Accident not found.");

        return ApiResponse<AccidentDto>.Ok(AccidentMapper.MapAccident(accident, accident.Dog?.Name ?? "Unknown"));
    }
}

public class GetAccidentsByDogHandler : IRequestHandler<GetAccidentsByDogQuery, ApiResponse<List<AccidentListDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetAccidentsByDogHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<List<AccidentListDto>>> Handle(GetAccidentsByDogQuery request, CancellationToken ct)
    {
        var accidents = await _uow.Repository<Accident>()
            .TableNoTracking
            .Include(a => a.Dog)
            .Where(a => a.DogId == request.DogId)
            .OrderByDescending(a => a.AccidentDate)
            .ToListAsync(ct);

        var dtos = accidents.Select(a => AccidentMapper.MapAccidentList(a)).ToList();
        return ApiResponse<List<AccidentListDto>>.Ok(dtos);
    }
}

public class GetAccidentsByCompanyHandler : IRequestHandler<GetAccidentsByCompanyQuery, ApiResponse<List<AccidentListDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetAccidentsByCompanyHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<List<AccidentListDto>>> Handle(GetAccidentsByCompanyQuery request, CancellationToken ct)
    {
        var accidents = await _uow.Repository<Accident>()
            .TableNoTracking
            .Include(a => a.Dog)
            .Where(a => a.CompanyId == request.CompanyId)
            .OrderByDescending(a => a.AccidentDate)
            .ToListAsync(ct);

        var dtos = accidents.Select(a => AccidentMapper.MapAccidentList(a)).ToList();
        return ApiResponse<List<AccidentListDto>>.Ok(dtos);
    }
}

public class GetAllAccidentsHandler : IRequestHandler<GetAllAccidentsQuery, ApiResponse<List<AccidentListDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetAllAccidentsHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<List<AccidentListDto>>> Handle(GetAllAccidentsQuery request, CancellationToken ct)
    {
        var accidents = await _uow.Repository<Accident>()
            .TableNoTracking
            .Include(a => a.Dog)
            .Where(a => a.CompanyId == request.CompanyId)
            .OrderByDescending(a => a.AccidentDate)
            .ToListAsync(ct);

        var dtos = accidents.Select(a => AccidentMapper.MapAccidentList(a)).ToList();
        return ApiResponse<List<AccidentListDto>>.Ok(dtos);
    }
}

public class GetAccidentsByTypeHandler : IRequestHandler<GetAccidentsByTypeQuery, ApiResponse<List<AccidentListDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetAccidentsByTypeHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<List<AccidentListDto>>> Handle(GetAccidentsByTypeQuery request, CancellationToken ct)
    {
        var accidents = await _uow.Repository<Accident>()
            .TableNoTracking
            .Include(a => a.Dog)
            .Where(a => a.CompanyId == request.CompanyId && (int)a.AccidentType == request.AccidentType)
            .OrderByDescending(a => a.AccidentDate)
            .ToListAsync(ct);

        var dtos = accidents.Select(a => AccidentMapper.MapAccidentList(a)).ToList();
        return ApiResponse<List<AccidentListDto>>.Ok(dtos);
    }
}

public class GetAccidentsBySeverityHandler : IRequestHandler<GetAccidentsBySeverityQuery, ApiResponse<List<AccidentListDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetAccidentsBySeverityHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<List<AccidentListDto>>> Handle(GetAccidentsBySeverityQuery request, CancellationToken ct)
    {
        var accidents = await _uow.Repository<Accident>()
            .TableNoTracking
            .Include(a => a.Dog)
            .Where(a => a.CompanyId == request.CompanyId && (int)a.Severity >= request.Severity)
            .OrderByDescending(a => a.AccidentDate)
            .ToListAsync(ct);

        var dtos = accidents.Select(a => AccidentMapper.MapAccidentList(a)).ToList();
        return ApiResponse<List<AccidentListDto>>.Ok(dtos);
    }
}

public class GetUnresolvedAccidentsHandler : IRequestHandler<GetUnresolvedAccidentsQuery, ApiResponse<List<AccidentListDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetUnresolvedAccidentsHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<List<AccidentListDto>>> Handle(GetUnresolvedAccidentsQuery request, CancellationToken ct)
    {
        var accidents = await _uow.Repository<Accident>()
            .TableNoTracking
            .Include(a => a.Dog)
            .Where(a => a.CompanyId == request.CompanyId && !a.IsResolved)
            .OrderByDescending(a => a.AccidentDate)
            .ToListAsync(ct);

        var dtos = accidents.Select(a => AccidentMapper.MapAccidentList(a)).ToList();
        return ApiResponse<List<AccidentListDto>>.Ok(dtos);
    }
}

internal static class AccidentMapper
{
    public static AccidentDto MapAccident(Accident a, string dogName) => new()
    {
        Id = a.Id,
        DogId = a.DogId,
        DogName = dogName,
        AccidentDate = a.AccidentDate,
        AccidentType = (int)a.AccidentType,
        AccidentTypeLabel = GetAccidentTypeLabel(a.AccidentType),
        Severity = (int)a.Severity,
        SeverityLabel = GetSeverityLabel(a.Severity),
        Description = a.Description,
        Location = a.Location,
        Resolution = a.Resolution,
        IsResolved = a.IsResolved,
        CompanyId = a.CompanyId,
        PersonName = a.PersonName,
        PersonPhoneNumber = a.PersonPhoneNumber,
        PersonEmail = a.PersonEmail,
        MedicalTreatmentProvided = a.MedicalTreatmentProvided,
        HospitalVisitRequired = a.HospitalVisitRequired,
        Symptoms = a.Symptoms,
        Treatment = a.Treatment,
        VeterinarianName = a.VeterinarianName,
        RecoveryDate = a.RecoveryDate,
        EstimatedDamageCost = a.EstimatedDamageCost,
        DamageDescription = a.DamageDescription,
        InsuranceClaimFiled = a.InsuranceClaimFiled,
        InsuranceClaimNumber = a.InsuranceClaimNumber
    };

    public static AccidentListDto MapAccidentList(Accident a) => new()
    {
        Id = a.Id,
        DogId = a.DogId,
        DogName = a.Dog?.Name ?? "Unknown",
        AccidentDate = a.AccidentDate,
        AccidentTypeLabel = GetAccidentTypeLabel(a.AccidentType),
        SeverityLabel = GetSeverityLabel(a.Severity),
        Description = a.Description,
        Location = a.Location,
        IsResolved = a.IsResolved
    };

    private static string GetAccidentTypeLabel(AccidentType type) => type switch
    {
        AccidentType.BitingIncident => "Biting Incident",
        AccidentType.SicknessRecord => "Sickness Record",
        AccidentType.PropertyDamage => "Property Damage",
        _ => "Unknown"
    };

    private static string GetSeverityLabel(AccidentSeverity severity) => severity switch
    {
        AccidentSeverity.Low => "Low",
        AccidentSeverity.Medium => "Medium",
        AccidentSeverity.High => "High",
        AccidentSeverity.Critical => "Critical",
        _ => "Unknown"
    };
}
