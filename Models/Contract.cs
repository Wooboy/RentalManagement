using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalManagement.Models;

public class Contract
{
    public int Id { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; }
    public int PropertyId { get; set; }
    public ICollection<Property> Properties { get; set; } = new List<Property>();

    public DateTime? CreateAt { get; set; }

    public int? CreateBy { get; set; }
    public DateTime? UpdateAt { get; set; }

    public int? UpdateBy { get; set; }
    public DateTime? DeleteAt { get; set; }

    public int? DeleteBy { get; set; }
}
