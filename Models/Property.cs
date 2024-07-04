using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalManagement.Models;

public class Property
{
    public int Id { get; set; }

    [StringLength(50)]
    public string Name { get; set; }

    [StringLength(50)]
    public string? Location { get; set; }

    [StringLength(50)]
    public int? ParentPropertyId { get; set; }
    
    public DateTime? CreateAt { get; set; }

    public int? CreateBy { get; set; }
    public DateTime? UpdateAt { get; set; }

    public int? UpdateBy { get; set; }
    public DateTime? DeleteAt { get; set; }

    public int? DeleteBy { get; set; }

    public ICollection<Contract> Contracts {get;set;}
}
