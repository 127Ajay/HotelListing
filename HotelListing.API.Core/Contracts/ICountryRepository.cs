﻿using HotelListing.API.Core.Models.Country;
using HotelListing.API.Data;

namespace HotelListing.API.Core.Contracts
{
    public interface ICountryRepository: IGenericRepository<Country>
    {
        Task<CountryDTO> GetDetails(int id);
    }
}
