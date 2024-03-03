using Newtonsoft.Json.Linq;


namespace SFTPReaderQueue.Helper
{
    public class ODataHelper
    {

        public static string ExtractValueObject(string jsonString)
        {
            // parse the JSON string into a JObject
            JObject jsonObject = JObject.Parse(jsonString);

            // get the value property as a JToken
            JToken valueToken = jsonObject["value"];

            // convert the JToken to a string
            string valueString = valueToken.ToString();

            return valueString;
        }

    }
}
