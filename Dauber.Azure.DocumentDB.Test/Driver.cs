using System;
using System.Collections.Generic;
using Dauber.Core.Contracts;

namespace Dauber.Azure.DocumentDb.Test
{
    public class Driver : ViewModel
    {   
        public Guid FleetId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DriversLicense DriversLicense { get; set; }
        public bool IsOnCall { get; set; }
        public Guid? MostRecentJobLoadId { get; set; }
        public string ImageUrl { get; set; } = "https://fleetimages.azureedge.net/images/placeholder/driver.png";
        public string ProfileImageUrl { get; set; }
        public List<SimpleTag> Tags { get; set; }
        public bool IsActive { get; set; } = true;
        //public Guid? StartOfDayTruckId { get; set; } 
    }
}