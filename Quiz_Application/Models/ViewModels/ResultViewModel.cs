using System;

namespace Quiz_Application.Models.ViewModels
{
    public class ResultViewModel
    {
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public TimeSpan? Duration => (StartTime.HasValue && EndTime.HasValue)
            ? EndTime - StartTime
            : null;
    }
}
