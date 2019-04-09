# CustomPersonMaker
Created As Convenience Utility For Integrating Project HoloRekognition With Azure Face Resource.

*Project HoloRekognition is an academic project planned and developed in the Winter of 2019 by the following individuals as a part of the capstone requirement for the BYU Marriott School of Business MISM Program:*

* *Levi Bowser*
* *Cameron Spilker*
* *Nathan Barton*

*[Project HoloRekognition](https://github.com/LeviBowser/HoloRekognition)*

*[Person Group Python Utility](https://github.com/CameronSpilker/DownloadPersonGroupInformation)*

*The following is a quick start walkthrough for convenience. See the CREATING FACE RESOURCE Walkthrough at the bottom for help getting started with Microsoft Azure Face API.*

# CUSTOM PERSON MAKER WALKTHROUGH
## Objective(s)
* Learn how to create, train, fetch, update, and delete people and groups in your Face resource.

## Prerequisite(s) 
* Have a subscription key from Face resource (See * Walkthrough 1 Face Resource)
* Visual studios environment set up (See Walkthrough 4 Setting Up Development Env)
* Downloaded the Custom Person Maker application (See Walkthrough …)

## Configure Project
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

## Create
### Person Group
A Person Group is the Face resource object that contains Persons. You must create/fetch a Person Group before working with Persons

* Enter the Face Resource Key in Step 1
* Enter a text id for the Person Group ID in step 2
  * This must be lowercase and a single word without spaces
* Enter a friendly text name for the Person Group Name in step 2
* Click Create Person Group to create the group

![Person Group](PersonMaker/PersonMaker/github/images/persongroup.png?raw=true "Person Group")

### Person
The Person object contains the Name, User Data, and Facial Recognition model for a person

* Enter the First and Last name of the Person in Step 3
* Click Create Person to create the Person object in the Person Group

![Person Name](PersonMaker/PersonMaker/github/images/username.png?raw=true "Person Name")

### User Data
User Data is optional but can be added as one or more key/value (label/value) pair. Create a list of these data pairs before submitting to Azure to be associated with the Person.

* Add a Label in the first text box of Step 4
* Add a Value associated with the Label in the second text box of Step 4
* Click Add To List to add that pair to the User Data list
* Repeat this until you have all the data that you would like to be associated with the Person
* Click Submit User Data to save the User Data list you created

![Person Data](PersonMaker/PersonMaker/github/images/userdata.png?raw=true "Person Data")

### Person Images
Training the recognition model requires at least 10 good quality photos of the person. These photos must be isolated to contain only the person’s face

* Collect, or take, 10 photos of a person’s face.

### Create/Open Photo Folder
For ease of use, the Custom Person Maker creates a folder for the Person in the Pictures directory.

* Click Create/Open Folder button
* Put the photos collected in step 5 in the folder created

### Submit To Azure
This step should be completed after the photos have been put in the created folder in step 6.

* Click Submit To Azure
* Wait for the status to say completed

### Train
Facial recognition requires training a model based on a minimum of 10 photos. Complete this step after the photos have been submitted to Azure

* Click Train Model button

## Fetch
Each Person Group or Person must be fetched before working with them after creation. For example, in order to add additional User Data on John Smith you will need to fetch the Person Group using the Person Group ID, and fetch the Person by entering John Smith in the text box and clicking the Fetch Person button. You will then see the User Data associated with John Smith and can then add additional User Data to the list and re-submit.

## Delete
To delete a Person Group or Person, first, fetch the resource and then click on the delete button. That resource (Person Group or Person) will be removed permanently with all of the associated data.

## Summary
If you need to interface with the data in the Face resource, this application will allow you to create, train, fetch, update, and delete all items in the resource. 

# CREATE FACE RESOURCE WALKTHROUGH
## Objective(s)
* Create a Face resource in the **WEST US** location. 
* Get subscription key(s)

## Prerequisite(s)
* An Azure subscription. If you do not have one, you can sign up for a [free account](https://azure.microsoft.com/pricing/free-trial/).

## The Azure Portal
To use the *Face API* service in Azure, you will need to configure an instance of the service to be made available to your application.

1. First, log in to the [Azure Portal](https://portal.azure.com/).
2. Once you are logged in, click on **Create a resource** in the top left corner, and search for Face, press Enter

![Create Resource](/PersonMaker/PersonMaker/github/images/createresourcebutton.png?raw=true "Create Resource")

3. The new page will provide a description of the Face API service. At the bottom left of this prompt, select the **Create** button, to create an association with this service.

![Create Face](/PersonMaker/PersonMaker/github/images/createfacebutton.png?raw=true "Create Face")

4. Once you have clicked on Create:
  * Insert your desired name for this service instance.
  * Select a subscription. (I have to do “Pay-As-You-Go”, because my free tier trial is over)
  * For location, choose **West US** (this region is necessary for our application)
  * Select the pricing tier appropriate for you, if this is the first time creating a Face API Service, a free tier (named **F0**) should be available to you.
  * Choose a Resource Group or create a new one. A resource group provides a way to monitor, control access, provision and manage billing for a collection of Azure assets. It is recommended to keep all the Azure services associated with a single project (e.g. such as these labs) under a common resource group).

*If you wish to read more about Azure Resource Groups, please [visit the resource group article](https://docs.microsoft.com/azure/azure-resource-manager/resource-group-portal).*

  * You will also need to confirm that you have understood the Terms and Conditions applied to this Service.
  * Select **Create**.

![Create Face Form](/PersonMaker/PersonMaker/github/images/createface.png?raw=true "Create Face Form")

5. Once you have clicked on **Create**, you will have to wait for the service to be created, this might take a minute.
6. A **notification** will appear in the portal once the Service instance is created.
7. **Click** on the notifications to explore your new Service instance.
8. When you are ready, click **Go to resource button** in the notification to explore your new Service instance.

![Go to Resource](/PersonMaker/PersonMaker/github/images/gotoresource.png?raw=true "Go to Resource")

9. The HoloReckognition application will need to make calls to your service, which is done through using your service's subscription 'key'. From the Quick start page, of your Face API service, the first point is number 1, to Grab your keys and click **keys**.
(Alternatively you can click the Keys link in the services navigation menu to the left, denoted by the 'key' icon, to reveal your keys.)

![Grab your Keys](/PersonMaker/PersonMaker/github/images/grabkeys.png?raw=true "Grab Keys")

*******Important Note********
Take note of either one of the keys and safeguard it, as you will need it later.

![My Keys](/PersonMaker/PersonMaker/github/images/mykeys.png?raw=true "My keys")

## Summary
You should have created a Face resource and retrieved your subscription keys. You will need to have the subscription key handy to interact with the Face API.

# __
*CustomPersonMaker extends the [Microsoft Open Source project PersonMaker](https://github.com/MicrosoftDocs/mixed-reality).*

*The intent of the project was to create technology resources to be used by university researchers. Technologies used in the implementation of the project (i.e. Microsoft Azure Face API) were used under the appropriate licenses and are not the property of the students.*