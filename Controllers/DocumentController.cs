using Plagiarism.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Plagiarism.Controllers
{
    public class DocumentController : BaseController
    {
        PlagiarismEntities db = new PlagiarismEntities();

        [Authorize]
        public ActionResult Index(string filter = null, int page = 1,
                          int pageSize = 5)
        {
            var records = new PagedList<Document>();
            ViewBag.filter = filter;
            records.Content = db.Documents
                        .Where(x => filter == null ||
                                 x.Title.Contains(filter) ||
                                 x.Account.UserName.Contains(filter)
                              )
                              .OrderBy(x => x.Id)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

            records.TotalRecords = db.Documents
             .Where(x => filter == null ||
                                 x.Title.Contains(filter) ||
                                 x.Account.UserName.Contains(filter)).Count();

            records.CurrentPage = page;
            records.PageSize = pageSize;

            return View(records);
        }
        [Authorize]
        public ActionResult MyDocuments(string filter = null, int page = 1,
                  int pageSize = 5)
        {
            int id = GetCurrentUserId();
            var documents = db.Documents.Where(x => x.UploaderId == id);
            var records = new PagedList<Document>();
            ViewBag.filter = filter;
            records.Content = documents
                        .Where(x => filter == null ||
                                 x.Title.Contains(filter)
                              )
                              .OrderBy(x => x.Id)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

            records.TotalRecords = documents
             .Where(x => filter == null ||
                                 x.Title.Contains(filter)).Count();

            records.CurrentPage = page;
            records.PageSize = pageSize;

            return View(records);
        }

        [Authorize]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Document document = db.Documents.Find(id);
            if (document == null)
            {
                return HttpNotFound();
            }
            return View(document);
        }

        public FileResult Download(int? id)
        {
            Document document = db.Documents.Find(id);
            byte[] fileBytes = System.IO.File.ReadAllBytes(@"" + document.FilePath + "");
            string fileName = Path.GetFileName(@"" + document.FilePath + "");
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        public ActionResult UploadFile()
        {
            return View();
        }
    }
}