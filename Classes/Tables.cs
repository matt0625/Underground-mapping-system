using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Nodes;
using System.Linq.Expressions;

namespace TUBETREKWPFV1.Classes
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        public string Username { get; set; }

        public string PasswordHash { get; set; }
    
    }

    public class UserJourneyLink
    {
        // Column order creates a composite primary key for this table
        //[Key]
        //[Column(Order=0)]
        public int UserID { get; set; }

        //[Key]
        //[Column(Order=1)]
        public int JourneyID { get; set; }

        [ForeignKey(nameof(UserID))]
        public User user { get; set; }

        [ForeignKey(nameof(JourneyID))]

        public UserJourneys journey { get; set; }
    }

    public class UserJourneys
    {
        [Key]
        public int JourneyID { get; set; }

        // convert to unix for sorting
        public DateTime? DateTime { get; set; }

        public string StartStation { get; set;}

        public string Destination {  get; set; }


        // field to store the JSON serialised version of the calculated path for easy retrieval
        public string Journey {  get; set; }

        public int FareID { get; set; }

        [ForeignKey(nameof(FareID))]

        public Fare fare { get; set;}
    }

    public class Fare
    {
        [Key]
        public int FareID { get; set;}

        public int ZoneStart { get; set; }

        public int ZoneEnd { get; set; }
        
        public string PeakFare { get; set; }

        public string OffPeakFare { get; set; } 
    }
}
