using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using FirebaseAdmin.Auth;
using FirebaseAdmin;
using Google.Cloud.Firestore.V1;
using System.Text;
using System.Windows;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FireAdmin
{
    internal class FirestoreDatabaseWithAdminSDK
    {
        /// <summary>
        /// Firebase Admin SDK bypasses rules of Firestore Database.
        /// It is mainly suitable to use it at the server side.
        /// Never share "FirestoreServiceAccount.json" with client.
        /// </summary>
        private GoogleCredential _credential { get; }
        public static FirebaseApp _app;
        public static FirestoreDb _db;
        public FirebaseAuth _auth { get; private set; }
        public FirestoreDatabaseWithAdminSDK(FirebaseApp app = null)
        {
            _app = app;

            try
            {
                // Initialize _auth
                _auth = FirebaseAuth.GetAuth(_app);
                // Initialize database with _credential

                _credential = _app.Options.Credential;
                var projectId = ((ServiceAccountCredential)_credential.UnderlyingCredential).ProjectId;
                _db = FirestoreDb.Create(projectId.ToString(), new FirestoreClientBuilder
                {
                    Credential = _app.Options.Credential
                }.Build());
            }
            catch (Exception ex)
            {
                // Log the error or show a message box here if needed
                MessageBox.Show($"Unexpected error, Error: {ex}", "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                throw;
            }
        }

        #region Authentication
        public class FirebaseUser
        {
            public string DisplayName { get; set; }
            public string Email { get; set; }
            public string Uid { get; set; }
            public bool EmailVerified { get; set; } = false;
            public string PhoneNumber { get; set; }
            public bool Disabled { get; set; } = false;

            public FirebaseUser(string displayname, string email, string uid, bool emailVerified, string phoneNumber, bool disabled)
            {
                DisplayName = displayname ?? "not obtained";
                Email = email ?? "not obtained";
                Uid = uid ?? "not obtained";
                EmailVerified = emailVerified;
                PhoneNumber = phoneNumber ?? "not obtained";
                Disabled = disabled;
            }
        }
        public async Task<(bool Success, string ErrorMessage, List<FirebaseUser> Users)> GetUsersAsync()
        {
            //if (nullComponetExists)
            //{
            //    return (false, $"Error getting users, please check your service account.", null);
            //}
            try
            {
                var result = new List<FirebaseUser>();

                var exportedUserRecords = _auth.ListUsersAsync(new ListUsersOptions());

                var userRecords = await exportedUserRecords.ToListAsync();

                foreach (var userRecord in userRecords)
                {
                    result.Add(new FirebaseUser(userRecord.DisplayName, userRecord.Email, userRecord.Uid, userRecord.EmailVerified, userRecord.PhoneNumber, userRecord.Disabled));
                }

                return (true, null, result);
            }
            catch (FirebaseAdmin.Auth.FirebaseAuthException ex)
            {
                return (false, $"Error getting users: {ex.Message}", null);
            }
        }
        public async Task<(bool Success, string Result)> UpdateUserStatusAsync(string uid, bool enableUser)
        {
            try
            {
                var args = new UserRecordArgs()
                {
                    Uid = uid,
                    Disabled = !enableUser
                };

                await _auth.UpdateUserAsync(args);

                var action = enableUser ? "enabled" : "disabled";
                return (true, $"User has been {action} successfully.");
            }
            catch (FirebaseAdmin.Auth.FirebaseAuthException ex)
            {
                return (false, $"Error updating user status: {ex.Message}");
            }
        }
        public async Task<(bool Success, string Result)> DeleteUserAsync(string uid)
        {
            try
            {
                await _auth.DeleteUserAsync(uid);

                return (true, "User has been deleted successfully.");
            }
            catch (FirebaseAdmin.Auth.FirebaseAuthException ex)
            {
                return (false, $"Error deleting user: {ex.Message}");
            }
        }

        #endregion

        #region Firestore

        //Read-Write-Update-Delete
        public  async Task<(bool, string)> ReadField(string rootCollectionId, string DocumentID, string FieldID)
        {
            bool success = false;
            string result = "";

            try
            {
                // Create a document reference for the user
                DocumentReference userDocRef = _db.Collection(rootCollectionId).Document(DocumentID);

                // Read the user data from Firestore
                DocumentSnapshot snapshot = await userDocRef.GetSnapshotAsync();

                // Check if the document exists
                if (snapshot.Exists)
                {
                    // Get the value of the field
                    if (snapshot.TryGetValue(FieldID, out object value))
                    {
                        success = true;
                        result = $"Read operation completed successfully. Field {FieldID} value: {value.ToString()}";
                    }
                    else
                    {
                        result = $"Field {FieldID} not found in document {DocumentID} in collection {rootCollectionId}";
                    }
                }
                else
                {
                    result = $"Document {DocumentID} not found in collection {rootCollectionId}";
                }
            }
            catch (Exception ex)
            {
                // Handle the exception
                success = false;
                result = "Read operation not completed!: " + ex.Message;
            }

            return (success, result);
        }
        public  async Task<(bool, string)> WriteField(string rootCollectionId, string DocumentID, string FieldID, string FieldValue)
        {
            bool success = false;
            string result = "";

            // Create Dictionary
            Dictionary<string, object> userData = new Dictionary<string, object>
                {
                  { FieldID, FieldValue },
                };

            try
            {
                // Create a batched write operation
                WriteBatch batch = _db.StartBatch();

                // Create a document reference for the user
                DocumentReference userDocRef = _db.Collection(rootCollectionId).Document(DocumentID);

                // Write the user data to Firestore
                batch.Set(userDocRef, userData);

                // Commit the batched write operation
                await batch.CommitAsync();

                success = true;
                result = "Write operation completed successfully" ;
            }
            catch (Exception ex)
            {
                // Handle the exception
                success = false;
                result = "Write operation not completed!: " + ex.Message;
            }

            return (success, result);
        }
        public async Task<(bool, string)> UpdateField(string rootCollectionId, string DocumentID, string FieldID, string FieldValue)
        {
            try
            {
                // Create a document reference for the user
                DocumentReference userDocRef = _db.Collection(rootCollectionId).Document(DocumentID);

                // Create a dictionary with the field to update
                Dictionary<string, object> updateData = new Dictionary<string, object>
                {
                    { FieldID, FieldValue }
                };

                // Update the field in Firestore
                await userDocRef.UpdateAsync(updateData);

                return (true, "Field has been updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating field: {ex.Message}");
            }
        }
        public async Task<(bool, string)> DeleteField(string rootCollectionId, string DocumentID, string FieldID)
        {
            bool success = false;
            string result = "";

            try
            {
                // Create a batched write operation
                WriteBatch batch = _db.StartBatch();

                // Create a document reference for the user
                DocumentReference userDocRef = _db.Collection(rootCollectionId).Document(DocumentID);

                // Remove the field from the document
                batch.Update(userDocRef, new Dictionary<string, object> { { FieldID, FieldValue.Delete } });

                // Commit the batched write operation
                await batch.CommitAsync();

                success = true;
                result = "Delete operation completed successfully";
            }
            catch (Exception ex)
            {
                // Handle the exception
                success = false;
                result = "Delete operation not completed!: " + ex.Message;
            }

            return (success, result);
        }
        public async Task<(bool, string)> DeleteDocument(string rootCollectionId, string documentId)
        {
            try
            {
                // Get a reference to the document
                var documentRef = _db.Collection(rootCollectionId).Document(documentId);

                // Delete the document
                await documentRef.DeleteAsync();

                return (true, $"Document '{documentId}' deleted successfully from collection '{rootCollectionId}'.");
            }
            catch (Exception ex)
            {
                // Handle the exception and return failure
                Console.WriteLine($"Failed to delete document: {ex.Message}");
                return (false, $"Failed to delete document '{documentId}' from collection '{rootCollectionId}': {ex.Message}");
            }
        }

        //Snapshots
        public async Task<(bool success, string result, List<string> collectionIds)> ListOfRootCollectionIDs()
        {
            List<string> collectionIds = new List<string>();
            bool success = false;
            string result = "";

            try
            {
                // Get a snapshot of all the collections in the database
                var collectionsSnapshot = await _db.ListRootCollectionsAsync().ToListAsync();

                // Extract the collection IDs from the snapshot and add them to the list
                foreach (var collectionRef in collectionsSnapshot)
                {
                    collectionIds.Add(collectionRef.Id);
                }

                success = true;
                result = "Collections retrieved successfully.";
            }
            catch (Exception ex)
            {
                // Handle the exception
                success = false;
                result = $"Failed to get collections: {ex.Message}";
            }

            return (success, result, collectionIds);
        }
        public async Task<(bool Success, List<string> DocumentIds, string Result)> ListOfDocumentIDsUnderRootCollection(string rootCollectionId)
        {
            List<string> documentIds = new List<string>();
            try
            {
                var rootCollectionRef = _db.Collection(rootCollectionId);
                var documentsSnapshot = await rootCollectionRef.ListDocumentsAsync().ToListAsync();
                foreach (var documentRef in documentsSnapshot)
                {
                    documentIds.Add(documentRef.Id);
                }
                return (true, documentIds, "Success");
            }
            catch (Exception ex)
            {
                // Handle the exception
                Console.WriteLine($"Failed to get subcollections: {ex.Message}");
                return (false, null, $"Error retrieving document IDs under root collection {rootCollectionId}: {ex.Message}");
            }
        }
        public async Task<(bool Success, string Result)> GetRootCollectionSnapshotTree(string rootCollectionId, bool isFullDatabaseSnaphot)
        {
            StringBuilder treeBuilder = new StringBuilder();
            QuerySnapshot snapshot = await _db.CollectionGroup(rootCollectionId).GetSnapshotAsync();
            try
            {
                int i = 1;
                int count = snapshot.Documents.Count;

                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    string linePrefix = i == count ? "├── " : "├── ";
                    treeBuilder.AppendLine($"{linePrefix}{document.Reference.Path}");
                    Dictionary<string, object> data = document.ToDictionary();
                    foreach (KeyValuePair<string, object> field in data)
                    {
                        // Check if this is the last field
                        bool isLastField = data.Last().Key == field.Key;

                        // Draw the appropriate line
                        treeBuilder.AppendLine($"│   {(isLastField ? "└──" : "├──")} {field.Key}: {field.Value}");
                    }

                    i++;
                }
                if (isFullDatabaseSnaphot)
                {
                    Logs.SaveRootCollectionSnapshot(treeBuilder.ToString(), rootCollectionId);
                }

                return (true, treeBuilder.ToString());
            }
            catch (Exception ex)
            {
                return (false, $"Error retrieving database snapshot: {ex.Message}");
            }
        }
        public async Task<(bool, string)> GetDocumentSnapshotTree(string rootCollectionId, string documentId)
        {
            try
            {
                StringBuilder treeBuilder = new StringBuilder();
                DocumentSnapshot snapshot = await _db.Collection(rootCollectionId).Document(documentId).GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    treeBuilder.AppendLine($"├── {documentId}");
                    Dictionary<string, object> data = snapshot.ToDictionary();
                    foreach (KeyValuePair<string, object> field in data)
                    {
                        // Check if this is the last field
                        bool isLastField = data.Last().Key == field.Key;

                        // Draw the appropriate line
                        treeBuilder.AppendLine($"│   {(isLastField ? "└──" : "├──")} {field.Key}: {field.Value}");
                    }

                    return (true, treeBuilder.ToString());
                }
                else
                {
                    return (false, $"Document with ID {documentId} does not exist in collection {rootCollectionId}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error retrieving snapshot tree: {ex.Message}");
            }
        }
        public async Task<(bool Success, string Result)> GetDatabaseSnapshotTree()
        {
            Logs.DeleteLogFiles(); //clear existing root collection logs
            StringBuilder treeBuilder = new StringBuilder();
            var (success, result, collectionIds) = await ListOfRootCollectionIDs();//read count +

            if (!success)
            {
                return (false, result);
            }

            foreach (string collectionId in collectionIds)
            {
                var (snapshotSuccess, snapshotResult) = await GetRootCollectionSnapshotTree(collectionId, true);
                if (snapshotSuccess)
                {
                    treeBuilder.AppendLine(collectionId);
                    treeBuilder.Append(snapshotResult);
                    treeBuilder.AppendLine();
                }
                else
                {
                    return (false, $"Error retrieving snapshot for collection '{collectionId}': {snapshotResult}");
                }
            }

            return (true, treeBuilder.ToString());
        }

        #endregion

    }

    internal static class Logs
    {
        public static string GetAllLogs()
        {
            string folderPath = AppDomain.CurrentDomain.BaseDirectory;
            string[] files = Directory.GetFiles(folderPath, "Logs.txt", SearchOption.TopDirectoryOnly);

            StringBuilder logsBuilder = new StringBuilder();
            foreach (string file in files)
            {
                string logs = File.ReadAllText(file);
                logsBuilder.AppendLine(logs);
            }

            return logsBuilder.ToString();
        }


        public static void SaveToLogFile(string text)
        {
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);

            string filePath = Path.Combine(folderPath, "Logs.txt");

            // read existing content of file
            string existingContent = "";
            if (File.Exists(filePath))
            {
                existingContent = File.ReadAllText(filePath);
            }

            // append new text to the top of existing content
            string newContent = $"{text}\n{existingContent}";

            // Uncomment below for limit log size
            //string[] lines = newContent.Split('\n');
            //if (lines.Length > 50)
            //{
            //    newContent = string.Join("\n", lines.Take(50));
            //}

            // write updated content to file
            File.WriteAllText(filePath, newContent);
        }

        public static void DeleteLogFiles()
        {
            string logsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            // Check if the Logs folder exists
            if (!Directory.Exists(logsFolder))
            {
                // If the Logs folder does not exist, return without doing anything
                return;
            }

            // Get all the text files in the Logs folder
            string[] logFiles = Directory.GetFiles(logsFolder, "*.txt");

            // Delete all the text files
            foreach (string logFile in logFiles)
            {
                File.Delete(logFile);
            }
        }
        public static void SaveRootCollectionSnapshot(string snapshot, string rootCollectionId)
        {
            //add rootCollectionId at top of snapshot string
            snapshot = $"{rootCollectionId}\n{snapshot}";

            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, $"{rootCollectionId}_{DateTime.Now:yyyyMMddHHmmss}.txt");
            using (StreamWriter streamWriter = File.CreateText(filePath))
            {
                streamWriter.Write(snapshot);
            }
        }
        public static string CombineLogFiles()
        {
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            string[] filePaths = Directory.GetFiles(folderPath, "*.txt");

            StringBuilder combinedLogs = new StringBuilder();

            foreach (string filePath in filePaths)
            {
                string fileContent = File.ReadAllText(filePath);
                combinedLogs.Append(fileContent);
                combinedLogs.Append(Environment.NewLine);
            }

            return combinedLogs.ToString();
        }

    }






}









