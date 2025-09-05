using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingApp.Models
{
    [Table("roles")]
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        [Column("role_id")]
        public long RoleId { get; set; }
        [Required]
        [Column("role_name",TypeName ="varchar(100)")]
        [RegularExpression("^[a-zA-Z]{5,25}$",
            ErrorMessage = "Role Name Should be in alphabets within the range of 5,25")]
       // [DefaultValue("")]
        public string RoleName { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
