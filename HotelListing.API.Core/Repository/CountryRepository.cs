using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.API.Core.Contracts;
using HotelListing.API.Core.Exceptions;
using HotelListing.API.Core.Models.Country;
using HotelListing.API.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.API.Core.Repository
{
    public class CountryRepository : GenericRepository<Country>, ICountryRepository
    {
        private readonly HotelListingDbContext _context;
        private readonly IMapper mapper;

        public CountryRepository(HotelListingDbContext context, IMapper mapper) : base(context, mapper)
        {
            _context = context;
            this.mapper = mapper;
        }

        public async Task<CountryDTO> GetDetails(int id)
        {
            var country = await _context.Countries.Include(q => q.Hotels)
                .ProjectTo<CountryDTO>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(q=> q.Id == id);

            if(country is null)
            {
                throw new NotFoundException(nameof(GetDetails), id);
            }

            return country;
        }
    }
}
