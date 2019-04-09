# CustomPersonMaker
Created As Convenience Utility For Integrating Project HoloRekognition With Azure Face Resource

Create, train, fetch, update, and delete (people and groups)
# Objective(s)
* Learn how to create, train, fetch, update, and delete people and groups in your Face resource.

# Prerequisite(s) 
* Have a subscription key from Face resource (See * Walkthrough 1 Face Resource)
* Visual studios environment set up (See Walkthrough 4 Setting Up Development Env)
* Downloaded the Custom Person Maker application (See Walkthrough …)

# Configure Project
* Open the solution in Visual Studio
* Right click on the Project and select properties

![Properties](PersonMaker/PersonMaker/github/images/properties.png?raw=true "Properties")

* Select the Build tab
* Change the platform target to x86 and then save the properties (CTRL + S)

![Build](PersonMaker/PersonMaker/github/images/build.png?raw=true "Properties")

* Build the project
* Select Local Machine as the debug target
* Start debugging

![Form](PersonMaker/PersonMaker/github/images/startup.png?raw=true "Form")

# Create
## Person Group
A Person Group is the Face resource object that contains Persons. You must create/fetch a Person Group before working with Persons

* Enter the Face Resource Key in Step 1
* Enter a text id for the Person Group ID in step 2
  * This must be lowercase and a single word without spaces
* Enter a friendly text name for the Person Group Name in step 2
* Click Create Person Group to create the group

![Person Group](PersonMaker/PersonMaker/github/images/persongroup.png?raw=true "Person Group")

## Person
The Person object contains the Name, User Data, and Facial Recognition model for a person

* Enter the First and Last name of the Person in Step 3
* Click Create Person to create the Person object in the Person Group

![Person Name](PersonMaker/PersonMaker/github/images/username.png?raw=true "Person Name")

## User Data
User Data is optional but can be added as one or more key/value (label/value) pair. Create a list of these data pairs before submitting to Azure to be associated with the Person.

* Add a Label in the first text box of Step 4
* Add a Value associated with the Label in the second text box of Step 4
* Click Add To List to add that pair to the User Data list
* Repeat this until you have all the data that you would like to be associated with the Person
* Click Submit User Data to save the User Data list you created

![Person Data](PersonMaker/PersonMaker/github/images/userdata.png?raw=true "Person Data")

## Person Images
Training the recognition model requires at least 10 good quality photos of the person. These photos must be isolated to contain only the person’s face

* Collect, or take, 10 photos of a person’s face.

## Create/Open Photo Folder
For ease of use, the Custom Person Maker creates a folder for the Person in the Pictures directory.

* Click Create/Open Folder button
* Put the photos collected in step 5 in the folder created

## Submit To Azure
This step should be completed after the photos have been put in the created folder in step 6.

* Click Submit To Azure
* Wait for the status to say completed

## Train
Facial recognition requires training a model based on a minimum of 10 photos. Complete this step after the photos have been submitted to Azure

* Click Train Model button

# Fetch
Each Person Group or Person must be fetched before working with them after creation. For example, in order to add additional User Data on John Smith you will need to fetch the Person Group using the Person Group ID, and fetch the Person by entering John Smith in the text box and clicking the Fetch Person button. You will then see the User Data associated with John Smith and can then add additional User Data to the list and re-submit.

# Delete
To delete a Person Group or Person, first, fetch the resource and then click on the delete button. That resource (Person Group or Person) will be removed permanently with all of the associated data.

# Summary
If you need to interface with the data in the Face resource, this application will allow you to create, train, fetch, update, and delete all items in the resource. 
