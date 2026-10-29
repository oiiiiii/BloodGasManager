using BGShared.Models;

namespace BGService.Data;

public interface IPatientRepository
{
    List<Patient> GetAll(bool onlyActive = false);
    List<Patient> Search(string keyword, int limit = 50);
    Patient? GetById(string patientId);
    Patient? FindByIdOrBedNoOrName(string patientInfo);
    Patient? FindByPatientId(string patientId);
    bool Exists(string patientId);
    bool CheckPatientIdExists(string patientId, string? excludePatientId = null);
    bool CheckBedNoExists(string bedNo, string? excludePatientId = null);
    void Insert(Patient patient);
    bool Update(Patient patient);
    bool Delete(string patientId);
}