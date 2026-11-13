
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


        // POST: api/AdminUsers/{id}/unlock
        // Administrator: Unlock a temporarily locked user account.
        [HttpPost("{id}/unlock")]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User account not found."
                });
            }

            // Do not modify an account that is not locked.
            var isLockedOut = await _userManager.IsLockedOutAsync(user);

            if (!isLockedOut)
            {
                return BadRequest(new
                {
                    message = "This user account is not currently locked."
                });
            }

            // Clear the lockout and failed login attempts.
            var unlockResult = await _userManager.SetLockoutEndDateAsync(
                user,
                DateTimeOffset.UtcNow);

            if (!unlockResult.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Unable to unlock the user account."
                });
            }

            var resetResult = await _userManager.ResetAccessFailedCountAsync(user);

            if (!resetResult.Succeeded)
            {
                return BadRequest(new
                {
                    message = "The account lockout was cleared, but resetting failed login attempts was unsuccessful."
                });
            }

            return Ok(new
            {
                message = "User account unlocked successfully.",
                userId = user.Id,
                email = user.Email
            });
        }

    }
}
