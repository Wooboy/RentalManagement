using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class SYS_User
{
    public int Id { get; set; }

    [StringLength(50)]
    public string Username { get; set; }

    [StringLength(50)]
    public string? DisplayName { get; set; }

    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(200)]
    public string? Password { get; set; }
    public bool Enable {get;set;} = true;
    public DateTime NotValidBefore { get; set; }
    public DateTime NotValidAfter { get; set; }
    public string? Photo { get; set; }
    public DateTime? CreateAt { get; set; }

    public int? CreateBy { get; set; }
    public DateTime? UpdateAt { get; set; }

    public int? UpdateBy { get; set; }
    public DateTime? DeleteAt { get; set; }

    public int? DeleteBy { get; set; }
}
