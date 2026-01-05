<div align="center"> <img width="613" height="617" alt="DDNS" src="https://github.com/user-attachments/assets/89358f9d-9c71-43ec-bb45-ae3bdd737a0e" />
 </div>
<h1 align="center">Cloudflare DDNS Client <a href="https://github.com/7odaifa-ab/Cloudflare-DDNS-Client"></a></h1>
<p align="center">
  <a target="_blank" href="https://github.com/7odaifa-ab/Cloudflare-DDNS-Client/releases/latest/download/DDNS.Cloudflare.API.exe">
      <img src="https://img.shields.io/badge/Download-V2.1-brightgreen"></a>
  <a target="_blank" href="https://github.com/7odaifa-ab/Cloudflare-DDNS-Client/releases"><img src="https://img.shields.io/badge/Releases-Versions%20List-lightgrey"></a>
  <a target="_blank" href="https://dotnet.microsoft.com/en-us/download/dotnet/8.0"><img src="https://img.shields.io/badge/.NET-8.0-purple?logo=.NET"></a>
  <a target="_blank" href="https://github.com/lepoco/wpfui"><img src="https://img.shields.io/badge/Library-WPF UI-lightblue"></a>
  <a target="_blank" href="LICENSE"><img src="https://img.shields.io/badge/Licence-The%20Unlicens-blue"></a>
</p>

<p align="center">Cloudflare DDNS Client is a Windows GUI application designed for managing DNS records on Cloudflare.</p>

<i><p align="center">
  Idea & Author: <a target="_blank" href="https://github.com/7odaifa-ab">Hudaifa Abdullah</a><br>
</p></i>

# Cloudflare DDNS Client

## Overview

`Cloudflare DDNS Client` is a WPF (Windows Presentation Foundation) application designed to interact with the Cloudflare API. The application serves as a Dynamic DNS (DDNS) client, allowing users to manage DNS records for a domain hosted on Cloudflare. It features functionality to fetch, update, and display DNS records, with support for different DNS record types and various user-configurable options.

## Features

- **Fetch DNS Records**: Retrieve and display DNS records for a specified zone.
- **Update DNS Records**: Update existing DNS records with new values.
- **Manage Profiles**: Save and manage different profiles for API keys and zone IDs.
- **System Tray Integration**: Minimize to the system tray and manage application state.
- **Log Management**: Keep a history of API responses in a log file.

## Getting Started

### Prerequisites

- **.NET Framework 4.8** or later
- **WPF UI Library**: Make sure to include the `Wpf.Ui` library in your project.
- **Cloudflare API Access**: You need a valid API key and zone ID from Cloudflare.

### Installation

1. **Clone the Repository**:

    ```bash
    git clone https://github.com/7odaifa-ab/Cloudflare-DDNS-Client.git
    ```

2. **Open the Project**:
   
   Open the solution file (`.sln`) in Visual Studio.

3. **Install Dependencies**:

   Ensure all necessary NuGet packages are installed by restoring the project’s packages.

4. **Build the Project**:

   Build the project to ensure all dependencies are correctly resolved.

### Configuration

1. **API Key and Zone ID**:

   Enter your Cloudflare API key and zone ID in the respective text boxes on the `Records` page.

2. **Image Assets**:

   Place your image assets in the `Assets` folder. Ensure their Build Action is set to `Resource`.

### Usage

1. **Fetch DNS Records**:

   - Navigate to the `Records` page.
   - Enter your API Key and Zone ID.
   - Click the `Get DNS Records` button to fetch and display DNS records.

2. **Update DNS Records**:

   - Use the `Update` button to send an update request with the new record details.

3. **System Tray Integration**:

   - The application minimizes to the system tray on window minimize.
   - Right-click the tray icon to access options to show or exit the application.

4. **View Logs**:

   - Navigate to the `Logs` page to view the history of API responses.

### Code Structure

- **`MainWindow.xaml`**: The main application window, including system tray integration and navigation setup.
- **`Records.xaml`**: Page for fetching and displaying DNS records.
- **`DataPage.xaml`**: Page for viewing the history of API responses.
- **`DataViewModel.cs`**: ViewModel for handling data operations.
- **`App.xaml`**: Application-level configuration and startup.

### Contributing

Contributions are welcome! Please fork the repository and submit a pull request with your changes. Ensure to follow the project's coding conventions and include relevant test cases.

**`I Need Help Implementing:`**

- Rich Dashboard
- Add custom profile naming.
- Rich Tutorial
- New UI and better UX
- Portable version
  
### License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

### Contact

For questions or feedback, please contact [7odaifa@HuimangTech.com](mailto:7odaifa@HuimangTech.com).
