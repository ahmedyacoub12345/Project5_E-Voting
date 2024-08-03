using E_Voting.Models;
using System;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Xml;
using System.Threading;

namespace E_Voting.Controllers
{
    public class USERController : Controller
    {
        private ElectionEntities DB = new ElectionEntities();

        // GET: USER
        public ActionResult Index()
        {
            return View();
        }


        public ActionResult Login(User user)
        {
            try
            {
                if (user == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return View();
                }

                var existingUser = DB.Users.FirstOrDefault(u => u.NationalNumber == user.NationalNumber);

                if (existingUser == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return View();
                }

                if (existingUser.Password == "password")
                {
                    string newPassword = GenerateRandomPassword();
                    existingUser.Password = newPassword;
                    DB.SaveChanges();

                    SendConfirmationEmail(existingUser.Email, newPassword);

                    ViewBag.Emailsent = "The code has been sent to your Email";
                    return RedirectToAction("LoginUser", new { NationalNumber = user.NationalNumber });
                }
                else
                {
                    ViewBag.NationalId = existingUser.NationalNumber;
                    return RedirectToAction("LoginUser", new { NationalNumber = user.NationalNumber });
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while processing your request. Please try again later.");
                Console.WriteLine("Exception message: " + ex.Message);
            }

            return View();
        }

        public ActionResult LoginUser(string NationalNumber, string Email, string password)
        {
            var user = DB.Users.FirstOrDefault(u => u.NationalNumber == NationalNumber);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View();
            }

            if (ModelState.IsValid)
            {
                if (Email == user.Email && password == user.Password)
                {
                    TempData["LoggedUser"] = JsonConvert.SerializeObject(user);
                    return RedirectToAction("TypeOfElection", new { id = user.ID });
                }

                ModelState.AddModelError("", "Invalid login attempt.");
            }

            return View();
        }

        private string GenerateRandomPassword()
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            int length = 8;
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

        // Sends a confirmation email with the new password
        private void SendConfirmationEmail(string toEmail, string confirmationCode)
        {
            string fromEmail = System.Configuration.ConfigurationManager.AppSettings["FromEmail"];
            string smtpUsername = System.Configuration.ConfigurationManager.AppSettings["SmtpUsername"];
            string smtpPassword = System.Configuration.ConfigurationManager.AppSettings["SmtpPassword"];

            string subjectText = "Your Confirmation Code";
            string messageText = $"Your confirmation code is {confirmationCode}";

            string smtpServer = "smtp.gmail.com";
            int smtpPort = 587;

            using (MailMessage mailMessage = new MailMessage())
            {
                mailMessage.From = new MailAddress(fromEmail);
                mailMessage.To.Add(toEmail);
                mailMessage.Subject = subjectText;
                mailMessage.Body = messageText;
                mailMessage.IsBodyHtml = false;

                using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = true;

                    smtpClient.Send(mailMessage);
                }
            }
        }
        public ActionResult TypeOfElection(int id)
        {
            var user = DB.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            //LocalElection
            if (user.LocalElections is false)
            {

                ViewBag.LocalElectionsPath = "LocalElections";
            }
            else
            {
                ViewBag.LocalElectionsPath = null;
            }
            //WhiteLocalElection
            if (user.whitePaperLocalElections is false)
            {

                ViewBag.LocalElectionsPath = "White LocalElections";
            }
            else
            {
                ViewBag.LocalElectionsPath = null;
            }

            //PartyElections
            if (user.PartyElections is false)
            {

                ViewBag.PartyElections = "PartyElections";
            }
            else
            {
                ViewBag.PartyElections = null;

            }

            return View();
        }

        public ActionResult LocalElections()
        {
            var userJson = TempData["LoggedUser"].ToString();
            var user = JsonConvert.DeserializeObject<User>(userJson);


            return View();
        }
        public ActionResult PartyElections()
        {
            var userJson = TempData["LoggedUser"].ToString();
            var user = JsonConvert.DeserializeObject<User>(userJson);
            var party = DB.GeneralListings.ToList();
            return View();
        }
    }

}
