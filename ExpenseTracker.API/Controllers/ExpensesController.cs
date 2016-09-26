using ExpenseTracker.Repository;
using ExpenseTracker.Repository.Factories;
using Marvin.JsonPatch;
using System;
using System.Net;
using System.Web.Http;

namespace ExpenseTracker.API.Controllers
{
    using Helpers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Http.Routing;

    [RoutePrefix("api")]
    public class ExpensesController : ApiController
    {
        readonly IExpenseTrackerRepository _repository;
        readonly ExpenseFactory _expenseFactory = new ExpenseFactory();
        private const int MaxPageSize = 2;

        public ExpensesController()
        {
            _repository = new ExpenseTrackerEFRepository(new Repository.Entities.ExpenseTrackerContext());
        }

        public ExpensesController(IExpenseTrackerRepository repository)
        {
            _repository = repository;
        }

        [Route("expenses/{id}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {

                var result = _repository.DeleteExpense(id);

                if (result.Status == RepositoryActionStatus.Deleted)
                {
                    return StatusCode(HttpStatusCode.NoContent);
                }
                else if (result.Status == RepositoryActionStatus.NotFound)
                {
                    return NotFound();
                }

                return BadRequest();
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        [Route("expenses")]
        public IHttpActionResult Post([FromBody]DTO.Expense expense)
        {
            try
            {
                if (expense == null)
                {
                    return BadRequest();
                }

                // map
                var exp = _expenseFactory.CreateExpense(expense);

                var result = _repository.InsertExpense(exp);
                if (result.Status == RepositoryActionStatus.Created)
                {
                    // map to dto
                    var newExp = _expenseFactory.CreateExpense(result.Entity);
                    return Created(Request.RequestUri + "/" + newExp.Id.ToString(), newExp);
                }

                return BadRequest();

            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }


        [Route("expenses/{id}")]
        public IHttpActionResult Put(int id, [FromBody]DTO.Expense expense)
        {
            try
            {
                if (expense == null)
                {
                    return BadRequest();
                }

                // map
                var exp = _expenseFactory.CreateExpense(expense);

                var result = _repository.UpdateExpense(exp);
                if (result.Status == RepositoryActionStatus.Updated)
                {
                    // map to dto
                    var updatedExpense = _expenseFactory.CreateExpense(result.Entity);
                    return Ok(updatedExpense);
                }
                else if (result.Status == RepositoryActionStatus.NotFound)
                {
                    return NotFound();
                }

                return BadRequest();
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }


        [Route("expenses/{id}")]
        [HttpPatch]
        public IHttpActionResult Patch(int id, [FromBody]JsonPatchDocument<DTO.Expense> expensePatchDocument)
        {
            try
            {
                // find 
                if (expensePatchDocument == null)
                {
                    return BadRequest();
                }

                var expense = _repository.GetExpense(id);
                if (expense == null)
                {
                    return NotFound();
                }

                //// map
                var exp = _expenseFactory.CreateExpense(expense);

                // apply changes to the DTO
                expensePatchDocument.ApplyTo(exp);

                // map the DTO with applied changes to the entity, & update
                var result = _repository.UpdateExpense(_expenseFactory.CreateExpense(exp));

                if (result.Status == RepositoryActionStatus.Updated)
                {
                    // map to dto
                    var updatedExpense = _expenseFactory.CreateExpense(result.Entity);
                    return Ok(updatedExpense);
                }

                return BadRequest();
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        //api/expensegroups/1/expenses
        [Route("expensegroups/{expenseGroupId}/expenses")]
        public IHttpActionResult Get(int expenseGroupId, string sort = "date", string fields = null, int page = 1,
            int pageSize = MaxPageSize)
        {
            try
            {
                List<string> listOfFields = new List<string>();
                if (fields != null)
                    listOfFields = fields.ToLower().Split(',').ToList();

                var expenses = _repository.GetExpenses(expenseGroupId);
                if (expenses == null)
                {
                    return NotFound();
                }

                pageSize = pageSize > MaxPageSize ? MaxPageSize : pageSize;

                //calculate data for metadata
                var totalCount = expenses.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var urlHelper = new UrlHelper(Request);
                var prevlink = page > 1
                    ? urlHelper.Link("ExpenseGroupsList",
                        new
                        {
                            page = page - 1,
                            pageSize = pageSize,
                            sort = sort,
                            fields = fields
                        }) : "";

                var nextLink = page < totalPages
                    ? urlHelper.Link("ExpenseGroupsList",
                        new
                        {
                            page = page + 1,
                            pageSize = pageSize,
                            sort = sort,
                            fields = fields
                        }) : "";

                var paginationHeader = new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = totalPages,
                    previousPageLink = prevlink,
                    nextPageLink = nextLink
                };

                HttpContext.Current.Response.Headers.Add("X-Pagination",
                    Newtonsoft.Json.JsonConvert.SerializeObject(paginationHeader));

                //return result

                return Ok(expenses
                    .ApplySort(sort)
                    .Skip(pageSize * (page - 1))
                    .Take(pageSize)
                    .ToList()
                    .Select(eg => _expenseFactory.CreateDataShapedObject(eg, listOfFields)));
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        [VersionedRoute("expensegroups/{expenseGroupId}/expenses/{expenseId}", 1)]
        [VersionedRoute("expenses/{expenseId}", 1)]
        public IHttpActionResult Get(int expenseId, int? expenseGroupId = null)
        {
            try
            {
               Repository.Entities.Expense expense = null;
                if (expenseGroupId == null)
                {
                    expense = _repository.GetExpense(expenseId);
                }
                else
                {
                    var expnsesForGroup = _repository.GetExpenses(
                        (int)expenseGroupId);
                    if (expnsesForGroup != null)
                    {
                        expense = expnsesForGroup.FirstOrDefault(eg => eg.Id == expenseId);
                    }
                }
                if (expense != null)
                {
                    var returnValue = _expenseFactory.CreateExpense(expense);
                    return Ok(returnValue);
                }
                return NotFound();
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        [VersionedRoute("expensegroups/{expenseGroupId}/expenses/{expenseId}", 2)]
        [VersionedRoute("expenses/{expenseId}", 2)]
        public IHttpActionResult GetV2(int expenseId, int? expenseGroupId = null)
        {
            try
            {
                Repository.Entities.Expense expense = null;
                if (expenseGroupId == null)
                {
                    expense = _repository.GetExpense(expenseId);
                }
                else
                {
                    var expnsesForGroup = _repository.GetExpenses(
                        (int)expenseGroupId);
                    if (expnsesForGroup != null)
                    {
                        expense = expnsesForGroup.FirstOrDefault(eg => eg.Id == expenseId);
                    }
                }
                if (expense != null)
                {
                    var returnValue = _expenseFactory.CreateExpense(expense);
                    return Ok(returnValue);
                }
                return NotFound();
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }
    }
}