using Microsoft.AspNetCore.Mvc;
using ClothingShop.Data;
using Microsoft.EntityFrameworkCore;

namespace ClothingShop.Controllers
{
    public class NotificationsController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // POST: Đánh dấu thông báo đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        // POST: Đánh dấu tất cả thông báo đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
