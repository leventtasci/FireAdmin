using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using static FireAdmin.FirestoreDatabaseWithAdminSDK;


namespace FireAdmin.ViewModels
{
    class FireAdminMainViewModel : INotifyPropertyChanged
    {
        public FireAdminMainViewModel()
        {
            Is_Buttons_Enabled = true;
            ListOfOperations = new List<string>
            {
                "Select a misson to operate",
                "Firebase Authentication: Get Authentication Table",//.....................1 OK
                "Firebase Authentication: Disable User",//.................................2 OK
                "Firebase Authentication: Enable User",//..................................3 OK
                "Firebase Authentication: Delete User",//..................................4 OK
                "Firestore Database: Read Field",//........................................5 OK
                "Firestore Database: Write Field",//.......................................6 OK
                "Firestore Database: Update Field",//......................................7 OK
                "Firestore Database: Delete Field",//......................................8 OK
                "Firestore Database: Get Root Collection IDs",//...........................9 OK
                "Firestore Database: Get Document IDs Under Root Collection",//...........10 OK
                "Firestore Database: Delete Document under Root Collection",//............11 OK
                "Firestore Database: Get Document Snapshot Tree",//.......................12 OK
                "Firestore Database: Get Root Collection Snapshot Tree",//................13 OK
                "Firestore Database: Get Database Snapshot Tree",//.......................14 OK
                "Log: Get Last Database Snapshot Tree"//..................................15 OK
            };
            Height_Pop_Database = 300;
            GridHeight_DocumentID = 45;
            GridHeight_FieldID = 45;
            GridHeight_FieldValue = 45;

            ConsoleContentAlignment = TextAlignment.Center;
            _sentences = new List<string> { "<Hello>",
                                            "You'll see operation results here.",
                                            "First, choose your Service Account json file from \"Import Service Account Button\"",
                                            "You can embed your service account in this software too.",
                                            "Never share your service account with client.",
                                            "Select a mission from the list and press the Perform Operation button.",
                                            "Read, write and delete operations may incur additional costs, so use this software at your own risk.",
                                            " "};
            StartTypingEffect();
        }

        private FirestoreDatabaseWithAdminSDK _firestoreDB;

        private FirebaseApp _app;

        private CancellationTokenSource _cts;

        private string ServiceAccountJsonFilePath;

        #region Binding

        //Firebase Authentication:

        private List<string> _listOfUserUIDs;
        public List<string> ListOfUserUIDs
        {
            get
            {
                return _listOfUserUIDs;
            }
            set
            {
                _listOfUserUIDs = value;
                OnPropertyChanged(nameof(ListOfUserUIDs));
            }
        }

        private string _selectedUserID;
        public string SelectedUserID
        {
            get
            {
                return _selectedUserID;
            }
            set
            {
                _selectedUserID = value;
                OnPropertyChanged(nameof(SelectedUserID));
            }
        }

        //Firestore
        //Root Collections
        private List<string> _listOfRootCollectionNames;
        public List<string> ListOfRootCollectionNames
        {
            get
            {
                return _listOfRootCollectionNames;
            }
            set
            {
                _listOfRootCollectionNames = value;
                OnPropertyChanged(nameof(ListOfRootCollectionNames));
            }
        }
        //Document Names Under a Collection
        private List<string> _listOfDocumentsInRootCollection;
        public List<string> ListOfDocumentsInRootCollection
        {
            get
            {
                return _listOfDocumentsInRootCollection;
            }
            set
            {
                _listOfDocumentsInRootCollection = value;
                OnPropertyChanged(nameof(ListOfDocumentsInRootCollection));
            }
        }
        //Console string binded to console
        private string _consoleString;
        public string ConsoleString
        {
            get
            {
                return _consoleString;
            }
            set
            {
                _consoleString = value;
                OnPropertyChanged(nameof(ConsoleString));
            }
        }
        //SelectedRootCollection
        private string _selectedRootCollectionID;
        public string SelectedRootCollectionID
        {
            get
            {
                return _selectedRootCollectionID;
            }
            set
            {
                _selectedRootCollectionID = value;
                OnPropertyChanged(nameof(SelectedRootCollectionID));
            }
        }
        //Selected Documnet
        private string _selectedDocumentID;
        public string SelectedDocumentID
        {
            get
            {
                return _selectedDocumentID;
            }
            set
            {
                _selectedDocumentID = value;
                OnPropertyChanged(nameof(SelectedDocumentID));
            }
        }
        //Field ID
        private string _selectedFieldID;
        public string SelectedFieldID
        {
            get
            {
                return _selectedFieldID;
            }
            set
            {
                _selectedFieldID = value;
                OnPropertyChanged(nameof(SelectedFieldID));
            }
        }
        //Field Value
        private string _selectedFieldValue;
        public string SelectedFieldValue
        {
            get
            {
                return _selectedFieldValue;
            }
            set
            {
                _selectedFieldValue = value;
                OnPropertyChanged(nameof(SelectedFieldValue));
            }
        }

        //Operations
        private List<string> _listOfOperations;
        public List<string> ListOfOperations
        {
            get
            {
                return _listOfOperations;
            }
            set
            {
                _listOfOperations = value;
                OnPropertyChanged(nameof(ListOfOperations));
            }
        }

        private int _selectedOperationIndex;
        public int SelectedOperationIndex
        {
            get { return _selectedOperationIndex; }
            set
            {
                if (_selectedOperationIndex != value)
                {
                    _selectedOperationIndex = value;
                    OnPropertyChanged(nameof(SelectedOperationIndex));
                }
            }
        }

        //Logs
        private string _logsFromTextFile;
        public string LogsFromTextFile
        {
            get
            {
                return _logsFromTextFile;
            }
            set
            {
                _logsFromTextFile = value;
                OnPropertyChanged(nameof(LogsFromTextFile));
            }
        }

        #endregion

        #region Check Service Account Validity

        //Check before start
        public bool isImportedServiceAccountValid()
        {
            //check json is valid
            try
            {
                GoogleCredential _credential = _app.Options.Credential;
                var projectId = ((ServiceAccountCredential)_credential.UnderlyingCredential).ProjectId;
                FirestoreDb checkdb = FirestoreDb.Create(projectId, new FirestoreClientBuilder
                {
                    Credential = _app.Options.Credential
                }.Build());

                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool isEmbededServiceAccountValid()
        {
            //check embeded json is valid
            try
            {
                GoogleCredential _credential = GoogleCredential.FromJson(FirebaseServiceIDs.GetJsonFromEmbededFile());
                var projectId = ((ServiceAccountCredential)_credential.UnderlyingCredential).ProjectId;
                FirestoreDb checkdb = FirestoreDb.Create(projectId, new FirestoreClientBuilder
                {
                    Credential = _credential
                }.Build());
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }
        public (bool Success, string Result) CheckServiceValidity()
        {
            try
            {
                if (_firestoreDB == null && isEmbededServiceAccountValid())//json not imported and there is a valid embeded json file
                {
                    try
                    {
                        if (_app == null)//if json is embeded
                        {
                            _app = FirebaseApp.Create(new AppOptions
                            {
                                Credential = GoogleCredential.FromJson(FirebaseServiceIDs.GetJsonFromEmbededFile())
                            });
                        }
                        _firestoreDB = new FirestoreDatabaseWithAdminSDK(_app);
                        return (true, "Service account is valid.");
                    }
                    catch (Exception ex)
                    {
                        return (false, $"An error occurred while creating the Firebase app: {ex.Message}");
                    }
                }
                else if (_firestoreDB != null && isImportedServiceAccountValid())//_firestoreDB initialized with valid json file with import
                {
                    return (true, "Service account is valid.");
                }
                else
                {
                    return (false, "Service account is not valid.");
                }
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred while checking service validity: {ex.Message}");
            }
        }

        #endregion

        #region Methods

        public async void ListUsers()//This method runs in PerformOperation()
        {
            ///<summary>
            ///Creates a detailed table from FirebaseAuth object
            ///</summary>

            ConsoleString = "";

            _cts = new CancellationTokenSource();
            CancellationToken cancellationToken = _cts.Token;
            var progressBarTask = AnimateProgressBarAsync(cancellationToken);

            var Result = await _firestoreDB.GetUsersAsync();

            if (Result.Success)
            {
                var table = new ConsoleTable();
                table.SetHeaders("Number", "DisplayName", "email", "UID", "emailVerified", "PhoneNumber", "Disabled");

                //Populate ListOfUserUIDs for combobox: 
                ListOfUserUIDs = new List<string>();

                int usernumber = 0;
                foreach (FirebaseUser user in Result.Users)
                {
                    table.AddRow(usernumber.ToString(), user.DisplayName, user.Email, user.Uid, user.EmailVerified.ToString(), user.PhoneNumber.ToString(), user.Disabled.ToString());
                    ListOfUserUIDs.Add(user.Uid.ToString());
                    usernumber++;
                }
                ConsoleString = table.ToString();

                //Save log-update ListOfLogs
                PopulateLogs($"{usernumber} user listed. {DateTime.Now:yyyy:MM:dd-HH:mm:ss}");
            }
            else
            {
                ConsoleString = Result.ErrorMessage;
            }
            _cts.Cancel();
            await progressBarTask;

        }
        public async void DeleteUser()
        {
            ///<summary>
            ///Deletes a user from Firebase Authentication
            ///</summary>

            if (!String.IsNullOrEmpty(SelectedUserID))
            {
                if (MessageBox.Show($"Are you sure to delete user: {SelectedUserID}{Environment.NewLine}Once you delete user, you can't undo this process.", ProjectName, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _cts = new CancellationTokenSource();
                    CancellationToken cancellationToken = _cts.Token;
                    var progressBarTask = AnimateProgressBarAsync(cancellationToken);

                    (bool success, string result) = await _firestoreDB.DeleteUserAsync(SelectedUserID);

                    ConsoleString = "Result: " + result;

                    _cts.Cancel();
                    await progressBarTask;

                    await Task.Delay(1500);//wait 1.5 seconds
                    ListUsers();

                    //Save log-update ListOfLogs
                    PopulateLogs($"User {SelectedUserID} deleted. {DateTime.Now:yyyy:MM:dd-HH:mm:ss}");
                }
                else
                {
                    MessageBox.Show("Operation cancelled.", ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                if (ListOfUserUIDs == null || ListOfUserUIDs.Count == 0)
                {
                    MessageBox.Show(Message_GetAuthTable, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(Message_SelectUID, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        public async void ChangeUsersStatus(bool status)
        {
            ///<summary>
            /// Disables/Enables user at firebase authentication
            ///</summary>

            string actionname = status ? "Enable" : "Disable";

            if (!String.IsNullOrEmpty(SelectedUserID))
            {
                if (MessageBox.Show($"Are you sure to {actionname} user: {SelectedUserID}", ProjectName, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _cts = new CancellationTokenSource();
                    CancellationToken cancellationToken = _cts.Token;
                    var progressBarTask = AnimateProgressBarAsync(cancellationToken);

                    (bool success, string result) = await _firestoreDB.UpdateUserStatusAsync(SelectedUserID, status);

                    ConsoleString = "Result: " + result;

                    _cts.Cancel();
                    await progressBarTask;
                    await Task.Delay(1500);//wait 1.5 seconds
                    ListUsers();

                    //Save log-update ListOfLogs
                    PopulateLogs($"User {SelectedUserID} {actionname}d. {DateTime.Now:yyyy:MM:dd-HH:mm:ss}");
                }
                else
                {
                    MessageBox.Show(Operation_Canceled, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                if (ListOfUserUIDs == null || ListOfUserUIDs.Count == 0)
                {
                    MessageBox.Show(Message_GetAuthTable, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(Message_SelectUID, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        public async void ReadFieldFromDatabase()
        {
            ///<summary>
            ///Read a field from the databse
            ///</summary>

            if (!String.IsNullOrEmpty(SelectedRootCollectionID) | !String.IsNullOrEmpty(SelectedDocumentID) | !String.IsNullOrEmpty(SelectedFieldID))
            {
                if (MessageBox.Show($"Read operation will be performed:{Environment.NewLine}Do you proceed?",
                    ProjectName, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _cts = new CancellationTokenSource();
                    CancellationToken cancellationToken = _cts.Token;
                    var progressBarTask = AnimateProgressBarAsync(cancellationToken);

                    (bool success, string result) = await _firestoreDB.ReadField(SelectedRootCollectionID, SelectedDocumentID, SelectedFieldID);

                    ConsoleString = "result: " + result;

                    _cts.Cancel();
                    await progressBarTask;

                    //Save log-update ListOfLogs
                    PopulateLogs($"Read field operation at: {SelectedRootCollectionID}/{SelectedDocumentID}/{SelectedFieldID}. {DateTime.Now:yyyy:MM:dd-HH:mm:ss}");
                }
                else
                {
                    MessageBox.Show(Operation_Canceled, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show(Message_EnterDatabaseIDs, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async void WriteFieldToDatabase()
        {
            ///<summary>
            ///Write a field to the databse
            ///</summary>
            if (!String.IsNullOrEmpty(SelectedRootCollectionID) | !String.IsNullOrEmpty(SelectedDocumentID) | !String.IsNullOrEmpty(SelectedFieldID) | !String.IsNullOrEmpty(SelectedFieldValue))
            {
                if (MessageBox.Show($"Write operation will be performed:{Environment.NewLine}This operation overwrites existing documents" +
                    $"{Environment.NewLine}If you want to add field to existing document please use update method." +
                    $"{Environment.NewLine}Do you proceed to perform write operation?",
                    ProjectName, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _cts = new CancellationTokenSource();
                    CancellationToken cancellationToken = _cts.Token;
                    var progressBarTask = AnimateProgressBarAsync(cancellationToken);

                    (bool success, string result) = await _firestoreDB.WriteField(SelectedRootCollectionID, SelectedDocumentID, SelectedFieldID, SelectedFieldValue);
                    ConsoleString = "result: " + result;

                    _cts.Cancel();
                    await progressBarTask;

                    //Save log-update ListOfLogs
                    PopulateLogs($"Write field operation at: {SelectedRootCollectionID}/{SelectedDocumentID}/{SelectedFieldID}:{SelectedFieldValue}. {DateTime.Now:yyyy:MM:dd-HH:mm:ss}");
                }
                else
                {
                    MessageBox.Show(Operation_Canceled, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show(Message_EnterDatabaseIDs, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async void UpdateFieldOfDatabase()
        {
            ///<summary>
            ///Update a field  value of the databse
            ///</summary>
            if (!String.IsNullOrEmpty(SelectedRootCollectionID) | !String.IsNullOrEmpty(SelectedDocumentID) | !String.IsNullOrEmpty(SelectedFieldID) | !String.IsNullOrEmpty(SelectedFieldValue))
            {
                if (MessageBox.Show($"Update operation will be performed:{Environment.NewLine}Do you proceed?",
                    ProjectName, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _cts = new CancellationTokenSource();
                    CancellationToken cancellationToken = _cts.Token;
                    var progressBarTask = AnimateProgressBarAsync(cancellationToken);

                    (bool success, string result) = await _firestoreDB.UpdateField(SelectedRootCollectionID, SelectedDocumentID, SelectedFieldID, SelectedFieldValue);
                    ConsoleString = "result: " + result;

                    _cts.Cancel();
                    await progressBarTask;

                    //Save log-update ListOfLogs
                    PopulateLogs($"Update field operation at: {SelectedRootCollectionID}/{SelectedDocumentID}/{SelectedFieldID}:{SelectedFieldValue}. {DateTime.Now:yyyy:MM:dd-HH:mm:ss}");
                }
                else
                {
                    MessageBox.Show(Operation_Canceled, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show(Message_EnterDatabaseIDs, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async void DeleteFieldFromDatabase()
        {
            ///<summary>
            ///Delete field from databse
            ///</summary>
            if (!String.IsNullOrEmpty(SelectedRootCollectionID) | !String.IsNullOrEmpty(SelectedDocumentID) | !String.IsNullOrEmpty(SelectedFieldID))
            {
                if (MessageBox.Show($"Delete operation will be performed:{Environment.NewLine}Do you proceed?",
                    ProjectName, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _cts = new CancellationTokenSource();
                    CancellationToken cancellationToken = _cts.Token;
                    var progressBarTask = AnimateProgressBarAsync(cancellationToken);

                    (bool success, string result) = await _firestoreDB.DeleteField(SelectedRootCollectionID, SelectedDocumentID, SelectedFieldID);
                    ConsoleString = "result: " + result;

                    _cts.Cancel();
                    await progressBarTask;

                    //Save log-update ListOfLogs
                    PopulateLogs($"Delete field operation at: {SelectedRootCollectionID}/{SelectedDocumentID}/{SelectedFieldID}. {DateTime.Now:yyyy:MM:dd-HH:mm:ss}");
                }
                else
                {
                    MessageBox.Show(Operation_Canceled, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
                }

            }
            else
            {
                MessageBox.Show(Message_EnterDatabaseIDs, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async void ListRootCollectionIDs()//This method runs in PerformOperation()
        {
            ///<summary>
            ///List root collection IDs and creates a table in console
            ///</summary>
            _cts = new CancellationTokenSource();
            CancellationToken cancellationToken = _cts.Token;
            var progressBarTask = AnimateProgressBarAsync(cancellationToken);

            (bool success, string result, List<string> collectionIds) = await _firestoreDB.ListOfRootCollectionIDs();
            if (success & (collectionIds.Count != 0))
            {
                var table = new ConsoleTable();
                table.SetHeaders("Number", "CollectionID");

                //Populate ListOfRootCollectionNames for combobox
                ListOfRootCollectionNames = new List<string>();

                int collectionnumber = 1;
                foreach (string collectionID in collectionIds)
                {
                    table.AddRow(collectionnumber.ToString(), collectionID);
                    ListOfRootCollectionNames.Add(collectionID);
                    collectionnumber++;
                }
                ConsoleString = table.ToString();

                //Save log-update ListOfLogs
                PopulateLogs($"Root collection IDs listed. {DateTime.Now:yyyy:MM:dd-HH:mm:ss}");
            }
            else
            {
                ConsoleString = "Result: " + (collectionIds.Count == 0 ? "No collections in database" : result);
            }
            _cts.Cancel();
            await progressBarTask;

        }
        public async void ListDocumentIDsInRootCollection()
        {
            ///<summary>
            ///List document IDs in a root collection and creates a table in console
            ///</summary>
            ///
            if (!String.IsNullOrEmpty(SelectedRootCollectionID))
            {
                if (MessageBox.Show($"Read operation will be performed:{Environment.NewLine}" +
                    $"Do you proceed?",
                    ProjectName, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _cts = new CancellationTokenSource();
                    CancellationToken cancellationToken = _cts.Token;
                    var progressBarTask = AnimateProgressBarAsync(cancellationToken);

                    var (success, documentIds, result) = await _firestoreDB.ListOfDocumentIDsUnderRootCollection(SelectedRootCollectionID);

                    if (success & (documentIds.Count != 0))
                    {

                        var table = new ConsoleTable();
                        table.SetHeaders("Number", "DocumentID");

                        //Populate list for combobox
                        ListOfDocumentsInRootCollection = new List<string>();

                        int documentnumber = 1;
                        foreach (string documentID in documentIds)
                        {
                            table.AddRow(documentnumber.ToString(), documentID);
                            ListOfDocumentsInRootCollection.Add(documentID);
                            documentnumber++;
                        }
                        ConsoleString = table.ToString();

                        //Save log-update ListOfLogs
                        PopulateLogs($"Documents IDs listed in {SelectedRootCollectionID}. {DateTime.Now:yyyy:MM:dd-HH:mm:ss}");
                    }
                    else
                    {
                        ConsoleString = "Result: " + (documentIds.Count == 0 ? "No collections in database" : result);
                    }

                    _cts.Cancel();
                    await progressBarTask;
                }
                else
                {
                    MessageBox.Show(Operation_Canceled, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show(Message_EnterDatabaseIDs, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async void GetDocumentSnapshotTreeFromDatabase()
        {
            ///<summary>
            ///Get snapshot and create treeview of the document
            ///</summary>
            if (!String.IsNullOrEmpty(SelectedRootCollectionID) | !String.IsNullOrEmpty(SelectedDocumentID))
            {
                if (MessageBox.Show($"Read operation will be performed:{Environment.NewLine}Do you proceed?",
                    ProjectName, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _cts = new CancellationTokenSource();
                    CancellationToken cancellationToken = _cts.Token;
                    var progressBarTask = AnimateProgressBarAsync(cancellationToken);

                    (bool success, string result) = await _firestoreDB.GetDocumentSnapshotTree(SelectedRootCollectionID, SelectedDocumentID);
                    ConsoleString = result;

                    _cts.Cancel();
                    await progressBarTask;

                    //Save log-update ListOfLogs
                    PopulateLogs($"Document snapshot at  {SelectedRootCollectionID}/{SelectedDocumentID}. {DateTime.Now:yyyy:MM:dd-HH:mm:ss}");
                }
                else
                {
                    MessageBox.Show(Operation_Canceled, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show(Message_EnterDatabaseIDs, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public async void GetRootCollectionSnapshotTreeFromDatabase()
        {
            ///<summary>
            ///Get snapshot and create treeview of Root Collection
            ///</summary>
            if (!String.IsNullOrEmpty(SelectedRootCollectionID))
            {
                if (MessageBox.Show($"Read operation will be performed:{Environment.NewLine}Do you proceed?",
                    ProjectName, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _cts = new CancellationTokenSource();
                    CancellationToken cancellationToken = _cts.Token;
                    var progressBarTask = AnimateProgressBarAsync(cancellationToken);

                    (bool success, string result) = await _firestoreDB.GetRootCollectionSnapshotTree(SelectedRootCollectionID, false);
                    ConsoleString = result;

                    _cts.Cancel();
                    await progressBarTask;

                    //Save log-update ListOfLogs
                    PopulateLogs($"Root collection snapshot at  {SelectedRootCollectionID}. {DateTime.Now:yyyy:MM:dd-HH:mm:ss}");
                }
                else
                {
                    MessageBox.Show(Operation_Canceled, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
                }


            }
            else
            {
                MessageBox.Show(Message_EnterDatabaseIDs, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }
        public async void DeleteDocumentUnderRootCollection()
        {
            ///<summary>
            ///Delete a document from root collection
            ///</summary>
            if (!String.IsNullOrEmpty(SelectedRootCollectionID) | !String.IsNullOrEmpty(SelectedDocumentID))
            {
                if (MessageBox.Show($"Delete operation will be performed:{Environment.NewLine}" +
                                    $"Once document is deleted, there is no undo for this process.{Environment.NewLine}Do you proceed?",
                                    ProjectName, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _cts = new CancellationTokenSource();
                    CancellationToken cancellationToken = _cts.Token;
                    var progressBarTask = AnimateProgressBarAsync(cancellationToken);

                    (bool success, string result) = await _firestoreDB.DeleteDocument(SelectedRootCollectionID, SelectedDocumentID);
                    ConsoleString = "Results: " + result;

                    _cts.Cancel();
                    await progressBarTask;

                    //Save log-update ListOfLogs
                    PopulateLogs($"Document deleted at  {SelectedRootCollectionID}/{SelectedDocumentID}. {DateTime.Now:yyyy:MM:dd-HH:mm:ss}");
                }
                else
                {
                    MessageBox.Show(Operation_Canceled, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show(Message_EnterDatabaseIDs, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }
        public async void GetDatabaseSnapshotTree()//This method runs in PerformOperation()
        {
            ///<summary>
            ///Get snapshot and create treeview of database
            ///</summary>
            _cts = new CancellationTokenSource();
            CancellationToken cancellationToken = _cts.Token;
            var progressBarTask = AnimateProgressBarAsync(cancellationToken);

            (bool success, string result) = await _firestoreDB.GetDatabaseSnapshotTree();
            ConsoleString = result;

            _cts.Cancel();
            await progressBarTask;

            //Save log-update ListOfLogs
            PopulateLogs($"Database snapshot. {DateTime.Now:yyyy:MM:dd-HH:mm:ss}");

        }
        public void GetLastDatabaseSnapshotFromLog()
        {
            ///<summary>
            ///Load last database snapshot
            ///</summary>
            ConsoleString = Logs.CombineLogFiles();
        }
        public void PopulateLogs(string logstring)
        {
            //Save log
            Logs.SaveToLogFile(logstring);
            //update ListOfLogs
            LogsFromTextFile = Logs.GetAllLogs();
        }

        #endregion

        #region ICommands

        //Show pop ups or perform direct actions
        private ICommand _performOperationCommand;
        public void PerformOperation()
        {
            ///<summary>
            /// Determines which popup to show
            ///</summary>

            if (SelectedOperationIndex > 0)
            {
                // Call the CheckServiceValidity() method to check the service account validity.
                (bool ServiceAccountSuccess, string ServiceAccountCondition) = CheckServiceValidity();
                // Check the value of the 'success' variable to see if the service account is valid.
                if (!ServiceAccountSuccess)
                {
                    MessageBox.Show(ServiceAccountCondition, ProjectName, MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConsoleString = ServiceAccountCondition;
                    return;
                }
            }

            switch (_selectedOperationIndex)
            {
                case 1:

                    Is_Buttons_Enabled = false;
                    if (MessageBox.Show($"Authentication users will be listed{Environment.NewLine}Do you proceed?", ProjectName, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        ConsoleContentAlignment = TextAlignment.Center;
                        ListUsers();
                    }
                    Is_Buttons_Enabled = true;
                    break;
                case 2:
                    Is_Pop_UIDs_Visible = true;
                    break;
                case 3:
                    Is_Pop_UIDs_Visible = true;
                    break;
                case 4:
                    Is_Pop_UIDs_Visible = true;
                    break;
                case 5:
                    Is_Pop_Database_Visible = true;
                    Height_Pop_Database = 255;
                    GridHeight_DocumentID = 45;
                    GridHeight_FieldID = 45;
                    GridHeight_FieldValue = 0;
                    break;
                case 6:
                    Is_Pop_Database_Visible = true;
                    Height_Pop_Database = 300;
                    GridHeight_DocumentID = 45;
                    GridHeight_FieldID = 45;
                    GridHeight_FieldValue = 45;
                    break;
                case 7:
                    Is_Pop_Database_Visible = true;
                    Height_Pop_Database = 300;
                    GridHeight_DocumentID = 45;
                    GridHeight_FieldID = 45;
                    GridHeight_FieldValue = 45;
                    break;
                case 8:
                    Is_Pop_Database_Visible = true;
                    Height_Pop_Database = 255;
                    GridHeight_DocumentID = 45;
                    GridHeight_FieldID = 45;
                    GridHeight_FieldValue = 0;
                    break;
                case 9:
                    Is_Buttons_Enabled = false;
                    if (MessageBox.Show($"Root collection IDs will be listed{Environment.NewLine}Do you proceed?", ProjectName, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        ListRootCollectionIDs();
                    }
                    Is_Buttons_Enabled = true;
                    break;
                case 10:
                    Is_Pop_Database_Visible = true;
                    Height_Pop_Database = 165;
                    GridHeight_DocumentID = 0;
                    GridHeight_FieldID = 0;
                    GridHeight_FieldValue = 0;
                    break;
                case 11:
                    Is_Pop_Database_Visible = true;
                    Height_Pop_Database = 210;
                    GridHeight_DocumentID = 45;
                    GridHeight_FieldID = 0;
                    GridHeight_FieldValue = 0;
                    break;
                case 12:
                    Is_Pop_Database_Visible = true;
                    Height_Pop_Database = 210;
                    GridHeight_DocumentID = 45;
                    GridHeight_FieldID = 0;
                    GridHeight_FieldValue = 0;
                    break;
                case 13:
                    Is_Pop_Database_Visible = true;
                    Height_Pop_Database = 165;
                    GridHeight_DocumentID = 0;
                    GridHeight_FieldID = 0;
                    GridHeight_FieldValue = 0;
                    break;
                case 14:
                    Is_Buttons_Enabled = false;
                    if (MessageBox.Show($"Be aware that you are getting full snapshot of database{Environment.NewLine}Do you proceed?", ProjectName, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        ConsoleContentAlignment = TextAlignment.Left;
                        GetDatabaseSnapshotTree();
                    }
                    else
                    {
                        MessageBox.Show(Operation_Canceled, ProjectName, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    Is_Buttons_Enabled = true;
                    break;
                case 15:
                    Is_Buttons_Enabled = false;
                    ConsoleContentAlignment = TextAlignment.Left;
                    GetLastDatabaseSnapshotFromLog();
                    Is_Buttons_Enabled = true;
                    break;
            }
        }
        public ICommand PerformOperationCommand
        {
            get
            {
                if (_performOperationCommand == null)
                {
                    _performOperationCommand = new RelayCommand(p => PerformOperation());
                }
                return _performOperationCommand;
            }
        }

        //Execute UID mission
        private ICommand _executePopUpEntryCommand;
        public void ExecutePopUpEntry()
        {
            ///<summary>
            /// Perform selected operation
            ///</summary>

            if (SelectedOperationIndex > 0)
            {
                // Call the CheckServiceValidity() method to check the service account validity.
                (bool ServiceAccountSuccess, string ServiceAccountCondition) = CheckServiceValidity();
                // Check the value of the 'success' variable to see if the service account is valid.
                if (!ServiceAccountSuccess)
                {
                    MessageBox.Show(ServiceAccountCondition, ProjectName, MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConsoleString = ServiceAccountCondition;
                    return;
                }
            }

            switch (SelectedOperationIndex)
            {
                //1 is in PerformOperation

                case 2:
                    ConsoleContentAlignment = TextAlignment.Center;
                    Is_Buttons_Enabled = false;
                    ChangeUsersStatus(false);
                    Is_Buttons_Enabled = true;
                    break;
                case 3:
                    ConsoleContentAlignment = TextAlignment.Center;
                    Is_Buttons_Enabled = false;
                    ChangeUsersStatus(true);
                    Is_Buttons_Enabled = true;
                    break;
                case 4:
                    ConsoleContentAlignment = TextAlignment.Center;
                    Is_Buttons_Enabled = false;
                    DeleteUser();
                    Is_Buttons_Enabled = true;
                    break;
                case 5:
                    ConsoleContentAlignment = TextAlignment.Center;
                    Is_Buttons_Enabled = false;
                    ReadFieldFromDatabase();
                    Is_Buttons_Enabled = true;
                    break;
                case 6:
                    ConsoleContentAlignment = TextAlignment.Center;
                    Is_Buttons_Enabled = false;
                    WriteFieldToDatabase();
                    Is_Buttons_Enabled = true;
                    break;
                case 7:
                    ConsoleContentAlignment = TextAlignment.Center;
                    Is_Buttons_Enabled = false;
                    UpdateFieldOfDatabase();
                    Is_Buttons_Enabled = true;
                    break;
                case 8:
                    ConsoleContentAlignment = TextAlignment.Center;
                    Is_Buttons_Enabled = false;
                    DeleteFieldFromDatabase();
                    Is_Buttons_Enabled = true;
                    break;

                //9 is in PerformOperation, no need to popup

                case 10:
                    ConsoleContentAlignment = TextAlignment.Center;
                    Is_Buttons_Enabled = false;
                    ListDocumentIDsInRootCollection();
                    Is_Buttons_Enabled = true;
                    break;
                case 11:
                    ConsoleContentAlignment = TextAlignment.Center;
                    Is_Buttons_Enabled = false;
                    DeleteDocumentUnderRootCollection();
                    Is_Buttons_Enabled = true;
                    break;
                case 12:
                    ConsoleContentAlignment = TextAlignment.Left;
                    Is_Buttons_Enabled = false;
                    GetDocumentSnapshotTreeFromDatabase();
                    Is_Buttons_Enabled = true;
                    break;
                case 13:
                    ConsoleContentAlignment = TextAlignment.Left;
                    Is_Buttons_Enabled = false;
                    GetRootCollectionSnapshotTreeFromDatabase();
                    Is_Buttons_Enabled = true;
                    break;

            }
        }
        public ICommand ExecutePopUpEntryCommand
        {
            get
            {
                if (_executePopUpEntryCommand == null)
                {
                    _executePopUpEntryCommand = new RelayCommand(p => ExecutePopUpEntry());
                }
                return _executePopUpEntryCommand;
            }
        }

        //Load service account
        public void LoadServiceAccount()
        {
            ServiceAccountJsonFilePath = Dialogs.LoadFileName("Import Service Account File", "json File |*.json");

            // Check if the JSON file exists
            if (!File.Exists(ServiceAccountJsonFilePath))
            {
                string usermessage = $"The specified JSON file does not exist.";
                MessageBox.Show(usermessage, ProjectName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Parse the JSON file
            try
            {
                string jsonContent = File.ReadAllText(ServiceAccountJsonFilePath);
                JObject jObject = JObject.Parse(jsonContent);
                JToken type = jObject.SelectToken("type");
                JToken project_id = jObject.SelectToken("project_id");
                JToken private_key_id = jObject.SelectToken("private_key_id");
                JToken private_key = jObject.SelectToken("private_key");
                JToken client_email = jObject.SelectToken("client_email");
                JToken client_id = jObject.SelectToken("client_id");
                JToken auth_uri = jObject.SelectToken("auth_uri");
                JToken token_uri = jObject.SelectToken("token_uri");
                JToken auth_provider_x509_cert_url = jObject.SelectToken("auth_provider_x509_cert_url");
                JToken client_x509_cert_url = jObject.SelectToken("client_x509_cert_url");

                // Check if all the required fields are present
                if (type == null || project_id == null || private_key_id == null || private_key == null
                    || client_email == null || client_id == null || auth_uri == null || token_uri == null
                    || auth_provider_x509_cert_url == null || client_x509_cert_url == null)
                {
                    string usermessage = $"The specified JSON file does not contain all the necessary fields for a GoogleCredential.";
                    ConsoleString = usermessage;
                    MessageBox.Show(usermessage, ProjectName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                string usermessage = $"Error parsing the specified JSON file. Error:{ex}";
                MessageBox.Show(usermessage, ProjectName, MessageBoxButton.OK, MessageBoxImage.Error);
                ConsoleString = usermessage;
                return;
            }

            // Create the FirebaseApp and FirestoreDatabaseWithAdminSDK objects
            _app = FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(ServiceAccountJsonFilePath)
            });

            _firestoreDB = new FirestoreDatabaseWithAdminSDK(_app);
            ConsoleString = "Service account contents read successfully.";
        }

        private ICommand _loadServiceAccountCommand;
        public ICommand LoadServiceAccountCommand
        {
            get
            {
                if (_loadServiceAccountCommand == null)
                {
                    _loadServiceAccountCommand = new RelayCommand(p => LoadServiceAccount());
                }
                return _loadServiceAccountCommand;
            }
        }

        //Close App
        private ICommand _closeCommand;
        public void CloseApp(object obj)
        {
            FireAdminMainUI win = obj as FireAdminMainUI;
            if (_app != null)
            {
                _app.Delete();
            }
            win.Close();
        }
        public ICommand CloseAppCommand
        {
            get
            {
                if (_closeCommand == null)
                {
                    _closeCommand = new RelayCommand(p => CloseApp(p));
                }
                return _closeCommand;
            }
        }

        //Maximize App
        public void MaxApp(object obj)
        {

            FireAdminMainUI win = obj as FireAdminMainUI;
            if (win.WindowState == WindowState.Normal)
            {
                win.WindowState = WindowState.Maximized;
            }
            else if (win.WindowState == WindowState.Maximized)
            {
                win.WindowState = WindowState.Normal;
            }

        }
        private ICommand _maxCommand;
        public ICommand MaxAppCommand
        {
            get
            {
                if (_maxCommand == null)
                {
                    _maxCommand = new RelayCommand(p => MaxApp(p));
                }
                return _maxCommand;
            }
        }

        #endregion

        #region Popups

        //Delete User
        private ICommand _show_Pop_UIDs_Command;
        public void Show_Pop_UIDs()
        {
            ///<summary>
            ///Reads a field from databse
            ///</summary>

            Is_Pop_UIDs_Visible = true;
        }
        public ICommand Show_Pop_UIDs_Command
        {
            get
            {
                if (_show_Pop_UIDs_Command == null)
                {
                    _show_Pop_UIDs_Command = new RelayCommand(p => Show_Pop_UIDs());
                }
                return _show_Pop_UIDs_Command;
            }
        }

        private bool _is_Pop_UIDs_Visible;
        public bool Is_Pop_UIDs_Visible
        {
            get { return _is_Pop_UIDs_Visible; }
            set
            {
                _is_Pop_UIDs_Visible = value;
                OnPropertyChanged(nameof(Is_Pop_UIDs_Visible));
            }
        }

        private bool _is_Pop_Database_Visible;
        public bool Is_Pop_Database_Visible
        {
            get { return _is_Pop_Database_Visible; }
            set
            {
                _is_Pop_Database_Visible = value;
                OnPropertyChanged(nameof(Is_Pop_Database_Visible));
            }
        }

        #endregion

        #region ProgressBar
        private async Task AnimateProgressBarAsync(CancellationToken cancellationToken)
        {
            int totalCircles = 20;
            int currentCircle = 0;
            int animationInterval = 100; // in milliseconds

            ConsoleString = "Please wait..."; // Write the message to the console

            while (!cancellationToken.IsCancellationRequested)
            {
                // Remove the filled circle symbol and add a blank space
                string progressBar = "\nPlease wait...\n" + "\u25CB" + new string('\u25CB', totalCircles - 1) + "\r"; // use \r to overwrite the previous line

                progressBar = progressBar.Insert(currentCircle + 16, '\u25CF'.ToString()); // Add 12 to align the progress bar with the "Please wait..." message

                // Output the progress bar
                ConsoleString = progressBar;

                // Wait for the animation interval
                await Task.Delay(animationInterval);

                // Increment the current circle and wrap around when it reaches the end
                currentCircle = (currentCircle + 1) % totalCircles;

                // Replace the blank space with the filled circle symbol
                progressBar = progressBar.Remove(currentCircle + 12, 1); // Add 12 to align the progress bar with the "Please wait..." message
                progressBar = progressBar.Insert(currentCircle + 12, "\u25CF");
            }
        }

        #endregion

        #region Welcome Screen

        public void StartTypingEffect()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(20);
            _timer.Tick += OnTimerTick;
            _currentSentenceIndex = 0;
            _currentLetterIndex = 0;
            ConsoleString = _sentences[_currentSentenceIndex].Substring(0, _currentLetterIndex);
            _timer.Start();
        }
        private void OnTimerTick(object sender, EventArgs e)
        {
            if (_currentSentenceIndex >= _sentences.Count)
            {
                _timer.Stop();
                ConsoleString = ReleaseNotes[0];
                return;
            }

            if (_currentLetterIndex < _sentences[_currentSentenceIndex].Length)
            {
                // add next letter to the current sentence
                _timer.Interval = TimeSpan.FromMilliseconds(20);
                _currentLetterIndex++;
                ConsoleString = _sentences[_currentSentenceIndex].Substring(0, _currentLetterIndex);
            }
            else
            {
                // current sentence is complete, wait 1.5 seconds before starting the next one
                _timer.Interval = TimeSpan.FromSeconds(1.2);
                _currentSentenceIndex++;
                _currentLetterIndex = 0;
            }
        }

        private DispatcherTimer _timer;
        private List<string> _sentences;
        private int _currentSentenceIndex;
        private int _currentLetterIndex;

        #endregion

        #region Static strings

        public static string ProjectName = "Fire Admin";

        public static string Message_GetAuthTable = "First, you have to get authentication table.";

        public static string Message_SelectUID = "Please select User UID from the list";

        public static string Message_EnterDatabaseIDs = "Please enter necessary id's for read operation";

        public static string Operation_Canceled = "Operation canceled.";

        public static string CommonError = "Unknown error, please check your service account file. Error: ";

        public static List<string> ReleaseNotes = new List<string>{"Version 1.0-Released in May 2023-Created by Levent Tasci"};

        #endregion

        #region UI components

        private double _gridHeight_DocumentID;
        public double GridHeight_DocumentID
        {
            get { return _gridHeight_DocumentID; }
            set
            {
                _gridHeight_DocumentID = value;
                OnPropertyChanged(nameof(GridHeight_DocumentID)); // Implement INotifyPropertyChanged interface
            }
        }
        private double _gridHeight_FieldID;
        public double GridHeight_FieldID
        {
            get { return _gridHeight_FieldID; }
            set
            {
                _gridHeight_FieldID = value;
                OnPropertyChanged(nameof(GridHeight_FieldID)); // Implement INotifyPropertyChanged interface
            }
        }
        private double _gridHeight_FieldValue;
        public double GridHeight_FieldValue
        {
            get { return _gridHeight_FieldValue; }
            set
            {
                _gridHeight_FieldValue = value;
                OnPropertyChanged(nameof(GridHeight_FieldValue)); // Implement INotifyPropertyChanged interface
            }
        }
        private double _height_Pop_Database;
        public double Height_Pop_Database
        {
            get { return _height_Pop_Database; }
            set
            {
                _height_Pop_Database = value;
                OnPropertyChanged(nameof(Height_Pop_Database)); // Implement INotifyPropertyChanged interface
            }
        }

        private TextAlignment _consoleContentAlignment;
        public TextAlignment ConsoleContentAlignment
        {
            get { return _consoleContentAlignment; }
            set
            {
                _consoleContentAlignment = value;
                OnPropertyChanged(nameof(ConsoleContentAlignment)); // Implement INotifyPropertyChanged interface
            }
        }

        private bool _is_Buttons_Enabled;
        public bool Is_Buttons_Enabled
        {
            get { return _is_Buttons_Enabled; }
            set
            {
                _is_Buttons_Enabled = value;
                OnPropertyChanged(nameof(Is_Buttons_Enabled));
            }
        }

        #endregion

        #region PropertyChanged: Have to implement the interface members for INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        #endregion

    }
}
