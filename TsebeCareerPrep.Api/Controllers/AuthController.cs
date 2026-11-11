
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


        // POST: api/Auth/register
        [HttpPost("register")]
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

            // Create the Identity user.
            var user = new IdentityUser
            {
                UserName = request.Email,
                Email = request.Email
            };

            // Identity securely hashes the password.
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

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    message = "Account registered successfully.",
                    userId = user.Id,
                    email = user.Email
                });
        }


        // POST: api/Auth/login

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(
                request.Email);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                request.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return Unauthorized(new
                {
                    message = "Account temporarily locked. Try again later."
                });
            }

            if (result.IsNotAllowed)
            {
                return Unauthorized(new
                {
                    message = "Login is not permitted for this account."
                });
            }

            if (!result.Succeeded)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            return Ok(new
            {
                message = "Login successful.",
                userId = user.Id,
                email = user.Email
            });
        }


        [HttpPost("logout")]
        [Microsoft.AspNetCore.Authorization.Authorize]
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
