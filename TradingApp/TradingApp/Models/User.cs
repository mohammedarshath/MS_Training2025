using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradingApp.Models
{
    [Table("users")]
    
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("user_id")]
        public long UserId { get; set; }
        [Required]
        [Column("user_name", TypeName = "varchar(100)")]
        [RegularExpression("^[a-zA-Z]{5,25}$",
           ErrorMessage = "User Name Should be in alphabets within the range of 5,25")]
       // [DefaultValue("")]
        public string Username { get; set; }
        [Required]
        [Column("email", TypeName = "varchar(100)")]
        [RegularExpression("^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$",
            ErrorMessage = "Email should be in proper format")]
        public string Email { get; set; }
        public ICollection<Role> Roles { get; set; } = new List<Role>();

    }
}
