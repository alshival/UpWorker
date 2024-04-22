# UpworkER
[Demo Video](https://vimeo.com/936644607?share=copy)

**Developed by:** The Data Team @ [Alshival's Data Service](https://Alshival.com)

<p align="center">
    <a href="https://www.microsoft.com/store/productId/9P6LSJ5QS86P?ocid=pdpshare" target="_blank">
       <img src="https://github.com/alshival/UpworkER/assets/129638420/69b129a0-a076-453a-b0f6-a7506fdfc382" alt="Open Microsoft Store">
    </a>
</p>



![Screenshot 2024-04-20 162141](https://github.com/alshival/UpworkER/assets/129638420/11bf2672-04d1-4ef9-9abf-7f0a3e650812)


Stay ahead in the freelance game with **UpworkER**—your personal job alert assistant. With customizable RSS feeds directly from Upwork, never miss out on an opportunity that matches your skills. Get instant toast notifications for new job postings, and with a single click, view the full listing within **UpworkER**. Plus, explore our Jobs Feed section to swiftly navigate through the latest openings. Originally crafted as an in-house tool by The Data Team at Alshival's Data Service, **UpworkER** is now available for all to streamline their job search.


[![Design 11](https://github.com/alshival/UpworkER/assets/129638420/f47b4f0f-8f40-4dea-b258-fd12260dfb94)](https://vimeo.com/936644607?share=copy)

**NOTE**: Currently, you can browse jobs through the app, but you cannot submit proposals when you try to apply from the webview (though applying from popup windows still works). Issue is related to [this](https://github.com/microsoft/microsoft-ui-xaml/issues/5570) issue, which was apparently solved in newer versions of WebView2. 
For now, to submit a proposal, click on the globe in the bottom right corner to open it in your browser. Perhaps in the future we will get this feature to work.

### **Features:**
- **Customizable RSS Feeds:** Tailor your job search with feeds that cater to your expertise.
- **Instant Notifications:** Receive real-time alerts for jobs that interest you.
- **One-Click Access:** Jump straight to job listings from notifications with ease.
- **Jobs Feed:** Browse through a curated list of opportunities within the app.
- **User-Friendly Interface:** A sleek design that makes job hunting simple and efficient.

### **Benefits:**
- **Save Time:** Quickly find relevant jobs without sifting through numerous postings.
- **Stay Organized:** Keep track of new and interesting jobs in one place.
- **Increase Productivity:** Spend more time applying and less time searching.

#### **About The Developers:**
From custom machine-learning models and recommender systems to chatbots, The Data Team at Alshival's Data Service specializes in creating innovative solutions to data-driven challenges. 

We are working on getting the app deployed to the Microsoft Store, though we have shared the source code for those of you who'd prefer to customize the solution or would like to contribute. Here's how to get the code running on Visual Studio 2022 and how to deploy it locally:

### **Getting Started with Visual Studio 2022**

1. **Install Visual Studio 2022**
   - If you haven't already, download and install Visual Studio 2022 from the official [Microsoft website](^1^).

2. **Open Your Project**
   - Launch Visual Studio 2022.
   - Select **Open a project or solution** from the start window.
   - Navigate to your project's location and select the solution file (`.sln`).

3. **Restore NuGet Packages**
   - Right-click on the solution in Solution Explorer and select **Restore NuGet Packages** to ensure all dependencies are correctly installed.

4. **Build the Solution**
   - Go to the **Build** menu and select **Build Solution** to compile the project.

5. **Run the Application**
   - Press **F5** or click on the **Start Debugging** button to run the application with debugging enabled. Alternatively, press **Ctrl + F5** to run without attaching the debugger³.

### **Deploying Locally**

1. **Publish to a Local Folder**
   - In Solution Explorer, right-click on the project and select **Publish**.
   - If you haven't configured any publishing profiles, select **Create new profile**.
   - Choose **Folder** as the publish target and select a location on your local machine[^10^].

2. **Configure the Application**
   - Make any necessary configuration changes for the local environment, such as connection strings or app settings.

3. **Run the Published Application**
   - Navigate to the published folder and run the executable to start the application locally.

4. **Make Changes and Redeploy**
   - After making changes to the code, you can republish to the same local folder to update the deployed application.
