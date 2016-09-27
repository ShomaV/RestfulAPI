using ExpenseTracker.DTO;
using System.Collections.Generic;
using System.Web.Mvc;

namespace ExpenseTracker.WebClient.Controllers
{
    using Helpers;
    using Newtonsoft.Json;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class ExpenseGroupsController : Controller
    {
        public async Task<ActionResult> Index()
        {
            var client = ExpenseTrackerHttpClient.GetClient();
            HttpResponseMessage response = await client.GetAsync("api/expensegroups");
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                var model = JsonConvert.DeserializeObject<IEnumerable<ExpenseGroup>>(content);
                return View(model.ToList());
            }
            return Content("An error occured");
        }


        // GET: ExpenseGroups/Details/5
        public ActionResult Details(int id)
        {
            return Content("Not implemented yet.");
        }

        // GET: ExpenseGroups/Create

        public ActionResult Create()
        {
            return View();
        }

        // POST: ExpenseGroups/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ExpenseGroup expenseGroup)
        {
            return View();
        }

        // GET: ExpenseGroups/Edit/5

        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ExpenseGroups/Edit/5   
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, ExpenseGroup expenseGroup)
        {
            return View();
        }


        // POST: ExpenseGroups/Delete/5
        //public ActionResult Delete(int id)
        //{
        //    return View();
        //}
    }
}
