using System.Linq;
using System.Text.RegularExpressions;

namespace APIseverino.Helpers
{
    public static class CpfHelper
    {
        public static bool IsValidCpf(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return false;

            // Remove caracteres não numéricos
            cpf = Regex.Replace(cpf, @"[^\d]", "");

            // CPF deve ter 11 dígitos
            if (cpf.Length != 11)
                return false;

            // Verifica CPFs com todos os dígitos iguais (considerados inválidos)
            if (new string(cpf[0], 11) == cpf)
                return false;

            // Calcula o primeiro dígito verificador
            int sum = 0;
            for (int i = 0; i < 9; i++)
                sum += int.Parse(cpf[i].ToString()) * (10 - i);
            int remainder = sum % 11;
            int digit1 = remainder < 2 ? 0 : 11 - remainder;

            // Verifica o primeiro dígito
            if (int.Parse(cpf[9].ToString()) != digit1)
                return false;

            // Calcula o segundo dígito verificador
            sum = 0;
            for (int i = 0; i < 10; i++)
                sum += int.Parse(cpf[i].ToString()) * (11 - i);
            remainder = sum % 11;
            int digit2 = remainder < 2 ? 0 : 11 - remainder;

            // Verifica o segundo dígito
            if (int.Parse(cpf[10].ToString()) != digit2)
                return false;

            return true;
        }
    }
}