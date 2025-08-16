using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Wellness_Journal_App.Data;
using Student_Wellness_Journal_App.Models;
using System.Net.Http;
using System.Text.Json;



namespace Student_Wellness_Journal_App.Controllers
{
    [Authorize]
    public class JournalController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;   // ✅ added

        public JournalController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IHttpClientFactory httpClientFactory,
            IConfiguration config)   // ✅ inject config
        {
            _db = db;
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        // GET: /Journal
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var entries = await _db.JournalEntries
                                   .Where(j => j.UserId == user.Id)
                                   .OrderByDescending(j => j.Timestamp)
                                   .ToListAsync();

            // ✅ Build absolute URL
            var controllerUrl = Url.Action("GetMotivationalQuote", "Journal", null, Request.Scheme);

            using var client = _httpClientFactory.CreateClient();
            var quote = await client.GetStringAsync(controllerUrl);

            ViewData["QuoteOfTheDay"] = quote;

            return View(entries);
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetMotivationalQuote([FromServices] IHttpClientFactory httpClientFactory)
        {
            var apiKey = _config["OpenAI:ApiKey"];   // ✅ read from config
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization",
                $"Bearer {apiKey}");
            var prompts = new[]
{
    "Give me a motivational quote about resilience.",
    "Share an uplifting thought for mental health.",
    "Provide an inspiring quote about positivity and strength.",
    "What’s a powerful quote about overcoming challenges?",
    "Give me a fresh motivational quote to boost morale."
};

            var random = new Random();
            var chosenPrompt = prompts[random.Next(prompts.Length)];

            var tokenLimit = new[]{40,50,60,70,80};
            var chosenTokenLimit = tokenLimit[random.Next(tokenLimit.Length)];
            var requestBody = new
            {
                model = "gpt-4o-mini", // lightweight + fast
                temperature = 1.2,     // 🔥 more randomness
                messages = new[]
                {
            new { role = "system", content = "You are a supportive assistant that provides uplifting motivational quotes related to mental health and positivity." },
            new { role = "user", content = chosenPrompt }
        },
                max_tokens = chosenTokenLimit
            };

            // ✅ Use Uri explicitly to avoid relative path issues
            var requestUri = new Uri("https://api.openai.com/v1/chat/completions");

            var response = await client.PostAsJsonAsync(requestUri, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                return Content("Stay strong! You are capable of more than you realize. 🌟\n\nHelpline (India): 1800-599-0019");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var quote = result
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            // Append helpline
            quote += "\n\n📞 Mental Health Helpline (India): 1800-599-0019";

            return Content(quote);
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
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _db.JournalEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (result == null)
                return NotFound();

            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JournalEntry model)
        {
            if (id != model.Id)
                return BadRequest();

            if (model.Content?.Length > 2000)
            {
                ModelState.AddModelError(nameof(model.Content), "Content cannot exceed 2000 characters.");
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var entry = await _db.JournalEntries.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);
            if (entry == null)
                return NotFound();

            // Update fields
            entry.Content = model.Content;
            entry.Mood = model.Mood;
            entry.Timestamp = DateTime.UtcNow;

            _db.JournalEntries.Update(entry);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]

        public async Task<IActionResult> Delete(int id)
        {
            var result = await _db.JournalEntries.Where(x => x.Id == id).FirstOrDefaultAsync();

            _db.JournalEntries.Remove(result);
            await _db.SaveChangesAsync();


            return RedirectToAction("Index", "Journal");

        }



    }
}
