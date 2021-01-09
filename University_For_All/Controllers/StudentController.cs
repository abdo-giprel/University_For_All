﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using University_For_All.Models;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Drawing;
using University_For_All.ViewModels;
using System.IO;
using System.Net;
using System.Web.Security;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;

namespace University_For_All.Controllers
{
    public class StudentController : Controller
    {
        static ApplicationDbContext db = new ApplicationDbContext();
        UserManager<ApplicationUser> UserManeger = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
        [HttpGet]
        public ActionResult Index()
        {
            var Students = db.Student.Include(s => s.Faculty).ToList();
            return View(Students);
        }
        //View Form For Adding new student
        [HttpGet]
        public ActionResult New()
        {
            var departments = db.Departments.ToList();
            var faculties = db.Faculty.ToList();
            if (faculties.Count==0 && departments.Count==0)
            {
                return Content("Please add Faculty or Department first");
            }
            var newStudent = new StudentNewStudentViewModels()
            {
                Departments = departments,
                Faculties = faculties
            };

            return View(newStudent);
        }
        //Action for send new Student To DataBase
        [HttpPost]
        public ActionResult New(Student student,HttpPostedFileBase upload)
        {
            var userWithSameEmail = db.Student.SingleOrDefault(s => s.st_email == student.st_email);
            var userWithSamephone = db.Student.SingleOrDefault(s => s.st_phone == student.st_phone);
            if (ModelState.IsValid)
            {
                if (upload!=null)
                {
                    var path = Path.Combine(Server.MapPath("~/Upload/StudentImage"), upload.FileName);
                    upload.SaveAs(path);
                }

                if (userWithSameEmail==null && userWithSamephone==null)
                {
                    student.st_picture = upload == null ? "~/images/male-student-icon-png_251938.jpg" : "~/Upload/StudentImage/" + upload.FileName;
                    student.enroll_date = DateTime.Now;
                    db.Student.Add(student);
                    db.SaveChanges();
                    ApplicationUser newUser = new ApplicationUser()
                    {
                        Email = student.st_email,
                        UserName = student.st_email,
                    };
                    var check = UserManeger.Create(newUser, student.st_password);
                    if (check.Succeeded)
                    {
                        UserManeger.AddToRole(newUser.Id, "student");
                    }
                }
                else
                    return Content("<h2>Thers is User With The same <span style='color:red'> Phone Number</span> | <span style='color:Blue'> Email</span></h2>");
            }
            return RedirectToAction("Index");
        }
            
        // GET: Student
       
        [HttpGet]
        public ActionResult Details(int? id)
        {
            var studentDetails = db.Takes.Include(t=>t.Student.Department).Include(t => t.Student).Include(t => t.Course).Include(t => t.Grade).ToList()
                .Where(t => t.Studentid == id);
            if (studentDetails.Count()==0)
            {
                var studentDetails2 = db.Student.Include(s=>s.Department).Include(s=>s.Faculty).SingleOrDefault(s => s.id == id);
                return View("JustStudent",studentDetails2);
            }
            return View(studentDetails);
        }

        [HttpGet]
        [Route("Student/Edit/{id}/{routename?}")]
        public ActionResult Edit(int id,string routename)
        {
            var student = db.Student.Include(s => s.Faculty).Include(s => s.Department).SingleOrDefault(s => s.id == id);
            if (routename== "change Password")
            {
                return View("ChangePassword", student);
            }
            else if (routename== "change Email")
            {
                return View("ChangeEmail",student);
            }
            
            return View(student);
        }
        [HttpPost]
        public ActionResult Save(Student student,int id,string newPassword,HttpPostedFileBase upload, string confirmPassword,string oldPassword, string from)
        {
            
            var editedstudent = db.Student.SingleOrDefault(s => s.id == id);
            if (from== "changePassword")
            {
                if (oldPassword == editedstudent.st_password)
                {
                    editedstudent.st_password = newPassword;
                    editedstudent.st_confirmPassword = confirmPassword;
                    db.SaveChanges();
                    var user = UserManeger.FindByEmail(editedstudent.st_email);
                    Logout();

                }
            }
            else if (from== "changeEmail")
            {
                var user = UserManeger.FindByEmail(editedstudent.st_email);
                user.UserName = student.st_email;
                user.Email = student.st_email;
                UserManeger.Update(user);
                editedstudent.st_email = student.st_email;
                db.SaveChanges();
                return Logout();
            }
            else if (from=="Edit")
            {

                editedstudent.st_address = student.st_address;
                editedstudent.st_city = student.st_city;
                editedstudent.st_phone = student.st_phone;
                var oldImg = editedstudent.st_picture.Substring(22);
                System.IO.File.Delete(@"D:\abdo\FCI\my work\web\University_For_All\University_For_All\Upload\StudentImage\"+ oldImg);
                var path = Path.Combine(Server.MapPath("~/Upload/StudentImage"), upload.FileName);
                upload.SaveAs(path);
                editedstudent.st_picture = "~/Upload/StudentImage/" + upload.FileName;
                db.SaveChanges();

            }
            return Content("saved");

        }
        public ActionResult Delete(int id)
        {
            var student = db.Student.SingleOrDefault(s => s.id == id);
            return View(student);
        }
        [HttpPost]
        public ActionResult Delete(Student student)
        {
            var editedstudent = db.Student.SingleOrDefault(s => s.id == student.id);

            var user = UserManeger.FindByEmail(editedstudent.st_email);
            UserManeger.Delete(user);
            db.Student.Remove(db.Student.Single(s => s.id ==editedstudent.id));
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }
        public ActionResult Logout()
        {
            AuthenticationManager.SignOut();
            return RedirectToAction("login", "Account");
        }

    }
}