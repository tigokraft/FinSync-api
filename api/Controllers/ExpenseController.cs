using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinSync.Data;
using FinSync.DTOs;
using FinSync.Models;
using System.Security.Claims;

namespace FinSync.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExpenseController : ControllerBase
    {
        private readonly FinSyncContext _context;

        public ExpenseController(FinSyncContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddExpense([FromBody] CreateExpenseDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Invalid token.");

            var expense = new Expense
            {
                UserId = userId.Value,
                Amount = dto.Amount,
                Tags = dto.Tags,
                Description = dto.Description,
                Date = dto.Date,
                CategoryId = dto.CategoryId
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Expense added." });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetExpenses()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Invalid token.");

            var expenses = await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == userId.Value)
                .OrderByDescending(e => e.Date)
                .Select(e => new ExpenseDto
                {
                    ExpenseId = e.ExpenseId,
                    Amount = e.Amount,
                    Tags = e.Tags,
                    Description = e.Description,
                    Date = e.Date,
                    CategoryName = e.Category.CategoryName
                })
                .ToListAsync();

            return Ok(expenses);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized("Invalid token.");

            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.ExpenseId == id && e.UserId == userId.Value);
            if (expense == null)
                return NotFound("Expense not found.");

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Expense deleted." });
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst("userId")?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}
