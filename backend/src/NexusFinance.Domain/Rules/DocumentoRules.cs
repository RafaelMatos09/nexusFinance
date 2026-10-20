namespace NexusFinance.Domain.Rules;

public static class DocumentoRules
{
    public static string OnlyDigits(string? value) =>
        new(value?.Where(char.IsDigit).ToArray() ?? []);

    public static bool IsValid(string? value)
    {
        var digits = OnlyDigits(value);
        return digits.Length switch
        {
            11 => IsCpf(digits),
            14 => IsCnpj(digits),
            _ => false
        };
    }

    private static bool IsCpf(string digits)
    {
        if (digits.Distinct().Count() == 1)
        {
            return false;
        }

        var numbers = digits.Select(character => character - '0').ToArray();
        var first = CheckDigit(numbers, 9, 10);
        var second = CheckDigit(numbers, 10, 11);
        return numbers[9] == first && numbers[10] == second;
    }

    private static bool IsCnpj(string digits)
    {
        if (digits.Distinct().Count() == 1)
        {
            return false;
        }

        var numbers = digits.Select(character => character - '0').ToArray();
        int[] firstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] secondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        return numbers[12] == WeightedDigit(numbers, firstWeights)
            && numbers[13] == WeightedDigit(numbers, secondWeights);
    }

    private static int CheckDigit(int[] numbers, int length, int weightStart)
    {
        var sum = 0;
        for (var index = 0; index < length; index++)
        {
            sum += numbers[index] * (weightStart - index);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static int WeightedDigit(int[] numbers, int[] weights)
    {
        var sum = 0;
        for (var index = 0; index < weights.Length; index++)
        {
            sum += numbers[index] * weights[index];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
