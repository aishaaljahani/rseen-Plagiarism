using Plagiarism.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Plagiarism.Controllers
{
    public class MessageController : BaseController
    {
        PlagiarismEntities db = new PlagiarismEntities();
        public ActionResult Inbox(string filter = null, int page = 1, int pageSize = 5)
        {
            int id = GetCurrentUserId();
            var messeages = db.Messages.Where(x => x.AccountTo.Id == id).ToList();

            var records = new Models.PagedList<Message>();
            ViewBag.filter = filter;
            records.Content = messeages.OrderByDescending(x => x.Id)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

            records.TotalRecords = messeages.Count();

            records.CurrentPage = page;
            records.PageSize = pageSize;

            return View(records);
        }

        public ActionResult outbox(string filter = null, int page = 1, int pageSize = 5)
        {
            int id = GetCurrentUserId();
            var messeages = db.Messages.Where(x => x.AccountFrom.Id == id).ToList();

            var records = new Models.PagedList<Message>();
            ViewBag.filter = filter;
            records.Content = messeages.OrderByDescending(x => x.Id)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

            records.TotalRecords = messeages.Count();

            records.CurrentPage = page;
            records.PageSize = pageSize;

            return View(records);
        }

        public ActionResult SendMessage()
        {
            int id = GetCurrentUserId();
            var users = db.Accounts.Where(x => x.Id != id).ToList();
            ViewBag.MessageToId = new SelectList(users, "Id", "UserName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendMessage([Bind(Include = "MessageToId,Content")] Message message)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    int id = GetCurrentUserId();
                    message.MessageFromId = id;
                    message.MessageDate = DateTime.Now;
                    db.Messages.Add(message);
                    db.SaveChanges();
                    return RedirectToAction("Outbox");
                }

                return View(message);
            }
            catch
            {
                return View();
            }
        }

        public ActionResult MessageDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Message message = db.Messages.Find(id);
            if (message == null)
            {
                return HttpNotFound();
            }

            ViewBag.MessageId = id.Value;

            var replies = db.Replies.Where(d => d.MessageId.Equals(id.Value)).ToList();
            ViewBag.Replies = replies;


            return View(message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddReply(FormCollection form)
        {
            int id = GetCurrentUserId();
            var ReplayText = form["ReplayText"].ToString();
            var MessageId = int.Parse(form["MessageId"]);
            Reply reply = new Reply()
            {
                UserId = id,
                ReplayText = ReplayText,
                ReplyDate = DateTime.Now,
                MessageId = MessageId
            };

            db.Replies.Add(reply);
            db.SaveChanges();

            return RedirectToAction("MessageDetails", new { id = MessageId });
        }
    }
}