using ExpenseTracker.Repository;
using ExpenseTracker.Repository.Factories;
using System;
using System.Linq;
using System.Web.Http;

namespace ExpenseTracker.API.Controllers
{
    using Helpers;
    using Marvin.JsonPatch;
    using Repository.Entities;
    using System.Collections.Generic;
    using System.Net;
    using System.Web;
    using System.Web.Http.Cors;
    using System.Web.Http.Routing;

    [EnableCors("*", "*", "GET,POST")]
    public class ExpenseGroupsController : ApiController
    {
        readonly IExpenseTrackerRepository _repository;
        readonly ExpenseGroupFactory _expenseGroupFactory = new ExpenseGroupFactory();
        const int maxPageSize = 10;

        public ExpenseGroupsController()
        {
            _repository = new ExpenseTrackerEFRepository(new
                Repository.Entities.ExpenseTrackerContext());
        }

        public ExpenseGroupsController(IExpenseTrackerRepository repository)
        {
            _repository = repository;
        }

        [Route("api/expensegroups", Name = "ExpenseGroupsList")]
        public IHttpActionResult Get(string sort = "id", string status = null, string userId = null,
            string fields = null, int page = 1, int pageSize = 5)
        {
            try
            {
                bool includeExpenses = false;
                var listOfFields = new List<string>();
                if (fields != null)
                {
                    listOfFields = fields.ToLower().Split(',').ToList();
                    includeExpenses = listOfFields.Any(f => f.Contains("expenses"));
                }

                int statusId = -1;
                if (status != null)
                {
                    switch (status.ToLower())
                    {
                        case "open":
                            statusId = 1;
                            break;
                        case "confirmed":
                            statusId = 2;
                            break;
                        case "processed":
                            statusId = 3;
                            break;
                    }
                }

                IQueryable<ExpenseGroup> expenseGroups = null;
                if (includeExpenses)
                    expenseGroups = _repository.GetExpenseGroupsWithExpenses();
                else
                {
                    expenseGroups = _repository.GetExpenseGroups();
                }

                //get expensegroups from repository
                expenseGroups = expenseGroups
                    .ApplySort(sort)
                    .Where(eg => (statusId == -1) || eg.ExpenseGroupStatusId == statusId)
                    .Where(eg => (userId == null || eg.UserId == userId));

                pageSize = pageSize > maxPageSize ? maxPageSize : pageSize;

                //calculate data for metadata
                var totalCount = expenseGroups.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var urlHelper = new UrlHelper(Request);
                var prevlink = page > 1
                    ? urlHelper.Link("ExpenseGroupsList",
                        new
                        {
                            page = page - 1,
                            pageSize = pageSize,
                            sort = sort,
                            status = status,
                            userId = userId,
                            fields = fields
                        }) : "";

                var nextLink = page < totalPages
                    ? urlHelper.Link("ExpenseGroupsList",
                        new
                        {
                            page = page + 1,
                            pageSize = pageSize,
                            sort = sort,
                            status = status,
                            userId = userId,
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

                return Ok(expenseGroups
                    //.ApplySort(sort)
                    .Skip(pageSize * (page - 1))
                    .Take(pageSize)
                    .ToList()
                    .Select(eg => _expenseGroupFactory.CreateDataShapedObject(eg, listOfFields)));

            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        public IHttpActionResult Get(int id)
        {
            try
            {
                var expenseGroup = _repository.GetExpenseGroup(id);
                if (expenseGroup == null)
                    return NotFound();
                return Ok(_expenseGroupFactory.CreateExpenseGroup(expenseGroup));
            }
            catch (Exception e)
            {

                return InternalServerError();
            }
        }

        public IHttpActionResult Post([FromBody] DTO.ExpenseGroup expenseGroup)
        {
            try
            {
                if (expenseGroup == null)
                    return BadRequest();
                var eg = _expenseGroupFactory.CreateExpenseGroup(expenseGroup);
                var result = _repository.InsertExpenseGroup(eg);
                if (result.Status == RepositoryActionStatus.Created)
                {
                    var newExpenseGroup = _expenseGroupFactory.CreateExpenseGroup(result.Entity);
                    return Created(Request.RequestUri + "/", newExpenseGroup);
                }
                return BadRequest();
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        public IHttpActionResult Put(int id, [FromBody] DTO.ExpenseGroup expenseGroup)
        {
            try
            {
                if (expenseGroup == null)
                    return BadRequest();

                var eg = _expenseGroupFactory.CreateExpenseGroup(expenseGroup);
                var result = _repository.UpdateExpenseGroup(eg);
                if (result.Status == RepositoryActionStatus.Updated)
                {
                    var updatedExpenseGroup = _expenseGroupFactory.CreateExpenseGroup(result.Entity);
                    return Ok(updatedExpenseGroup);
                }

                if (result.Status == RepositoryActionStatus.NotFound)
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

        [HttpPatch]
        public IHttpActionResult Patch(int id,
            [FromBody] JsonPatchDocument<DTO.ExpenseGroup> expenseGroupPatchDocument)
        {
            try
            {
                if (expenseGroupPatchDocument == null)
                    return BadRequest();
                var expenseGroup = _repository.GetExpenseGroup(id);
                if (expenseGroup == null)
                    return NotFound();

                var eg = _expenseGroupFactory.CreateExpenseGroup(expenseGroup);
                expenseGroupPatchDocument.ApplyTo(eg);

                var result = _repository.UpdateExpenseGroup(
                    _expenseGroupFactory.CreateExpenseGroup(eg));
                if (result.Status == RepositoryActionStatus.Updated)
                {
                    var updatedExpenseGroup = _expenseGroupFactory.CreateExpenseGroup(result.Entity);
                    return Ok(updatedExpenseGroup);
                }

                if (result.Status == RepositoryActionStatus.NotFound)
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

        public IHttpActionResult Delete(int id)
        {
            try
            {
                var result = _repository.DeleteExpenseGroup(id);
                if (result.Status == RepositoryActionStatus.Deleted)
                {
                    return StatusCode(HttpStatusCode.NoContent);
                }
                if (result.Status == RepositoryActionStatus.NotFound)
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
    }
}
