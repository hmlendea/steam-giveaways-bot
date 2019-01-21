using System.Collections.Generic;
using System.Linq;

namespace SteamGiveawaysBot.Entities
{
    public sealed class Country
    {
        static Dictionary<string, Country> countries = new Dictionary<string, Country>
        {
            { Andorra.Code, Andorra },
            { Angola.Code, Angola },
            { Argentina.Code, Argentina },
            { Australia.Code, Australia },
            { Austria.Code, Austria },
            { Belarus.Code, Belarus },
            { Belgium.Code, Belgium },
            { Bolivia.Code, Bolivia },
            { Brazil.Code, Brazil },
            { Canada.Code, Canada },
            { CapeVerde.Code, CapeVerde },
            { Chile.Code, Chile },
            { CostaRica.Code, CostaRica },
            { Colombia.Code, Colombia },
            { Cuba.Code, Cuba },
            { Cyprus.Code, Cyprus },
            { DominicanRepublic.Code, DominicanRepublic },
            { Ecuador.Code, Ecuador },
            { ElSalvador.Code, ElSalvador },
            { EquatorialGuinea.Code, EquatorialGuinea },
            { France.Code, France },
            { Germany.Code, Germany },
            { Greece.Code, Greece },
            { GuineaBissau.Code, GuineaBissau },
            { Italy.Code, Italy },
            { Liechtenstein.Code, Liechtenstein },
            { Luxembourg.Code, Luxembourg },
            { Mexico.Code, Mexico },
            { Moldova.Code, Moldova },
            { Monaco.Code, Monaco },
            { Mozambique.Code, Mozambique },
            { Netherlands.Code, Netherlands },
            { NewZealand.Code, NewZealand },
            { Paraguay.Code, Paraguay },
            { Peru.Code, Peru },
            { Portugal.Code, Portugal },
            { Romania.Code, Romania },
            { Russia.Code, Russia },
            { SanMarino.Code, SanMarino },
            { SaoTomeAndPrincipe.Code, SaoTomeAndPrincipe },
            { Spain.Code, Spain },
            { Surniame.Code, Surniame },
            { Switzerland.Code, Switzerland },
            { Ukraine.Code, Ukraine },
            { UnitedKingdom.Code, UnitedKingdom },
            { UnitedStates.Code, UnitedStates },
            { Uruguay.Code, Uruguay }
        };

        public string Name { get; }

        public string Code { get; }

        public int SgCode { get; }

        public Language Language { get; }

        Country(string name, string code, int sgCode, Language language)
        {
            Name = name;
            Code = code;
            SgCode = sgCode;
            Language = language;
        }

        public static Country Andorra => new Country(PutSpacesInName(nameof(Andorra)), "AD", 5, Language.Spanish);
        public static Country Angola => new Country(PutSpacesInName(nameof(Angola)), "AO", 6, Language.Portuguese);
        public static Country Argentina => new Country(PutSpacesInName(nameof(Argentina)), "AR", 10, Language.Spanish);
        public static Country Australia => new Country(PutSpacesInName(nameof(Australia)), "AU", 13, Language.English);
        public static Country Austria => new Country(PutSpacesInName(nameof(Austria)), "AT", 14, Language.German);
        public static Country Belarus => new Country(PutSpacesInName(nameof(Belarus)), "BY", 20, Language.Russian);
        public static Country Belgium => new Country(PutSpacesInName(nameof(Belgium)), "BE", 21, Language.French);
        public static Country Bolivia => new Country(PutSpacesInName(nameof(Bolivia)), "BO", 26, Language.Spanish);
        public static Country Brazil => new Country(PutSpacesInName(nameof(Brazil)), "BR", 30, Language.Portuguese);
        public static Country Canada => new Country(PutSpacesInName(nameof(Canada)), "CA", 40, Language.French);
        public static Country CapeVerde => new Country(PutSpacesInName(nameof(CapeVerde)), "CV", 42, Language.Portuguese);
        public static Country Chile => new Country(PutSpacesInName(nameof(Chile)), "CL", 46, Language.Spanish);
        public static Country Colombia => new Country(PutSpacesInName(nameof(Colombia)), "CO", 55, Language.Spanish);
        public static Country CostaRica => new Country(PutSpacesInName(nameof(CostaRica)), "CR", 55, Language.Spanish);
        public static Country Cuba => new Country(PutSpacesInName(nameof(Cuba)), "CU", 57, Language.Spanish);
        public static Country Cyprus => new Country(PutSpacesInName(nameof(Cyprus)), "CY", 58, Language.Greek);
        public static Country DominicanRepublic => new Country(PutSpacesInName(nameof(DominicanRepublic)), "DO", 64, Language.Spanish);
        public static Country Ecuador => new Country(PutSpacesInName(nameof(Ecuador)), "EC", 67, Language.Spanish);
        public static Country ElSalvador => new Country(PutSpacesInName(nameof(ElSalvador)), "SV", 69, Language.Spanish);
        public static Country EquatorialGuinea => new Country(PutSpacesInName(nameof(EquatorialGuinea)), "GQ", 70, Language.Spanish);
        public static Country France => new Country(PutSpacesInName(nameof(France)), "FR", 78, Language.French);
        public static Country Germany => new Country(PutSpacesInName(nameof(Germany)), "DE", 86, Language.German);
        public static Country Greece => new Country(PutSpacesInName(nameof(Greece)), "GR", 89, Language.Greek);
        public static Country GuineaBissau => new Country(PutSpacesInName(nameof(GuineaBissau)), "GW", 97, Language.Portuguese);
        public static Country Italy => new Country(PutSpacesInName(nameof(Italy)), "IT", 112, Language.Italian);
        public static Country Liechtenstein => new Country(PutSpacesInName(nameof(Liechtenstein)), "LI", 129, Language.German);
        public static Country Luxembourg => new Country(PutSpacesInName(nameof(Luxembourg)), "LU", 131, Language.French);
        public static Country Mexico => new Country(PutSpacesInName(nameof(Mexico)), "MX", 146, Language.Spanish);
        public static Country Moldova => new Country(PutSpacesInName(nameof(Moldova)), "MD", 149, Language.Romanian);
        public static Country Monaco => new Country(PutSpacesInName(nameof(Monaco)), "MC", 150, Language.French);
        public static Country Mozambique => new Country(PutSpacesInName(nameof(Mozambique)), "MZ", 155, Language.Portuguese);
        public static Country Netherlands => new Country(PutSpacesInName(nameof(Netherlands)), "NL", 160, Language.Dutch);
        public static Country NewZealand => new Country(PutSpacesInName(nameof(NewZealand)), "NZ", 164, Language.English);
        public static Country Paraguay => new Country(PutSpacesInName(nameof(Paraguay)), "PY", 182, Language.Spanish);
        public static Country Peru => new Country(PutSpacesInName(nameof(Peru)), "PE", 184, Language.Spanish);
        public static Country Portugal => new Country(PutSpacesInName(nameof(Portugal)), "PT", 188, Language.Portuguese);
        public static Country Romania => new Country(PutSpacesInName(nameof(Romania)), "RO", 191, Language.Romanian);
        public static Country Russia => new Country(PutSpacesInName(nameof(Russia)), "RU", 192, Language.Russian);
        public static Country SanMarino => new Country(PutSpacesInName(nameof(SanMarino)), "SM", 199, Language.Italian);
        public static Country SaoTomeAndPrincipe => new Country(PutSpacesInName(nameof(SaoTomeAndPrincipe)), "ST", 227, Language.Portuguese);
        public static Country Spain => new Country(PutSpacesInName(nameof(Spain)), "ES", 218, Language.Spanish);
        public static Country Surniame => new Country(PutSpacesInName(nameof(Surniame)), "SR", 221, Language.Dutch);
        public static Country Switzerland => new Country(PutSpacesInName(nameof(Switzerland)), "CH", 225, Language.German);
        public static Country Ukraine => new Country(PutSpacesInName(nameof(Ukraine)), "UA", 246, Language.Russian);
        public static Country UnitedKingdom => new Country(PutSpacesInName(nameof(UnitedKingdom)), "GB", 249, Language.English);
        public static Country UnitedStates => new Country(PutSpacesInName(nameof(UnitedStates)), "US", 250, Language.English);
        public static Country Uruguay => new Country(PutSpacesInName(nameof(Uruguay)), "UY", 252, Language.Spanish);

        public static IEnumerable<Country> Values => countries.Values;

        public static Country FromSteamCode(string code)
            => countries.Values.FirstOrDefault(x => x.Code == code);

        public static Country FromSteamGiftsCode(int code)
            => countries.Values.FirstOrDefault(x => x.SgCode == code);

        public override string ToString() => Name;

        static string PutSpacesInName(string name)
        {
            string newName = name.Substring(0, 1);

            for (int i = 1; i < name.Length; i++)
            {
                if (name[i] >= 'A' && name[i] <= 'Z')
                {
                    newName += $" {name[i]}";
                }
                else
                {
                    newName += name[i];
                }
            }

            return newName;
        }
    }
}