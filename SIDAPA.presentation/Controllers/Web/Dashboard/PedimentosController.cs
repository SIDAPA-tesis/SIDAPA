using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SIDAPA.presentation.Controllers.Web.Dashboard
{
    public class PedimentosController : Controller
    {
        // GET: PedimentosController
        public ActionResult Index()
        {
            return View();
        }

        // GET: PedimentosController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: PedimentosController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PedimentosController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: PedimentosController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: PedimentosController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: PedimentosController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: PedimentosController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
