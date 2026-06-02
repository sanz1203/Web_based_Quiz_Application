namespace Quiz_Application.Models.Entities
{
    public class Option
    {
        public Guid Id { get; set; }

        public required string Text { get; set; }

        public Guid QuestionId { get; set; }

    }
}


