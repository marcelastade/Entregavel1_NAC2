using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entregavel1_NAC2.Model
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string Brand { get; set; } 
        public string Model { get; set; }
        public int Year { get; set; }
        public string Plate { get; set; }
    }
}