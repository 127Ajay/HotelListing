using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelListing.API.Data;
using HotelListing.API.Contracts;
using AutoMapper;
using HotelListing.API.Repository;
using HotelListing.API.Models.Hotel;
using Microsoft.AspNetCore.Authorization;

namespace HotelListing.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotelsController : ControllerBase
    {
        private readonly IHotelRepository _hotelRepository;
        private readonly ILogger<HotelsController> _logger;
        private readonly IMapper _mapper;

        public HotelsController(IHotelRepository hotelRepository, IMapper mapper, ILogger<HotelsController> logger)
        {
            _hotelRepository = hotelRepository;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/Hotels
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetHotelDTO>>> GetHotels()
        {
            var hotels = await _hotelRepository.GetAllAsync();
            var records = _mapper.Map<List<GetHotelDTO>>(hotels);
            return Ok(records);
        }

        // GET: api/Hotels/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GetHotelDTO>> GetHotel(int id)
        {
            var hotel = await _hotelRepository.GetAsync(id);
            if (hotel == null)
            {
                _logger.LogWarning($"No record found for {nameof(GetHotel)} with id: {id}");
                return NotFound();
            }
            var record = _mapper.Map<GetHotelDTO>(hotel);
            return Ok(record);
        }

        // PUT: api/Hotels/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutHotel(int id, UpdateHotelDTO updateHotelDTO)
        {
            if (id != updateHotelDTO.Id)
            {
                return BadRequest();
            }

            var hotel = await _hotelRepository.GetAsync(id);

            if (hotel == null)
            {
                return NotFound();
            }

            _mapper.Map(updateHotelDTO, hotel);

            try
            {
                await _hotelRepository.UpdateAsync(hotel);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await HotelExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Hotels
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CreateHotelDTO>> PostHotel(CreateHotelDTO hotelDto)
        {
            var hotel = _mapper.Map<Hotel>(hotelDto);
            await _hotelRepository.AddAsync(hotel);

            return CreatedAtAction("GetHotel", new { id = hotel.Id }, hotel);
        }

        // DELETE: api/Hotels/5
        [HttpDelete("{id}")]
        [Authorize(Roles ="Administrator")]
        public async Task<IActionResult> DeleteHotel(int id)
        {
            var hotel = await _hotelRepository.GetAsync(id);
            if (hotel == null)
            {
                return NotFound();
            }

            await _hotelRepository.DeleteAsync(id);

            return NoContent();
        }

        private async Task<bool> HotelExists(int id)
        {
            return await _hotelRepository.Exists(id);
        }
    }
}
