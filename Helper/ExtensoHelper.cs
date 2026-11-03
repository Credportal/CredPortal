namespace Analise.Helper
{
    public static class ExtensoHelper
    {
        public static string ConverterParaExtenso(decimal valor)
        {
            long inteiro = (long)decimal.Truncate(valor);
            int centavos = (int)((valor - inteiro) * 100);

            if (inteiro == 0)
                return "zero meticais";

            string extenso = ConverterNumeroParaPalavras(inteiro);
            extenso += (inteiro == 1) ? " metical" : " meticais";

            if (centavos > 0)
            {
                extenso += " e " + ConverterNumeroParaPalavras(centavos) + " centavos";
            }

            return extenso;
        }

        private static string ConverterNumeroParaPalavras(long n)
        {
            if (n == 0) return "";

            if (n < 0) return "menos " + ConverterNumeroParaPalavras(-n);

            if (n <= 19)
            {
                string[] unidades = new[] {
                    "", "um", "dois", "três", "quatro", "cinco", "seis", "sete", "oito", "nove",
                    "dez", "onze", "doze", "treze", "catorze", "quinze", "dezasseis", "dezassete", "dezoito", "dezanove"
                };
                return unidades[n];
            }

            if (n <= 99)
            {
                string[] dezenas = new[] {
                    "", "", "vinte", "trinta", "quarenta", "cinquenta", "sessenta", "setenta", "oitenta", "noventa"
                };
                long dez = n / 10;
                long resto = n % 10;
                if (resto == 0) return dezenas[dez];
                return dezenas[dez] + " e " + ConverterNumeroParaPalavras(resto);
            }

            if (n <= 999)
            {
                long cent = n / 100;
                long resto = n % 100;
                string[] centenas = new[] {
                    "", "cento", "duzentos", "trezentos", "quatrocentos", "quinhentos", "seiscentos", "setecentos", "oitocentos", "novecentos"
                };
                if (n == 100) return "cem";
                if (resto == 0) return centenas[cent];
                return centenas[cent] + " e " + ConverterNumeroParaPalavras(resto);
            }

            if (n <= 999999)
            {
                long mil = n / 1000;
                long resto = n % 1000;
                string milStr = (mil == 1) ? "mil" : ConverterNumeroParaPalavras(mil) + " mil";
                if (resto == 0) return milStr;
                return milStr + (resto < 100 ? " e " : ", ") + ConverterNumeroParaPalavras(resto);
            }

            if (n <= 999999999)
            {
                long milhoes = n / 1000000;
                long resto = n % 1000000;
                string milhoesStr = (milhoes == 1) ? "um milhão" : ConverterNumeroParaPalavras(milhoes) + " milhões";
                if (resto == 0) return milhoesStr;
                return milhoesStr + (resto < 100 ? " e " : ", ") + ConverterNumeroParaPalavras(resto);
            }

            long milharMilhoes = n / 1000000000;
            long restoBi = n % 1000000000;
            string bilioesStr = (milharMilhoes == 1) ? "um bilião" : ConverterNumeroParaPalavras(milharMilhoes) + " biliões";
            if (restoBi == 0) return bilioesStr;
            return bilioesStr + (restoBi < 100 ? " e " : ", ") + ConverterNumeroParaPalavras(restoBi);
        }
    }
}