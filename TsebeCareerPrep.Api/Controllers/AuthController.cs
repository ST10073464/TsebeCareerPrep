
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
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // ======================================================
        // POST: api/Auth/register
        // PUBLIC CANDIDATE REGISTRATION
        // ======================================================

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request)
        {
            // Check whether the email is already registered.
            var existingUser = await _userManager
                .FindByEmailAsync(request.Email);

            if (existingUser != null)
            {
                var deactivated = await _context.DeactivatedUsers
                    .AnyAsync(u => u.UserId == existingUser.Id);
                if (deactivated)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                    var reset = await _userManager.ResetPasswordAsync(existingUser, token, request.Password);
                    if (!reset.Succeeded)
                    {
                        return BadRequest(new
                        {
                            message = "The account could not be restored. Check that the new password meets the password requirements.",
                            errors = reset.Errors.Select(error => error.Description)
                        });
                    }

                    _context.DeactivatedUsers.Remove(
                        await _context.DeactivatedUsers.SingleAsync(u => u.UserId == existingUser.Id));
                    _context.UserAdminActions.Add(new Models.UserAdminAction
                    {
                        TargetUserId = existingUser.Id,
                        TargetEmail = existingUser.Email ?? request.Email,
                        Action = "Restored",
                        PerformedByUserId = existingUser.Id,
                        OccurredUtc = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();

                    var existingRoles = await _userManager.GetRolesAsync(existingUser);
                    var restoredRole = existingRoles.Contains("SuperAdministrator")
                        ? "SuperAdministrator"
                        : existingRoles.Contains("Administrator") ? "Administrator" : "Candidate";

                    return StatusCode(StatusCodes.Status201Created, new
                    {
                        message = "Your previous account has been restored. Sign in using your new password.",
                        userId = existingUser.Id,
                        email = existingUser.Email,
                        role = restoredRole
                    });
                }

                return Conflict(new
                {
                    message = "An account with this email already exists."
                });
            }

            // Create a new Identity user.
            var user = new IdentityUser
            {
                UserName = request.Email,
                Email = request.Email
            };

            // Identity securely hashes and validates the password.
            var result = await _userManager.CreateAsync(
                user,
                request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Registration failed.",
                    errors = result.Errors.Select(
                        error => error.Description)
                });
            }

            // Public registration must always assign
            // the Candidate role.
            // Never accept an Administrator role
            // from registration input.

            const string candidateRole = "Candidate";

            var roleResult = await _userManager.AddToRoleAsync(
                user,
                candidateRole);

            if (!roleResult.Succeeded)
            {
                // Remove the newly created account if
                // its Candidate role cannot be assigned.
                var deleteResult =
                    await _userManager.DeleteAsync(user);

                if (!deleteResult.Succeeded)
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new
                        {
                            message =
                                "Registration could not be completed. Please contact support."
                        });
                }

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "Account registration could not be completed. Please try again."
                    });
            }

            // Registration completed successfully.
            // The user can now sign in.

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    message =
                        "Account registered successfully. You can now sign in.",
                    userId = user.Id,
                    email = user.Email,
                    role = candidateRole
                });
        }

        // ======================================================
        // POST: api/Auth/login
        // EXISTING LOGIN FUNCTIONALITY
        // ======================================================

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            // Find the user by email.
            var user = await _userManager
                .FindByEmailAsync(request.Email);

            if (user != null && await _context.DeactivatedUsers
                    .AnyAsync(u => u.UserId == user.Id))
            {
                return Unauthorized(new { message = "This account is deactivated. Register again with this email to restore it." });
            }

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            // Authenticate using ASP.NET Core Identity.
            var result = await _signInManager
                .PasswordSignInAsync(
                    user,
                    request.Password,
                    isPersistent: false,
                    lockoutOnFailure: true);

            // Check whether the account is locked.
            if (result.IsLockedOut)
            {
                return Unauthorized(new
                {
                    message =
                        "Account temporarily locked. Try again later."
                });
            }

            // Check whether login is permitted.
            if (result.IsNotAllowed)
            {
                return Unauthorized(new
                {
                    message =
                        "Login is not permitted for this account."
                });
            }

            // Reject invalid credentials.
            if (!result.Succeeded)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            // Retrieve the authenticated user's Identity roles.

            var roles = await _userManager.GetRolesAsync(user);

            string role;

            if (roles.Contains("SuperAdministrator"))
            {
                role = "SuperAdministrator";
            }
            else if (roles.Contains("Administrator"))
            {
                role = "Administrator";
            }
            else
            {
                role = "Candidate";
            }


            // Return authenticated user details and role.
            return Ok(new
            {
                message = "Login successful.",
                userId = user.Id,
                email = user.Email,
                role = role
            });
        }

        // ======================================================
        // POST: api/Auth/logout
        // EXISTING LOGOUT FUNCTIONALITY
        // ======================================================

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return Ok(new
            {
                message = "Logout successful."
            });
        }
    }
}
