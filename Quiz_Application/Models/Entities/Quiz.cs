using System.ComponentModel.DataAnnotations;

namespace Quiz_Application.Models.Entities
{
    public class Quiz
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public bool IsPublished { get; set; }
        public string? MethodologyFileName { get; set; }


        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}

