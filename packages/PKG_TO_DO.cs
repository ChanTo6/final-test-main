using Exam.Model;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace Exam.packages
{
    public interface IPKG_TO_DO
    {

       
        public void get_tasks();
        public Task AddEmployee(string name, string password);
        public Task<LoginResponse> LoginUser(string username, string password);
        public void ParseJson(List<Questions> questions);
        public List<Questions> TakeParsedData();
        public string CreateToken(UserData user);



    }
    public class PKG_TO_DO : PKG_BASE, IPKG_TO_DO
    {

           UserData user = new UserData();
        private readonly IConfiguration _configuration;

        public PKG_TO_DO(IConfiguration configuration) : base(configuration)
        {
            _configuration = configuration;
        }

        public void get_tasks()
        {
            OracleConnection conn = new OracleConnection();

            conn.ConnectionString = Connstr;

            conn.Open();

        }

        public async Task AddEmployee(string name, string password)
        {


            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            try
            {
                using (OracleConnection conn = new OracleConnection(Connstr))
                {
                    conn.Open();
                    using (OracleCommand cmd = new OracleCommand("chanturia_nika_exam.Registration", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("p_name", OracleDbType.Varchar2).Value = name;
                        cmd.Parameters.Add("p_hashpassword", OracleDbType.Varchar2).Value = hashedPassword;

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (OracleException ex)
            {
                Console.WriteLine("OracleException: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
        }


       


        public async Task<LoginResponse> LoginUser(string username, string password)
        {
            UserData userData = null;

            try
            {
                using (var conn = new OracleConnection(Connstr))
                {
                    await conn.OpenAsync();

                    using (var cmd = new OracleCommand("chanturia_nika_exam.login", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("p_username", OracleDbType.Varchar2).Value = username;
                        var pUserData = new OracleParameter("p_user_data", OracleDbType.RefCursor)
                        {
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(pUserData);

                        await cmd.ExecuteNonQueryAsync();
                        using (var reader = ((OracleRefCursor)pUserData.Value).GetDataReader())
                        {
                            if (await reader.ReadAsync())
                            {
                                var hashedPassword = reader.GetString(4);

                                if (BCrypt.Net.BCrypt.Verify(password, hashedPassword))
                                {
                                    userData = new UserData
                                    {
                                        Role = reader.GetString(3),
                                        
                                    };
                                }
                            }
                        }
                    }
                }
            }
            catch (OracleException ex)
            {
                Console.WriteLine($"OracleException: {ex.Message}\nError Code: {ex.ErrorCode}\nStack Trace: {ex.StackTrace}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}\nStack Trace: {ex.StackTrace}");
                return null;
            }

            if (userData == null)
            {
                return null;
            }

            var token = CreateToken(userData);
            return new LoginResponse
            {
                Token = token,
                Role = userData.Role,
               
            };
        }



        // დამატება კითხვების და პასუხების;

        public void ParseJson(List<Questions> questions)
        {

        
            string jsonData = JsonConvert.SerializeObject(questions);

            using (OracleConnection conn = new OracleConnection(Connstr))
            {
                conn.Open();

                using (OracleCommand cmd = new OracleCommand("nika_questions_answears.parse_json", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    //ჯეისონისთვის ქლობს აქ ვაკეთებ
                    OracleClob clob = new OracleClob(conn);
                    clob.Write(jsonData.ToCharArray(), 0, jsonData.Length);

                    //ჯეისონისტივს პარამერტებს აქ ვაკეტებ
                    var clobParameter = new OracleParameter("p_json_data", OracleDbType.Clob)
                    {
                        Value = clob
                    };
                    cmd.Parameters.Add(clobParameter);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("JSON data parsed and inserted successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while parsing JSON data: " + ex.Message);
                    }
                    finally
                    {
                        // იუსინგის მაგივრად დისფოზს ვაკეთებ აქ <<
                        clob.Dispose();
                    }
                }
            }
        }

        // წამოღება კითხვების და პასუხების
        public List<Questions> TakeParsedData()
        {
            List<Questions> quizzes = new List<Questions>();

            using (OracleConnection conn = new OracleConnection(Connstr))
            {
                conn.Open();

                using (OracleCommand cmd = new OracleCommand("nika_questions_answears.get_quiz", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_curs", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int questionId = Convert.ToInt32(reader["question_id"]);
                            string questionText = reader["question"].ToString();

                            Questions quiz = quizzes.FirstOrDefault(q => q.id == questionId);

                            if (quiz == null)
                            {
                                quiz = new Questions
                                {
                                    id = questionId,
                                    Question = questionText,
                                    Answers = new List<Answers>()
                                };
                                quizzes.Add(quiz);
                            }

                            quiz.Answers.Add(new Answers
                            {
                                Answer = reader["answer"].ToString(),
                                Corect = Convert.ToInt32(reader["corect"])
                            });
                        }
                    }
                }
            }
            return quizzes;
        }








        public string CreateToken(UserData user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, user.Role ?? "User"),       
            };


            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value!));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: cred
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }

}
