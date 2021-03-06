﻿using System;
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
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //Resource and Group information. Can have many Person's
        string authKey;
        string personGroupId;
        string personGroupName;

        //Person information. Person is a part of a group
        Guid personId;
        string personName;
        StorageFolder personFolder;

        //For interfacing with the face service resource
        private FaceServiceClient faceServiceClient;
        private PersonGroup knownGroup;
        private Person knownPerson;
        private int minPhotos = 6;

        //Data structures for manipulating data pre- and post- face service resource interfaces
        string personUserData;
        string personDataName;
        string jsonString;
        List<UserData> userDataPayload = new List<UserData> { };

        /// <summary>
        /// The <c>UserData</c> class.
        /// </summary>
        /// <remarks>
        /// <para>This class models the User Data represented by Label/Value pairs</para>
        /// </remarks>
        public class UserData
        {
            /// <value>
            /// Gets/Sets the value of UserDataLabel property
            /// </value>
            public string UserDataLabel { get; set; }
            /// <value>
            /// Gets/Sets the value of the UserDataValue property
            /// </value>
            public string UserDataValue { get; set; }
        }

        /// <summary>
        /// The <c>Attributes</c> class.
        /// </summary>
        /// <remarks>
        /// <para>This class models the User Data as a list of <c>UserData</c> objects</para>
        /// </remarks>
        public class Attributes
        {
            /// <value>
            /// Gets/Sets the list of all user <c>Data</c> for Person's as attributes.
            /// </value>
            public List<UserData> Data { get; set; }
        }

        /// <summary>
        /// Initializes the page and sets the form fields to emtpy.
        /// </summary>
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
        /// Append <c>UserData</c> to the list in preparation to send to Azure
        /// </summary>
        /// <param name="sender">A Sender object</param>
        /// <param name="e">RoutedEventArgs</param>
        private void AddUserDataToListButton_Click(object sender, RoutedEventArgs e)
        {
            //Clear Globals
            personDataName = PersonUserDataNameTextBox.Text;
            personUserData = PersonUserDataTextBox.Text;

            //Reset UI Globals
            SubmissionStatusTextBlock.Text = "";
            TrainStatusTextBlock.Text = "";

            //Reset UI Colors
            SubmissionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            TrainStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            //Logic
            if (personDataName.Length > 0 && personUserData.Length > 0)
            {
                userDataPayload.Add(new UserData() { UserDataLabel = personDataName, UserDataValue = personUserData });
            }

            jsonString = JsonConvert.SerializeObject(userDataPayload);
            UpdateUserDataStatusTextBlock.Text = "User Data added to payload with the following User Data: ";
            UpdateUserDataPayloadTextBlock.Text = jsonString;

            //Using Chilkat library to help pretty print the user data
            Chilkat.JsonObject json = new Chilkat.JsonObject();
            string emittedJson = "";

            foreach (var j in userDataPayload)
            {
                string jsonStr = JsonConvert.SerializeObject(j);

                bool success = json.Load(jsonStr);
                if (success != true)
                {
                    Debug.WriteLine(json.LastErrorText);
                    return;
                }

                //  To pretty-print, set the EmitCompact property equal to false
                json.EmitCompact = false;

                //  If bare-LF line endings are desired, turn off EmitCrLf
                //  Otherwise CRLF line endings are emitted.
                json.EmitCrLf = true;

                //  Emit the formatted JSON:
                emittedJson = emittedJson + json.Emit();
            }
            JSONTextBlock.Text = emittedJson;
            JSONHeaderTextBlock.Text = knownPerson.Name + " User Data:";

            PersonUserDataNameTextBox.Text = "";
            PersonUserDataTextBox.Text = "";

            UpdateUserDataStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
        }

        /// <summary>
        /// Create UI buttons for the Person's in the group
        /// </summary>
        /// <param name="group">An optional <c>PersonGroup</c> object.</param>
        /// <param name="people">An optional <c>Person</c> list.</param>
        /// <remarks>
        /// <para>Both parameters are optional, BUT at least one must be provided</para>
        /// </remarks>
        private async void AddPersonButtons(PersonGroup group = null, Person[] people = null)
        {
            //Reset the UI elements including the buttons
            btns.Children.Clear();
            InfoHeaderTextBlock.Text = "";

            if (null != group || null != people)
            {
                //Set people ONLY if the group was sent but NOT the people
                if (null != group && null == people)
                {
                    //Prep API Call
                    await ApiCallAllowed(true);
                    faceServiceClient = new FaceServiceClient(authKey);
                    people = await faceServiceClient.ListPersonsAsync(group.PersonGroupId);
                }

                if (people.Count() <= 50)
                {
                    foreach (var p in people)
                    {
                        //UWP button object
                        Button newButton = new Button
                        {
                            Content = p.Name,
                            Margin = new Thickness(20, 5, 10, 10),
                            Height = 50,
                            Width = 200,
                        };
                        newButton.Click += SelectPerson_Click;
                        btns.Children.Add(newButton);
                    }
                }
                else
                {
                    btns.Children.Clear();
                    string peopleText = "";
                    foreach (var p in people)
                    {
                        peopleText += "\n\r" + p.Name;
                    }
                    JSONTextBlock.Text = peopleText;
                }

                InfoHeaderTextBlock.Text = "People In " + knownGroup.Name + ":";
            }
            else
            {
                InfoHeaderTextBlock.Text = "There seems to be a problem with the Group";
            }
        }

        /// <summary>
        /// Create a Folder for images if it doesn't already exist.
        /// </summary>
        /// <param name="sender">A sender object</param>
        /// <param name="e">RoutedEventArgs</param>
        /// <remarks>
        /// <para>The folder is created in the Pictures directory and is named using the person name.</para>
        /// <para>If the folder already exists, it will be opened in file explorer.</para>
        /// <para>Pictures in the folder will be uploaded with <c>SubmitToAzureButton_ClickAsync</c> method.</para>
        /// </remarks>
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

        /// <summary>
        /// Create a new Person in the Person Group of the Face Resource
        /// </summary>
        /// <param name="sender">A sender object</param>
        /// <param name="e">RoutedEventArgs</param>
        /// <remarks>
        /// <para>Person can't already exist in the Person Group</para>
        /// </remarks>
        private async void CreatePersonButton_ClickAsync(object sender, RoutedEventArgs e)
        {

            //Clear Globals
            personName = PersonNameTextBox.Text;

            //Reset UI Globals
            JSONTextBlock.Text = "";
            JSONHeaderTextBlock.Text = "";
            InfoHeaderTextBlock.Text = "";
            UpdateUserDataPayloadTextBlock.Text = "";
            UpdateUserDataStatusTextBlock.Text = "";
            SubmissionStatusTextBlock.Text = "";
            TrainStatusTextBlock.Text = "";

            //Reset UI Colors
            UpdateUserDataStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            SubmissionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            TrainStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            PersonStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            //Logic
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
                    FetchPersonButton_ClickAsync(this, new RoutedEventArgs());
                }
            }
            else
            {
                CreatePersonErrorText.Text = "Please provide a name above, and ensure that the above person group section has been completed.";
                CreatePersonErrorText.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Create a Person Group with ID and name provided if none can be found in the service.
        /// </summary>
        /// <param name="sender">A sender object</param>
        /// <param name="e">RoutedEventArgs</param>
        /// <remarks>
        /// <para>Method for creating a new person group. Must create a group to work with Persons</para>
        /// </remarks>
        private async void CreatePersonGroupButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            //Clear Globals
            personGroupId = PersonGroupIdTextBox.Text;
            personGroupName = PersonGroupNameTextBox.Text;
            authKey = AuthKeyTextBox.Text;

            //Reset UI Globals
            PersonStatusTextBlock.Text = "";
            UpdateUserDataStatusTextBlock.Text = "";
            SubmissionStatusTextBlock.Text = "";
            TrainStatusTextBlock.Text = "";

            //Reset UI Colors
            PersonStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            UpdateUserDataStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            SubmissionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            TrainStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            //Logic
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

        /// <summary>
        /// Delete a Person
        /// </summary>
        /// <param name="sender">A sender object</param>
        /// <param name="e">RoutedEventArgs</param>
        /// <remarks>
        /// <para>Removes the object in the Person Group of the Face resource.</para>
        /// <para>Deletes all associated data.</para>
        /// </remarks>
        private async void DeletePersonButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            //Clear Globals
            userDataPayload.Clear();
            personName = PersonNameTextBox.Text;

            //Reset UI Globals
            JSONTextBlock.Text = "";
            JSONHeaderTextBlock.Text = "";
            InfoHeaderTextBlock.Text = "";
            UpdateUserDataPayloadTextBlock.Text = "";
            UpdateUserDataStatusTextBlock.Text = "";
            SubmissionStatusTextBlock.Text = "";
            TrainStatusTextBlock.Text = "";

            //Reset UI Colors
            UpdateUserDataStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            SubmissionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            TrainStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            //Logic
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
                //Get the list of people again after the deletion
                ppl = await GetKnownPeople();
                AddPersonButtons(people: ppl);
            }
            else
            {
                CreatePersonErrorText.Text = "Cannot delete: No name has been provided.";
                CreatePersonErrorText.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Delete User Data
        /// </summary>
        /// <param name="sender">A sender object</param>
        /// <param name="e">RoutedEventArgs</param>
        /// <remarks>
        /// <para>Removes ALL Label/Value pairs for user data</para>
        /// </remarks>
        private async void DeleteUserDataButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            //Clear Globals
            personUserData = "{}";
            userDataPayload.Clear();
            
            //Reset UI Globals
            TrainStatusTextBlock.Text = "";
            SubmissionStatusTextBlock.Text = "";

            //Reset UI Colors
            SubmissionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            TrainStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            //Logic
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
                        JSONTextBlock.Text = "";
                        JSONHeaderTextBlock.Text = "";
                        InfoHeaderTextBlock.Text = "";
                    }
                    else
                    {
                        UpdateUserDataPayloadTextBlock.Text = knownPerson.UserData;
                    }
                }
            }
        }

        /// <summary>
        /// Check if Person exists and retrieve that Person object to work with.
        /// </summary>
        /// <param name="sender">A sender object</param>
        /// <param name="e">RoutedEventArgs</param>
        /// <remarks>
        /// <para>Can't add/change user data, pictures, and model until Person has been fetched.</para>
        /// </remarks>
        private async void FetchPersonButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            //Clear Globals
            userDataPayload.Clear();
            personName = PersonNameTextBox.Text;
            authKey = AuthKeyTextBox.Text;

            //Reset UI Globals
            UpdateUserDataStatusTextBlock.Text = "";
            SubmissionStatusTextBlock.Text = "";
            TrainStatusTextBlock.Text = "";
            UpdateUserDataPayloadTextBlock.Text = "";
            JSONTextBlock.Text = "";
            JSONHeaderTextBlock.Text = "";

            //Reset UI Colors
            UpdateUserDataStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            SubmissionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            TrainStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            PersonStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            //Prep API Call
            await ApiCallAllowed(true);
            faceServiceClient = new FaceServiceClient(authKey);

            //Logic
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

                    try
                    {
                        Attributes attributes = new Attributes();
                        attributes.Data = JsonConvert.DeserializeObject<List<UserData>>(knownPerson.UserData);

                        foreach (var item in attributes.Data)
                        {
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
                            //For if the person has had user data in the past but has now been deleted and no longer has user data
                            UpdateUserDataStatusTextBlock.Text = knownPerson.Name + " does not have user data.";
                            UpdateUserDataPayloadTextBlock.Text = "No User Data List";
                            JSONHeaderTextBlock.Text = "No user data for " + knownPerson.Name;
                            JSONTextBlock.Text = "To add data to this person, enter one or more Label and a Value pairs in step 4 and select Add To List.\n\rSubmit User Data once all the Label/Value pairs have been added.";
                        }
                        else
                        {
                            UpdateUserDataStatusTextBlock.Text = "User Data for " + knownPerson.Name + ":";
                            UpdateUserDataPayloadTextBlock.Text = knownPerson.UserData;
                            Chilkat.JsonObject json = new Chilkat.JsonObject();

                            string emittedJson = "";
                            foreach (var j in userDataPayload)
                            {
                                string jsonStr = JsonConvert.SerializeObject(j);

                                bool success = json.Load(jsonStr);
                                if (success != true)
                                {
                                    Debug.WriteLine(json.LastErrorText);
                                    return;
                                }

                                //  To pretty-print, set the EmitCompact property equal to false
                                json.EmitCompact = false;

                                //  If bare-LF line endings are desired, turn off EmitCrLf
                                //  Otherwise CRLF line endings are emitted.
                                json.EmitCrLf = true;

                                //  Emit the formatted JSON:
                                emittedJson = emittedJson + json.Emit();
                            }
                            JSONTextBlock.Text = emittedJson;
                            JSONHeaderTextBlock.Text = knownPerson.Name + " User Data:";
                        }
                    }
                    catch
                    {
                        //If the person has never had any user data associated with it
                        UpdateUserDataStatusTextBlock.Text = "No User Data";
                        JSONHeaderTextBlock.Text = "No user data for " + knownPerson.Name;
                        JSONTextBlock.Text = "To add data to this person, enter one or more Label and a Value pairs in step 4 and select Add To List.\n\rSubmit User Data once all the Label/Value pairs have been added.";
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

        /// <summary>
        /// Check if Person Group exists and retrieve that Person Group object to work with.
        /// </summary>
        /// <param name="sender">A sender object</param>
        /// <param name="e">RoutedEventArgs</param>
        /// <remarks>
        /// <para>Can't work with Persons until Person Group has been fetched.</para>
        /// </remarks>
        private async void FetchPersonGroup_Click(object sender, RoutedEventArgs e)
        {
            //Clear Globals
            authKey = AuthKeyTextBox.Text;
            personGroupId = PersonGroupIdTextBox.Text;
            personGroupName = PersonGroupNameTextBox.Text;

            //Reset UI Globals
            PersonStatusTextBlock.Text = "";
            UpdateUserDataStatusTextBlock.Text = "";
            SubmissionStatusTextBlock.Text = "";
            TrainStatusTextBlock.Text = "";

            //Reset UI Colors
            PersonStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            UpdateUserDataStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            SubmissionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            TrainStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            //Prep API Call
            await ApiCallAllowed(true);
            faceServiceClient = new FaceServiceClient(authKey);

            //Logic
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

                        AddPersonButtons(knownGroup);
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
                catch (Exception ex)
                {
                    PersonGroupStatusTextBlock.Text = "Verify that your Group ID and API Key are correct.";
                    PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    Debug.WriteLine(ex.ToString());
                }
            }
            else
            {
                PersonGroupStatusTextBlock.Text = "Verify that your Group ID and API Key are correct.";
                PersonGroupStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        /// <summary>
        /// Click event handlers for <c>btns</c> Person buttons
        /// </summary>
        /// <param name="sender">A sender object</param>
        /// <param name="e">RoutedEventArgs</param>
        /// <remarks>
        /// <para>Fetches the Person named on the Person button</para>
        /// </remarks>
        private void SelectPerson_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            string s = btn.Content.ToString();

            PersonNameTextBox.Text = s;
            FetchPersonButton_ClickAsync(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Sends images to the Azure Face Resource
        /// </summary>
        /// <param name="sender">A sender object</param>
        /// <param name="e">RoutedEventArgs</param>
        /// <remarks>
        /// <para>Submits the folder created in <c>CreateFolderButton_ClickAsync</c>.</para>
        /// <para>Images should already be stored in the folder</para>
        /// </remarks>
        private async void SubmitToAzureButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            //Clear Globals
            string successfullySubmitted = string.Empty;

            //Reset UI Globals
            TrainStatusTextBlock.Text = "";

            //Reset UI Colors
            TrainStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            SubmissionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);

            //Logic
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

        /// <summary>
        /// Trains the Facial Recognition Model in the Azure Face Resource
        /// </summary>
        /// <param name="sender">A sender object</param>
        /// <param name="e">RoutedEventArgs</param>
        /// <remarks>
        /// <para>Model training is blackboxed</para>
        /// </remarks>
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

        /// <summary>
        /// Updates Face Resource Person User Data
        /// </summary>
        /// <param name="sender">A sender object</param>
        /// <param name="e">RoutedEventArgs</param>
        /// <remarks>
        /// <para>Can submit a single Key/Value pair without adding to the list using <c>AddUserDataToListButton_Click</c>.</para>
        /// <para>Submission will replace any key/value pairs that already exist for the Person</para>
        /// </remarks>
        private async void UpdateUserDataButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            //Clear Globals
            personDataName = PersonUserDataNameTextBox.Text;
            personUserData = PersonUserDataTextBox.Text;

            //Reset UI Globals
            SubmissionStatusTextBlock.Text = "";
            TrainStatusTextBlock.Text = "";

            //Reset UI Colors
            SubmissionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            TrainStatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);

            //Logic
            if (personDataName.Length > 0 && personUserData.Length > 0)
            {
                userDataPayload.Add(new UserData() { UserDataLabel = personDataName, UserDataValue = personUserData });
            }

            jsonString = JsonConvert.SerializeObject(userDataPayload);

            //UI Text Change
            UpdateUserDataStatusTextBlock.Text = "User Data added to payload with the following User Data: " + jsonString;
            PersonUserDataNameTextBox.Text = "";
            PersonUserDataTextBox.Text = "";

            //UI Color Change
            PersonUserDataTextBox.Foreground = new SolidColorBrush(Colors.Black);
            PersonUserDataNameTextBox.Foreground = new SolidColorBrush(Colors.Black);

            if (knownGroup != null && knownPerson != null && knownPerson.Name.Length > 0)
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

                        //Change UI Text
                        UpdateUserDataStatusTextBlock.Text = "Updated Person: " + knownPerson.Name + " with the following User Data: " + knownPerson.UserData;
                        UpdateUserDataPayloadTextBlock.Text = knownPerson.UserData;

                        //Change UI Colors
                        UpdateUserDataStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);

                        Chilkat.JsonObject json = new Chilkat.JsonObject();
                        string emittedJson = "";

                        foreach (var j in userDataPayload)
                        {
                            string jsonStr = JsonConvert.SerializeObject(j);

                            bool success = json.Load(jsonStr);
                            if (success != true)
                            {
                                Debug.WriteLine(json.LastErrorText);
                                return;
                            }

                            //  To pretty-print, set the EmitCompact property equal to false
                            json.EmitCompact = false;

                            //  If bare-LF line endings are desired, turn off EmitCrLf
                            //  Otherwise CRLF line endings are emitted.
                            json.EmitCrLf = true;

                            //  Emit the formatted JSON:
                            emittedJson = emittedJson + json.Emit();
                        }
                        JSONTextBlock.Text = emittedJson;
                        JSONHeaderTextBlock.Text = knownPerson.Name + " User Data:";
                    }
                }
            }
            else
            {
                UpdateUserDataErrorText.Text = "There was a problem with the request. Please check that you have successfully Fetched a Person Group and Person and that you have entered valid User Data";
                UpdateUserDataErrorText.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Task for assigning all people to <c>People</c> via Face API
        /// </summary>
        /// <returns></returns>
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

        //API call to remove person
        /// <summary>
        /// Task for Deleting Face Person in knownGroup
        /// </summary>
        /// <param name="person">A <c>Person</c> object</param>
        /// <returns></returns>
        internal async Task RemovePerson(Person person)
        {
            if (null != person)
            {
                await ApiCallAllowed(true);
                await faceServiceClient.DeletePersonAsync(personGroupId, person.PersonId);
            }
        }

        //For API Throttling
        #region Image Upload Throttling

        /// <summary>
        /// Maximum API Calls per minute based on pricing tier.
        /// </summary>
        /// <remarks>
        /// <para>Max is 20 for free tier.</para>
        /// </remarks>
        public int apiMaxCallsPerMinute = 20;
        [DllImport("kernel32")]
        extern static UInt64 GetTickCount64();

        /// <summary>
        /// List for keeping track when API calls were made
        /// </summary>
        public List<UInt64> apiCallTimes = new List<UInt64>();
        /// <summary>
        /// Tick for keeping track of when API call occurs
        /// </summary>
        public void NoteApiCallTime()
        {
            apiCallTimes.Add(GetTickCount64());
        }

        /// <summary>
        /// Task for preparing an API call
        /// </summary>
        /// <param name="addAnApiCall">A Bool to allow API call</param>
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
            ThrottlingActive.Text = string.Format("Status: {0}", throttleActive == true ? "THROTTLING!" : "NOT THROTTLING");
        }

        #endregion


    }
}
