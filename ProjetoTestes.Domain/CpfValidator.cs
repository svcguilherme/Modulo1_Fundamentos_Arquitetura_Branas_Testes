namespace ProjetoTestes.Domain;

public static class CpfValidator
{
    public static bool ValidateCpf(string? str)
    {
        if (str != null)
        {
            if (str.Length >= 11 && str.Length <= 14)
            {
                // cleaning cpf
                str = str
                    .Replace(".", "")
                    .Replace("-", "")
                    .Replace(" ", "");
                // tudo igual
                if (!str.All(c => c == str[0]))
                {
                    try
                    {
                        int d1, d2;
                        int dg1, dg2, rest;
                        int digito;
                        string nDigResult;
                        d1 = d2 = 0;
                        dg1 = dg2 = rest = 0;

                        for (var nCount = 1; nCount < str.Length - 1; nCount++)
                        {
                            digito = int.Parse(str.Substring(nCount - 1, 1));
                            d1 = d1 + (11 - nCount) * digito;

                            d2 = d2 + (12 - nCount) * digito;
                        }

                        rest = d1 % 11;

                        // se for menor que 2 é 0, senão é 11 menos o resto
                        dg1 = (rest < 2) ? 0 : 11 - rest;
                        d2 += 2 * dg1;
                        rest = d2 % 11;
                        if (rest < 2)
                            dg2 = 0;
                        else
                            dg2 = 11 - rest;

                        var nDigVerific = str.Substring(str.Length - 2, 2);
                        nDigResult = "" + dg1 + "" + dg2;

                        return nDigVerific == nDigResult;
                    }
                    // se der problema...
                    catch (Exception e)
                    {
                        Console.Error.WriteLine("Erro !" + e.Message);

                        return false;
                    }
                }
                else return false;
            }
            else return false;
        }
        else return false;
    }
}
