using LandPortal.Api.Data;
using LandPortal.Api.Dtos.Users;
using LandPortal.Api.DTOs;
using LandPortal.Api.Entities;
using LandPortal.Api.Helpers;
using LandPortal.Api.Security;
using LandPortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.RegularExpressions;


namespace LandPortal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly LandPortalDbContext _db;
        private readonly IJwtTokenService _jwt;
        private readonly EmailService _email;
        private readonly SmsService _sms;
        private readonly WhatsAppService _whatsAppService;
        private readonly GcpStorageService _storage;


        public AuthController(LandPortalDbContext db,IJwtTokenService jwt,EmailService email,WhatsAppService whatsAppService, GcpStorageService storage, SmsService sms)
        {
            _db = db;
            _jwt = jwt;
            _email = email;
            _sms = sms;
            _whatsAppService = whatsAppService;
            _storage = storage;
        }

        // ================= REGISTER =================

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
        {
            if (req == null)
                return BadRequest("Request body is required");

            req.Email = req.Email.Trim().ToLower();
            req.FullName = req.FullName.Trim();
            req.Phone = new string(req.Phone?.Where(char.IsDigit).ToArray());

            if (await _db.Users.AnyAsync(u => u.Email == req.Email))
                return BadRequest("Email already registered");

            if (!string.IsNullOrEmpty(req.Phone))
            {
                if (!Regex.IsMatch(req.Phone, @"^[6-9]\d{9}$"))
                    return BadRequest("Invalid phone number");

                if (await _db.Users.AnyAsync(u => u.Phone == req.Phone))
                    return BadRequest("Phone number already registered");
            }

            if (string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Password) ||
                string.IsNullOrWhiteSpace(req.FullName))
                return BadRequest("FullName, Email and Password are required");

            // ✅ Check verified OTP exists
            var verifiedOtp = await _db.EmailOtps
                .Where(x => x.Email == req.Email && x.IsVerified)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (verifiedOtp == null)
                return BadRequest("Please verify your email first");
                _db.EmailOtps.Remove(verifiedOtp);
                await _db.SaveChangesAsync();

            // ✅ Prevent duplicate users
            if (await _db.Users.AnyAsync(u => u.Email == req.Email))
                return BadRequest("Email already registered");

            var user = new User
            {
                Email = req.Email,
                FullName = req.FullName,
                Phone = req.Phone,
                PasswordHash = PasswordHasher.Hash(req.Password),
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var token = _jwt.CreateToken(user);

            return Ok(new AuthResponse
            {
                AccessToken = token,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                UserId = user.Id.ToString()
            });
        }

        // ================= LOGIN =================
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
        {
            // 1️⃣ Validate password
            if (string.IsNullOrWhiteSpace(req?.Password))
                return BadRequest(new { message = "Password is required." });

            // 2️⃣ Require either email OR phone
            if (string.IsNullOrWhiteSpace(req.Email) && string.IsNullOrWhiteSpace(req.Phone))
                return BadRequest(new { message = "Email or phone is required." });

            User? user = null;

            // 3️⃣ Try login via email
            if (!string.IsNullOrWhiteSpace(req.Email))
            {
                var email = req.Email.Trim().ToLower();
                user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
            }
            // 4️⃣ Else try login via phone
            else if (!string.IsNullOrWhiteSpace(req.Phone))
            {
                var phone = new string(req.Phone.Where(char.IsDigit).ToArray());
                user = await _db.Users.SingleOrDefaultAsync(u => u.Phone == phone);
            }

            if (user == null)
                return Unauthorized(new { message = "Email or phone is not registered" });

            if (!user.IsActive)
                return Unauthorized(new { message = "Account is disabled" });

            // 5️⃣ Verify password + active status
            if (!PasswordHasher.Verify(req.Password, user.PasswordHash!))
                return Unauthorized(new { message = "Incorrect password" });

            // 6️⃣ Generate JWT
            var token = _jwt.CreateToken(user);

            return Ok(new AuthResponse
            {
                AccessToken = token,
                Email = user.Email,
                FullName = user.FullName!,
                Role = user.Role,
                UserId = user.Id.ToString()
            });
        }

        // ================= PROFILE =================
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim);
            var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            return Ok(new
            {
                id = user.Id,
                fullName = user.FullName,
                email = user.Email,
                phone = user.Phone,
                role = user.Role,
                profilePhotoUrl = user.ProfilePhotoUrl   // ⭐ THIS WAS MISSING
            });
        }


        [Authorize]
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {
            var userId = User.GetUserId();
            var user = await _db.Users.FindAsync(userId);

            if (user == null)
                return NotFound("User not found");

            // 🔴 Validate Full Name
            if (string.IsNullOrWhiteSpace(dto.FullName))
                return BadRequest("Full name is required");

            // 🔴 Validate Email format
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                dto.Email = dto.Email.Trim().ToLower();

                if (!System.Text.RegularExpressions.Regex.IsMatch(
                    dto.Email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return BadRequest("Invalid email address");
                }

                // 🔴 Check duplicate email (exclude self)
                var emailUsed = await _db.Users.AnyAsync(u =>
                    u.Email == dto.Email && u.Id != userId);

                if (emailUsed)
                    return BadRequest("Email already used. Use a different email");
            }

            // 🔴 Validate Phone
            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                dto.Phone = new string(dto.Phone.Where(char.IsDigit).ToArray());

                if (dto.Phone.Length < 10)
                    return BadRequest("Invalid phone number");

                // 🔴 Check duplicate phone
                var phoneUsed = await _db.Users.AnyAsync(u =>
                    u.Phone == dto.Phone && u.Id != userId);

                if (phoneUsed)
                    return BadRequest("Phone number already used. Use a different number");
            }

            // ✅ Update
            user.FullName = dto.FullName.Trim();
            user.Email = dto.Email;
            user.Phone = dto.Phone;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Phone,
                user.Role
            });
        }


        // ================= CHANGE PASSWORD =================
        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("UserId claim missing");

            var userId = Guid.Parse(userIdClaim);
            var user = await _db.Users.FindAsync(userId);

            if (user == null)
                return NotFound("User not found");

            // ✅ Verify using SAME hasher
            if (!PasswordHasher.Verify(dto.CurrentPassword, user.PasswordHash!))
                return BadRequest("Current password is incorrect");

            // ✅ Hash using SAME hasher
            user.PasswordHash = PasswordHasher.Hash(dto.NewPassword);

            await _db.SaveChangesAsync();

            return Ok("Password changed successfully");
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("Email is required");

            var email = dto.Email.Trim().ToLower();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            // ❌ NOW we explicitly block non-registered emails
            if (user == null)
                return BadRequest("Email is not registered");

            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();

            user.PasswordResetOtp = otp;
            user.PasswordResetExpiry = DateTime.UtcNow.AddMinutes(10);
            user.PasswordResetAttempts = 0;

            await _db.SaveChangesAsync();

            try
            {
                await _email.SendOtpAsync(user.Email, otp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email OTP failed: " + ex.Message);
                return StatusCode(500, "Failed to send OTP");
            }

            return Ok("OTP sent successfully");
        }



        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Otp) ||
                string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest("Invalid request");
            }

            var email = dto.Email.Trim().ToLower();

            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Email == email &&
                u.PasswordResetOtp == dto.Otp &&
                u.PasswordResetExpiry != null &&
                u.PasswordResetExpiry > DateTime.UtcNow
            );

            if (user == null)
                return BadRequest("Invalid or expired OTP");

            if (user.PasswordResetAttempts >= 3)
                return BadRequest("Too many attempts. Please resend OTP.");

            user.PasswordResetAttempts++;

            user.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
            user.PasswordResetOtp = null;
            user.PasswordResetExpiry = null;
            user.PasswordResetAttempts = 0;

            await _db.SaveChangesAsync();

            return Ok("Password reset successful");
        }


        [Authorize]
        [HttpPost("profile-photo")]
        public async Task<IActionResult> UploadProfilePhoto(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim);

            try
            {
                var photoUrl = await _storage.UploadProfilePhotoAsync(
                    file.OpenReadStream(),
                    file.FileName,
                    file.ContentType,
                    userId
                );

                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                    return NotFound();

                user.ProfilePhotoUrl = photoUrl;
                await _db.SaveChangesAsync();

                return Ok(new { profilePhotoUrl = photoUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpDelete("profile-photo")]
        public async Task<IActionResult> DeleteProfilePhoto()
        {
            var userId = Guid.Parse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value
            );

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            user.ProfilePhotoUrl = null;
            await _db.SaveChangesAsync();

            return Ok();
        }


        [HttpPost("send-email-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> SendEmailOtp([FromBody] SendEmailOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required");

            var email = request.Email.Trim().ToLower();

            // ✅ NEW: Check if email already exists in Users table
            var emailExists = await _db.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
            {
                return Conflict(new { message = "Email already registered. Please login." });
            }

            // 🔁 Invalidate old OTPs
            var oldOtps = await _db.EmailOtps
                .Where(x => x.Email == email && !x.IsVerified)
                .ToListAsync();

            foreach (var o in oldOtps)
                o.IsVerified = true;

            // 🔐 Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();

            var record = new EmailOtp
            {
                Email = email,
                Otp = otp,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.EmailOtps.Add(record);
            await _db.SaveChangesAsync();

            await _email.SendOtpAsync(email, otp);

            return Ok(new { message = "OTP sent successfully" });
        }



        [HttpPost("verify-email-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyEmailOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Otp))
                return BadRequest("Email and OTP are required");

            var email = request.Email.Trim().ToLower();

            var otpRow = await _db.EmailOtps
                .Where(x =>
                    x.Email == email &&
                    !x.IsVerified &&
                    x.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRow == null)
                return BadRequest("OTP expired or not found");

            if (otpRow.Otp != request.Otp)
                return BadRequest("Invalid OTP");

            otpRow.IsVerified = true;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Email verified successfully" });
        }

        [Authorize]
        [HttpPost("profile-photo/save")]
        public async Task<IActionResult> SaveProfilePhoto([FromBody] string photoUrl)
        {
            var userId = User.GetUserId();

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.ProfilePhotoUrl = photoUrl;
            await _db.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        [HttpGet("unlocked-contacts")]
        public async Task<IActionResult> GetUnlockedContacts()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim);

            var data = await _db.ContactUnlockLogs
                .Where(x => x.UnlockedByUserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new UnlockedContactDto
                {
                    PropertyId = x.PropertyId,
                    PropertyTitle = x.PropertyTitle!,
                    PaymentAmount = x.PaymentAmount,
                    PaymentStatus = x.PaymentStatus,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return Ok(data);
        }
    }


}
