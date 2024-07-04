using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace RentalManagement.Models;
public class Client
{
    public int Id {get;set;}
    [StringLength(50)]
    public string Name {get;set;}
    [StringLength(50)]
    public string Identity {get;set;}
    [StringLength(1000)]
    public string Comment {get;set;}
    
    public DateTime? CreateAt { get; set; }

    public int? CreateBy { get; set; }
    public DateTime? UpdateAt { get; set; }

    public int? UpdateBy { get; set; }
    public DateTime? DeleteAt { get; set; }

    public int? DeleteBy { get; set; }

    public ICollection<Contract>? Contracts {get;} = new List<Contract>();
}