using System.Text;
using APBD_PJATK_Cw6_s33103.DTOs;
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

    public Task<AppointmentDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<CreateAppointmentRequestDto> CreateAsync(CreateAppointmentRequestDto appointment, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(int id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}