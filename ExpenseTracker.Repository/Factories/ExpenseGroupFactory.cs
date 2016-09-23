using ExpenseTracker.Repository.Entities;
using ExpenseTracker.Repository.Helpers;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace ExpenseTracker.Repository.Factories
{
    public class ExpenseGroupFactory
    {
        ExpenseFactory expenseFactory = new ExpenseFactory();

        public ExpenseGroupFactory()
        {

        }

        public ExpenseGroup CreateExpenseGroup(DTO.ExpenseGroup expenseGroup)
        {
            return new ExpenseGroup()
            {
                Description = expenseGroup.Description,
                ExpenseGroupStatusId = expenseGroup.ExpenseGroupStatusId,
                Id = expenseGroup.Id,
                Title = expenseGroup.Title,
                UserId = expenseGroup.UserId,
                Expenses = expenseGroup.Expenses == null ? new List<Expense>() : expenseGroup.Expenses.Select(e => expenseFactory.CreateExpense(e)).ToList()
            };
        }


        public DTO.ExpenseGroup CreateExpenseGroup(ExpenseGroup expenseGroup)
        {
            return new DTO.ExpenseGroup()
            {
                Description = expenseGroup.Description,
                ExpenseGroupStatusId = expenseGroup.ExpenseGroupStatusId,
                Id = expenseGroup.Id,
                Title = expenseGroup.Title,
                UserId = expenseGroup.UserId,
                Expenses = expenseGroup.Expenses.Select(e => expenseFactory.CreateExpense(e)).ToList()
            };
        }

        public object CreateDataShapedObject(ExpenseGroup expenseGroup, List<string> lstOfFields)
        {
            return CreateDataShapedObject(CreateExpenseGroup(expenseGroup), lstOfFields);
        }

        public object CreateDataShapedObject(DTO.ExpenseGroup expenseGroup, List<string> listOfFields)
        {
            var listOfFieldsToWorkWith = new List<string>(listOfFields);
            if (!listOfFieldsToWorkWith.Any())
                return expenseGroup;

            //does it include any expense-related field?
            List<string> listOfExpenseFields = listOfFieldsToWorkWith.Where(f => f.Contains("expenses")).ToList();

            //if one of those fields is "expenses", we need to ensure FULL expense is returned. If
            //it's only subfields, only those subfields have to be returned.
            bool returnPartialExpense = listOfExpenseFields.Any() && !listOfExpenseFields.Contains("expenses");

            if (returnPartialExpense)
            {
                //remove all expense-related fields from the list of fields,
                //as we will use the CreateDataShapeObject function in the ExpenseFactory for that
                listOfFieldsToWorkWith.RemoveRange(listOfExpenseFields);
                listOfExpenseFields = listOfExpenseFields.Select(f => f.Substring(f.IndexOf(".") + 1)).ToList();
            }
            else
            {
                //we shouldnt return a partial expense, but the consumer might still have
                //asked for a subfield together with the main field, ie., expense, expense.id. We
                //need to remove those subfields in that case.
                listOfExpenseFields.Remove("expenses");
                listOfFieldsToWorkWith.RemoveRange(listOfExpenseFields);
            }

            //create a new ExpandoObject & dynamically create the properties for this object
            //if we have an expense

            ExpandoObject objectToReturn = new ExpandoObject();
            foreach (var field in listOfFieldsToWorkWith)
            {
                //need to include public and instance, b/c specifying a binding flag overwrites the
                //already-existing binding flags.

                var fieldValue = expenseGroup.GetType()
                    .GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                    .GetValue(expenseGroup, null);

                ((IDictionary<string, object>)objectToReturn).Add(field, fieldValue);
            }

            if (returnPartialExpense)
            {
                //add a list of expenses and in that add all those expenses
                List<object> expenses = new List<object>();
                foreach (var expense in expenseGroup.Expenses)
                {
                    expenses.Add(expenseFactory.CreateDataShapedObject(expense, listOfExpenseFields));
                }
                ((IDictionary<string, object>)objectToReturn).Add("expenses", expenses);
            }
            return objectToReturn;
        }

    }
}
