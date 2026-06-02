using System.ComponentModel.DataAnnotations;

namespace Quiz_Application.Models.Entities
{
    public class Question
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Text { get; set; } = string.Empty;

        // Foreign key to Quiz
        public Guid QuizId { get; set; }

        public Quiz Quiz { get; set; } = null!;

        public List<Option> Options { get; set; } = new();

        // Stores the ID of the correct Option
        public Guid CorrectOptionId { get; set; }
    }
}


