using E_Voting.Models;
using System;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Web.Mvc;
using Newtonsoft.Json;

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

        // الأكشن لتسجيل الدخول
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
                }

                // تخزين المستخدم في الجلسة وإعادة التوجيه إلى LoginUser
                Session["LoggedUser"] = JsonConvert.SerializeObject(existingUser);
                return RedirectToAction("LoginUser", new { ID =user.ID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while processing your request. Please try again later.");
                Console.WriteLine("Exception message: " + ex.Message);
            }

            return View();
        }

        // الأكشن لاستقبال طلب تسجيل الدخول
        public ActionResult LoginUser(string nationalNumber)
        {
            var user = DB.Users.FirstOrDefault(u => u.NationalNumber == nationalNumber);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View();
            }

            // تخزين المستخدم في الجلسة
            Session["LoggedUser"] = JsonConvert.SerializeObject(user);
            ViewBag.NationalNumber = nationalNumber;
            ViewBag.Email = user.Email;
            return RedirectToAction("TypeOfElection", new { id = user.ID});

        }

        // توليد كلمة مرور عشوائية
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

        // إرسال بريد تأكيد بكلمة المرور الجديدة
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

        // الأكشن لتحديد نوع الانتخابات
        public ActionResult TypeOfElection(int id)
        {
            var user = DB.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            // تحديد مسارات الانتخابات المحلية
            if ((bool)!user.LocalElections)
            {
                ViewBag.LocalElectionsPath = "LocalElections";
            }
            else
            {
                ViewBag.LocalElectionsPath = null;
            }

            // تحديد مسارات الانتخابات المحلية بالورقة البيضاء
            if ((bool)!user.whitePaperLocalElections)
            {
                ViewBag.WhiteLocalElectionsPath = "White LocalElections";
            }
            else
            {
                ViewBag.WhiteLocalElectionsPath = null;
            }

            // تحديد مسارات انتخابات الحزب
            if ((bool)!user.PartyElections)
            {
                ViewBag.PartyElections = "PartyElections";
            }
            else
            {
                ViewBag.PartyElections = null;
            }

            return View();
        }

        // الأكشن للانتخابات المحلية
        public ActionResult LocalElections()
        {
            if (Session["LoggedUser"] == null)
            {
                return RedirectToAction("Login");
            }

            var userJson = Session["LoggedUser"].ToString();
            var user = JsonConvert.DeserializeObject<User>(userJson);

            ViewBag.UserId = user.ID;

            var localLists = DB.LocalLists.ToList();
            return View(localLists);
        }

        [HttpPost]
        public ActionResult LocalElections(int selectedListId)
        {
            if (Session["LoggedUser"] == null)
            {
                return RedirectToAction("Login");
            }

            var userJson = Session["LoggedUser"].ToString();
            var user = JsonConvert.DeserializeObject<User>(userJson);

            var selectedList = DB.LocalLists.Find(selectedListId);
            if (selectedList != null)
            {
                // Update user table to reflect that the user has voted in local elections
                user.LocalElections = true;
                DB.Entry(user).State = System.Data.Entity.EntityState.Modified;
                DB.SaveChanges();
            }

            return RedirectToAction("TypeOfElection", new { id = user.ID });
        }


        public ActionResult PartyElections()
        {
            if (Session["LoggedUser"] == null)
            {
                return RedirectToAction("Login");
            }

            var userJson = Session["LoggedUser"].ToString();
            var user = JsonConvert.DeserializeObject<User>(userJson);

            ViewBag.UserId = user.ID;

            var partyLists = DB.GeneralListings.ToList();
            return View(partyLists);
        }

        [HttpPost]
        public ActionResult PartyElections(int selectedPartyListId)
        {
            if (Session["LoggedUser"] == null)
            {
                return RedirectToAction("Login");
            }

            var userJson = Session["LoggedUser"].ToString();
            var user = JsonConvert.DeserializeObject<User>(userJson);

            var selectedPartyList = DB.GeneralListings.Find(selectedPartyListId);
            if (selectedPartyList != null)
            {
                // Update user table to reflect that the user has voted in party elections
                user.PartyElections = true;
                DB.Entry(user).State = System.Data.Entity.EntityState.Modified;
                DB.SaveChanges();
            }

            return RedirectToAction("TypeOfElection", new { id = user.ID });
        }


    }
}
