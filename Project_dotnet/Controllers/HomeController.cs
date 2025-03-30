using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_dotnet.Models;
using System;
using System.Diagnostics;
using System.Text.Json;

namespace Project_dotnet.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly SpendSmartDbContext _context;

        private readonly UserManager<Users> _userManager;

        public HomeController(ILogger<HomeController> logger, SpendSmartDbContext context, UserManager<Users> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var allExpenses = _context.Expenses.ToList();
            return View(allExpenses);
        }


        public async Task<IActionResult> Expenses(string sortOrder, string filter, decimal? filterPrice, string priceCondition, DateTime? filterDate, string dateCondition, int pageNumber = 1,int pageSize = 10)
        {
            var userId = _userManager.GetUserId(User); // Get the current user's ID

            // Get user's expenses
            var userExpenses = _context.Expenses.Where(e => e.UserId == userId);

            
            
            // Apply filtering by description
            if (!string.IsNullOrEmpty(filter))
            {
                userExpenses = userExpenses.Where(e => e.Description.Contains(filter));
            }

            // Apply filtering by price
            if (filterPrice.HasValue && !string.IsNullOrEmpty(priceCondition))
            {
                userExpenses = priceCondition switch
                {
                    "lower" => userExpenses.Where(e => e.Value < filterPrice.Value),
                    "higher" => userExpenses.Where(e => e.Value > filterPrice.Value),
                    "equal" => userExpenses.Where(e => e.Value == filterPrice.Value),
                    _ => userExpenses
                };
            }

            // Apply filtering by date
            if (filterDate.HasValue && !string.IsNullOrEmpty(dateCondition))
            {
                userExpenses = dateCondition switch
                {
                    "before" => userExpenses.Where(e => e.Date < filterDate.Value),
                    "after" => userExpenses.Where(e => e.Date > filterDate.Value),
                    "equal" => userExpenses.Where(e => e.Date.Date == filterDate.Value.Date), // Compare date without time
                    _ => userExpenses
                };
            }

            // Fetch data into memory
            var expensesList = await userExpenses.ToListAsync();

            // Apply sorting
            expensesList = sortOrder switch
            {
                "price_desc" => expensesList.OrderByDescending(e => (double)e.Value).ToList(),
                "price_asc" => expensesList.OrderBy(e => (double)e.Value).ToList(),
                "date_desc" => expensesList.OrderByDescending(e => e.Date).ToList(),
                "date_asc" => expensesList.OrderBy(e => e.Date).ToList(),
                _ => expensesList.OrderBy(e => e.Id).ToList(), // Default sorting by Id
            };




            // Group expenses by date and calculate total for each day
            var groupedData = expensesList
                .GroupBy(e => e.Date.Date) // Group by date (only date, ignoring time)
                .Select(g => new
                {
                    Date = g.Key,
                    Total = g.Sum(e => (double)e.Value)
                })
                .OrderBy(g => g.Date) // Sort by date
                .ToList();


            // Create cumulative total by adding previous day's totals
            double cumulativeTotal = 0;
            var cumulativeData = groupedData.Select(g => new
            {
                Date = g.Date.ToString("yyyy-MM-dd"),
                CumulativeTotal = (cumulativeTotal += g.Total) // Add the current day's total to the running total
            }).ToList();

            // Serialize both data sets to JSON
            var graphDataCumulative = JsonSerializer.Serialize(cumulativeData);
            var graphDataDaily = JsonSerializer.Serialize(groupedData);

            // Passing the graph data to the view
            ViewBag.GraphDataCumulative = graphDataCumulative;
            ViewBag.GraphDataDaily = graphDataDaily;



            // Calculate total expenses
            var totalExpenses = expensesList.Sum(x => (double)x.Value);

            ViewBag.Expenses = totalExpenses;
            ViewBag.SortOrder = sortOrder;
            ViewBag.Filter = filter;
            ViewBag.FilterPrice = filterPrice;
            ViewBag.PriceCondition = priceCondition;
            ViewBag.FilterDate = filterDate;
            ViewBag.DateCondition = dateCondition;

            // Pagination logic
            var totalRecords = expensesList.Count;
            var paginatedList = expensesList.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);


            return View(paginatedList);
        }


        public IActionResult CreateEditExpense(int? id)
        {
            if (id != null)
            {

                var userId = _userManager.GetUserId(User); // Get the current user's ID
                var expenseInDb = _context.Expenses.SingleOrDefault(e => e.Id == id && e.UserId == userId);
                if (expenseInDb == null)
                {
                    return NotFound(); // Return a 404 if the expense doesn't belong to the user
                }
                return View(expenseInDb);
            }

            return View();
        }

        public IActionResult DeleteExpense(int id)
        {
            var userId = _userManager.GetUserId(User); // Get the current user's ID
            var expenseInDb = _context.Expenses.SingleOrDefault(e => e.Id == id && e.UserId == userId);
            if (expenseInDb == null)
            {
                return NotFound(); // Return a 404 if the expense doesn't belong to the user
            }

            _context.Expenses.Remove(expenseInDb);
            _context.SaveChanges();

            return RedirectToAction("Expenses");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEditExpenseForm(Expense model)
        {

            var userId = _userManager.GetUserId(User);
            model.UserId = userId;

            if (model.Id == 0) // If no Id, assume it's a new record
            {
                Console.WriteLine("Adding new expense.");
                _context.Expenses.Add(model);
            }
            else
            {
                Console.WriteLine("Updating existing expense.");
                _context.Expenses.Update(model);
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Saved changes to database.");
            return RedirectToAction("Expenses");
        }


        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Graphs()
        {
            var userId = _userManager.GetUserId(User); // Get the current user's ID

            // Get user's expenses
            var userExpenses = _context.Expenses.Where(e => e.UserId == userId).ToList();

            // Group expenses by date and calculate total for each day
            var groupedData = userExpenses
                .GroupBy(e => e.Date.Date) // Group by date (only date, ignoring time)
                .Select(g => new
                {
                    Date = g.Key,
                    Total = g.Sum(e => (double)e.Value)
                })
                .OrderBy(g => g.Date) // Sort by date
                .ToList();

            // Create cumulative total by adding previous day's totals
            double cumulativeTotal = 0;
            var cumulativeData = groupedData.Select(g => new
            {
                Date = g.Date.ToString("yyyy-MM-dd"),
                CumulativeTotal = (cumulativeTotal += g.Total) // Add the current day's total to the running total
            }).ToList();

            // Serialize both data sets to JSON
            var graphDataCumulative = JsonSerializer.Serialize(cumulativeData);
            var graphDataDaily = JsonSerializer.Serialize(groupedData);

            // Passing the graph data to the view
            ViewBag.GraphDataCumulative = graphDataCumulative;
            ViewBag.GraphDataDaily = graphDataDaily;

            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}