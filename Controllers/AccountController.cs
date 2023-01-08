using Plagiarism.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Plagiarism.Controllers
{
    public class AccountController : BaseController
    {
        PlagiarismEntities db = new PlagiarismEntities();
        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }

        [ValidateAntiForgeryToken, HttpPost, AllowAnonymous]
        public ActionResult Login(LoginViewModel model)
        {
            var pass = string.IsNullOrEmpty(model.Password) ? "" : Utilities.Hash(model.Password);
            var user = db.Accounts.FirstOrDefault(x => x.UserName.ToLower().Trim() == model.UserName.Trim().ToLower() && x.Password == pass);
            if (user != null)
            {
                var authTicket = new FormsAuthenticationTicket(1, user.UserName, DateTime.Now, DateTime.Now.AddMinutes(30), true, user.RoleName);
                string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                var userCookie = new HttpCookie("UserFullName", user.UserName);
                var roleCookie = new HttpCookie("UserRoles", user.RoleName);
                var userIdCookie = new HttpCookie("UserId", user.Id.ToString());
                HttpContext.Response.Cookies.Add(authCookie);
                HttpContext.Response.Cookies.Add(userCookie);
                HttpContext.Response.Cookies.Add(roleCookie);
                HttpContext.Response.Cookies.Add(userIdCookie);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Invalid username/password");
            }
            return View();
        }

        [ValidateAntiForgeryToken, HttpPost]
        public ActionResult Logout()
        {
            HttpContext.Request.Cookies.Remove("UserFullName");
            HttpContext.Request.Cookies.Remove("UserRoles");
            HttpContext.Request.Cookies.Remove("UserId");
            Session.Abandon();
            RemoveCookie("UserFullName");
            RemoveCookie("UserRoles");
            RemoveCookie("UserId");
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }
        private void RemoveCookie(string cookieName)
        {
            HttpCookie currentUserCookie = HttpContext.Request.Cookies[cookieName];
            if (currentUserCookie != null)
            {
                currentUserCookie.Expires = DateTime.Now.AddDays(-10);
                currentUserCookie.Value = null;
            }
            HttpContext.Response.Cookies.Remove(cookieName);
        }


        [Authorize]
        public ActionResult Manage()
        {
            int id = GetCurrentUserId();

            Account account = db.Accounts.Find(id);
            if (account == null)
            {
                return HttpNotFound();
            }
            AccountViewModel model = new AccountViewModel { Id = account.Id, UserName = account.UserName, Password = account.Password, RoleName = account.RoleName };
            return View(model);
        }

        // This action will accept the submitted city details modified by admin.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Manage(AccountViewModel model, string NewPassword, string ConfirmNewPassword)
        {
            if (NewPassword != ConfirmNewPassword)
            {
                ModelState.AddModelError("Password", "New password and confirm new password doesn't match.");
            }
            else if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(NewPassword))
                {
                    model.Password = Utilities.Hash(NewPassword);
                }

                var account = new Account { Id = model.Id, UserName = model.UserName, Password = model.Password, RoleName = model.RoleName };

                db.Entry(account).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult AdminUsers(string filter = null, int page = 1,
                  int pageSize = 5)
        {
            var admins = db.Accounts.Where(x => x.RoleName == "Admin").ToList();
            var records = new PagedList<Account>();
            ViewBag.filter = filter;
            records.Content = admins
                        .Where(x => filter == null ||
                                 x.UserName?.Contains(filter) == true ||
                                 x.Admin.FullName?.Contains(filter) == true ||
                                 x.Admin.Phone?.Contains(filter) == true ||
                                 x.Admin.Address?.Contains(filter) == true ||
                                 x.Admin.Email?.Contains(filter) == true
                              )
                              .OrderBy(x => x.Id)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

            records.TotalRecords = admins
             .Where(x => filter == null ||
                                 x.UserName?.Contains(filter) == true ||
                                 x.Admin.FullName?.Contains(filter) == true ||
                                 x.Admin.Phone?.Contains(filter) == true ||
                                 x.Admin.Address?.Contains(filter) == true ||
                                 x.Admin.Email?.Contains(filter) == true).Count();

            records.CurrentPage = page;
            records.PageSize = pageSize;

            return View(records);
        }
        [Authorize(Roles = "Admin")]
        public ActionResult AdminUsersCreate()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminUsersCreate(RegisterAdminViewModel model)
        {
            if (db.Accounts.FirstOrDefault(x => x.Admin.Email.ToLower().Trim() == model.Email.ToLower().Trim()) != null)
            {
                ModelState.AddModelError("Email", "Email already exists.");
            }
            if (db.Accounts.FirstOrDefault(x => x.UserName.ToLower().Trim() == model.UserName.ToLower().Trim()) != null)
            {
                ModelState.AddModelError("UserName", "UserName already exists.");
            }
            if (ModelState.IsValid)
            {
                Account account = new Account { UserName = model.UserName, Password = Utilities.Hash(model.Password), RoleName = "Admin" };

                db.Accounts.Add(account);
                db.SaveChanges();
                var admin = new Admin { FullName = model.FullName, Email = model.Email, Address = model.Address, Phone = model.Phone, Account = account };
                db.Admins.Add(admin);
                db.SaveChanges();
                return RedirectToAction("AdminUsers");
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult AdminUsersDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Account account = db.Accounts.Find(id);
            if (account == null)
            {
                return HttpNotFound();
            }
            return View(account);
        }


        [Authorize(Roles = "Admin")]
        public ActionResult AdminUsersEdit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Account account = db.Accounts.Find(id);
            if (account == null)
            {
                return HttpNotFound();
            }
            EditAdminViewModel model = new EditAdminViewModel { Id = account.Id, UserName = account.UserName, Password = account.Password, FullName = account.Admin.FullName, Email = account.Admin.Email, Address = account.Admin.Address, Phone = account.Admin.Phone, AdminId = account.Admin.Id };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminUsersEdit(EditAdminViewModel model)
        {
            if (db.Accounts.FirstOrDefault(x => x.Id != model.Id && x.Admin.Email.ToLower().Trim() == model.Email.ToLower().Trim()) != null)
            {
                ModelState.AddModelError("Email", "Email already exists.");
            }
            if (db.Accounts.FirstOrDefault(x => x.Id != model.Id && x.UserName.ToLower().Trim() == model.UserName.ToLower().Trim()) != null)
            {
                ModelState.AddModelError("UserName", "User Name already exists.");
            }
            if (ModelState.IsValid)
            {
                Account account = new Account { Id = model.Id, UserName = model.UserName, Password = model.Password, RoleName = "Admin" };
                db.Entry(account).State = EntityState.Modified;
                db.SaveChanges();

                Admin admin = new Admin { Id = model.AdminId, FullName = model.FullName, Email = model.Email, Address = model.Address, Phone = model.Phone };
                db.Entry(admin).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("AdminUsers");
            }

            return View(model);
        }




        [Authorize(Roles = "Admin")]
        public ActionResult StudentUsers(string filter = null, int page = 1,
                 int pageSize = 5)
        {
            var students = db.Accounts.Where(x => x.RoleName == "Student").ToList();
            var records = new PagedList<Account>();
            ViewBag.filter = filter;
            records.Content = students
                        .Where(x => filter == null ||
                                 x.UserName?.Contains(filter) == null ||
                                 x.Student.FullName?.Contains(filter) == null ||
                                 x.Student.Phone?.Contains(filter) == null ||
                                 x.Student.Address?.Contains(filter) == null ||
                                 x.Student.Email?.Contains(filter) == null
                              )
                              .OrderBy(x => x.Id)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

            records.TotalRecords = students
             .Where(x => filter == null ||
                                 x.UserName?.Contains(filter) == null ||
                                 x.Student.FullName?.Contains(filter) == null ||
                                 x.Student.Phone?.Contains(filter) == null ||
                                 x.Student.Address?.Contains(filter) == null ||
                                 x.Student.Email?.Contains(filter) == null).Count();

            records.CurrentPage = page;
            records.PageSize = pageSize;

            return View(records);
        }
        [Authorize(Roles = "Admin")]
        public ActionResult StudentUsersCreate()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StudentUsersCreate(RegisterStudentViewModel model)
        {
            if (db.Accounts.FirstOrDefault(x => x.Student.Email.ToLower().Trim() == model.Email.ToLower().Trim()) != null)
            {
                ModelState.AddModelError("Email", "Email already exists.");
            }
            if (db.Accounts.FirstOrDefault(x => x.UserName.ToLower().Trim() == model.UserName.ToLower().Trim()) != null)
            {
                ModelState.AddModelError("UserName", "UserName already exists.");
            }
            if (ModelState.IsValid)
            {
                Account account = new Account { UserName = model.UserName, Password = Utilities.Hash(model.Password), RoleName = "Student" };

                db.Accounts.Add(account);
                db.SaveChanges();
                var student = new Student { FullName = model.FullName, Email = model.Email, Address = model.Address, Phone = model.Phone, Account = account };
                db.Students.Add(student);
                db.SaveChanges();
                return RedirectToAction("StudentUsers");
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult StudentUsersDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Account account = db.Accounts.Find(id);
            if (account == null)
            {
                return HttpNotFound();
            }
            return View(account);
        }


        [Authorize(Roles = "Admin")]
        public ActionResult StudentUsersEdit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Account account = db.Accounts.Find(id);
            if (account == null)
            {
                return HttpNotFound();
            }
            EditStudentViewModel model = new EditStudentViewModel { Id = account.Id, UserName = account.UserName, Password = account.Password, FullName = account.Student.FullName, Email = account.Student.Email, Address = account.Student.Address, Phone = account.Student.Phone, StudentId = account.Student.Id };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StudentUsersEdit(EditStudentViewModel model)
        {
            if (db.Accounts.FirstOrDefault(x => x.Id != model.Id && x.Student.Email.ToLower().Trim() == model.Email.ToLower().Trim()) != null)
            {
                ModelState.AddModelError("Email", "Email already exists.");
            }
            if (db.Accounts.FirstOrDefault(x => x.Id != model.Id && x.UserName.ToLower().Trim() == model.UserName.ToLower().Trim()) != null)
            {
                ModelState.AddModelError("UserName", "User Name already exists.");
            }
            if (ModelState.IsValid)
            {
                Account account = new Account { Id = model.Id, UserName = model.UserName, Password = model.Password, RoleName = "Student" };
                db.Entry(account).State = EntityState.Modified;
                db.SaveChanges();

                Student student = new Student { Id = model.StudentId, FullName = model.FullName, Email = model.Email, Address = model.Address, Phone = model.Phone };
                db.Entry(student).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("StudentUsers");
            }

            return View(model);
        }



        [Authorize(Roles = "Admin")]
        public ActionResult TeacherUsers(string filter = null, int page = 1,
         int pageSize = 5)
        {
            var teachers = db.Accounts.Where(x => x.RoleName == "Teacher").ToList();
            var records = new PagedList<Account>();
            ViewBag.filter = filter;
            records.Content = teachers
                        .Where(x => filter == null ||
                                 x.UserName?.Contains(filter) == null ||
                                 x.Teacher?.FullName.Contains(filter) == null ||
                                 x.Teacher?.Phone.Contains(filter) == null ||
                                 x.Teacher?.Major.Contains(filter) == null ||
                                 x.Teacher?.Email.Contains(filter) == null
                              )
                              .OrderBy(x => x.Id)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

            records.TotalRecords = teachers
             .Where(x => filter == null ||
                                 x.UserName.Contains(filter) ||
                                 x.Teacher.FullName.Contains(filter) ||
                                 x.Teacher.Phone.Contains(filter) ||
                                 x.Teacher.Major.Contains(filter) ||
                                 x.Teacher.Email.Contains(filter)).Count();

            records.CurrentPage = page;
            records.PageSize = pageSize;

            return View(records);
        }
        [Authorize(Roles = "Admin")]
        public ActionResult TeacherUsersCreate()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TeacherUsersCreate(RegisterTeacherViewModel model)
        {
            if (db.Accounts.FirstOrDefault(x => x.Teacher.Email.ToLower().Trim() == model.Email.ToLower().Trim()) != null)
            {
                ModelState.AddModelError("Email", "Email already exists.");
            }
            if (db.Accounts.FirstOrDefault(x => x.UserName.ToLower().Trim() == model.UserName.ToLower().Trim()) != null)
            {
                ModelState.AddModelError("UserName", "UserName already exists.");
            }
            if (ModelState.IsValid)
            {
                Account account = new Account { UserName = model.UserName, Password = Utilities.Hash(model.Password), RoleName = "Teacher" };

                db.Accounts.Add(account);
                db.SaveChanges();
                var teacher = new Teacher { FullName = model.FullName, Email = model.Email, Major = model.Major, Phone = model.Phone, Account = account };
                db.Teachers.Add(teacher);
                db.SaveChanges();
                return RedirectToAction("TeacherUsers");
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult TeacherUsersDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Account account = db.Accounts.Find(id);
            if (account == null)
            {
                return HttpNotFound();
            }
            return View(account);
        }


        [Authorize(Roles = "Admin")]
        public ActionResult TeacherUsersEdit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Account account = db.Accounts.Find(id);
            if (account == null)
            {
                return HttpNotFound();
            }
            EditTeacherViewModel model = new EditTeacherViewModel { Id = account.Id, UserName = account.UserName, Password = account.Password, FullName = account.Teacher.FullName, Email = account.Teacher.Email, Major = account.Teacher.Major, Phone = account.Teacher.Phone, TeacherId = account.Teacher.Id };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TeacherUsersEdit(EditTeacherViewModel model)
        {
            if (db.Accounts.FirstOrDefault(x => x.Id != model.Id && x.Teacher.Email.ToLower().Trim() == model.Email.ToLower().Trim()) != null)
            {
                ModelState.AddModelError("Email", "Email already exists.");
            }
            if (db.Accounts.FirstOrDefault(x => x.Id != model.Id && x.UserName.ToLower().Trim() == model.UserName.ToLower().Trim()) != null)
            {
                ModelState.AddModelError("UserName", "User Name already exists.");
            }
            if (ModelState.IsValid)
            {
                Account account = new Account { Id = model.Id, UserName = model.UserName, Password = model.Password, RoleName = "Teacher" };
                db.Entry(account).State = EntityState.Modified;
                db.SaveChanges();

                Teacher teacher = new Teacher { Id = model.TeacherId, FullName = model.FullName, Email = model.Email, Major = model.Major, Phone = model.Phone };
                db.Entry(teacher).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("TeacherUsers");
            }

            return View(model);
        }
    }
}