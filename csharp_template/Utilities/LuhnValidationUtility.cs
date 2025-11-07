namespace csharp_template.Utilities;

public static class LuhnValidationUtility
{
    public static bool Validate(string cardNumber)
    {
        var sum = 0;
        var alternate = false;
        
        for (var i = cardNumber.Length - 1; i >= 0; i--)
        {
            var digit = cardNumber[i] - '0';
            
            if (alternate)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }
            
            sum += digit;
            alternate = !alternate;
        }
        
        return sum % 10 == 0;
    }
}