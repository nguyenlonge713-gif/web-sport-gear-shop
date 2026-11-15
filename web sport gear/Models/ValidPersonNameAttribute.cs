
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace web_sport_gear.Models
{
    public class ValidPersonNameAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(
            object value,
            ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            string name = value.ToString()
                .Trim()
                .Normalize(NormalizationForm.FormC);

            string pattern =
                @"^[\p{L}\p{M}]+(?:[ '-][\p{L}\p{M}]+)*$";

            if (!Regex.IsMatch(name, pattern))
            {
                return new ValidationResult(
                    ErrorMessage ??
                    "Họ và tên không hợp lệ."
                );
            }

            return ValidationResult.Success;
        }
    }
}
