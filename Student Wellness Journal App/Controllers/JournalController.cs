using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Wellness_Journal_App.Data;
using Student_Wellness_Journal_App.Models;



namespace Student_Wellness_Journal_App.Controllers
{
    [Authorize]
    public class JournalController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public JournalController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: /Journal
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var entries = await _db.JournalEntries
                                   .Where(j => j.UserId == user.Id)
                                   .OrderByDescending(j => j.Timestamp)
                                   .ToListAsync();
            return View(entries);
        }

        // GET: /Journal/Create
        public IActionResult Create() => View();

        // POST: /Journal/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JournalEntry model)
        {
            // Validate model (includes [MaxLength(2000)] from model annotation)
            //if (!ModelState.IsValid)
            //{
                //return View(model);
            //}

            // Ensure content length is explicitly checked server-side
            if (model.Content?.Length > 2000)
            {
                ModelState.AddModelError(nameof(model.Content), "Content cannot exceed 2000 characters.");
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            model.UserId = user.Id;
            model.Timestamp = DateTime.UtcNow;

            _db.JournalEntries.Add(model);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Simple API: returns last 30 days mood counts
        [HttpGet]
        public async Task<IActionResult> MoodData(int days = 30)
        {
            var user = await _userManager.GetUserAsync(User);
            var from = DateTime.UtcNow.Date.AddDays(-days);
            var data = await _db.JournalEntries
                .Where(e => e.UserId == user.Id && e.Timestamp >= from)
                .GroupBy(e => e.Timestamp.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    CountHappy = g.Count(e => e.Mood == Mood.Happy),
                    CountNeutral = g.Count(e => e.Mood == Mood.Neutral),
                    CountSad = g.Count(e => e.Mood == Mood.Sad),
                    CountAngry = g.Count(e => e.Mood == Mood.Angry),
                    CountAnxious = g.Count(e => e.Mood == Mood.Anxious),
                    CountCalm = g.Count(e => e.Mood == Mood.Calm)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return Ok(data);
        }
    }
}
