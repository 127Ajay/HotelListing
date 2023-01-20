﻿using System.ComponentModel.DataAnnotations;

namespace HotelListing.API.Models.Users
{
    public class APIUserDTO: LoginDTO
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }

    }
}
