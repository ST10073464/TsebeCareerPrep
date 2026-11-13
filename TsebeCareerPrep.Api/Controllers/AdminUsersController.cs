
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TsebeCareerPrep.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator")]
    public class AdminUsersController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AdminUsersController(
            UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: api/AdminUsers
        // Administrator: View registered users and roles.
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users
                .AsNoTracking()
                .OrderBy(u => u.Email)
                .ToListAsync();

            var results = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager
                    .GetRolesAsync(user);

                var isLockedOut =
                    await _userManager.IsLockedOutAsync(user);

                results.Add(new
                {
                    user.Id,
                    user.Email,
                    user.UserName,
                    Roles = roles,
                    IsLockedOut = isLockedOut
                });
            }

            return Ok(results);
        }
    }
}
