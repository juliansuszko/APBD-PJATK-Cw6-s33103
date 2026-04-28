using APBD_PJATK_Cw6_s33103.DTOs;
using APBD_PJATK_Cw6_s33103.Exceptions;
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

    [HttpGet("{idAppointment:int}")]
    public async Task<IActionResult> GetByIdAsync([FromRoute]int idAppointment, CancellationToken cancellationToken)
    {
        var appointment = await appointmentService.GetByIdAsync(idAppointment, cancellationToken);

        if (appointment is null)
        {
            return NotFound($"Appointment with id {idAppointment} not found");
        }
        
        return Ok(appointment);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateAppointmentRequestDto appointment,
        CancellationToken cancellationToken)
    {
        try
        {
            var newAppointment = await appointmentService.CreateAsync(appointment, cancellationToken);

            return Created($"/api/appointments/{newAppointment.IdAppointment}", newAppointment);
        }
        catch (BadRequestException e)
        {
            return BadRequest(e.Message);
        } 
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ConflictException e)
        {
            return Conflict(e.Message);
        }
    }
    
    [HttpDelete("{idAppointment:int}")]
    public async Task<IActionResult> DeleteAsync([FromRoute] int idAppointment, CancellationToken cancellationToken)
    {
        try
        {
            await appointmentService.DeleteAsync(idAppointment, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException e) { return NotFound(e.Message); }
        catch (ConflictException e) { return Conflict(e.Message); }
    }
    
    [HttpPut("{idAppointment:int}")]
    public async Task<IActionResult> UpdateAsync([FromRoute] int idAppointment, [FromBody] UpdateAppointmentRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            await appointmentService.UpdateAsync(idAppointment, dto, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException e) { return NotFound(e.Message); }
        catch (ConflictException e) { return Conflict(e.Message); }
        catch (BadRequestException e) { return BadRequest(e.Message); }
    }
}