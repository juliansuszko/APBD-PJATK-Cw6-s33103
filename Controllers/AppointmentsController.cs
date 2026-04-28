using APBD_PJATK_Cw6_s33103.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace APBD_PJATK_Cw6_s33103.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    
    
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? patientLastName, CancellationToken cancellationToken)
    {
        return Ok(await appointmentService.GetAllAsync(status, patientLastName, cancellationToken));

    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(int id, CancellationToken cancellationToken)
    {
        var appointment = await appointmentService.GetByIdAsync(id, cancellationToken);

        if (appointment is null)
        {
            return NotFound($"Appointment with id {id} not found");
        }
        
        return Ok(appointment);
    }
    
    
}