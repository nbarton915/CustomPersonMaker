using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Windows.System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Microsoft.VisualBasic;
using System.Collections.ObjectModel;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PersonMaker
{ 
    public class UploadPerson
    {
        public string PersonGroup { get; set; }
        public string Name { get; set; }
        public List<UserData> Data { get; set; }
    }

    public class DownloadPerson
    {
        public string PersonGroup { get; set; }
        public string Name { get; set; }
        public List<UserData> Data { get; set; }
    }

    public class UserData
    {
        public string UserDataLabel { get; set; }
        public string UserDataValue { get; set; }
    }

    public class Attributes
    {
        public List<UserData> Data { get; set; }
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class MainPage : Page
    {
        string authKey;
        string personGroupId;
        string personGroupName;

        Guid personId;
        string personName;
        StorageFolder personFolder;

        private FaceServiceClient faceServiceClient;
        private PersonGroup knownGroup;
        private int minPhotos = 6;

        string personUserData;
        string personDataName;
        string jsonString;
        List<UserData> userDataPayload = new List<UserData> { };
        private Person knownPerson;

        public MainPage()
        {
            this.InitializeComponent();
            personName = string.Empty;
            authKey = string.Empty;
            personGroupId = string.Empty;
            personGroupName = string.Empty;
            personId = Guid.Empty;
        }

        /// <summary>
        /// Create a person group with ID and name provided if none can be found in the service.
        /// </summary>
        /// 

        private void AddNameValueToPayloadButton_Click(object sender, RoutedEventArgs e)
        {
            personDataName = PersonUserDataNameTextBox.Text;
            personUserData = PersonUserDataTextBox.Text;

            if (personDataName.Length > 0 && personUserData.Length > 0)
            {
                userDataPayload.Add(new UserData() { UserDataLabel = personDataName, UserDataValue = personUserData });
            }

            jsonString = JsonConvert.SerializeObject(userDataPayload);
            UpdateUserDataStatusTextBlock.Text = "User Data added to payload with the following User Data: ";
            UpdateUserDataPayloadTextBlock.Text = jsonString;
            PersonUserDataNameTextBox.Text = "";
            PersonUserDataTextBox.Text = "";

            UpdateUserDataStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
        }

        private async void BulkUploadButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Bulk Upload Button");

            // Configure open file dialog box
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".csv");
            picker.FileTypeFilter.Add(".txt");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                BulkUploadStatusTextBlock.Text = "Picked file: " + file.Name;
            }
            else
            {
                BulkUploadStatusTextBlock.Text = "Operation cancelled.";
            }

            List<UploadPerson> people = new List<UploadPerson>();

            var rawText = await Windows.Storage.FileIO.ReadTextAsync(file);
            var records = rawText.Split('\n');
            List<String> labels = new List<string>();
            foreach (var record in records)
            {
                UploadPerson psn = new UploadPerson();
                var fields = record.Split(',');
                if (record.Length <= 0)
                {
                    break;
                }
                psn.PersonGroup = fields[0];
                psn.Name = fields[2];
                int i = 0;
                List<UserData> lstUserData = new List<UserData>();
                foreach (var field in fields)
                {
                    //Debug.WriteLine(field);
                    if (i > 2)
                    {
                        if (people.Count() <= 0)
                        {
                            labels.Add(field);
                        }
                        else
                        {
                            Debug.WriteLine("Adding User Data");
                            lstUserData.Add(new UserData() { UserDataLabel = labels[i - 3], UserDataValue = field });
                        }
                    }
                    i += 1;
                }
                psn.Data = lstUserData;
                people.Add(psn);
            }

            authKey = AuthKeyTextBox.Text;
            await ApiCallAllowed(true);
            faceServiceClient = new FaceServiceClient(authKey);
            var result = await faceServiceClient.ListPersonsAsync("testing");
            
            foreach (var r in result)
            {
                Debug.WriteLine(r.Name);
                Debug.WriteLine(r.UserData);
            }

            foreach (var pson in people)
            {
                //reset knownPerson for a successful FetchPerson check later on
                knownPerson = null;
                Debug.WriteLine(pson.PersonGroup);
                Debug.WriteLine(pson.Name);

                PersonGroupIdTextBox.Text = pson.PersonGroup;

                try
                {
                    if (pson.Name != "name")
                    {
                        FetchPersonGroup_Click(this, new RoutedEventArgs());
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        PersonNameTextBox.Text = pson.Name;
                    }
                }
                catch
                {
                    Debug.WriteLine($"There was a problem with Fetching the person group {pson.PersonGroup}.");
                }

                try
                {
                    if (pson.Name != "name")
                    {
                        CreatePersonButton_ClickAsync(this, new RoutedEventArgs());
                    }
                }
                catch
                {
                    Debug.WriteLine($"There was a problem with Creating the person {pson.Name}.");
                }

                if (pson.Data != null)
                {
                    foreach (var lst in pson.Data)
                    {
                        Debug.WriteLine(lst.UserDataLabel + " " + lst.UserDataValue);
                    }
                }
            }
        }

        private async void CreateFolderButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (personName.Length > 0 && personId != Guid.Empty)
            {
                CreateFolderErrorText.Visibility = Visibility.Collapsed;
                StorageFolder picturesFolder = KnownFolders.PicturesLibrary;
                personFolder = await picturesFolder.CreateFolderAsync(personName, CreationCollisionOption.OpenIfExists);
                await Launcher.LaunchFolderAsync(personFolder);
            }
            else
            {
                CreateFolderErrorText.Text = "You must have created a person in section 3.";
                CreateFolderErrorText.Visibility = Visibility.Visible;
            }
        }

        private async void CreatePersonButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            personName = PersonNameTextBox.Text;
            PersonStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            if (knownGroup != null && personName.Length > 0)
            {
                CreatePersonErrorText.Visibility = Visibility.Collapsed;
                //Check if this person already exist
                bool personAlreadyExist = false;
                Person[] ppl = await GetKnownPeople();
                foreach (Person p in ppl)
                {
                    if (p.Name == personName)
                    {
                        personAlreadyExist = true;
                        PersonStatusTextBlock.Text = $"Person already exist: {p.Name} ID: {p.PersonId}";

                        PersonStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    }
                }

                if (!personAlreadyExist)
                {
                    await ApiCallAllowed(true);
                    CreatePersonResult result = await faceServiceClient.CreatePersonAsync(personGroupId, personName);
                    if (null != result && null != result.PersonId)
                    {
                        personId = result.PersonId;

                        PersonStatusTextBlock.Text = "Created new person: " + result.PersonId;

                        PersonStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    }
                }
            }
            else
            {
                CreatePersonErrorText.Text = "Please provide a name above, and ensure that the above person group section has been completed.";
                CreatePersonErrorText.Visibility = Visibility.Visible;
            }
        }

        private async void CreatePersonGroupButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            personGroupId = PersonGroupIdTextBox.Text;
            personGroupName = PersonGroupNameTextBox.Text;
            PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            authKey = AuthKeyTextBox.Text;

            if (string.IsNullOrWhiteSpace(personGroupId) == false && string.IsNullOrWhiteSpace(personGroupName) == false && string.IsNullOrWhiteSpace(authKey) == false)
            {
                PersonGroupCreateErrorText.Visibility = Visibility.Collapsed;
                await ApiCallAllowed(true);
                faceServiceClient = new FaceServiceClient(authKey);

                if (null != faceServiceClient)
                {
                    // You may experience issues with this below call, if you are attempting connection with
                    // a service location other than 'West US'
                    PersonGroup[] groups = await faceServiceClient.ListPersonGroupsAsync();
                    var matchedGroups = groups.Where(p => p.PersonGroupId == personGroupId);

                    if (matchedGroups.Count() > 0)
                    {
                        knownGroup = matchedGroups.FirstOrDefault();

                        PersonGroupStatusTextBlock.Text = "Found existing: " + knownGroup.Name;
                    }

                    if (null == knownGroup)
                    {
                        await ApiCallAllowed(true);
                        await faceServiceClient.CreatePersonGroupAsync(personGroupId, personGroupName);
                        knownGroup = await faceServiceClient.GetPersonGroupAsync(personGroupId);

                        PersonGroupStatusTextBlock.Text = "Created new group: " + knownGroup.Name;
                    }

                    if (PersonGroupStatusTextBlock.Text != "- Person Group status -")
                    {
                        PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    }
                    else
                    {
                        PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    }
                }
            }
            else
            {
                PersonGroupCreateErrorText.Text = "Make sure you provide: a Person Group ID, a Person Group Name, and the Authentication Key in the section above.";
                PersonGroupCreateErrorText.Visibility = Visibility.Visible;
            }
        }

        private async void DeletePersonButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            personName = PersonNameTextBox.Text;

            if (string.IsNullOrWhiteSpace(personName) == false)
            {
                CreatePersonErrorText.Visibility = Visibility.Collapsed;
                bool personExist = false;
                Person[] ppl = await GetKnownPeople();
                foreach (Person p in ppl)
                {
                    if (p.Name == personName)
                    {
                        personExist = true;
                    
                        PersonStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                        await RemovePerson(p);
                        PersonStatusTextBlock.Text = $"Deleted: {p.Name} ID: {p.PersonId}";
                    }
                }
                if (!personExist)
                {
                    PersonStatusTextBlock.Text = $"No persons found to delete.";
                }
            }
            else
            {
                CreatePersonErrorText.Text = "Cannot delete: No name has been provided.";
                CreatePersonErrorText.Visibility = Visibility.Visible;
            }
        }

        private async void DeleteUserDataButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            personUserData = "{}";
            userDataPayload.Clear();

            if (knownPerson.Name.Length <= 0)
            {
                UpdateUserDataStatusTextBlock.Text = $"Person not found. Fetch a known Person";

                UpdateUserDataStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                await ApiCallAllowed(true);
                await faceServiceClient.UpdatePersonAsync(personGroupId, knownPerson.PersonId, knownPerson.Name, personUserData);

                Person[] people = await GetKnownPeople();
                var matchedPeople = people.Where(p => p.Name == personName);

                if (matchedPeople.Count() > 0)
                {
                    knownPerson = matchedPeople.FirstOrDefault();

                    UpdateUserDataStatusTextBlock.Text = "User Data for Person: " + knownPerson.Name + " has been deleted. ";
                    if (knownPerson.UserData == "{}")
                    {
                        UpdateUserDataPayloadTextBlock.Text = "No User Data...";
                    }
                    else
                    {
                        UpdateUserDataPayloadTextBlock.Text = knownPerson.UserData;
                    }
                }
            }
        }

        private async void FetchPersonButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            userDataPayload = null;

            personName = PersonNameTextBox.Text;
            PersonStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            authKey = AuthKeyTextBox.Text;

            await ApiCallAllowed(true);
            faceServiceClient = new FaceServiceClient(authKey);

            if (null != faceServiceClient && null != knownGroup && personName.Length > 0)
            {
                // You may experience issues with this below call, if you are attempting connection with
                // a service location other than 'West US'
                Person[] people = await GetKnownPeople();
                var matchedPeople = people.Where(p => p.Name == personName);

                if (matchedPeople.Count() > 0)
                {
                    knownPerson = matchedPeople.FirstOrDefault();

                    PersonStatusTextBlock.Text = "Found existing: " + knownPerson.Name;

                    //Attributes UserAttributes = new Attributes();
                    //var json = JsonConvert.SerializeObject(knownPerson.UserData);
                    //var json2 = JsonConvert.DeserializeObject(json);
                    

                    //Debug.WriteLine(json);
                    //UserData UserAttributes = new UserData();
                    //UserAttributes.Data = JsonConvert.DeserializeObject<List<UserData>>(json);

                    try
                    {
                        Attributes attributes = new Attributes();
                        attributes.Data = JsonConvert.DeserializeObject<List<UserData>>(knownPerson.UserData);
                        //Debug.WriteLine(json);
                        //Attributes UserAttributes = new Attributes();
                        //UserAttributes = JsonConvert.DeserializeObject<Attributes>(json);

                        foreach (var item in attributes.Data)
                        {
                            Debug.WriteLine("Label: {0}, Value: {1}", item.UserDataLabel.ToString(), item.UserDataValue.ToString());
                            userDataPayload.Add(new UserData() { UserDataLabel = item.UserDataLabel.ToString(), UserDataValue = item.UserDataValue.ToString() });
                        }
                    }
                    catch
                    {
                        Debug.WriteLine("There was a problem deserializing the User Data");
                    }

                    try
                    {
                        if (knownPerson.UserData == "{}")
                        {
                            UpdateUserDataStatusTextBlock.Text = knownPerson.Name + " does not have user data.";
                            UpdateUserDataPayloadTextBlock.Text = "No payload";
                        }
                        else
                        {
                            UpdateUserDataStatusTextBlock.Text = "User Data for " + knownPerson.Name + ":";
                            UpdateUserDataPayloadTextBlock.Text = knownPerson.UserData;
                        }
                    }
                    catch
                    {
                        UpdateUserDataStatusTextBlock.Text = "No User Data";
                    }
                }

                if (null == knownPerson)
                {
                    PersonStatusTextBlock.Text = "Could not find person: " + personName;
                }

                if (PersonStatusTextBlock.Text.ToLower().Contains("found"))
                {
                    PersonStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    PersonStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
        }

        private async void FetchPersonGroup_Click(object sender, RoutedEventArgs e)
        {
            personGroupId = PersonGroupIdTextBox.Text;
            personGroupName = PersonGroupNameTextBox.Text;
            PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            authKey = AuthKeyTextBox.Text;

            await ApiCallAllowed(true);
            faceServiceClient = new FaceServiceClient(authKey);

            if (null != faceServiceClient && authKey != "")
            {
 
                try
                {
                    // You may experience issues with this below call, if you are attempting connection with
                    // a service location other than 'West US'
                    PersonGroup[] groups = await faceServiceClient.ListPersonGroupsAsync();
                    var matchedGroups = groups.Where(p => p.PersonGroupId == personGroupId);


                    if (matchedGroups.Count() > 0)
                    {
                        knownGroup = matchedGroups.FirstOrDefault();

                        PersonGroupStatusTextBlock.Text = "Found existing: " + knownGroup.Name;
                    }

                    if (null == knownGroup)
                    {
                        PersonGroupStatusTextBlock.Text = "Could not find group: " + personGroupId;
                    }

                    if (PersonGroupStatusTextBlock.Text.ToLower().Contains("found"))
                    {
                        PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    }
                    else
                    {
                        PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    }

                }
                catch
                {
                    PersonGroupStatusTextBlock.Text = "Verify that your Group ID and API Key are correct.";
                    PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
            else
            {
                PersonGroupStatusTextBlock.Text = "Verify that your Group ID and API Key are correct.";
                PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private async void SubmitToAzureButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            string successfullySubmitted = string.Empty;
            SubmissionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);

            int imageCounter = 0;
            if (null != personFolder)
            {
                var items = await personFolder.GetFilesAsync();

                if (items.Count > 0)
                {
                    List<StorageFile> imageFilesToUpload = new List<StorageFile>();
                    foreach (StorageFile item in items)
                    {
                        //Windows Cam default save type is jpg
                        if (item.FileType.ToLower() == ".jpg" || item.FileType.ToLower() == ".png")
                        {
                            imageCounter++;
                            imageFilesToUpload.Add(item);
                        }
                        else
                        {
                            Debug.WriteLine(string.Format("Photo {0}, from {1}, is in the wrong format. Images must be jpg or png!", item.DisplayName, item.Path));
                        }
                    }

                    if (imageCounter >= minPhotos)
                    {
                        imageCounter = 0;
                        try
                        {
                            foreach (StorageFile imageFile in imageFilesToUpload)
                            {
                                imageCounter++;
                                using (Stream s = await imageFile.OpenStreamForReadAsync())
                                {
                                    await ApiCallAllowed(true);
                                    AddPersistedFaceResult addResult = await faceServiceClient.AddPersonFaceAsync(personGroupId, personId, s);
                                    Debug.WriteLine("Add result: " + addResult + addResult.PersistedFaceId);
                                }
                                SubmissionStatusTextBlock.Text = string.Format("Submission Status: {0}", imageCounter);
                            }
                            SubmissionStatusTextBlock.Text = "Submission Status: Total Images submitted: " + imageCounter;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Submission Exc: " + ex.Message);
                        }
                    }
                    else
                    {
                        SubmissionStatusTextBlock.Text = $"Submission Status: Please add at least {minPhotos} face images to the person folder.";
                    }
                }
                else
                {
                    successfullySubmitted = "Submission Status: No Image Files Found.";
                }
            }
            else
            {
                successfullySubmitted = "Submission Status: No person folder found! Have you completed section five?";
            }

            if (successfullySubmitted != string.Empty)
            {
                SubmissionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                SubmissionStatusTextBlock.Text = successfullySubmitted;
            }
            else
            {
                SubmissionStatusTextBlock.Text = "Submission completed successfully! Now train your service!";
            }
        }

        private async void TrainButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (personGroupId.Length > 0)
            {
                TrainStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                await ApiCallAllowed(true);
                await faceServiceClient.TrainPersonGroupAsync(personGroupId);

                TrainingStatus trainingStatus = null;
                while (true)
                {
                    await ApiCallAllowed(true);
                    trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);

                    if (trainingStatus.Status != Status.Running)
                    {
                        break;
                    }
                    await Task.Delay(1000);
                }

                TrainStatusTextBlock.Text = "Submission Status: Training Completed!";
            }
            else
            {
                TrainStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                TrainStatusTextBlock.Text = "Submission Status: No person group ID found. Have you completed section two?";
            }
        }

        //To Do: Change CreateUserDataButton Method to update again, after it has been updated once
        private async void UpdateUserDataButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            personDataName = PersonUserDataNameTextBox.Text;
            personUserData = PersonUserDataTextBox.Text;

            if (personDataName.Length > 0 && personUserData.Length > 0)
            {
                userDataPayload.Add(new UserData() { UserDataLabel = personDataName, UserDataValue = personUserData });
            }

            jsonString = JsonConvert.SerializeObject(userDataPayload);
            UpdateUserDataStatusTextBlock.Text = "User Data added to payload with the following User Data: " + jsonString;
            PersonUserDataNameTextBox.Text = "";
            PersonUserDataTextBox.Text = "";
            PersonUserDataTextBox.Foreground = new SolidColorBrush(Colors.Black);
            PersonUserDataNameTextBox.Foreground = new SolidColorBrush(Colors.Black);

            if (knownGroup != null && knownPerson.Name.Length > 0)
            {
                UpdateUserDataErrorText.Visibility = Visibility.Collapsed;
                //Check if this person already exist
                bool personAlreadyExist = false;
                Person[] ppl = await GetKnownPeople();
                foreach (Person p in ppl)
                {
                    if (p.Name == knownPerson.Name)
                    {
                        personAlreadyExist = true;
                    }
                }

                if (!personAlreadyExist)
                {
                    UpdateUserDataStatusTextBlock.Text = $"Person not found. Fetch a known Person";

                    UpdateUserDataStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }

                if (personAlreadyExist)
                {
                    await ApiCallAllowed(true);
                    await faceServiceClient.UpdatePersonAsync(personGroupId, knownPerson.PersonId, knownPerson.Name, jsonString);

                    Person[] people = await GetKnownPeople();
                    var matchedPeople = people.Where(p => p.Name == personName);

                    if (matchedPeople.Count() > 0)
                    {
                        knownPerson = matchedPeople.FirstOrDefault();

                        UpdateUserDataStatusTextBlock.Text = "Updated Person: " + knownPerson.Name + " with the following User Data: " + knownPerson.UserData;
                        UpdateUserDataStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    }
                }
            }
            else
            {
                UpdateUserDataErrorText.Text = "There was a problem with the request. Please check that you have successfully Fetched a Person Group and Person and that you have entered valid User Data";
                UpdateUserDataErrorText.Visibility = Visibility.Visible;
            }
        }

        internal async Task<Person[]> GetKnownPeople()
        {
            Person[] people = null;
            if (null != faceServiceClient)
            {
                await ApiCallAllowed(true);
                people = await faceServiceClient.ListPersonsAsync(personGroupId);
            }
            return people;
        }

        internal async Task RemovePerson(Person person)
        {
            if (null != person)
            {
                await ApiCallAllowed(true);
                await faceServiceClient.DeletePersonAsync(personGroupId, person.PersonId);
            }
        }

        #region Image Upload Throttling

        public int apiMaxCallsPerMinute = 20;
        [DllImport("kernel32")]
        extern static UInt64 GetTickCount64();
        public List<UInt64> apiCallTimes = new List<UInt64>();
        public void NoteApiCallTime()
        {
            apiCallTimes.Add(GetTickCount64());
        }

        public async Task ApiCallAllowed(bool addAnApiCall)
        {
            bool throttleActive = false;
            UInt64 now = GetTickCount64();
            UInt64 boundary = now - 60 * 1000; // one minute ago
            // remove any in list longer than one minute ago
            while (true && apiCallTimes.Count > 0)
            {
                UInt64 sample = apiCallTimes[0];
                if (sample < boundary)
                {
                    apiCallTimes.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }

            if (apiCallTimes.Count >= apiMaxCallsPerMinute)
            {
                throttleActive = true;
                Debug.WriteLine("forced to wait for " + (61 * 1000 - (int)(now - apiCallTimes[0])));
                await Task.Delay(61 * 1000 - (int)(now - apiCallTimes[0]));
            }
            if (addAnApiCall)
            {
                NoteApiCallTime();
            }

            ThrottlingActive.Foreground = new SolidColorBrush(throttleActive == true ? Colors.Red : Colors.Green);
            ThrottlingActive.Text = string.Format("Throttling Status: {0}", throttleActive == true ? "ACTIVE!" : "IN-ACTIVE");
        }

        #endregion


    }
}
