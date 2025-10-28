using Axion.API.Validation.Validators.Abstractions;

namespace Axion.API.Validation.Validators;

public class ValidatorFactory(IRequestFieldValidator requestFieldValidator)
{
    private readonly Dictionary<string, IFieldValidator> _validators = new(StringComparer.OrdinalIgnoreCase)
    {
        { "string", new StringFieldValidator() },
        { "decimal", new DecimalFieldValidator() },
        { "float", new DecimalFieldValidator() },
        { "double", new DecimalFieldValidator() },
        { "integer", new IntegerFieldValidator() },
        { "int", new IntegerFieldValidator() },
        { "boolean", new BooleanFieldValidator() },
        { "bool", new BooleanFieldValidator() },
        { "card_number", new CardNumberValidator() },
        { "card_expire_year", new CardExpireYearValidator() },
        { "card_expire_month", new CardExpireMonthValidator() },
        { "card_cvv", new CardCvvValidator() },
        { "number_amount", new NumberAmountValidator() },
        { "string_amount", new StringAmountValidator() },
        { "array", new ArrayFieldValidator(requestFieldValidator) }
    };

    public IFieldValidator? GetValidator(string fieldType)
    {
        return _validators.TryGetValue(fieldType, out var validator) ? validator : null;
    }
}
