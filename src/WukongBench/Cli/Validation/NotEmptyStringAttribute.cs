using System.Text.RegularExpressions;

using Spectre.Console;
using Spectre.Console.Cli;

namespace WukongBench.Cli.Validation;

public sealed class NotEmptyStringAttribute : ParameterValidationAttribute
{
    public NotEmptyStringAttribute() : base(string.Empty)
    {
    }

    public override ValidationResult Validate(CommandParameterContext context)
    {
        if (context.Value is string text && string.IsNullOrWhiteSpace(text))
        {
            string optionName = ToOptionName(context.Parameter.PropertyName);

            return ValidationResult.Error($"{optionName} requires a non-empty value.");
        }

        return ValidationResult.Success();
    }

    private static string ToOptionName(string propertyName)
    {
        string kebabCase = Regex.Replace(propertyName, "(?<!^)([A-Z])", "-$1").ToLowerInvariant();

        return "--" + kebabCase;
    }
}
