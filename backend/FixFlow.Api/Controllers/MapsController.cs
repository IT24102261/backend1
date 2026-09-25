using FixFlow.Application.DTOs.Maps;
using FixFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/maps")]
public class MapsController(IMapService maps) : ControllerBase
{
    [HttpPost("geocode")]
    public async Task<IActionResult> Geocode(GeocodeAddressRequest request, CancellationToken cancellationToken) =>
        Ok(await maps.GeocodeAddressAsync(request.Address, cancellationToken));

    [HttpPost("reverse")]
    public async Task<IActionResult> Reverse(ReverseGeocodeRequest request, CancellationToken cancellationToken) =>
        Ok(await maps.ReverseGeocodeAsync(request.Latitude, request.Longitude, cancellationToken));
}
