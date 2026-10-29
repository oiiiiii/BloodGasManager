using BGShared.Models;
using BGService.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BGService.Api;

public static class PatientEndpoints
{
    public static RouteGroupBuilder MapPatientEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAll);
        group.MapGet("/search", Search);
        group.MapGet("/find", FindPatient);
        group.MapGet("/findbyid", FindByExactPatientId);
        group.MapGet("/{id}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id}", Update);
        group.MapDelete("/{id}", Delete);
        return group;
    }

    private static Ok<List<Patient>> GetAll(IPatientRepository repo, bool onlyActive = false)
    {
        return TypedResults.Ok(repo.GetAll(onlyActive));
    }

    private static Ok<List<Patient>> Search(IPatientRepository repo, string q, int limit = 50)
    {
        return TypedResults.Ok(repo.Search(q, limit));
    }

    private static Results<Ok<Patient>, NotFound> FindPatient(IPatientRepository repo, string q)
    {
        var patient = repo.FindByIdOrBedNoOrName(q);
        if (patient == null) return TypedResults.NotFound();
        return TypedResults.Ok(patient);
    }

    private static Results<Ok<Patient>, NotFound> FindByExactPatientId(IPatientRepository repo, string patientId)
    {
        var patient = repo.FindByPatientId(patientId);
        if (patient == null) return TypedResults.NotFound();
        return TypedResults.Ok(patient);
    }

    private static Results<Ok<Patient>, NotFound> GetById(IPatientRepository repo, string id)
    {
        var patient = repo.GetById(id);
        if (patient == null) return TypedResults.NotFound();
        return TypedResults.Ok(patient);
    }

    private static Results<Created<Patient>, BadRequest<string>> Create([FromBody] Patient patient, IPatientRepository repo)
    {
        if (repo.CheckPatientIdExists(patient.PatientId))
            return TypedResults.BadRequest("病案号已存在");
        
        if (!string.IsNullOrEmpty(patient.BedNo) && repo.CheckBedNoExists(patient.BedNo))
            return TypedResults.BadRequest("床号已存在");
        
        patient.CreatedAt = DateTime.Now;
        patient.UpdatedAt = DateTime.Now;
        patient.IsActive = true;
        repo.Insert(patient);
        return TypedResults.Created($"/api/patients/{patient.PatientId}", patient);
    }

    private static Results<Ok<Patient>, NotFound, BadRequest<string>> Update(string id, [FromBody] Patient patient, IPatientRepository repo)
    {
        if (repo.CheckPatientIdExists(patient.PatientId, id))
            return TypedResults.BadRequest("病案号已存在");
        
        if (!string.IsNullOrEmpty(patient.BedNo) && repo.CheckBedNoExists(patient.BedNo, id))
            return TypedResults.BadRequest("床号已存在");
        
        patient.PatientId = id;
        patient.UpdatedAt = DateTime.Now;
        patient.IsActive = true;
        bool ok = repo.Update(patient);
        if (!ok) return TypedResults.NotFound();
        return TypedResults.Ok(patient);
    }

    private static Results<Ok<object>, NotFound> Delete(IPatientRepository repo, string id)
    {
        bool ok = repo.Delete(id);
        if (!ok) return TypedResults.NotFound();
        return TypedResults.Ok<object>(new { success = true });
    }
}