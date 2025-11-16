
public class SecretService
{
    private Dictionary<string, string> SecretDictionary = new Dictionary<string, string>();

    public SecretService(string[] files)
    {
        // Read the files (/run/secrets/{})
        foreach (string fileName in files)
        {
            try
            {
                FileStream stream = File.OpenRead(Path.Combine("/run/secrets/", fileName));
                StreamReader reader = new StreamReader(stream);

                string value = reader.ReadToEnd();

                SecretDictionary.Add(fileName, value);
            }
            catch (Exception) { }
        }
    }

    public string? this[string s]
    {
        get
        {
            string output;
            bool success = SecretDictionary.TryGetValue(s, out output);

            if (success)
                return output;
            else
                return null;
        }
    }
}