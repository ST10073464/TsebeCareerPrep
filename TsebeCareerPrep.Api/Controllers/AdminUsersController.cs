
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TsebeCareerPrep.Api.Data;
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator,SuperAdministrator")]
    public class AdminUsersController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminUsersController(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
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
            var deactivatedIds = await _context.DeactivatedUsers
                .AsNoTracking().Select(u => u.UserId).ToListAsync();
            var deactivatedIdSet = deactivatedIds.ToHashSet();

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
                    IsLockedOut = isLockedOut,
                    IsDeactivated = deactivatedIdSet.Contains(user.Id)
                });
            }

            return Ok(results);
        }

        // POST: api/AdminUsers/{id}/promote
        // Only a SuperAdministrator can grant the Administrator role.
        [HttpPost("{id}/promote")]
        [Authorize(Roles = "SuperAdministrator")]
        public async Task<IActionResult> PromoteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User account not found." });
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (await _context.DeactivatedUsers.AnyAsync(u => u.UserId == id))
                return Conflict(new { message = "This account is deactivated." });

            if (roles.Contains("Administrator") ||
                roles.Contains("SuperAdministrator"))
            {
                return Conflict(new { message = "This user already has an administrative role." });
            }

            // Candidate accounts (including new public registrations) are
            // eligible. Role-less legacy accounts can also be promoted.
            if (roles.Any() && !roles.Contains("Candidate"))
            {
                return BadRequest(new { message = "Only candidate accounts can be promoted." });
            }

            if (!roles.Any())
            {
                var candidateResult = await _userManager.AddToRoleAsync(user, "Candidate");
                if (!candidateResult.Succeeded)
                    return BadRequest(new { message = "Unable to assign the Candidate role." });
            }

            var result = await _userManager.AddToRoleAsync(user, "Administrator");

            if (!result.Succeeded)
            {
                if (!roles.Any())
                    await _userManager.RemoveFromRoleAsync(user, "Candidate");
                return BadRequest(new
                {
                    message = "Unable to assign the Administrator role.",
                    errors = result.Errors.Select(error => error.Description)
                });
            }

            await RecordAction(user, "Promoted");

            return Ok(new
            {
                message = "Administrator role assigned successfully.",
                userId = user.Id,
                email = user.Email
            });
        }

        [HttpPost("{id}/demote")]
        [Authorize(Roles = "SuperAdministrator")]
        public async Task<IActionResult> DemoteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { message = "User account not found." });

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Administrator") || roles.Contains("SuperAdministrator"))
                return Conflict(new { message = "Only administrator accounts can be demoted." });

            var result = await _userManager.RemoveFromRoleAsync(user, "Administrator");
            if (!result.Succeeded) return BadRequest(new { message = "Unable to remove the Administrator role." });

            if (!roles.Contains("Candidate"))
            {
                var candidateResult = await _userManager.AddToRoleAsync(user, "Candidate");
                if (!candidateResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Administrator");
                    return BadRequest(new { message = "Unable to restore the Candidate role." });
                }
            }

            await RecordAction(user, "Demoted");
            return Ok(new { message = "Administrator role removed successfully." });
        }

        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "SuperAdministrator")]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { message = "User account not found." });

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Administrator") || roles.Contains("SuperAdministrator"))
                return Conflict(new { message = "Only administrator accounts can be removed." });
            if (string.Equals(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, id, StringComparison.Ordinal))
                return BadRequest(new { message = "You cannot remove your own account." });

            if (!await _context.DeactivatedUsers.AnyAsync(u => u.UserId == id))
            {
                _context.DeactivatedUsers.Add(new DeactivatedUser
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    DeactivatedUtc = DateTime.UtcNow
                });
                await RecordAction(user, "Deleted", saveChanges: false);
                await _context.SaveChangesAsync();
            }

            await _userManager.UpdateSecurityStampAsync(user);
            return Ok(new { message = "Administrator account deactivated." });
        }

        private async Task RecordAction(IdentityUser target, string action, bool saveChanges = true)
        {
            _context.UserAdminActions.Add(new UserAdminAction
            {
                TargetUserId = target.Id,
                TargetEmail = target.Email ?? string.Empty,
                Action = action,
                PerformedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                OccurredUtc = DateTime.UtcNow
            });
            if (saveChanges) await _context.SaveChangesAsync();
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
