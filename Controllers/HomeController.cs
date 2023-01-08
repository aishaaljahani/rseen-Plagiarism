using Plagiarism.Models;
using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace Plagiarism.Controllers
{
    public class HomeController : BaseController
    {
        PlagiarismEntities db = new PlagiarismEntities();

        [Authorize]
        public ActionResult Index()
        {
            var projects = db.Documents.Count();
            var rejectedProjects = db.Documents.Where(x => x.Status == false).Count();
            var students = db.Students.Count();
            var teachers = db.Teachers.Count();

            ViewBag.projects = projects;
            ViewBag.rejectedProjects = rejectedProjects;
            ViewBag.students = students;
            ViewBag.teachers = teachers;

            return View();
        }
        [Authorize]
        public ActionResult Upload()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Upload(DocumentViewModel model)
        {
            int id = GetCurrentUserId();

            if (db.Documents.FirstOrDefault(x => x.Title.ToLower().Trim() == model.Title.ToLower().Trim()) != null)
            {
                ModelState.AddModelError("", "Document already exists with same title.");
            }
            if (model.FileUpload == null || model.FileUpload.ContentLength == 0)
            {
                ModelState.AddModelError("", "Please Upload File");
            }
            if (ModelState.IsValid)
            {
                string fileName = "";
                string extention = "";
                string path = "";
                string result = "";
                bool status = false;

                fileName = Path.GetFileNameWithoutExtension(model.FileUpload.FileName);
                extention = Path.GetExtension(model.FileUpload.FileName);
                path = Path.Combine(Server.MapPath("~/Uploads/Projects"), fileName + extention);
                model.FileUpload.SaveAs(path);

                PlagiarismUtil util = new PlagiarismUtil();

                string popularWords = Path.Combine(Server.MapPath("~/PopularWords"), "PopularWordsFile.txt");

                if (extention == ".docx")
                    result = util.OpenWordDocument(path);
                else if (extention == ".pptx")
                    result = util.OpenPresentationDocument(path);
                else if (extention == ".xlsx")
                    result = util.OpenSpreadsheetDocument(path);
                else if (extention == ".pdf")
                    result = util.OpenPdfDocument(path);

                if (string.IsNullOrEmpty(result))
                    ModelState.AddModelError("", "Please select valid document of allowed types.");
                else
                {
                    ViewBag.WordsCount = result.Split(' ').Count();
                    ViewBag.PopularWordsCount = util.SearchPopularWordsCount(result, popularWords);
                    string detail = util.SearchSimilarity(result, popularWords);
                    ViewBag.Similarity = detail.Replace("%%%", fileName + extention);
                    if(detail== "System did not find any similarity.")
                    {
                        status = true;
                    }
                    Document document = new Document()
                    {
                        Title = model.Title,
                        Abstract = result,
                        UploaderId = id,
                        FilePath = path,
                        UploadDate = DateTime.Now,
                        Status = status,
                        StatusDetails = detail
                    };
                    db.Documents.Add(document);
                    db.SaveChanges();
                }
            }
            return View();
        }
    }
}