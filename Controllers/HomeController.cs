using Exam.Model;
using Exam.packages;
using Mailjet.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Exam.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        //EXam-----------------------------------------------------------------------
        IPKG_TO_DO package;

        private IConfiguration _configuration;
        private readonly MailjetClient _client;
        private readonly IPKG_TO_DO _package;


        public HomeController(IPKG_TO_DO package, IConfiguration configuration)
        {
            _package = package;
            _configuration = configuration;
        }



        public async Task<IActionResult> Login(UserDto request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Invalid request." });
            }

            try
            {
                var loginResponse = await _package.LoginUser(request.UserName, request.Password);

                if (loginResponse != null)
                {
                    return Ok(new
                    {
                        token = loginResponse.Token,
                        role = loginResponse.Role,
                    });
                }
                else
                {
                    return Unauthorized(new { message = "Invalid username or password." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"An error occurred: {ex.Message}" });
            }
        }



        [HttpPost("Register")]
        public async Task<IActionResult> Register(UserData userData)
        {
            if (userData.Name == null)
            {
                return BadRequest("არასწორი მონაცემები"); 
            }

            try
            {
                await _package.AddEmployee(userData.Name, userData.Password);
                return Ok(new { message = "User created successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"An error occurred: {ex.Message}" });
            }
        }




        [HttpGet("newParsedtakedata")]
        public IActionResult takedata()
        {
            var x = _package.TakeParsedData();

            return Ok(x);

        }



        [HttpPost("newParsedAddQuestions")]
        public IActionResult AddQuestions([FromBody] List<Questions> questions)
        {
            try
            {
                _package.ParseJson(questions);

                return Ok(new { message = "Questions added successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred: {ex.Message}" });
            }
        }




    }
}