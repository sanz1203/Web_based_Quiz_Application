using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Quiz_Application.Data;
using Quiz_Application.Models.ViewModels;
using System.Globalization;
using Quiz_Application.Models.Entities;

namespace Quiz_Application.Controllers
{
    [Authorize]
    public class QuizController : Controller
    {
        private readonly ApplicationDbContext dbContext;

        public QuizController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult All()
        {
            var publishedQuizzes = dbContext.Quizzes
                .Where(q => q.IsPublished)
                .ToList();

            return View(publishedQuizzes);
        }

        [HttpGet]
        public IActionResult Attempt(Guid id)
        {
            var quiz = dbContext.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefault(q => q.Id == id);

            if (quiz == null)
                return NotFound(); // Quiz ID invalid

            var now = DateTime.Now;

            if (now < quiz.StartTime)
            {
                ViewBag.Message = "⏳ This quiz is not accessible yet. It will start on " + quiz.StartTime.ToString("f");
                return View("QuizUnavailable");
            }

            if (now > quiz.EndTime)
            {
                ViewBag.Message = "❌ This quiz has already ended on " + quiz.EndTime.ToString("f");
                return View("QuizUnavailable");
            }

            // ✅ Store start time in TempData
            TempData["StartTime"] = DateTime.Now.ToString("o"); // round-trip format
            TempData.Keep("StartTime");

            var viewModel = new QuizViewModel
            {
                QuizId = quiz.Id,
                Questions = quiz.Questions.Select(q => new QuestionItem
                {
                    Id = q.Id,
                    Text = q.Text,
                    Options = q.Options.Select(o => new OptionItem
                    {
                        Value = o.Id,
                        Text = o.Text
                    }).ToList()
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Submit(Dictionary<Guid, Guid> userAnswers, Guid quizId)
        {
            if (userAnswers == null || userAnswers.Count == 0)
            {
                ViewBag.Score = 0;
                ViewBag.TotalScore = 0;
                ViewBag.ErrorMessage = "No answers were submitted.";
                return View("Results");
            }

            var questions = dbContext.Questions
                .Where(q => q.QuizId == quizId && userAnswers.Keys.Contains(q.Id))
                .ToList();

            int score = 0;
            int totalQuestions = questions.Count;

            foreach (var question in questions)
            {
                if (userAnswers.TryGetValue(question.Id, out var selectedOptionId))
                {
                    if (question.CorrectOptionId == selectedOptionId)
                    {
                        score++;
                    }
                }
            }

            ViewBag.Score = score;
            ViewBag.TotalScore = totalQuestions;

            DateTime endTime = DateTime.Now;
            ViewBag.EndTime = endTime.ToString("HH:mm:ss");

            if (TempData["StartTime"] != null)
            {
                DateTime startTime = DateTime.Parse(TempData["StartTime"]?.ToString() ?? "", null, DateTimeStyles.RoundtripKind);
                ViewBag.StartTime = startTime.ToString("HH:mm:ss");
                TimeSpan duration = endTime - startTime;
                ViewBag.Duration = duration.ToString(@"hh\:mm\:ss");
            }
            else
            {
                ViewBag.StartTime = "Unknown";
                ViewBag.Duration = "N/A";
            }

            return View("Results");
        }
    }
}
