using System.IO;
using System.Reflection;

namespace FireAdmin
{
    public class FirebaseServiceIDs
    {
        #region Firestore Database Admin SDK

        public static string resourcePath = "FireAdmin.Firebase.FirestoreServiceAccount.json";
        public static string GetJsonFromEmbededFile()
        {
            string json;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }
            }
            return json;
        }
        #endregion
    }
}
