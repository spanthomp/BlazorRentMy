using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RentMyApi.Configuration;
using RentMyApi.Models.DTOs.Requests;
using RentMyApi.Models.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace RentMyApi.Controllers
{
    //define route
    [Route("api/[controller]")] // api/authManagement
    [ApiController]
    //not utilising any ui stuff so only need to inherit from simpler controller base class
    public class AuthManagementController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        //user management already defined in startup class so just have to define
        private readonly JwtConfig _jwtConfig;
        //utilising jwt.configure - responsible for filling dependency injection as already filled out in startup class

        //using contsructor initialisation as again defined in startup class,
        //need to inject the user manager with identity user and call it userManager
        //second parameter will be options provider cause you will inherit options from startup class
        public AuthManagementController(
            UserManager<IdentityUser> userManager,
            IOptionsMonitor<JwtConfig> optionsMonitor)
        {
            //initialise user manager
            _userManager = userManager;
            //jwt config takes any value within app settings and injects into the controller
            _jwtConfig = optionsMonitor.CurrentValue;
        }

        [HttpPost]
        [Route("Register")]
        //utilise request created earlier - user registration dto from requests dto and call user
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto user)
        {
            //modelstate built in function - need to check model processing is valid
            if (ModelState.IsValid)
            {
                // need to check if email is already registered by utilising user manager
                var existingUser = await _userManager.FindByEmailAsync(user.Email);

                if (existingUser != null)
                {
                    return BadRequest(new RegistrationResponse()
                    {
                        //if user cannot register show list of errors
                        Errors = new List<string>() {
                                "Email already in use"
                            },
                        Success = false
                    });
                }

                //if email isnt in use and user is created, use usermanager to add it
                var newUser = new IdentityUser() { Email = user.Email, UserName = user.Username };
                var isCreated = await _userManager.CreateAsync(newUser, user.Password);
                //need to make sure hwne created you can resume otherwiser return a bad request
                if (isCreated.Succeeded)
                {
                    var jwtToken = GenerateJwtToken(newUser);

                    return Ok(new RegistrationResponse()
                    {
                        Success = true,
                        Token = jwtToken
                    });
                }
                else
                {
                    return BadRequest(new RegistrationResponse()
                    {
                        //within response check the error list, utilise descriptions
                        //and put into list
                        Errors = isCreated.Errors.Select(x => x.Description).ToList(),
                        Success = false
                        //define success as false (success is from auth result configuration class)
                    });
                }
            }
            //if not return the user a bad request from registration response in responses dto
            return BadRequest(new RegistrationResponse()
            {
                //within response check the error list again
                Errors = new List<string>() {
                        "Invalid payload"
                    },
                Success = false
            });
        }

        //added login functionality - similar to registration
        [HttpPost]
        [Route("Login")]
        //userloginrequest similar to user registration dto - simple attributes for users like email etc
        public async Task<IActionResult> Login([FromBody] UserLoginRequest user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(user.Email);

                if (existingUser == null)
                {
                    return BadRequest(new RegistrationResponse()
                    {
                        Errors = new List<string>() {
                                "Invalid login request"
                            },
                        Success = false
                    });
                }

                var isCorrect = await _userManager.CheckPasswordAsync(existingUser, user.Password);

                if (!isCorrect)
                {
                    return BadRequest(new RegistrationResponse()
                    {
                        Errors = new List<string>() {
                                "Invalid login request"
                            },
                        Success = false
                    });
                }

                var jwtToken = GenerateJwtToken(existingUser);

                return Ok(new RegistrationResponse()
                {
                    Success = true,
                    Token = jwtToken
                });
            }

            return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>() {
                        "Invalid payload"
                    },
                Success = false
            });
        }


        //new function to generate jwt function that can be used in register function for when user
        //is created
        private string GenerateJwtToken(IdentityUser user)
        {
            //start process of creating tokens using jwt token hander
            //jwt tokens have 3 parts
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            //first thing to define is getting the security key
            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);
            //descriptor is a class that contains claims
            //-which are information defined within jwt in order to read certain information
            var tokenDescriptor = new SecurityTokenDescriptor
            //then need to define the references
            {
                Subject = new ClaimsIdentity(new[]
                {
                    //user id just created
                    new Claim("Id", user.Id),
                    //default claim
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                    //jti is an id that will be used in order to utlise the refresh token 
                    //functionality later on
                    //in startup class required expiration time was set to false, once user logs in for the first time 
                    //they will get token directly from api
                    //another token they will get will be the reffresh token which has time stamps to say how long /
                    //it will last - this functionality will also be added later
                }),
                //next define the expiry of the tokens
                //then the signing credentials that defines what type of encryption algorithm will be used on tokens
                Expires = DateTime.UtcNow.AddHours(6),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            //next you prepare token to be created based on that descriptor created earlier
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);
            //this function should return a security token thats encrypted

            return jwtToken;
        }
    }
}