using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quiz_Application.Data;
using Quiz_Application.Models.Entities;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;


namespace Quiz_Application.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext dbContext;

        public AdminController(ApplicationDbContext context)
        {
            dbContext = context;
        }

        public IActionResult Index()
        {
            var quizzes = dbContext.Quizzes.ToList();
            return View(quizzes);
        }

        [HttpGet]
        public IActionResult CreateQuiz()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateQuiz(Quiz quiz)
        {
            if (ModelState.IsValid)
            {
                if (quiz.StartTime == default)
                    quiz.StartTime = DateTime.Now;

                if (quiz.EndTime == default || quiz.EndTime <= quiz.StartTime)
                    quiz.EndTime = quiz.StartTime.AddMinutes(30);

                dbContext.Quizzes.Add(quiz);
                dbContext.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(quiz);
        }

        [HttpGet]
        public IActionResult AddQuestion(Guid quizId)
        {
            var quiz = dbContext.Quizzes.Find(quizId);
            if (quiz == null) return NotFound();

            ViewBag.QuizId = quizId;
            return View();
        }

        [HttpPost]
        public IActionResult AddQuestion(Guid quizId, string Text, string[] Options, int CorrectOptionIndex)
        {
            if (Options.Length < 2)
            {
                ModelState.AddModelError("", "A question must have at least two options.");
                return View();
            }

            var correctOptionId = Guid.NewGuid();

            var optionEntities = Options.Select((opt, index) => new Option
            {
                Id = index == CorrectOptionIndex ? correctOptionId : Guid.NewGuid(),
                Text = opt.Trim()
            }).ToList();

            var question = new Question
            {
                Id = Guid.NewGuid(),
                Text = Text,
                QuizId = quizId,
                CorrectOptionId = correctOptionId,
                Options = optionEntities
            };

            dbContext.Questions.Add(question);
            dbContext.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult TogglePublish(Guid id)
        {
            var quiz = dbContext.Quizzes.Find(id);
            if (quiz == null) return NotFound();

            quiz.IsPublished = !quiz.IsPublished;
            dbContext.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult EditQuiz(Guid id)
        {
            var quiz = dbContext.Quizzes.FirstOrDefault(q => q.Id == id);
            if (quiz == null) return NotFound();

            return View(quiz);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuiz(Quiz updatedQuiz, IFormFile? methodologyFile)
        {
            if (!ModelState.IsValid)
            {
                var existing = dbContext.Quizzes.FirstOrDefault(q => q.Id == updatedQuiz.Id);
                if (existing != null)
                {
                    updatedQuiz.MethodologyFileName = existing.MethodologyFileName;
                }

                return View(updatedQuiz);
            }

            var quiz = dbContext.Quizzes.FirstOrDefault(q => q.Id == updatedQuiz.Id);
            if (quiz == null) return NotFound();

            quiz.Title = updatedQuiz.Title;
            quiz.Description = updatedQuiz.Description;
            quiz.StartTime = updatedQuiz.StartTime;
            quiz.EndTime = updatedQuiz.EndTime;

            if (methodologyFile != null && methodologyFile.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(methodologyFile.FileName)}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await methodologyFile.CopyToAsync(stream);
                }

                quiz.MethodologyFileName = fileName;
            }

            await dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> UploadMethodology(Guid quizId, IFormFile? methodologyFile)
        {
            if (methodologyFile == null || methodologyFile.Length == 0)
                return RedirectToAction("Index");

            var quiz = dbContext.Quizzes.FirstOrDefault(q => q.Id == quizId);
            if (quiz == null)
                return NotFound();

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(methodologyFile.FileName)}";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await methodologyFile.CopyToAsync(stream);
            }

            quiz.MethodologyFileName = fileName;
            dbContext.SaveChanges();

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> UploadExcel(Guid quizId, IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
                return BadRequest("No file uploaded.");

            var quiz = await dbContext.Quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == quizId);
            if (quiz == null) return NotFound();

            int importedCount = 0;
            int skippedCount = 0;

            using (var stream = new MemoryStream())
            {
                await excelFile.CopyToAsync(stream);
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                        return BadRequest("Excel file is empty or malformed.");

                    var rows = worksheet.RowsUsed().Skip(1);
                    foreach (var r in rows)
                    {
                        var text = r.Cell(1).GetString().Trim();
                        if (string.IsNullOrEmpty(text)) continue;

                        var options = new List<string>
                        {
                            r.Cell(2).GetString().Trim(),
                            r.Cell(3).GetString().Trim(),
                            r.Cell(4).GetString().Trim(),
                            r.Cell(5).GetString().Trim()
                        };

                        var correctIndexStr = r.Cell(6).GetString().Trim();
                        int correctIndex = ResolveCorrectIndex(correctIndexStr, options);

                        if (correctIndex == -1)
                        {
                            skippedCount++;
                            continue;
                        }

                        var correctOptionId = Guid.NewGuid();
                        var optionEntities = options.Select((opt, index) => new Option
                        {
                            Id = index == correctIndex ? correctOptionId : Guid.NewGuid(),
                            Text = opt
                        }).ToList();

                        var question = new Question
                        {
                            Id = Guid.NewGuid(),
                            Text = text,
                            QuizId = quizId,
                            CorrectOptionId = correctOptionId,
                            Options = optionEntities
                        };

                        dbContext.Questions.Add(question);
                        importedCount++;
                    }

                    await dbContext.SaveChangesAsync();
                }
            }

            TempData["ExcelImportMessage"] = $"✅ Successfully imported {importedCount} questions from Excel. ⚠️ Skipped {skippedCount} rows due to invalid correct option formats.";
            return RedirectToAction("ManageQuestions", new { quizId });
        }

        private static int ResolveCorrectIndex(string input, List<string> options)
        {
            if (string.IsNullOrWhiteSpace(input)) return -1;
            input = input.Trim().ToLowerInvariant();

            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].Trim().Equals(input, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            if (double.TryParse(input, out double val))
            {
                int index = (int)Math.Round(val);
                if (index >= 1 && index <= options.Count)
                {
                    return index - 1;
                }
                if (index >= 0 && index < options.Count)
                {
                    return index;
                }
            }

            if (input == "a") return 0;
            if (input == "b") return 1;
            if (input == "c") return 2;
            if (input == "d") return 3;

            return -1;
        }


        [HttpGet]
        public IActionResult ManageQuestions(Guid quizId)
        {
            var quiz = dbContext.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefault(q => q.Id == quizId);

            if (quiz == null) return NotFound();

            ViewBag.QuizId = quizId;
            return View(quiz.Questions.ToList());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteMethodology(Guid quizId)
        {
            var quiz = dbContext.Quizzes.FirstOrDefault(q => q.Id == quizId);
            if (quiz == null || string.IsNullOrEmpty(quiz.MethodologyFileName))
                return RedirectToAction("EditQuiz", new { id = quizId });

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", quiz.MethodologyFileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            quiz.MethodologyFileName = null;
            dbContext.SaveChanges();

            return RedirectToAction("EditQuiz", new { id = quizId });
        }

        [HttpGet]
        public IActionResult EditQuestion(Guid questionId)
        {
            var question = dbContext.Questions
                .Include(q => q.Options)
                .FirstOrDefault(q => q.Id == questionId);

            if (question == null) 
                return NotFound();

            question.Options = question.Options.OrderBy(o => o.Id).ToList();

            ViewBag.QuizId = question.QuizId;
            ViewBag.CorrectOptionId = question.CorrectOptionId;
            return View(question);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditQuestion(Guid questionId, Guid quizId, string Text, string[] Options, Guid CorrectOptionId)
        {
            var question = dbContext.Questions
                .Include(q => q.Options)
                .FirstOrDefault(q => q.Id == questionId);

            if (question == null) 
                return NotFound();

            question.Text = Text.Trim();
            question.CorrectOptionId = CorrectOptionId;

            var sortedOptions = question.Options.OrderBy(o => o.Id).ToList();
            for (int i = 0; i < sortedOptions.Count && i < Options.Length; i++)
            {
                sortedOptions[i].Text = Options[i].Trim();
            }

            dbContext.SaveChanges();
            return RedirectToAction("ManageQuestions", new { quizId });
        }

    }
}

