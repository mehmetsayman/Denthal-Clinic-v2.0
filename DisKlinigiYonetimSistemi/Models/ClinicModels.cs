using System.Text.Json.Serialization;

namespace DisKlinigiYonetimSistemi.Models;

public enum UserRole
{
    Admin,
    Doktor,
    Sekreter,
    Hasta
}

public enum AppointmentStatus
{
    TalepEdildi,
    Onaylandi,
    Geldi,
    Tamamlandi,
    Reddedildi,
    Iptal
}

public enum Gender
{
    Belirtilmedi,
    Kadin,
    Erkek
}

public sealed class UserAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string FullName { get; set; } = "";
    public UserRole Role { get; set; }
    public string Specialty { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Biography { get; set; } = "";
    public string RoomName { get; set; } = "";
    public string? AssignedDoctorUserId { get; set; }
    public string? LinkedPatientId { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class Patient
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TcNo { get; set; } = "";
    public string FullName { get; set; } = "";
    public Gender Gender { get; set; }
    public DateTime BirthDate { get; set; } = DateTime.Today.AddYears(-25);
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public string BloodType { get; set; } = "";
    public int HeightCm { get; set; }
    public decimal WeightKg { get; set; }
    public string AllergyNotes { get; set; } = "";
    public string ChronicDiseases { get; set; } = "";
    public string CurrentMedications { get; set; } = "";
    public string SmokingStatus { get; set; } = "";
    public string EmergencyContactName { get; set; } = "";
    public string EmergencyContactPhone { get; set; } = "";
    public string DentalHistory { get; set; } = "";
    public string RiskLevel { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public int Age
    {
        get
        {
            var age = DateTime.Today.Year - BirthDate.Year;
            if (BirthDate.Date > DateTime.Today.AddYears(-age)) age--;
            return Math.Max(age, 0);
        }
    }
}

public sealed class Appointment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PatientId { get; set; } = "";
    public string DoctorUserId { get; set; } = "";
    public string RequestedByUserId { get; set; } = "";
    public string? ApprovedByUserId { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.Now;
    public DateTime StartsAt { get; set; } = DateTime.Today.AddHours(10);
    public int DurationMinutes { get; set; } = 30;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.TalepEdildi;
    public string Complaint { get; set; } = "";
    public string Notes { get; set; } = "";
}

public sealed class Prescription
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PatientId { get; set; } = "";
    public string DoctorUserId { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Today;
    public string Topic { get; set; } = "";
    public List<string> MedicationIds { get; set; } = [];
    public string Medicines { get; set; } = "";
    public string UsageInstructions { get; set; } = "";
    public string Diagnosis { get; set; } = "";
    public string DoctorNote { get; set; } = "";
}

public sealed class MedicationTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string DefaultUsage { get; set; } = "";
    public string Warning { get; set; } = "";
}

public sealed class Radiograph
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PatientId { get; set; } = "";
    public string DoctorUserId { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Today;
    public string ToothRegion { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public string ImageFileName { get; set; } = "";
    public string ImageMimeType { get; set; } = "";
    public string ImageContentBase64 { get; set; } = "";
    public string Notes { get; set; } = "";
}

public sealed class TreatmentPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PatientId { get; set; } = "";
    public string DoctorUserId { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Today;
    public string ToothNo { get; set; } = "";
    public string ProcedureName { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Completed { get; set; }
}

public sealed class SystemLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string ActorUserId { get; set; } = "";
    public string ActorName { get; set; } = "";
    public UserRole ActorRole { get; set; }
    public string ActionType { get; set; } = "";
    public string? PatientId { get; set; }
    public string? DoctorUserId { get; set; }
    public string Description { get; set; } = "";
}

public sealed class NotificationMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PatientId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string EmailTo { get; set; } = "";
    public bool Read { get; set; }
}

public sealed class SupabaseSettings
{
    public string Url { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public bool Enabled => !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(ApiKey);
}

public sealed class DataSnapshot
{
    public int SeedVersion { get; set; } = 7;
    public List<UserAccount> Users { get; set; } = [];
    public List<Patient> Patients { get; set; } = [];
    public List<MedicationTemplate> Medications { get; set; } = [];
    public List<Appointment> Appointments { get; set; } = [];
    public List<Prescription> Prescriptions { get; set; } = [];
    public List<Radiograph> Radiographs { get; set; } = [];
    public List<TreatmentPlan> Treatments { get; set; } = [];
    public List<SystemLog> Logs { get; set; } = [];
    public List<NotificationMessage> Notifications { get; set; } = [];
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public long Version { get; set; } = DateTime.UtcNow.Ticks;
}
