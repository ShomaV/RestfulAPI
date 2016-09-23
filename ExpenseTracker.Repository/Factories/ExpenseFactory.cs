using ExpenseTracker.Repository.Entities;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace ExpenseTracker.Repository.Factories
{
    using System.Reflection;

    public class ExpenseFactory
    {
        public DTO.Expense CreateExpense(Expense expense)
        {
            return new DTO.Expense()
            {
                Amount = expense.Amount,
                Date = expense.Date,
                Description = expense.Description,
                ExpenseGroupId = expense.ExpenseGroupId,
                Id = expense.Id
            };
        }



        public Expense CreateExpense(DTO.Expense expense)
        {
            return new Expense()
            {
                Amount = expense.Amount,
                Date = expense.Date,
                Description = expense.Description,
                ExpenseGroupId = expense.ExpenseGroupId,
                Id = expense.Id
            };
        }

        public object CreateDataShapedObject(Expense expense, List<string> lstOfFields)
        {

            return CreateDataShapedObject(CreateExpense(expense), lstOfFields);
        }


        public object CreateDataShapedObject(DTO.Expense expense, List<string> lstOfFields)
        {

            if (!lstOfFields.Any())
            {
                return expense;
            }
            // create a new ExpandoObject & dynamically create the properties for this object
            ExpandoObject objectToReturn = new ExpandoObject();
            foreach (var field in lstOfFields)
            {
                var fieldValue = expense.GetType()
                    .GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                    .GetValue(expense, null);

                ((IDictionary<string, object>)objectToReturn).Add(field, fieldValue);
            }
            return objectToReturn;
        }
    }
}
