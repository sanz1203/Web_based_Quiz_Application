namespace Quiz_Application.Models.ViewModels
{
    public class QuizViewModel
    {
        public Guid QuizId { get; set; }  // To track which quiz is being attempted
        public List<QuestionItem> Questions { get; set; } = new();
    }

    public class QuestionItem
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<OptionItem> Options { get; set; } = new();
    }

    public class OptionItem
    {
        public Guid Value { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
