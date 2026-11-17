
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TsebeCareerPrep.Api.Models;

namespace TsebeCareerPrep.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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
