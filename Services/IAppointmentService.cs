using APBD_PJATK_Cw6_s33103.DTOs;

namespace APBD_PJATK_Cw6_s33103.Services;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAsync(string? status, string? patientLastName, CancellationToken cancellationToken = default);
    Task<AppointmentDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AppointmentDetailsDto> CreateAsync(CreateAppointmentRequestDto appointment, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, UpdateAppointmentRequestDto dto, CancellationToken cancellationToken = default);
}