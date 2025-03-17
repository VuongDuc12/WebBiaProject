﻿// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

namespace WebBiaProject.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
    }
}