using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.AspNetCore.OData.NetTopology.Sample.Models
{
    public class Student
    {
        public Guid StudentId { get; set; }

        public string Name { get; set; }
        public int Score { get; set; }

        [ForeignKey("School")]
        public Guid? SchoolId { get; set; }
        public School School { get; set; }
    }
}