using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Student_Wellness_Journal_App.Models
{
    public enum Mood { Happy, Neutral, Sad, Angry, Anxious, Calm }

    public class JournalEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }          // FK to AspNetUsers (Identity)

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }

        [Required]
        public Mood Mood { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
