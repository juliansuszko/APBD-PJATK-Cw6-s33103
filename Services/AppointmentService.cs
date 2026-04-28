using System.Text;
using APBD_PJATK_Cw6_s33103.DTOs;
using APBD_PJATK_Cw6_s33103.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;

namespace APBD_PJATK_Cw6_s33103.Services;

public class AppointmentService(IConfiguration configuration) : IAppointmentService
{
    public async Task<IEnumerable<AppointmentListDto>> GetAllAsync(string? status, string? patientLastName, CancellationToken cancellationToken = default)
    {   
        var result = new List<AppointmentListDto>();

        var sqlCommand = new StringBuilder("""

                                                   SELECT
                                                       a.IdAppointment,
                                                       a.AppointmentDate,
                                                       a.Status,
                                                       a.Reason,
                                                       p.FirstName + N' ' + p.LastName AS PatientFullName,
                                                       p.Email AS PatientEmail
                                                   FROM dbo.Appointments a
                                                   JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
                                           """
        );

        var conditions = new List<string>();
        var parameters = new List<SqlParameter>();

        if (status != null)
        {
            conditions.Add("a.Status = @Status");
            parameters.Add(new SqlParameter("@Status", status));
        }

        if (patientLastName != null)
        {
            conditions.Add("p.LastName = @PatientLastName");
            parameters.Add(new SqlParameter("@PatientLastName", patientLastName));
        }
        
        if (conditions.Count > 0)
        {
            sqlCommand.Append(" WHERE ");
            sqlCommand.Append(string.Join(" AND ", conditions));
        }
        
        sqlCommand.Append(" ORDER BY a.AppointmentDate;");
        
        await using var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await using var command = new SqlCommand(sqlCommand.ToString(), connection);
        
        command.Parameters.AddRange(parameters.ToArray());
        
        await connection.OpenAsync(cancellationToken);
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = await reader.IsDBNullAsync(2, cancellationToken) ? string.Empty : reader.GetString(2),
                Reason = await reader.IsDBNullAsync(3, cancellationToken) ? string.Empty : reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail = await reader.IsDBNullAsync(5, cancellationToken) ? string.Empty : reader.GetString(5),
            });
        }

        return result;
    }

    public async Task<AppointmentDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        AppointmentDetailsDto? result = null;

        const string sqlCommand = """
                                  SELECT
                                      a.IdAppointment,
                                      a.AppointmentDate,
                                      a.Status,
                                      a.Reason,
                                      a.InternalNotes,
                                      p.FirstName + N' ' + p.LastName AS PatientFullName,
                                      p.Email AS PatientEmail,
                                      p.PhoneNumber AS PatientPhone,
                                      d.FirstName + N' ' + d.LastName AS DoctorFullName,
                                      d.LicenseNumber AS DoctorLicenseNumber
                                  FROM dbo.Appointments a
                                  JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
                                  JOIN dbo.Doctors d ON d.IdDoctor = a.IdDoctor
                                  WHERE a.IdAppointment = @IdAppointment;
                                  """;
        
        await using var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await using var command = new SqlCommand(sqlCommand, connection);
        
        command.Parameters.Add(new SqlParameter("@IdAppointment", id));
        
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return new AppointmentDetailsDto
            {
                IdAppointment = reader.GetInt32(reader.GetOrdinal("IdAppointment")),
                AppointmentDate = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
            
                Reason = await reader.IsDBNullAsync(reader.GetOrdinal("Reason"), cancellationToken) ? string.Empty : reader.GetString(reader.GetOrdinal("Reason")),
                InternalNotes = await reader.IsDBNullAsync(reader.GetOrdinal("InternalNotes"), cancellationToken) ? string.Empty : reader.GetString(reader.GetOrdinal("InternalNotes")),
            
                PatientFullName = reader.GetString(reader.GetOrdinal("PatientFullName")),
                PatientEmail = await reader.IsDBNullAsync(reader.GetOrdinal("PatientEmail"), cancellationToken) ? string.Empty : reader.GetString(reader.GetOrdinal("PatientEmail")),
                PatientPhone = await reader.IsDBNullAsync(reader.GetOrdinal("PatientPhone"), cancellationToken) ? string.Empty : reader.GetString(reader.GetOrdinal("PatientPhone")),
            
                DoctorFullName = reader.GetString(reader.GetOrdinal("DoctorFullName")),
                DoctorLicenseNumber = reader.GetString(reader.GetOrdinal("DoctorLicenseNumber"))
            };
        }

        return null;
    }

    public async Task<AppointmentDetailsDto> CreateAsync(CreateAppointmentRequestDto appointment, CancellationToken cancellationToken = default)
    {
        if (appointment.AppointmentDate < DateTime.Now)
        {
            throw new BadRequestException("Appointment Date must be in the future");
        }
        
        if (string.IsNullOrWhiteSpace(appointment.Reason) || appointment.Reason.Length > 250)
            throw new BadRequestException("The visit description cannot be empty and must be a maximum of 250 characters.");
        
        await using var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand();
        
        command.Connection = connection;

        command.CommandText = "SELECT 1 FROM dbo.Patients WHERE IdPatient = @IdPatient AND IsActive = 1";
        command.Parameters.AddWithValue("@IdPatient", appointment.IdPatient);
        if (await command.ExecuteScalarAsync(cancellationToken) is null)
            throw new NotFoundException($"Patient with ID {appointment.IdPatient} not found.");
        command.Parameters.Clear();
        
        command.CommandText = "SELECT 1 FROM dbo.Doctors WHERE IdDoctor = @IdDoctor AND IsActive = 1";
        command.Parameters.AddWithValue("@IdDoctor", appointment.IdDoctor);
        if (await command.ExecuteScalarAsync(cancellationToken) is null)
            throw new NotFoundException($"Doctor with ID {appointment.IdDoctor} not found.");
        command.Parameters.Clear();
        
        command.CommandText = "SELECT 1 FROM dbo.Appointments WHERE IdDoctor = @IdDoctor AND AppointmentDate = @AppointmentDate";
        command.Parameters.AddWithValue("@IdDoctor", appointment.IdDoctor);
        command.Parameters.AddWithValue("@AppointmentDate", appointment.AppointmentDate);
        if (await command.ExecuteScalarAsync(cancellationToken) is not null)
            throw new ConflictException("The doctor already has an appointment scheduled for that date.");
        command.Parameters.Clear();
        
        command.CommandText = """
                                      INSERT INTO dbo.Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason, InternalNotes)
                                      OUTPUT INSERTED.IdAppointment
                                      VALUES (@IdPatient, @IdDoctor, @AppointmentDate, 'Scheduled', @Reason, '');
                              """;
        
        command.Parameters.AddWithValue("@IdPatient", appointment.IdPatient);
        command.Parameters.AddWithValue("@IdDoctor", appointment.IdDoctor);
        command.Parameters.AddWithValue("@AppointmentDate", appointment.AppointmentDate);
        command.Parameters.AddWithValue("@Reason", appointment.Reason);
        
        var newId = (int)await command.ExecuteScalarAsync(cancellationToken);

        var result = await GetByIdAsync(newId, cancellationToken);

        return result;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand { Connection = connection };

        command.CommandText = "SELECT Status FROM dbo.Appointments WHERE IdAppointment = @Id";
        command.Parameters.AddWithValue("@Id", id);

        var status = await command.ExecuteScalarAsync(cancellationToken);
        if (status is null)
            throw new NotFoundException($"Appointment with ID {id} not found.");

        if (status.ToString() == "Completed")
            throw new ConflictException("Cannot delete a completed appointment.");

        command.Parameters.Clear();

        command.CommandText = "DELETE FROM dbo.Appointments WHERE IdAppointment = @Id";
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
    
    public async Task UpdateAsync(int id, UpdateAppointmentRequestDto dto, CancellationToken cancellationToken = default)
    {
        var validStatuses = new[] { "Scheduled", "Completed", "Cancelled" };
        if (!validStatuses.Contains(dto.Status))
            throw new BadRequestException("Status must be one of: Scheduled, Completed, Cancelled.");

        await using var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand { Connection = connection };

        command.CommandText = "SELECT Status, AppointmentDate FROM dbo.Appointments WHERE IdAppointment = @Id";
        command.Parameters.AddWithValue("@Id", id);

        string oldStatus;
        DateTime oldDate;

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
                throw new NotFoundException($"Appointment with ID {id} not found.");
            
            oldStatus = reader.GetString(0);
            oldDate = reader.GetDateTime(1);
        }
        command.Parameters.Clear();

        if (oldStatus == "Completed" && dto.AppointmentDate != oldDate)
            throw new ConflictException("Cannot change the date of a completed appointment.");

        command.CommandText = "SELECT 1 FROM dbo.Patients WHERE IdPatient = @IdP AND IsActive = 1";
        command.Parameters.AddWithValue("@IdP", dto.IdPatient);
        if (await command.ExecuteScalarAsync(cancellationToken) is null)
            throw new NotFoundException("Patient not found or inactive.");
        command.Parameters.Clear();

        command.CommandText = "SELECT 1 FROM dbo.Doctors WHERE IdDoctor = @IdD AND IsActive = 1";
        command.Parameters.AddWithValue("@IdD", dto.IdDoctor);
        if (await command.ExecuteScalarAsync(cancellationToken) is null)
            throw new NotFoundException("Doctor not found or inactive.");
        command.Parameters.Clear();

        if (dto.AppointmentDate != oldDate)
        {
            command.CommandText = "SELECT 1 FROM dbo.Appointments WHERE IdDoctor = @IdD AND AppointmentDate = @Date AND IdAppointment != @Id";
            command.Parameters.AddWithValue("@IdD", dto.IdDoctor);
            command.Parameters.AddWithValue("@Date", dto.AppointmentDate);
            command.Parameters.AddWithValue("@Id", id);
            if (await command.ExecuteScalarAsync(cancellationToken) is not null)
                throw new ConflictException("Doctor already has an appointment at this time.");
            command.Parameters.Clear();
        }

        command.CommandText = @"
            UPDATE dbo.Appointments SET 
                IdPatient = @IdP, IdDoctor = @IdD, AppointmentDate = @Date, 
                Status = @Status, Reason = @Reason, InternalNotes = @Notes
            WHERE IdAppointment = @Id";
        
        command.Parameters.AddWithValue("@IdP", dto.IdPatient);
        command.Parameters.AddWithValue("@IdD", dto.IdDoctor);
        command.Parameters.AddWithValue("@Date", dto.AppointmentDate);
        command.Parameters.AddWithValue("@Status", dto.Status);
        command.Parameters.AddWithValue("@Reason", dto.Reason);
        command.Parameters.AddWithValue("@Notes", dto.InternalNotes);
        command.Parameters.AddWithValue("@Id", id);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}