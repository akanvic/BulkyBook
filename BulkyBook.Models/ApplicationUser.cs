using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BulkyBook.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }

        [ForeignKey("CompanyID")]
        public Company Company { get; set; }
        public int? CompanyID { get; set; }

        [NotMapped] //This property wont be added to our database
        public string Role { get; set; }
    }
}
