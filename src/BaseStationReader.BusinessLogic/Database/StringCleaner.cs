using System.Globalization;

namespace BaseStationReader.BusinessLogic.Database
{
    public static class StringCleaner
    {
        private readonly static TextInfo _textInfo = new CultureInfo("en-GB", false).TextInfo;

        /// <summary>
        /// Return a cleaned-up version of an IATA code
        /// </summary>
        /// <param name="iata"></param>
        /// <returns></returns>
        public static string CleanIATA(string iata)
            => Clean(iata).ToUpper();

        /// <summary>
        /// Return a cleaned-up version of an ICAO code
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public static string CleanICAO(string icao)
            => Clean(icao).ToUpper();

        /// <summary>
        /// Ensure a string is converted to a consistent case for storage in the database and
        /// subsequent searching
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string CleanName(string s)
        {
            var clean = s;

            // Check the string isn't null or empty
            if (!string.IsNullOrEmpty(s))
            {
                // Remove invalid characters, that can cause an exception in the title case conversion, and
                // convert to lowercase to ensure that the result truly is title case. Otherwise, strings
                // such as "The BEATLES" would remain unchanged, where we really want "The Beatles".
                clean = _textInfo.ToTitleCase(Clean(s)!.ToLower());
            }

            return clean;
        }

        /// <summary>
        /// Remove invalid characters from a string and trim it
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static string Clean(string s)
        {
            var clean = s;

            // Check the string isn't null or empty
            if (!string.IsNullOrEmpty(s))
            {
                // Remove commas that are not permitted and CR/LF then trim the string
                clean = s.Replace(",", "").Replace("\r", "").Replace("\n", "").Trim();
            }

            return clean;
        }
    }
}