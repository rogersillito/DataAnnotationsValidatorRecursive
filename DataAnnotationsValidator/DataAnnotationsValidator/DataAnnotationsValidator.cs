using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace DataAnnotationsValidator
{
	public class DataAnnotationsValidator : IDataAnnotationsValidator
	{
		public bool TryValidateObject(object obj, ICollection<ValidationResult> results, IDictionary<object, object> validationContextItems = null)
		{
			return Validator.TryValidateObject(obj, new ValidationContext(obj, null, validationContextItems), results, true);
		}

		public bool TryValidateObjectRecursive<T>(T obj, List<ValidationResult> results, IDictionary<object, object> validationContextItems = null)
		{
			bool result = TryValidateObject(obj, results, validationContextItems);

            var properties = obj.GetType().GetProperties().Where(prop => prop.CanRead 
                && !prop.GetCustomAttributes(typeof(SkipRecursiveValidation), false).Any() 
                && prop.GetIndexParameters().Length == 0).ToList();

			foreach (var property in properties)
			{
				if (property.PropertyType == typeof(string) || property.PropertyType.IsValueType) continue;

				var value = obj.GetPropertyValue(property.Name);

				if (value == null) continue;

				var asEnumerable = value as IEnumerable;
				if (asEnumerable != null)
				{
				    result = TryValidateEnumerableObjectRecursive<T>(asEnumerable, results, validationContextItems, result, property.Name);
				}
				else
				{
					var nestedResults = new List<ValidationResult>();
					if (!TryValidateObjectRecursive(value, nestedResults, validationContextItems))
					{
						result = false;
						foreach (var validationResult in nestedResults)
						{
							PropertyInfo property1 = property;
							results.Add(new ValidationResult(validationResult.ErrorMessage, validationResult.MemberNames.Select(x => property1.Name + '.' + x)));
						}
					};
				}
			}

			var objAsEnumerable = obj as IEnumerable;
			if (objAsEnumerable != null)
			{
			    result = TryValidateEnumerableObjectRecursive<T>(objAsEnumerable, results, validationContextItems, result, "{root}");
			}

			return result;
		}

		private bool TryValidateEnumerableObjectRecursive<T>(IEnumerable enumerableObject, List<ValidationResult> results,
				IDictionary<object, object> validationContextItems, bool isValid, string parentPropertyName)
		{
			foreach (var enumObj in enumerableObject)
			{
				var nestedResults = new List<ValidationResult>();
				if (!TryValidateObjectRecursive<object>(enumObj, nestedResults, validationContextItems))
				{
					isValid = false;
					foreach (var validationResult in nestedResults)
					{
						results.Add(new ValidationResult(validationResult.ErrorMessage,
								validationResult.MemberNames.Select(x => parentPropertyName + '.' + x)));
					}
				}
			}
			return isValid;
		}
	}
}
